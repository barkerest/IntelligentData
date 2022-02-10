using System;
using IntelligentData.Interfaces;

namespace IntelligentData.Errors
{
    public class ParameterizedSqlMissingContextException : InvalidOperationException, IIntelligentDataException
    {
        public IParameterizedSql ParameterizedSql { get; }

        public ParameterizedSqlMissingContextException(IParameterizedSql parameterizedSql)
            : base("The parameterized SQL object is missing a DbContext.")
        {
            ParameterizedSql = parameterizedSql;
        }
    }
}
