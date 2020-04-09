using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelligentData.Interfaces
{
    /// <summary>
    /// The definition for a temporary list.
    /// </summary>
    public interface ITempListDefinition
    {
        /// <summary>
        /// The type of value stored in the list.
        /// </summary>
        Type ValueType { get; }
        
        /// <summary>
        /// The entity type storing the values.
        /// </summary>
        Type EntityType { get; }
        
        /// <summary>
        /// The base table name for this temporary list.
        /// </summary>
        string BaseTableName { get; }
        
        /// <summary>
        /// Gets the value type name based on the supplied knowledge.
        /// </summary>
        /// <param name="knowledge"></param>
        /// <returns></returns>
        string GetValueTypeName(ISqlKnowledge knowledge);
        
        /// <summary>
        /// Gets the table name based on the supplied knowledge.
        /// </summary>
        /// <param name="knowledge"></param>
        /// <returns></returns>
        string GetTableName(ISqlKnowledge knowledge);
        
        /// <summary>
        /// Gets the create table command based on the supplied knowledge.
        /// </summary>
        /// <param name="knowledge"></param>
        /// <returns></returns>
        string GetCreateTableCommand(ISqlKnowledge knowledge);
        
        /// <summary>
        /// Gets the command to clear a temporary list based on the supplied knowledge.
        /// </summary>
        /// <param name="knowledge"></param>
        /// <param name="listId"></param>
        /// <returns></returns>
        string GetClearCommand(ISqlKnowledge knowledge, int listId);
        
        /// <summary>
        /// Gets the command to clear all temporary lists of this type based on the supplied knowledge.
        /// </summary>
        /// <param name="knowledge"></param>
        /// <returns></returns>
        string GetPurgeCommand(ISqlKnowledge knowledge);
        
        /// <summary>
        /// Gets the command to insert records into a temporary list based on the supplied knowledge.
        /// </summary>
        /// <param name="knowledge"></param>
        /// <param name="listId"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        string GetInsertCommand(ISqlKnowledge knowledge, int listId, int count);
        
        /// <summary>
        /// Customizes the property builder during model building.
        /// </summary>
        /// <param name="propertyBuilder"></param>
        void CustomizeValueProperty(PropertyBuilder propertyBuilder);

        /// <summary>
        /// Gets the DB set from the supplied context, must return IQueryable&lt;ITempListEntry&lt;ValueType&gt;&gt; type.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        /// <remarks>
        /// The query should also turn off tracking if appropriate.
        /// </remarks>
        IQueryable GetSet(DbContext context);
    }
}
