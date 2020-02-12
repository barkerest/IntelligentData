using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using IntelligentData.Enums;
using IntelligentData.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
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
        /// <summary>
        /// The linked DB context for this entity.
        /// </summary>
        protected TContext DbContext { get; }

        /// <summary>
        /// The linked logger for this entity.
        /// </summary>
        protected ILogger Logger => DbContext.Logger;

        private readonly EntityInfo _info;

        /// <summary>
        /// Initializes this entity with a DB context.
        /// </summary>
        /// <param name="dbContext">The DB context for this entity.</param>
        protected IntelligentEntity(TContext dbContext)
        {
            DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _info     = DbContext.GetEntityInfoFor(GetType());
            DbContext.InitializeEntity(this);
        }

        /// <summary>
        /// Gets the validation errors for this entity.
        /// </summary>
        /// <returns>Returns the validation errors (if any) for this entity.</returns>
        public IEnumerable<ValidationResult> GetValidationErrors()
        {
            var ctx     = new ValidationContext(this, DbContext.GetInfrastructure(), null);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(this, ctx, results, true);
            return results;
        }

        /// <summary>
        /// Validates this entity.
        /// </summary>
        /// <param name="validationResults">The results of the validation.</param>
        /// <returns>Returns true if the entity passes validation.</returns>
        public bool IsValidForDatabase(out IEnumerable<ValidationResult> validationResults)
        {
            var ctx     = new ValidationContext(this, DbContext.GetInfrastructure(), null);
            var results = new List<ValidationResult>();
            var ret     = Validator.TryValidateObject(this, ctx, results, true);
            validationResults = results;
            return ret;
        }

        /// <summary>
        /// Validates this entity.
        /// </summary>
        /// <returns>Returns true if the entity passes validation.</returns>
        public bool IsValidForDatabase()
        {
            var ctx = new ValidationContext(this, DbContext.GetInfrastructure(), null);
            return Validator.TryValidateObject(this, ctx, null, true);
        }

        private bool? _isNew = null;

        /// <summary>
        /// Determines if this is a new entity for the database.
        /// </summary>
        /// <returns>Returns true if this is a new entity.</returns>
        public bool IsNewEntity()
        {
            if (_isNew.HasValue) return _isNew.Value;

            _isNew = _info.HasDefaultPrimaryKey(this) // default primary key is always new.
                     || !ExistsInDatabase();          // but we'll also classify it as new if it doesn't exist in the database.

            return _isNew.Value;
        }

        /// <summary>
        /// Determines if this entity exists in the DB context.
        /// </summary>
        /// <returns>Returns true if the entity exists.</returns>
        public bool ExistsInDatabase()
        {
            var flag = DbContext.ChangeTracker.AutoDetectChangesEnabled;
            DbContext.ChangeTracker.AutoDetectChangesEnabled = false;
            try
            {
                var entityEntry = DbContext.Entry(this);
                var state       = entityEntry.State;
                switch (state)
                {
                    case EntityState.Unchanged:
                    case EntityState.Deleted:
                    case EntityState.Modified:
                        return true; // currently exists in the database.
                    case EntityState.Added:
                        return false; // does not currently exist in the database.
                    case EntityState.Detached:
                        // dig deeper.
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            finally
            {
                DbContext.ChangeTracker.AutoDetectChangesEnabled = flag;
            }

            if (_info.HasDefaultPrimaryKey(this))
                return false;

            var dbEntry = DbContext
                .Find(
                    _info.EntityType,
                    _info.GetPrimaryKey(this)
                );

            if (dbEntry is null)
                return false;

            DbContext.Entry(dbEntry).State = EntityState.Detached;

            return true;
        }

        private UpdateResult DoSaveChanges()
        {
            try
            {
                var changes = DbContext.SaveChanges();
                return changes == 0 ? UpdateResult.SuccessNoChanges : UpdateResult.Success;
            }
            catch (DbUpdateConcurrencyException e)
            {
                var exceptionEntry = e.Entries.Single();
                var databaseEntry  = exceptionEntry.GetDatabaseValues();
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
            if (IsNewEntity())
            {
                DbContext.Entry(this).State = EntityState.Detached;
                return UpdateResult.SuccessNoChanges;
            }

            if ((DbContext.AccessForEntity(this) & AccessLevel.Delete) != AccessLevel.Delete)
                return UpdateResult.FailedDeleteDisallowed;

            if (!ExistsInDatabase())
            {
                DbContext.Entry(this).State = EntityState.Detached;
                return UpdateResult.SuccessNoChanges;
            }

            DbContext.Entry(this).State = EntityState.Deleted;
            return DoSaveChanges();
        }

        /// <summary>
        /// Saves this entity to the DB context (adding or updating as necessary).
        /// </summary>
        /// <returns></returns>
        public UpdateResult SaveToDatabase()
        {
            if (!IsValidForDatabase())
                return UpdateResult.FailedValidation;

            var state = DbContext.Entry(this).State;

            switch (state)
            {
                case EntityState.Deleted:
                case EntityState.Unchanged:
                {
                    if ((DbContext.AccessForEntity(this) & AccessLevel.Update) != AccessLevel.Update)
                        return UpdateResult.FailedUpdateDisallowed;

                    // unchanged and deleted get converted to modified since we're saving.
                    DbContext.Entry(this).State = EntityState.Modified;
                    break;
                }
                case EntityState.Modified when (DbContext.AccessForEntity(this) & AccessLevel.Update) != AccessLevel.Update:
                    return UpdateResult.FailedUpdateDisallowed;
                case EntityState.Added when (DbContext.AccessForEntity(this) & AccessLevel.Insert) != AccessLevel.Insert:
                    return UpdateResult.FailedInsertDisallowed;
                case EntityState.Detached:
                    if (IsNewEntity())
                    {
                        if ((DbContext.AccessForEntity(this) & AccessLevel.Insert) != AccessLevel.Insert)
                            return UpdateResult.FailedInsertDisallowed;

                        DbContext.Add(this);
                    }
                    else if ((DbContext.AccessForEntity(this) & AccessLevel.Update) != AccessLevel.Update)
                    {
                        return UpdateResult.FailedUpdateDisallowed;
                    }
                    else
                    {
                        DbContext.Update(this);
                    }

                    break;
            }

            var ret = DoSaveChanges();

            if (ret == UpdateResult.Success)
            {
                _isNew = false;
            }

            return ret;
        }
    }
}
