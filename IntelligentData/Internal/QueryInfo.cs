using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IntelligentData.Extensions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace IntelligentData.Internal
{
    internal class QueryInfo
    {
        private QueryInfo()
        {
            
        }
        
        public SelectExpression Expression { get; private set; }
        
        public RelationalQueryContext Context { get; private set; }
        
        public RelationalCommandCache CommandCache { get; private set; }
        
        public IQuerySqlGeneratorFactory SqlGeneratorFactory { get; private set; }

        private Dictionary<string, object> _params = new Dictionary<string, object>();

        public static QueryInfo Create(IQueryable query)
        {
            return Create(query, out var message) ?? throw new InvalidOperationException(message);
        }
        
        public static QueryInfo Create(IQueryable query, out string message)
        {
            message = "";
            
            if (query is null) throw new ArgumentNullException(nameof(query));
            
            var ret = new QueryInfo();
            
            var provider = query.Provider as EntityQueryProvider;
            if (provider is null)
            {
                message = "Does not use an entity query provider.";
                return null;
            }

            // ReSharper disable once EF1001
            var enumerator = provider.Execute<IEnumerable>(query.Expression).GetEnumerator();

            ret.CommandCache = enumerator.GetNonPublicField<RelationalCommandCache>("_relationalCommandCache");

            if (ret.CommandCache is null)
            {
                message = "Does not appear to be a relational command.";
                return null;
            }

            ret.Context = enumerator.GetNonPublicField<RelationalQueryContext>("_relationalQueryContext");
            if (ret.Context is null)
            {
                message = "Does not provide a query context.";
                return null;
            }
            
            ret.Expression = ret.CommandCache.GetNonPublicField<SelectExpression>("_selectExpression");
            if (ret.Expression is null)
            {
                message = "Does not provide a select expression.";
                return null;
            }

            ret.SqlGeneratorFactory = ret.CommandCache.GetNonPublicField<IQuerySqlGeneratorFactory>("_querySqlGeneratorFactory");
            if (ret.SqlGeneratorFactory is null)
            {
                message = "Does not provide a SQL generator factory.";
                return null;
            }
            
            return ret;
        }
        
    }
}
