using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using IntelligentData.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelligentData.Internal
{
    /// <summary>
    /// The generic implementation used internally.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TempListDefinition<T> : ITempListDefinition
    {
        private TempListDefinition()
        {
            ValueType = typeof(T);
            EntityType = typeof(TempListEntry<T>);

            var typeName = ValueType.Name;

            if (typeName.StartsWith("System."))
            {
                typeName = typeName.Substring(7);
            }

            typeName = Regex.Replace(typeName, @"[^A-Za-z0-9]", "");
            BaseTableName = "ID__TempList" + typeName;
        }

        /// <summary>
        /// Creates a generic implementation.
        /// </summary>
        /// <param name="valueTypeName"></param>
        /// <param name="customize"></param>
        public TempListDefinition(string? valueTypeName = null, Action<PropertyBuilder>? customize = null)
            : this()
        {
            _valueTypeName = valueTypeName;
            _customize = customize;
        }

        private readonly string?                  _valueTypeName;
        private readonly Action<PropertyBuilder>? _customize;

        /// <inheritdoc />
        public Type ValueType { get; }

        /// <inheritdoc />
        public Type EntityType { get; }

        /// <inheritdoc />
        public string BaseTableName { get; }

        /// <inheritdoc />
        public string GetValueTypeName(ISqlKnowledge knowledge)
            => string.IsNullOrEmpty(_valueTypeName) ? knowledge.GetValueTypeName(ValueType) : _valueTypeName;

        /// <inheritdoc />
        public string GetTableName(ISqlKnowledge knowledge)
            => knowledge.CreateTemporaryTableName(BaseTableName);

        /// <inheritdoc />
        public string GetCreateTableCommand(ISqlKnowledge knowledge)
        {
            return knowledge.GetCreateTemporaryTableCommand(
                GetTableName(knowledge),
                "(ListId INTEGER NOT NULL, EntryValue " +
                GetValueTypeName(knowledge) +
                "NOT NULL, PRIMARY KEY (ListId, EntryValue))"
            );
        }

        /// <inheritdoc />
        public string GetClearCommand(ISqlKnowledge knowledge, int listId)
            => $"DELETE FROM {GetTableName(knowledge)} WHERE ListId={listId}";

        /// <inheritdoc />
        public string GetPurgeCommand(ISqlKnowledge knowledge)
            => $"DELETE FROM {GetTableName(knowledge)}";

        /// <inheritdoc />
        public string GetInsertCommand(ISqlKnowledge knowledge, int listId, int count)
        {
            var ret = new StringBuilder();

            ret.Append("INSERT INTO ")
               .Append(GetTableName(knowledge))
               .Append(" (ListId, EntryValue) ");

            if (count < 1) return ret.ToString();

            ret.Append("VALUES (").Append(listId).Append(", {0})");

            for (var i = 1; i < count; i++)
            {
                ret.Append(", (").Append(listId).Append(", {").Append(i).Append("})");
            }
            
            return ret.ToString();
        }

        /// <inheritdoc />
        public void CustomizeValueProperty(PropertyBuilder propertyBuilder)
            => _customize?.Invoke(propertyBuilder);
        
        /// <inheritdoc />
        public IQueryable GetSet(DbContext context)
            => context.Set<TempListEntry<T>>().AsNoTracking();
    }
}
