using System;
using System.Collections;
using System.Linq;
using IntelligentData.Internal;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

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
            if (query is null) throw new ArgumentNullException(nameof(query));

            return new QueryInfo(query).Command.CommandText; 
        }

        /// <summary>
        /// Gets the relational command from this query.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public static IRelationalCommand GetCommand(this IQueryable query)
        {
            if (query is null) throw new ArgumentNullException(nameof(query));

            return new QueryInfo(query).Command;
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
            if (query is null) return false;

            try
            {
                value = new QueryInfo(query).Command.CommandText;
                return true;
            }
            catch (InvalidOperationException)
            {
                value = "";
                return false;
            }
        }

        /// <summary>
        /// Tries to get the relational command from this query.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="command"></param>
        /// <returns></returns>
        public static bool TryGetCommand(this IQueryable query, out IRelationalCommand command)
        {
            command = null;
            if (query is null) return false;

            try
            {
                command = new QueryInfo(query).Command;
                return true;
            }
            catch (InvalidOperationException)
            {
                command = null;
                return false;
            }
        }
        
        
    }
}
