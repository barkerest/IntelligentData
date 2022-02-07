using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IntelligentData.Extensions;
using IntelligentData.Tests.Examples;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace IntelligentData.Tests
{
    public class QueryableExtensions_Should : IDisposable
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
            return new (string, Func<ExampleContext, IQueryable<object>>)[]
                {
                    ("Unfiltered", db => db.ReadOnlyEntities),

                    ("Const Filter", db => db.ReadOnlyEntities.Where(r => r.Name != "John")),

                    ("Param Filter", db =>
                        {
                            var name = "John";
                            return db.ReadOnlyEntities.Where(r => r.Name != name);
                        }),

                    ("In Subquery", db => db.ReadOnlyEntities.Where(r => db.ReadInsertEntities.Select(ri => ri.ID).Contains(r.ID))),

                    ("In Const List", db => db.ReadOnlyEntities.Where(r => new[] {1, 3, 5, 7, 9}.Contains(r.ID))),

                    ("In Param List", db =>
                        {
                            var list = new[] {1, 3, 5, 7, 9};
                            return db.ReadOnlyEntities.Where(r => list.Contains(r.ID));
                        }),

                    ("In Subquery In Param List", db =>
                        {
                            var list = new[] {"John", "George", "Larry"};
                            return db.ReadOnlyEntities.Where(r => db.ReadInsertEntities.Where(p => !list.Contains(p.Name)).Select(p => p.ID).Contains(r.ID));
                        }),
                    ("Calculated Column", db => 
                         db.ReadOnlyEntities.Select(r => new {r.ID, r.Name, SimilarCount = db.ReadInsertEntities.Count(x => x.Name == r.Name)})),
                    ("Calculated Column In List", db => 
                         db.ReadOnlyEntities.Where(r => new []{1,3,5,7}.Contains(r.ID))
                           .Select(r => new {r.ID, r.Name, SimilarCount = db.ReadInsertEntities.Count(x => new[]{2,4,6,8}.Contains(x.ID))})),
                    ("Simple Union", db =>
                         db.ReadOnlyEntities.Select(r => new {r.ID, r.Name})
                           .Union(db.ReadInsertEntities.Select(r => new {r.ID, r.Name}))
                           .Union(db.ReadUpdateEntities.Select(r => new {r.ID, r.Name}))),
                    ("Union Filtered by Param", db =>
                        {
                            var name = "George";
                            return db.ReadOnlyEntities.Where(r => r.Name != name).Select(r => new {r.ID, r.Name})
                                     .Union(db.ReadInsertEntities.Where(r => r.Name != name).Select(r => new {r.ID, r.Name}))
                                     .Union(db.ReadUpdateEntities.Where(r => r.Name != name).Select(r => new {r.ID, r.Name}));
                        }),
                    ("Union Filtered by List", db =>
                        {
                            var list = new[] {"John", "George", "Larry"};
                            return db.ReadOnlyEntities.Where(r => !list.Contains(r.Name)).Select(r => new {r.ID, r.Name})
                                     .Union(db.ReadInsertEntities.Where(r => !list.Contains(r.Name)).Select(r => new {r.ID, r.Name}))
                                     .Union(db.ReadUpdateEntities.Where(r => !list.Contains(r.Name)).Select(r => new {r.ID, r.Name}));
                        }),
                }
                .Select(x => new object[] {x.Item1, x.Item2});
        }

        [Theory]
        [MemberData(nameof(GetQueries))]
        public void GetSqlString(string title, Func<ExampleContext, IQueryable<object>> getQuery)
        {
            _output.WriteLine(title);

            var query = getQuery(_db);
            Assert.NotNull(query);

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

        public void Dispose()
        {
            _db?.Dispose();
        }
    }
}
