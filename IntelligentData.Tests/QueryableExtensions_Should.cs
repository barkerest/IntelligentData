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
    [Collection("Database Instance")]

    public class QueryableExtensions_Should : IDisposable
    {
        private readonly ExampleContext    _db;
        private readonly ITestOutputHelper _output;

        public QueryableExtensions_Should(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _db     = ExampleContext.CreateContext(output, false);
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

        [Fact]
        public void GenerateDifferentSqlWithDifferentFilterLists()
        {
            var list = new[] { "John", "George", "Larry" };
            var sql1 = _db.ReadInsertUpdateEntities.Where(r => list.Contains(r.Name)).ToParameterizedSql();
            
            var list2 = new[] { "Lynn", "Mary", "Sara" };
            var sql2  = _db.ReadInsertUpdateEntities.Where(r => list2.Contains(r.Name)).ToParameterizedSql();
            
            _output.WriteLine($"1: {sql1}\n2: {sql2}");
            Assert.NotEqual(sql1.SqlText, sql2.SqlText);

            sql1 = sql1.ToUpdate(x => new ReadInsertUpdateEntity() { Name = x.Name + " Updated" });
            sql2 = sql2.ToUpdate(x => new ReadInsertUpdateEntity() { Name = x.Name + " Updated" });
            _output.WriteLine($"1: {sql1}\n2: {sql2}");
            Assert.NotEqual(sql1.SqlText, sql2.SqlText);
            
        }
        
        public void Dispose()
        {
            _db?.Dispose();
        }
    }
}
