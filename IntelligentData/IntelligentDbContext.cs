using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using IntelligentData.Attributes;
using IntelligentData.Delegates;
using IntelligentData.Enums;
using IntelligentData.Extensions;
using IntelligentData.Interfaces;
using IntelligentData.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;

namespace IntelligentData
{
    /// <summary>
    /// The base class used for intelligent DB contexts.
    /// </summary>
    public abstract class IntelligentDbContext : DbContext
    {
        /// <summary>
        /// The current user provider for the context.
        /// </summary>
        public IUserInformationProvider CurrentUserProvider { get; }

        /// <summary>
        /// A prefix to apply to table names.
        /// </summary>
        public virtual string TableNamePrefix { get; } = null;
        
        /// <summary>
        /// Constructs the intelligent DB context.
        /// </summary>
        /// <param name="options">The options to construct the DB context with.</param>
        /// <param name="currentUserProvider">The current user provider.</param>
        protected IntelligentDbContext(DbContextOptions options, IUserInformationProvider currentUserProvider, ILogger logger)
            : base(options)
        {
            CurrentUserProvider = currentUserProvider ?? Nobody.Instance;
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region Entity Info
        private readonly Dictionary<Type, EntityInfo> _entityInfo = new Dictionary<Type, EntityInfo>();

        internal EntityInfo GetEntityInfoFor(Type t)
        {
            lock (_entityInfo)
            {
                if (_entityInfo.ContainsKey(t)) return _entityInfo[t];
                _entityInfo[t] = new EntityInfo(this, t);
                return _entityInfo[t];
            }
        }
        
        #endregion
        
        #region Logging
        
        /// <summary>
        /// The logger attached to this context.
        /// </summary>
        public ILogger Logger { get; }

        #endregion
        
        #region Access Control

        /// <summary>
        /// Defines the default access level for entities when no attribute or interface method is available.
        /// </summary>
        public virtual AccessLevel DefaultAccessLevel { get; } = AccessLevel.ReadOnly;

        private readonly Dictionary<Type, AccessLevel> _defaultAccessLevels = new Dictionary<Type, AccessLevel>();
        private          bool                          _allowSeedData       = false;

        private AccessLevel DefaultAccessForEntityType(Type entityType)
        {
            lock (_defaultAccessLevels)
            {
                if (_defaultAccessLevels.ContainsKey(entityType)) return _defaultAccessLevels[entityType];

                // the true default is readonly. 
                var level = AccessLevel.ReadOnly;
                var attribs = entityType.GetCustomAttributes()
                                        .OfType<IEntityAccessProvider>()
                                        .ToArray();

                if (attribs.Any())
                {
                    // when we have attributes present, they build upon the readonly default.
                    foreach (var attr in attribs)
                    {
                        level |= attr.EntityAccessLevel;
                    }
                }
                else
                {
                    // if there are no attributes, use the value from the context.
                    level = DefaultAccessLevel;
                }

                _defaultAccessLevels[entityType] = level;
                return level;
            }
        }

        /// <summary>
        /// Gets the access level for the specified entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public AccessLevel AccessForEntity(object entity)
        {
            if (entity is null) return AccessLevel.ReadOnly;

            if (_allowSeedData) return AccessLevel.Insert;

            if (entity is IEntityAccessProvider e) return e.EntityAccessLevel;

            return DefaultAccessForEntityType(entity.GetType());
        }

        private bool IsChangePermitted(EntityEntry entry)
        {
            if (entry is null) return false;

            var permitted = AccessForEntity(entry.Entity);

            if ((permitted & AccessLevel.FullAccess) == AccessLevel.FullAccess) return true;

            if ((entry.State == EntityState.Added &&
                 (permitted & AccessLevel.Insert) != AccessLevel.Insert) ||
                (entry.State == EntityState.Modified &&
                 (permitted & AccessLevel.Update) != AccessLevel.Update) ||
                (entry.State == EntityState.Deleted &&
                 (permitted & AccessLevel.Delete) != AccessLevel.Delete))
            {
                // non-permitted changes are removed from the ChangeTracker to prevent them from being saved.

                if (entry.State == EntityState.Added)
                {
                    // detach the new item.
                    Entry(entry.Entity)
                        .State = EntityState.Detached;
                }
                else
                {
                    // revert changes and set to unchanged.
                    Entry(entry.Entity)
                        .Reload();
                }

                return false;
            }

            return true;
        }

        #endregion

        #region Seed Data

        /// <summary>
        /// Puts the context into an insert-only state.  All entities will allow insert but not update or delete.
        /// </summary>
        /// <param name="seedData">The action to execute in insert-only state.</param>
        public void SeedData(Action seedData)
        {
            var originalAllowSeed = _allowSeedData;
            _allowSeedData = true;
            try
            {
                seedData();
            }
            finally
            {
                _allowSeedData = originalAllowSeed;
            }
        }

        #endregion

        #region Honor String Format

        private void HonorStringFormatProperties(IEnumerable<EntityEntry> entries)
        {
            foreach (var entry in entries)
            {
                var entity = entry.Entity;
                foreach (var property in entry.Metadata.GetProperties()
                                              .Where(p => p.HasStringFormat() && p.ClrType == typeof(string)))
                {
                    var fmt = property.GetStringFormatProvider(this.GetInfrastructure());
                    if (fmt is null) continue;
                    var val = property.PropertyInfo.GetValue(entity) as string;
                    if (string.IsNullOrEmpty(val)) continue;

                    var fmtVal = fmt(entity, val, this);
                    if (val == fmtVal) continue;

                    property.PropertyInfo.SetValue(entity, fmtVal);
                    entry.Property(property.Name)
                         .IsModified = true;
                }
            }
        }

        #endregion

        #region Honor Runtime Default Value

        private void HonorRuntimeDefaultProperties(IEnumerable<EntityEntry> entries)
        {
            foreach (var entry in entries.Where(x => x.State == EntityState.Added))
            {
                var entity = entry.Entity;
                foreach (var property in entry.Metadata.GetProperties()
                                              .Where(p => p.HasRuntimeDefault()))
                {
                    var provider = property.GetRuntimeDefaultValueProvider(this.GetInfrastructure());
                    if (provider is null) continue;
                    var val    = property.PropertyInfo.GetValue(entity);
                    var newVal = provider(entity, val, this);
                    if (newVal == val) continue;

                    property.PropertyInfo.SetValue(entity, newVal);
                    entry.Property(property.Name)
                         .IsModified = true;
                }
            }
        }

        #endregion

        #region Honor Auto Update Value

        private void HonorAutoUpdateProperties(IEnumerable<EntityEntry> entries)
        {
            foreach (var entry in entries)
            {
                var entity = entry.Entity;
                foreach (var property in entry.Metadata.GetProperties()
                                              .Where(p => p.HasAutoUpdate()))
                {
                    var provider = property.GetAutoUpdateValueProvider(this.GetInfrastructure());
                    if (provider is null) continue;
                    var val    = property.PropertyInfo.GetValue(entity);
                    var newVal = provider(entity, val, this);
                    if (newVal == val) continue;

                    property.PropertyInfo.SetValue(entity, newVal);
                    entry.Property(property.Name)
                         .IsModified = true;
                }
            }
        }

        #endregion

        #region Handle Versioned Models

        private void HandleVersionedModels(IEnumerable<EntityEntry> entries)
        {
            foreach (var entry in entries.Where(e => e.Entity is IVersionedEntity))
            {
                var entity = (IVersionedEntity) entry.Entity;
                var prop = entry.Property(nameof(IVersionedEntity.RowVersion));
                
                var ver = entity.RowVersion;
                prop.IsModified = true;
                prop.OriginalValue = ver;
                if (entry.State != EntityState.Deleted)
                {
                    entity.RowVersion = (entity is ITimestampedEntity)
                                            ? DateTime.Now.Ticks
                                            : (ver.GetValueOrDefault() + 1);
                }
            }
        }
        
        #endregion
        
        #region Save Changes

        private TResult ProcessEntitiesAndThen<TResult>(Func<TResult> save)
        {
            // technically we should check for a "disposed" flag from the base, but we don't have access to the private variable, so here we are.
            if (ChangeTracker.AutoDetectChangesEnabled) ChangeTracker.DetectChanges();

            // disable change tracking temporarily.
            var originalAutoDetectChanges = ChangeTracker.AutoDetectChangesEnabled;
            ChangeTracker.AutoDetectChangesEnabled = false;

            try
            {
                // get the changed records for further processing while filtering out the non-permitted changes.
                var records = ChangeTracker.Entries()
                                           .Where(
                                               x => x.State == EntityState.Added ||
                                                    x.State == EntityState.Modified ||
                                                    x.State == EntityState.Deleted
                                           )
                                           .Where(IsChangePermitted)
                                           .ToList();

                // handle concurrency versions first.
                HandleVersionedModels(records);

                // remove delete entries from the record list.
                records.RemoveAll(x => x.State == EntityState.Deleted);

                // process the remaining special handlers.
                HonorRuntimeDefaultProperties(records);
                HonorAutoUpdateProperties(records);
                HonorStringFormatProperties(records);
                
                // finally perform the save.
                return save();
            }
            finally
            {
                ChangeTracker.AutoDetectChangesEnabled = originalAutoDetectChanges;
            }
        }

        /// <inheritdoc />
        public override int SaveChanges()
            => SaveChanges(true);

        /// <inheritdoc />
        public override int SaveChanges(bool acceptAllChangesOnSuccess)
            => ProcessEntitiesAndThen(() => base.SaveChanges(acceptAllChangesOnSuccess));

        /// <inheritdoc />
        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = new CancellationToken())
            => SaveChangesAsync(true, cancellationToken);

        /// <inheritdoc />
        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = new CancellationToken())
            => ProcessEntitiesAndThen(() => base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken));

        #endregion

        #region On Model Creating

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var tableNamePrefix = (string.IsNullOrEmpty(TableNamePrefix) ? "" : (TableNamePrefix + "_"));
            
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (!(entityType.ClrType is Type et)) continue;
                if (entityType.IsKeyless) continue;

                var xt = modelBuilder.Entity(et);

                foreach (var property in entityType.GetProperties())
                {
                    // Annotate the properties with runtime defaults.
                    if (property.PropertyInfo.GetCustomAttributes()
                                .OfType<IRuntimeDefaultValueProvider>()
                                .FirstOrDefault() is IRuntimeDefaultValueProvider runtimeDefaultValueProvider)
                    {
                        property.HasRuntimeDefault(runtimeDefaultValueProvider);
                    }

                    // Annotate the properties with automatically updated values.
                    if (property.PropertyInfo.GetCustomAttributes()
                                .OfType<IAutoUpdateValueProvider>()
                                .FirstOrDefault() is IAutoUpdateValueProvider autoUpdateValueProvider)
                    {
                        property.HasAutoUpdate(autoUpdateValueProvider);
                    }

                    // Annotate the properties with default values computed at runtime.
                    if (property.PropertyInfo.GetCustomAttributes()
                                .OfType<IStringFormatProvider>()
                                .FirstOrDefault() is IStringFormatProvider stringFormatProvider)
                    {
                        property.HasStringFormat(stringFormatProvider);
                    }

                    // Handle customizer attributes. 
                    foreach (var propertyCustomizer in property.PropertyInfo.GetCustomAttributes().OfType<IPropertyCustomizer>())
                    {
                        propertyCustomizer.Customize(xt, property.Name);
                    }
                }

                if (typeof(IVersionedEntity).IsAssignableFrom(et))
                {
                    xt.Property(nameof(IVersionedEntity.RowVersion)).IsConcurrencyToken();
                }
                
                if (typeof(ITrackedEntity).IsAssignableFrom(et))
                {
                    xt.Property(nameof(ITrackedEntity.CreatedAt))
                      .HasRuntimeDefault<RuntimeDefaultNowAttribute>();
                    xt.Property(nameof(ITrackedEntity.LastModifiedAt))
                      .HasAutoUpdate<AutoUpdateToNowAttribute>();

                    if (typeof(ITrackedEntityWithUserName).IsAssignableFrom(et))
                    {
                        xt.Property(nameof(ITrackedEntityWithUserName.CreatedBy))
                          .HasRuntimeDefault<RuntimeDefaultCurrentUserNameAttribute>();
                        xt.Property(nameof(ITrackedEntityWithUserName.LastModifiedBy))
                          .HasAutoUpdate<AutoUpdateToCurrentUserNameAttribute>();
                    }

                    if (typeof(ITrackedEntityWithInt32UserID).IsAssignableFrom(et))
                    {
                        xt.Property(nameof(ITrackedEntityWithInt32UserID.CreatedByID))
                          .HasRuntimeDefault(new RuntimeDefaultCurrentUserIDAttribute(typeof(int)));
                        xt.Property(nameof(ITrackedEntityWithInt32UserID.LastModifiedByID))
                          .HasAutoUpdate(new AutoUpdateToCurrentUserIDAttribute(typeof(int)));
                    }

                    if (typeof(ITrackedEntityWithInt64UserID).IsAssignableFrom(et))
                    {
                        xt.Property(nameof(ITrackedEntityWithInt64UserID.CreatedByID))
                          .HasRuntimeDefault(new RuntimeDefaultCurrentUserIDAttribute(typeof(long)));
                        xt.Property(nameof(ITrackedEntityWithInt64UserID.LastModifiedByID))
                          .HasAutoUpdate(new AutoUpdateToCurrentUserIDAttribute(typeof(long)));
                    }

                    if (typeof(ITrackedEntityWithGuidUserID).IsAssignableFrom(et))
                    {
                        xt.Property(nameof(ITrackedEntityWithGuidUserID.CreatedByID))
                          .HasRuntimeDefault(new RuntimeDefaultCurrentUserIDAttribute(typeof(Guid)));
                        xt.Property(nameof(ITrackedEntityWithGuidUserID.LastModifiedByID))
                          .HasAutoUpdate(new AutoUpdateToCurrentUserIDAttribute(typeof(Guid)));
                    }

                    if (typeof(ITrackedEntityWithStringUserID).IsAssignableFrom(et))
                    {
                        xt.Property(nameof(ITrackedEntityWithStringUserID.CreatedByID))
                          .HasRuntimeDefault(new RuntimeDefaultCurrentUserIDAttribute(typeof(string)));
                        xt.Property(nameof(ITrackedEntityWithStringUserID.LastModifiedByID))
                          .HasAutoUpdate(new AutoUpdateToCurrentUserIDAttribute(typeof(string)));
                    }
                }
                
                if (!string.IsNullOrEmpty(tableNamePrefix))
                {
                    var name = entityType.GetTableName();
                    if (!name.StartsWith(tableNamePrefix))
                    {
                        xt.ToTable(tableNamePrefix + name);
                    }
                }

                // Handle customizer attributes.
                foreach (var entityCustomizer in et.GetCustomAttributes().OfType<IEntityCustomizer>())
                {
                    entityCustomizer.Customize(xt);
                }
            }
        }

        #endregion
        
        #region Intelligent Entity Initialization

        private static readonly IDictionary<Type, List<IntelligentEntityInitializer>> EntityInitializers = new Dictionary<Type, List<IntelligentEntityInitializer>>();

        /// <summary>
        /// Adds an entity initializer to the context type.
        /// </summary>
        /// <param name="initializer">The initializer to add.</param>
        /// <typeparam name="TContext">The context type to add the initializer to.</typeparam>
        /// <exception cref="ArgumentNullException"></exception>
        public static void AddEntityInitializer<TContext>(IntelligentEntityInitializer initializer)
            where TContext : IntelligentDbContext
        {
            if (initializer is null) throw new ArgumentNullException(nameof(initializer));
            var t = typeof(TContext);
            lock (EntityInitializers)
            {
                if (!EntityInitializers.ContainsKey(t))
                {
                    EntityInitializers[t] = new List<IntelligentEntityInitializer>();
                }
                EntityInitializers[t].Add(initializer);
            }
        }

        /// <summary>
        /// Adds an entity initializer to this context.
        /// </summary>
        /// <param name="initializer">The initializer to add.</param>
        /// <exception cref="ArgumentNullException"></exception>
        protected void AddEntityInitializer(IntelligentEntityInitializer initializer)
        {
            if (initializer is null) throw new ArgumentNullException(nameof(initializer));
            var t = GetType();
            lock (EntityInitializers)
            {
                if (!EntityInitializers.ContainsKey(t))
                {
                    EntityInitializers[t] = new List<IntelligentEntityInitializer>();
                }
                EntityInitializers[t].Add(initializer);
            }
        }

        /// <summary>
        /// Clears all initializers from the specified context.
        /// </summary>
        /// <typeparam name="TContext">The context to remove initializers from.</typeparam>
        public static void ClearEntityInitializers<TContext>()
            where TContext : IntelligentDbContext
        {
            var t = typeof(TContext);
            lock (EntityInitializers)
            {
                if (EntityInitializers.ContainsKey(t))
                {
                    EntityInitializers.Remove(t);
                }
            }
        }

        /// <summary>
        /// Clears all initializers from this context.
        /// </summary>
        protected void ClearEntityInitializers()
        {
            var t = GetType();
            lock (EntityInitializers)
            {
                if (EntityInitializers.ContainsKey(t))
                {
                    EntityInitializers.Remove(t);
                }
            }
        }

        /// <summary>
        /// Applies the initializers from this context against the entity.
        /// </summary>
        /// <param name="entity">The entity to initialize.</param>
        /// <remarks>
        /// The IntelligentEntity base class will apply this automatically when a new entity is created.
        /// </remarks>
        public void InitializeEntity(object entity)
        {
            if (entity is null) return;
            var t = GetType();
            IReadOnlyList<IntelligentEntityInitializer> initializers = null;
            lock (EntityInitializers)
            {
                if (EntityInitializers.ContainsKey(t))
                {
                    initializers = EntityInitializers[t].ToArray();
                }
            }

            if (initializers is null || initializers.Count == 0) return;

            foreach (var init in initializers)
            {
                init(this, entity);
            }
        }
        
        #endregion
    }
}
