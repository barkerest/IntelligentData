using System;
using System.Collections.Generic;

namespace IntelligentData.Interfaces
{
    /// <summary>
    /// Defines a common interface for the ParameterizedSQL objects.
    /// </summary>
    public interface IParameterizedSql
    {
        /// <summary>
        /// The SQL text.
        /// </summary>
        public string SqlText { get; }

        /// <summary>
        /// The parameters for the SQL text.
        /// </summary>
        public IReadOnlyDictionary<string, object?> ParameterValues { get; }

        /// <summary>
        /// Creates a formattable string from this parameterized SQL. 
        /// </summary>
        /// <returns></returns>
        public FormattableString ToFormattableString();
    }
}
