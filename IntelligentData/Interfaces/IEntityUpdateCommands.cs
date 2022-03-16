using System.Data;
using System.Reflection;

namespace IntelligentData.Interfaces
{
    /// <summary>
    /// Defines commands to insert, update, or remove records of an entity type.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public interface IEntityUpdateCommands<TEntity> where TEntity : class
    {
        /// <summary>
        /// Sets the properties that will be set on insert.  The default is all properties except keys with auto-generated values.
        /// </summary>
        /// <param name="properties"></param>
        void SetInsertProperties(params MemberInfo[] properties);
        
        /// <summary>
        /// Sets the properties that will be set on update.  The default is all properties except keys.
        /// </summary>
        /// <param name="properties"></param>
        void SetUpdateProperties(params MemberInfo[] properties);
        
        /// <summary>
        /// Sets the properties that will be set on remove.  The default is none which triggers a delete.
        /// </summary>
        /// <param name="properties"></param>
        void SetRemoveProperties(params MemberInfo[] properties);

        /// <summary>
        /// Inserts the entity into the database.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="transaction"></param>
        /// <returns>Returns true on success.</returns>
        /// <remarks>
        /// Entities with an auto-increment primary key will have the value retrieved after insert.
        /// </remarks>
        bool Insert(TEntity entity, IDbTransaction? transaction);

        /// <summary>
        /// Updates the entity in the database.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="transaction"></param>
        /// <returns>Returns true on success.</returns>
        /// <remarks>
        /// The entity should be known to the DbContext entity tracker so that original values can
        /// be retrieved for concurrency tokens.
        /// </remarks>
        bool Update(TEntity entity, IDbTransaction? transaction);

        /// <summary>
        /// Removes (or hides) the entity from the database.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="transaction"></param>
        /// <returns>Returns true on success.</returns>
        /// <remarks>
        /// The entity should be known to the DbContext entity tracker so that original values can
        /// be retrieved for concurrency tokens.
        /// If remove properties are set, then the entity will not be deleted but is instead assumed
        /// to be hidden.
        /// </remarks>
        bool Remove(TEntity entity, IDbTransaction? transaction);
    }
}
