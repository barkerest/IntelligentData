using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using IntelligentData.Enums;
using IntelligentData.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace IntelligentData
{
	/// <summary>
	/// The base class used for intelligent DB contexts.
	/// </summary>
	public abstract class IntelligentDbContext : DbContext
	{
		/// <summary>
		/// Constructs the intelligent DB context.
		/// </summary>
		/// <param name="options">The options to construct the DB context with.</param>
		protected IntelligentDbContext(DbContextOptions options) : base(options)
		{
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
				foreach (var attr in entityType.GetCustomAttributes().OfType<IEntityAccessProvider>())
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
					Entry(entry.Entity).State = EntityState.Detached;
				}
				else
				{
					// revert changes and set to unchanged.
					Entry(entry.Entity).Reload();
				}

				return false;
			}

			return true;
		}

		#endregion

		#region SeedData

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
				                           .Where(x => x.State == EntityState.Added ||
				                                       x.State == EntityState.Modified ||
				                                       x.State == EntityState.Deleted)
				                           .Where(IsChangePermitted)
				                           .ToList();

				// no further action needed for deleted entries.
				records.RemoveAll(x => x.State == EntityState.Deleted);
				
				// TODO: Additional processing of inserted and updated records.

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


	}
}
