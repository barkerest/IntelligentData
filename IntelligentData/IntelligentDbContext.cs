using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
using Microsoft.EntityFrameworkCore.Internal;
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
        /// <param name="logger">The logger to use within the IntelligentDbContext.</param>
        protected IntelligentDbContext(DbContextOptions options, IUserInformationProvider currentUserProvider, ILogger logger)
            : base(options)
        {
            CurrentUserProvider = currentUserProvider ?? Nobody.Instance;
            Logger              = logger ?? throw new ArgumentNullException(nameof(logger));
            _createTempLists    = options.WithTemporaryLists();
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
                var attribs = entityType.GetCustomAttributes(true)
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
                var prop   = entry.Property(nameof(IVersionedEntity.RowVersion));

                var ver = entity.RowVersion;
                prop.IsModified    = true;
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
                    if (property.PropertyInfo.GetCustomAttributes(true)
                                .OfType<IRuntimeDefaultValueProvider>()
                                .FirstOrDefault() is IRuntimeDefaultValueProvider runtimeDefaultValueProvider)
                    {
                        property.HasRuntimeDefault(runtimeDefaultValueProvider);
                    }

                    // Annotate the properties with automatically updated values.
                    if (property.PropertyInfo.GetCustomAttributes(true)
                                .OfType<IAutoUpdateValueProvider>()
                                .FirstOrDefault() is IAutoUpdateValueProvider autoUpdateValueProvider)
                    {
                        property.HasAutoUpdate(autoUpdateValueProvider);
                    }

                    // Annotate the properties with default values computed at runtime.
                    if (property.PropertyInfo.GetCustomAttributes(true)
                                .OfType<IStringFormatProvider>()
                                .FirstOrDefault() is IStringFormatProvider stringFormatProvider)
                    {
                        property.HasStringFormat(stringFormatProvider);
                    }

                    // Handle customizer attributes. 
                    foreach (var propertyCustomizer in property.PropertyInfo.GetCustomAttributes(true).OfType<IPropertyCustomizer>())
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
                foreach (var entityCustomizer in et.GetCustomAttributes(true).OfType<IEntityCustomizer>())
                {
                    entityCustomizer.Customize(xt);
                }
            }
            
            // do not prefix temp tables.
            if (_createTempLists)
            {
                AddTemporaryListsToModel(modelBuilder);
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
            var                                         t            = GetType();
            IReadOnlyList<IntelligentEntityInitializer> initializers = null;
            lock (EntityInitializers)
            {
                if (EntityInitializers.ContainsKey(t))
                {
                    initializers = EntityInitializers[t].ToArray();
                }
            }

            if (initializers is null ||
                initializers.Count == 0) return;

            foreach (var init in initializers)
            {
                init(this, entity);
            }
        }

        #endregion

        #region Knowledge

        private ISqlKnowledge _knowledge;

        /// <summary>
        /// Gets the SQL knowledge for this DB context.
        /// </summary>
        public ISqlKnowledge Knowledge
        {
            get
            {
                if (_knowledge != null) return _knowledge;
                _knowledge = SqlKnowledge.For(Database.ProviderName);
                return _knowledge;
            }
        }

        #endregion

        #region Persistent Connection

        private DbConnection _connection;

        private void EnsureConnectionIsPersistent()
        {
            if (IsConnectionPersistent()) return;

            if (_connection?.State == ConnectionState.Broken)
            {
                _connection = null;
                throw new DataException("The connection state is broken.");
            }

            _connection = Database.GetDbConnection();

            if (_connection.State == ConnectionState.Broken ||
                _connection.State == ConnectionState.Closed)
            {
                _connection.Open();
            }
        }

        /// <summary>
        /// Determines if the connection is persistent for this context.
        /// </summary>
        /// <returns></returns>
        public bool IsConnectionPersistent()
            => _connection != null &&
               _connection.State != ConnectionState.Closed &&
               _connection.State != ConnectionState.Broken;

        #endregion

        #region Temp Lists

        private readonly bool _createTempLists;

        /// <summary>
        /// Defines the temporary tables to create in this database context.
        /// </summary>
        protected virtual IDictionary<Type, ITempListDefinition> TempTableDefs { get; } = new Dictionary<Type, ITempListDefinition>()
        {
            {typeof(int), new TempListDefinition<int>("INTEGER")},
            {typeof(long), new TempListDefinition<long>("BIGINT")},
            {typeof(string), new TempListDefinition<string>("VARCHAR(250)")},
            {typeof(Guid), new TempListDefinition<Guid>()},
        };

        private ITempListDefinition TempTable<T>() => TempTable(typeof(T));

        private ITempListDefinition TempTable(Type t)
        {
            if (!TempTableDefs.ContainsKey(t)) throw new InvalidOperationException($"The {t} type is not a registered temp type.");
            return TempTableDefs[t];
        }

        private readonly List<Type>              _tempEnsured = new List<Type>();
        private readonly IDictionary<Type, bool> _tempInModel = new Dictionary<Type, bool>();

        private void EnsureTempListExists(Type t)
        {
            if (_tempEnsured.Contains(t)) return;
            var tt = TempTable(t);

            EnsureConnectionIsPersistent();
            Database.ExecuteSqlRaw(tt.GetCreateTableCommand(Knowledge));

            _tempEnsured.Add(t);
        }

        /// <summary>
        /// Determines if the model has a temporary list for this data type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public bool HasTempList<T>()
        {
            if (!_createTempLists) return false;

            var t = typeof(T);
            if (_tempInModel.ContainsKey(t)) return _tempInModel[t];

            var tt = TempTable(t);

            _tempInModel[t] = (Model.FindEntityType(tt.EntityType) != null);

            return _tempInModel[t];
        }

        /// <summary>
        /// Adds the temporary tables to the data model.
        /// </summary>
        /// <param name="modelBuilder"></param>
        protected void AddTemporaryListsToModel(ModelBuilder modelBuilder)
        {
            foreach (var tt in TempTableDefs.Values)
            {
                var eb = modelBuilder.Entity(tt.EntityType);
                eb.ToTable(tt.GetTableName(Knowledge));
                eb.Property("ListId");
                tt.CustomizeValueProperty(eb.Property("EntryValue"));
                eb.HasKey("ListId", "EntryValue");
            }
        }

        // fallback lists when not using temporary tables.
        private readonly IDictionary<Type, IDictionary> _contextTempLists = new Dictionary<Type, IDictionary>();

        private IDictionary<int, List<T>> GetContextTempLists<T>()
        {
            var t = typeof(T);
            if (!_contextTempLists.ContainsKey(t))
            {
                _contextTempLists[t] = new Dictionary<int, List<T>>();
            }

            return (Dictionary<int, List<T>>) _contextTempLists[t];
        }

        private List<T> GetContextTempList<T>(int listId)
        {
            var dict = GetContextTempLists<T>();

            if (!dict.ContainsKey(listId))
            {
                dict[listId] = new List<T>();
            }

            return dict[listId];
        }
        
        /// <summary>
        /// Gets a temporary list for queries.
        /// </summary>
        /// <param name="listId"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<T> TemporaryList<T>(int listId)
        {
            var tt = TempTable<T>();

            if (!HasTempList<T>())
            {
                return GetContextTempList<T>(listId);
            }

            EnsureTempListExists(tt.ValueType);

            return this.TempQuery<T>(tt)
                       .Where(x => x.ListId == listId)
                       .Select(x => x.EntryValue);
        }
        
        

        /// <summary>
        /// Clears a temporary list for queries.
        /// </summary>
        /// <param name="listId"></param>
        /// <typeparam name="T"></typeparam>
        public void ClearTemporaryList<T>(int listId)
        {
            var tt = TempTable<T>();

            if (!HasTempList<T>())
            {
                var l = GetContextTempList<T>(listId);
                l.Clear();
                return;
            }
            
            EnsureTempListExists(tt.ValueType);
            Database.ExecuteSqlRaw(tt.GetClearCommand(Knowledge, listId));
        }

        /// <summary>
        /// Clears all the temporary lists of the specified type.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public void ClearTemporaryLists<T>()
        {
            var tt = TempTable<T>();

            if (!HasTempList<T>())
            {
                var d = GetContextTempLists<T>();
                d.Clear();
                return;
            }
            
            EnsureTempListExists(tt.ValueType);
            Database.ExecuteSqlRaw(tt.GetPurgeCommand(Knowledge));
        }

        /// <summary>
        /// Sets the contents of a temporary list for queries.
        /// </summary>
        /// <param name="listId"></param>
        /// <param name="values"></param>
        /// <typeparam name="T"></typeparam>
        public void SetTemporaryList<T>(int listId, IEnumerable<T> values)
        {
            var tt = TempTable<T>();
            if (!HasTempList<T>())
            {
                var l = GetContextTempList<T>(listId);
                l.Clear();
                l.AddRange(values);
                return;
            }
            
            // force enumeration.
            values = values.ToArray();
            
            EnsureTempListExists(tt.ValueType);

            Database.ExecuteSqlRaw(tt.GetClearCommand(Knowledge, listId));
            var size = 256;
            var queue = values.ToArray();

            while (size > 0 &&
                   queue.Any())
            {
                if (queue.Length >= size)
                {
                    var cmd = tt.GetInsertCommand(Knowledge, listId, size);
                    while (queue.Length >= size)
                    {
                        var tmp = queue.Take(size).Cast<object>().ToArray();
                        queue = queue.Skip(size).ToArray();
                        if (Database.ExecuteSqlRaw(cmd, tmp) != size)
                        {
                            throw new DataException($"Failed to insert {size} records into {tt.BaseTableName}.");
                        }
                    }
                }

                size >>= 1;
            }

            if (queue.Any())
            {
                throw new InvalidOperationException("Queue did not clear.");
            }

            var cnt = TemporaryList<T>(listId).Count();
            if (cnt != values.Count())
            {
                throw new InvalidOperationException($"Temporary list count does not match value count. ({cnt} <> {values.Count()})\nThe table contains {this.TempQuery<T>(tt).Count()} records in total.");
            }
        }

        /// <summary>
        /// Sets the contents of a temporary list for queries.
        /// </summary>
        /// <param name="listId"></param>
        /// <param name="values"></param>
        /// <typeparam name="T"></typeparam>
        public void SetTemporaryList<T>(int listId, IQueryable<T> values)
        {
            var tt = TempTable<T>();
            if (!HasTempList<T>())
            {
                var l = GetContextTempList<T>(listId);
                l.Clear();
                l.AddRange(values);
                return;
            }

            FormattableString sql;

            try
            {
                var psql = values.ToParameterizedSql();

                if (!ReferenceEquals(this, psql.DbContext))
                {
                    throw new ArgumentException("Not from the same DbContext.");
                }

                if (!psql.SqlText.StartsWith("SELECT ", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException("Not a SELECT statement.");
                }
                
                var fmt = psql.ToFormattableString();
                var withListId = $"SELECT {listId}, {psql.SqlText.Substring(7)}";
                sql = FormattableStringFactory.Create(withListId, fmt.GetArguments());
            }
            catch (Exception e) when ((e is ArgumentException) || (e is InvalidOperationException))
            {
                SetTemporaryList(listId, values.ToArray());
                return;
            }
            
            EnsureTempListExists(tt.ValueType);

            Database.ExecuteSqlRaw(tt.GetClearCommand(Knowledge, listId));
            
            var cmd = tt.GetInsertCommand(Knowledge, listId, -1);

            Database.ExecuteSqlRaw(cmd + sql.Format, sql.GetArguments());
        }

        /// <summary>
        /// Sets the contents of a temporary list and returns the query filter.
        /// </summary>
        /// <param name="listId"></param>
        /// <param name="values"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<T> ToTemporaryList<T>(int listId, IEnumerable<T> values)
        {
            if (values is IQueryable<T> query)
            {
                SetTemporaryList(listId, query);
            }
            else
            {
                SetTemporaryList(listId, values);
            }

            return TemporaryList<T>(listId);
        }

        // use negative values for auto-lists.
        private int _autoTempListId = int.MinValue;

        /// <summary>
        /// Sets the contents of a temporary list and returns the query filter.
        /// </summary>
        /// <param name="values"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public IEnumerable<T> ToTemporaryList<T>(IEnumerable<T> values)
        {
            int listId;
            
            lock (this)
            {
                listId = _autoTempListId++;
            }

            return ToTemporaryList(listId, values);
        }

        #endregion
    }
}
