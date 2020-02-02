﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using IntelligentData.Attributes;
using IntelligentData.Enums;
using IntelligentData.Extensions;
using IntelligentData.Interfaces;
using IntelligentData.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

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
        /// Constructs the intelligent DB context.
        /// </summary>
        /// <param name="options">The options to construct the DB context with.</param>
        /// <param name="currentUserProvider">The current user provider.</param>
        protected IntelligentDbContext(DbContextOptions options, IUserInformationProvider currentUserProvider)
            : base(options)
        {
            CurrentUserProvider = currentUserProvider ?? new Nobody();
        }

        #region Access Control

        private readonly Dictionary<Type, AccessLevel> _defaultAccessLevels = new Dictionary<Type, AccessLevel>();
        private          bool                          _allowSeedData       = false;

        private AccessLevel DefaultAccessForEntityType(Type entityType)
        {
            lock (_defaultAccessLevels)
            {
                if (_defaultAccessLevels.ContainsKey(entityType)) return _defaultAccessLevels[entityType];

                var level = AccessLevel.ReadOnly;

                // default access comes from attributes and is always the most permissive set from the attributes.
                foreach (var attr in entityType.GetCustomAttributes()
                                               .OfType<IEntityAccessProvider>())
                {
                    level |= attr.EntityAccessLevel;
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
                    var val = property.PropertyInfo.GetValue(entity);
                    var newVal = provider(entity, val, this);
                    if (newVal == val) continue;

                    property.PropertyInfo.SetValue(entity, newVal);
                    entry.Property(property.Name)
                         .IsModified = true;
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

                // no further action needed for deleted entries.
                records.RemoveAll(x => x.State == EntityState.Deleted);

                // TODO: Additional processing of inserted and updated records.
                
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
                    
                    // TODO: Process additional attributes?
                }

                if (typeof(ITrackedEntity).IsAssignableFrom(et))
                {
                    entityType.FindProperty(nameof(ITrackedEntity.CreatedAt))
                              .HasRuntimeDefault<RuntimeDefaultNowAttribute>();
                    entityType.FindProperty(nameof(ITrackedEntity.LastModifiedAt))
                              .HasAutoUpdate<AutoUpdateToNowAttribute>();

                    if (typeof(ITrackedEntityWithUserName).IsAssignableFrom(et))
                    {
                        entityType.FindProperty(nameof(ITrackedEntityWithUserName.CreatedBy))
                                  .HasRuntimeDefault<RuntimeDefaultCurrentUserNameAttribute>();
                        entityType.FindProperty(nameof(ITrackedEntityWithUserName.LastModifiedBy))
                                  .HasAutoUpdate<AutoUpdateToCurrentUserNameAttribute>();
                    }

                    if (entityType.FindProperty(nameof(ITrackedEntityWithUserID<int>.CreatedByID)) is IMutableProperty createdById)
                    {
                        createdById.HasRuntimeDefault(new RuntimeDefaultCurrentUserIDAttribute() {UserIdType = createdById.PropertyInfo.PropertyType});
                    }

                    if (entityType.FindProperty(nameof(ITrackedEntityWithUserID<int>.LastModifiedByID)) is IMutableProperty modifiedById)
                    {
                        modifiedById.HasAutoUpdate(new AutoUpdateToCurrentUserIDAttribute() {UserIdType = modifiedById.PropertyInfo.PropertyType});
                    }
                }

                // TODO: Table modifications?
            }
        }

        #endregion
    }

    
}
