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
            
            CommandCache = enumerator.GetNonPublicField<RelationalCommandCache>("_relationalCommandCache")
                           ?? throw new ArgumentException("Does not appear to be a relational command.", nameof(query));
            
            Context = enumerator.GetNonPublicField<RelationalQueryContext>("_relationalQueryContext")
                      ?? throw new ArgumentException("Does not provide a query context.", nameof(query));
            
            Expression = CommandCache.GetNonPublicField<SelectExpression>("_selectExpression")
                         ?? throw new ArgumentException("Does not provide a select expression.", nameof(query));
            
            SqlGeneratorFactory = CommandCache.GetNonPublicField<IQuerySqlGeneratorFactory>("_querySqlGeneratorFactory")
                                  ?? throw new ArgumentException("Does not provide a SQL generator factory.", nameof(query));

            Expression.PatchInExpressions(Context);
        }

        public SelectExpression Expression { get; }

        public RelationalQueryContext Context { get; }

        public RelationalCommandCache CommandCache { get; }

        public IQuerySqlGeneratorFactory SqlGeneratorFactory { get; }

        private QuerySqlGenerator? _generator;

        public QuerySqlGenerator SqlGenerator
        {
            get
            {
                if (_generator is not null) return _generator;

                _generator = SqlGeneratorFactory.Create();

                return _generator;
            }
        }

        private IRelationalCommand? _command;

        public IRelationalCommand Command
        {
            get
            {
                if (_command is not null) return _command;

                _command = SqlGenerator.GetCommand(Expression);

                return _command;
            }
        }
    }
}
