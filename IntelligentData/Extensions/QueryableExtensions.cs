using System;
using System.Collections;
using System.Linq;
using IntelligentData.Internal;
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

            var provider = query.Provider as EntityQueryProvider;
            if (provider is null)
            {
                value = "Does not use an entity query provider.";
                return false;
            }

            // ReSharper disable once EF1001
            var enumerator = provider.Execute<IEnumerable>(query.Expression).GetEnumerator();

            var commandCache = enumerator.GetNonPublicField<RelationalCommandCache>("_relationalCommandCache");

            if (commandCache is null)
            {
                value = "Does not appear to be a relational command.";
                return false;
            }

            var queryContext = enumerator.GetNonPublicField<RelationalQueryContext>("_relationalQueryContext");
            if (queryContext is null)
            {
                value = "Does not provide a query context.";
                return false;
            }
            
            var selectExpression = commandCache.GetNonPublicField<SelectExpression>("_selectExpression");
            if (selectExpression is null)
            {
                value = "Does not provide a select expression.";
                return false;
            }

            selectExpression.PatchInExpressions(queryContext);

            var factory = commandCache.GetNonPublicField<IQuerySqlGeneratorFactory>("_querySqlGeneratorFactory");
            if (factory is null)
            {
                value = "Does not provide a SQL generator factory.";
                return false;
            }

            var generator = factory.Create();
            var command   = generator.GetCommand(selectExpression);
            value = command.CommandText;

            return true;
        }
    }
}
