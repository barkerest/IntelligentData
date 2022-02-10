using System;
using System.Collections.Generic;
using System.Data;
using IntelligentData.Interfaces;

namespace IntelligentData.Internal
{
    internal class CustomCommand : ICustomCommand
    {
        public CustomCommand(Type parameterSourceType, string sqlStatement, params Func<object, object?>[] accessors)
        {
            ParameterSourceType = parameterSourceType;
            SqlStatement        = sqlStatement;
            if (string.IsNullOrWhiteSpace(SqlStatement)) throw new ArgumentException("SQL statement cannot be blank.", nameof(sqlStatement));
            ParameterAccessors  = accessors;
        }
        
        /// <summary>
        /// The data type the parameter source must be assignable to.
        /// </summary>
        public Type ParameterSourceType { get; }

        /// <inheritdoc />
        public string SqlStatement { get; }

        /// <inheritdoc />
        public IReadOnlyList<Func<object, object?>> ParameterAccessors { get; }
        
        /// <inheritdoc />
        public IDbCommand CreateCommand(IDbConnection connection, object parameterSource)
        {
            if (!ParameterSourceType.IsInstanceOfType(parameterSource))
                throw new ArgumentException($"The parameter source must be of {ParameterSourceType} type.");

            var cmd = connection.CreateCommand();
            cmd.CommandText = SqlStatement;

            for (var i = 0; i < ParameterAccessors.Count; i++)
            {
                var paramAccessor = ParameterAccessors[i];
                var paramValue = paramAccessor(parameterSource) ?? DBNull.Value;
                var p = cmd.CreateParameter();
                p.ParameterName = $"@p_{i}";
                p.Value = paramValue;
                cmd.Parameters.Add(p);
            }

            return cmd;
        }
    }
}
