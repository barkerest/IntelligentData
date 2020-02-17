using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IntelligentData.Extensions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

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

        private QuerySqlGenerator _generator;

        public QuerySqlGenerator SqlGenerator
        {
            get
            {
                if (_generator != null) return _generator;

                _generator = SqlGeneratorFactory.Create();
                
                return _generator;
            }
        }
        
        private IRelationalCommand _command;

        public IRelationalCommand Command
        {
            get
            {
                if (_command != null) return _command;

                _command = SqlGenerator.GetCommand(Expression);
                
                return _command;
            }
        }
        
        
        public static QueryInfo Create(IQueryable query)
        {
            if (query is null) throw new ArgumentNullException(nameof(query));
            
            var ret = new QueryInfo();
            
            var provider = query.Provider as EntityQueryProvider;
            if (provider is null)
            {
                throw new InvalidOperationException("Does not use an entity query provider.");
            }

            // ReSharper disable once EF1001
            var enumerator = provider.Execute<IEnumerable>(query.Expression).GetEnumerator();

            ret.CommandCache = enumerator.GetNonPublicField<RelationalCommandCache>("_relationalCommandCache");

            if (ret.CommandCache is null)
            {
                throw new InvalidOperationException("Does not appear to be a relational command.");
            }

            ret.Context = enumerator.GetNonPublicField<RelationalQueryContext>("_relationalQueryContext");
            if (ret.Context is null)
            {
                throw new InvalidOperationException("Does not provide a query context.");
            }
            
            ret.Expression = ret.CommandCache.GetNonPublicField<SelectExpression>("_selectExpression");
            if (ret.Expression is null)
            {
                throw new InvalidOperationException("Does not provide a select expression.");
            }

            ret.SqlGeneratorFactory = ret.CommandCache.GetNonPublicField<IQuerySqlGeneratorFactory>("_querySqlGeneratorFactory");
            if (ret.SqlGeneratorFactory is null)
            {
                throw new InvalidOperationException("Does not provide a SQL generator factory.");
            }

            ret.Expression.PatchInExpressions(ret.Context);
            
            return ret;
        }
        
        public static QueryInfo Create(IQueryable query, out string message)
        {
            message = "";

            try
            {
                return Create(query);
            }
            catch (InvalidOperationException e)
            {
                message = e.Message;
                return null;
            }
        }
        
    }
}
