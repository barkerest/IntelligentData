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
            var queryInfo = QueryInfo.Create(query) 
                            ?? throw new InvalidOperationException("Failed to retrieve query info.");
            
            return queryInfo.Command.CommandText;
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
            
            value = queryInfo.Command.CommandText;

            return true;
        }
    }
}
