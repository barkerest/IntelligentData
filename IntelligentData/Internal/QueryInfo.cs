using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using IntelligentData.Extensions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace IntelligentData.Internal
{
    [SuppressMessage("Usage", "EF1001:Internal EF Core API usage.")]
    internal class QueryInfo
    {
        internal QueryInfo(IQueryable query)
        {
            if (query is null) throw new ArgumentNullException(nameof(query));

            var provider = query.Provider as EntityQueryProvider
                           ?? throw new ArgumentException("Does not use an entity query provider.", nameof(query));

            var enumerator = provider
                             .Execute<IEnumerable>(query.Expression)
                             .GetEnumerator()
                             ?? throw new ArgumentException("Entity provider does not generate an enumerator.");

            var commandCache = enumerator.GetNonPublicField<RelationalCommandCache>("_relationalCommandCache")
                               ?? throw new ArgumentException("Does not appear to be a relational command.", nameof(query));

            Context = enumerator.GetNonPublicField<RelationalQueryContext>("_relationalQueryContext")
                      ?? throw new ArgumentException("Does not provide a query context.", nameof(query));

            Expression = commandCache.GetNonPublicField<SelectExpression>("_selectExpression")
                         ?? throw new ArgumentException("Does not provide a select expression.", nameof(query));

            var sqlGeneratorFactory = commandCache.GetNonPublicField<IQuerySqlGeneratorFactory>("_querySqlGeneratorFactory")
                                      ?? throw new ArgumentException("Does not provide a SQL generator factory.", nameof(query));

            var generator = sqlGeneratorFactory.Create();

            Expression = Expression.PatchInExpressions(Context);

            Command = generator.GetCommand(Expression);
        }

        public SelectExpression Expression { get; }

        public RelationalQueryContext Context { get; }

        public IRelationalCommand Command { get; }
    }
}
