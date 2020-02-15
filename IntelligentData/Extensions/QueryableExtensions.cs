using System;
using System.Collections;
using System.Linq;
using IntelligentData.Internal;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace IntelligentData.Extensions
{
    /// <summary>
    /// Extension methods for queryable interfaces.
    /// </summary>
    public static class QueryableExtensions
    {
        /// <summary>
        /// Gets the SQL string from this query.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static string GetSqlString(this IQueryable query)
        {
            var result = TryGetSqlString(query, out var value);
            if (!result) throw new InvalidOperationException(value);
            return value;
        }

        /// <summary>
        /// Tries to get the SQL string from this query.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool TryGetSqlString(this IQueryable query, out string value)
        {
            value = "";

            var queryInfo = QueryInfo.Create(query, out value);
            if (queryInfo is null) return false;
            
            queryInfo.Expression.PatchInExpressions(queryInfo.Context);
            
            var generator = queryInfo.SqlGeneratorFactory.Create();
            var command   = generator.GetCommand(queryInfo.Expression);
            value = command.CommandText;

            return true;
        }
    }
}
