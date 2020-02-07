using System;
using System.Linq;
using IntelligentData.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IntelligentData
{
    /// <summary>
    /// A base class for an intelligent entity.
    /// </summary>
    /// <typeparam name="TContext"></typeparam>
    public abstract class IntelligentEntity<TContext> 
        where TContext : IntelligentDbContext
    {
        private readonly TContext _context;
        
        /// <summary>
        /// Gets the linked DB context for this model.
        /// </summary>
        /// <remarks>
        /// This will log a warning for lazy loading.
        /// </remarks>
        protected TContext DbContext
        {
            get
            {
                if (_context.WarnOnLazyLoading)
                    _context.Logger.LogWarning($"DbContext requested in {GetType()}, lazy loading should be avoided.");
                
                return _context;
            }
        }

        /// <summary>
        /// Initializes this entity with a DB context.
        /// </summary>
        /// <param name="dbContext">The DB context for this entity.</param>
        protected IntelligentEntity(TContext dbContext)
        {
            _context = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _context.InitializeEntity(this);
        }

        /// <summary>
        /// Determines if this entity exists in the DB context.
        /// </summary>
        /// <returns>Returns true if the entity exists.</returns>
        public virtual bool ExistsInDatabase()
        {
            var flag = _context.ChangeTracker.AutoDetectChangesEnabled;
            _context.ChangeTracker.AutoDetectChangesEnabled = false;
            try
            {
                var entityEntry = _context.Entry(this);
                var state = entityEntry.State;
                switch (state)
                {
                    case EntityState.Unchanged:
                    case EntityState.Deleted:
                    case EntityState.Modified:
                        return true;     // currently exists in the database.
                    case EntityState.Added:
                        return false;    // does not currently exist in the database.
                    case EntityState.Detached:
                        // dig deeper.
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            finally
            {
                _context.ChangeTracker.AutoDetectChangesEnabled = flag;
            }

            var type = GetType();
            var entityType = _context.Model.FindEntityType(type);
            if (entityType is null) throw new InvalidOperationException($"Entity type {type} is not part of the data model.");

            var dbEntry = _context
                .Find(
                    type,
                    entityType
                        .FindPrimaryKey()
                        .Properties
                        .Select(p => p.PropertyInfo.GetValue(this))
                        .ToArray()
                );

            if (dbEntry is null) return false;
            _context.Entry(dbEntry).State = EntityState.Detached;
            return true;
        }

        private UpdateResult DoSaveChanges()
        {
            try
            {
                _context.SaveChanges();
                return UpdateResult.Success;
            }
            catch (DbUpdateConcurrencyException e)
            {
                var exceptionEntry = e.Entries.Single();
                var databaseEntry = exceptionEntry.GetDatabaseValues();
                return databaseEntry is null
                           ? UpdateResult.FailedDeletedByOther
                           : UpdateResult.FailedUpdatedByOther;
            }
            catch (DbUpdateException)
            {
                return UpdateResult.FailedUnknownReason;
            }
        }

        /// <summary>
        /// Removes this entity from the DB context.
        /// </summary>
        /// <returns></returns>
        public UpdateResult DeleteFromDatabase()
        {
            _context.Entry(this).State = EntityState.Deleted;
            return DoSaveChanges();
        }

        /// <summary>
        /// Saves this entity to the DB context (adding or updating as necessary).
        /// </summary>
        /// <returns></returns>
        public UpdateResult SaveToDatabase()
        {
            var state = _context.Entry(this).State;

            if (state == EntityState.Deleted ||
                state == EntityState.Unchanged)
            {
                // unchanged and deleted get converted to modified.
                _context.Entry(this).State = EntityState.Modified;
            }
            // modified and added stay the same.
            // detached gets special treatment.
            else if (state == EntityState.Detached)
            {
                if (ExistsInDatabase())
                {
                    _context.Update(this);
                }
                else
                {
                    _context.Add(this);
                }
            }

            return DoSaveChanges();
        }
        
    }
}
