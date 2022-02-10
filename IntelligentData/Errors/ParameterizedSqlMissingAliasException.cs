using System;
using IntelligentData.Interfaces;

namespace IntelligentData.Errors
{
    public class ParameterizedSqlMissingAliasException : InvalidOperationException, IIntelligentDataException
    {
        public IParameterizedSql ParameterizedSql { get; }

        public ParameterizedSqlMissingAliasException(IParameterizedSql parameterizedSql)
            : base("The parameterized SQL is missing a table alias.")
        {
            ParameterizedSql = parameterizedSql;
        }
    }
}
