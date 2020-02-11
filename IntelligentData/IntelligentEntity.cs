using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using IntelligentData.Enums;
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
        /// Gets the validation errors for this entity.
        /// </summary>
        /// <returns>Returns the validation errors (if any) for this entity.</returns>
        public IEnumerable<ValidationResult> GetValidationErrors()
        {
            var ctx = new ValidationContext(this, _context.GetInfrastructure(), null);
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
            var ctx = new ValidationContext(this, _context.GetInfrastructure(), null);
            var results = new List<ValidationResult>();
            var ret = Validator.TryValidateObject(this, ctx, results, true);
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
                    PrimaryKey
                        .Properties
                        .Select(p => p.PropertyInfo.GetValue(this))
                        .ToArray()
                );

            if (dbEntry is null) return false;
            _context.Entry(dbEntry).State = EntityState.Detached;
            return true;
        }

        private IKey _pkey;
        
        /// <summary>
        /// Gets the primary key definition for this entity.
        /// </summary>
        protected IKey PrimaryKey
        {
            get
            {
                if (_pkey != null) return _pkey;

                _pkey = _context.Model.FindEntityType(GetType()).FindPrimaryKey() 
                        ?? new Key(new Property[0], ConfigurationSource.Explicit);
                
                return _pkey;
            }
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
            if ((_context.AccessForEntity(this) & AccessLevel.Delete) != AccessLevel.Delete)
                return UpdateResult.FailedDeleteDisallowed;
            
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

            if (state == EntityState.Deleted ||
                state == EntityState.Unchanged)
            {
                if ((_context.AccessForEntity(this) & AccessLevel.Update) != AccessLevel.Update)
                    return UpdateResult.FailedUpdateDisallowed;
                
                // unchanged and deleted get converted to modified.
                _context.Entry(this).State = EntityState.Modified;
            }
            // modified and added stay the same.
            // detached gets special treatment.
            else if (state == EntityState.Detached)
            {
                if (ExistsInDatabase())
                {
                    if ((_context.AccessForEntity(this) & AccessLevel.Update) != AccessLevel.Update)
                        return UpdateResult.FailedUpdateDisallowed;
                    
                    _context.Update(this);
                }
                else
                {
                    if ((_context.AccessForEntity(this) & AccessLevel.Update) != AccessLevel.Update)
                        return UpdateResult.FailedInsertDisallowed;

                    _context.Add(this);
                }
            }

            return DoSaveChanges();
        }
        
    }
}
