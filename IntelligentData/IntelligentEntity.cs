using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using IntelligentData.Enums;
using IntelligentData.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
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
        /// Gets the linked DB context for this entity.
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

        private readonly EntityInfo _info;

        /// <summary>
        /// Initializes this entity with a DB context.
        /// </summary>
        /// <param name="dbContext">The DB context for this entity.</param>
        protected IntelligentEntity(TContext dbContext)
        {
            _context = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _info    = _context.GetEntityInfoFor(GetType());
            _context.InitializeEntity(this);
        }

        /// <summary>
        /// Gets the validation errors for this entity.
        /// </summary>
        /// <returns>Returns the validation errors (if any) for this entity.</returns>
        public IEnumerable<ValidationResult> GetValidationErrors()
        {
            var ctx     = new ValidationContext(this, _context.GetInfrastructure(), null);
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
            var ctx     = new ValidationContext(this, _context.GetInfrastructure(), null);
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
            var ctx = new ValidationContext(this, _context.GetInfrastructure(), null);
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
            var flag = _context.ChangeTracker.AutoDetectChangesEnabled;
            _context.ChangeTracker.AutoDetectChangesEnabled = false;
            try
            {
                var entityEntry = _context.Entry(this);
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
                _context.ChangeTracker.AutoDetectChangesEnabled = flag;
            }

            if (_info.HasDefaultPrimaryKey(this))
                return false;

            var dbEntry = _context
                .Find(
                    _info.EntityType,
                    _info.GetPrimaryKey(this)
                );

            if (dbEntry is null)
                return false;

            _context.Entry(dbEntry).State = EntityState.Detached;

            return true;
        }

        private UpdateResult DoSaveChanges()
        {
            try
            {
                var changes = _context.SaveChanges();
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
                _context.Entry(this).State = EntityState.Detached;
                return UpdateResult.SuccessNoChanges;
            }

            if ((_context.AccessForEntity(this) & AccessLevel.Delete) != AccessLevel.Delete)
                return UpdateResult.FailedDeleteDisallowed;

            if (!ExistsInDatabase())
            {
                _context.Entry(this).State = EntityState.Detached;
                return UpdateResult.SuccessNoChanges;
            }
            
            _context.Entry(this).State = EntityState.Deleted;
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

            var state = _context.Entry(this).State;

            switch (state)
            {
                case EntityState.Deleted:
                case EntityState.Unchanged:
                {
                    if ((_context.AccessForEntity(this) & AccessLevel.Update) != AccessLevel.Update)
                        return UpdateResult.FailedUpdateDisallowed;

                    // unchanged and deleted get converted to modified since we're saving.
                    _context.Entry(this).State = EntityState.Modified;
                    break;
                }
                case EntityState.Modified when (_context.AccessForEntity(this) & AccessLevel.Update) != AccessLevel.Update:
                    return UpdateResult.FailedUpdateDisallowed;
                case EntityState.Added when (_context.AccessForEntity(this) & AccessLevel.Insert) != AccessLevel.Insert:
                    return UpdateResult.FailedInsertDisallowed;
                case EntityState.Detached:
                    if (IsNewEntity())
                    {
                        if ((_context.AccessForEntity(this) & AccessLevel.Insert) != AccessLevel.Insert)
                            return UpdateResult.FailedInsertDisallowed;

                        _context.Add(this);
                    }
                    else if ((_context.AccessForEntity(this) & AccessLevel.Update) != AccessLevel.Update)
                    {
                        return UpdateResult.FailedUpdateDisallowed;
                    }
                    else
                    {
                        _context.Update(this);
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
