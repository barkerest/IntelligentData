using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IntelligentData.Extensions;
using IntelligentData.Tests.Examples;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace IntelligentData.Tests
{
    public class QueryableExtensions_Should
    {
        private ExampleContext    _db;
        private ITestOutputHelper _output;

        public QueryableExtensions_Should(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _db     = ExampleContext.CreateContext(false);
        }

        public static IEnumerable<object[]> GetQueries()
        {
            return new Func<ExampleContext, IQueryable<ReadOnlyEntity>>[]
                {
                    // simple select all
                    db => db.ReadOnlyEntities,
                    
                    // with a simple filter.
                    db => db.ReadOnlyEntities.Where(r => r.Name != "John"),
                    
                    // with a subquery
                    db => db.ReadOnlyEntities.Where(r => db.ReadInsertEntities.Select(ri => ri.ID).Contains(r.ID)),
                    
                    // with a value list.
                    db => db.ReadOnlyEntities.Where(r => new []{1,3,5,7,9}.Contains(r.ID)),
                    
                    // with a value list parameter.
                    db =>
                    {
                        var list = new[] {1, 3, 5, 7, 9};
                        return db.ReadOnlyEntities.Where(r => list.Contains(r.ID));
                    }
                }
                .Select(x => new object[] {x});
        }
        
        [Theory]
        [MemberData(nameof(GetQueries))]
        public void GetSqlString(Func<ExampleContext,IQueryable<ReadOnlyEntity>> getQuery)
        {
            var query = getQuery(_db);
            Assert.NotNull(query);
            _output.WriteLine(query.ToString());

            var task = Task.Run<string>(() => query.GetSqlString());

            if (task.Wait(TimeSpan.FromSeconds(10)))
            {
                _output.WriteLine(task.Result);
            }
            else
            {
                throw new XunitException("Timeout waiting for SQL generation.");
            }
        }
    }
}
