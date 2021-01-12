using System;
using System.Linq;
using IntelligentData.Enums;
using IntelligentData.Extensions;
using IntelligentData.Tests.Examples;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace IntelligentData.Tests
{
    public class ParameterizedSql_Should : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly ILogger           _logger;
        private readonly ExampleContext    _db;

        public ParameterizedSql_Should(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _logger = new TestOutputLogger(output);
            _db     = ExampleContext.CreateContext(true);
            _db.SetDefaultAccessLevel(AccessLevel.FullAccess);
        }

        [Fact]
        public void CreateFromDbSet()
        {
            var psql = new ParameterizedSql<DefaultAccessEntity>(_db.DefaultAccessEntities, _logger);
            Assert.True(psql.IsSelect);
            Assert.False(psql.ReadOnly);
            Assert.Empty(psql.ParameterValues);
        }

        [Fact]
        public void CreateWithParam()
        {
            var param = "John Doe";
            var psql  = new ParameterizedSql<DefaultAccessEntity>(_db.DefaultAccessEntities.Where(x => x.Name == param), _logger);
            Assert.True(psql.IsSelect);
            Assert.False(psql.ReadOnly);
            Assert.NotEmpty(psql.ParameterValues);
        }

        [Fact]
        public void CreateFromDynamic()
        {
            var psql = _db.DefaultAccessEntities.Select(x => new {x.Name, IntVal = 1234}).ToParameterizedSql();
            _logger.LogDebug(psql.ToString());
            Assert.True(psql.IsSelect);
            Assert.True(psql.ReadOnly);
            Assert.Empty(psql.ParameterValues);
        }

        [Fact]
        public void ConvertToDelete()
        {
            var param = "John Doe";
            var psql  = new ParameterizedSql<DefaultAccessEntity>(_db.DefaultAccessEntities.Where(x => x.Name == param), _logger);
            var del   = psql.ToDelete();
            Assert.True(psql.IsSelect);
            Assert.False(del.IsSelect);
            Assert.True(del.IsDelete);
        }

        [Fact]
        public void ConvertToUpdate()
        {
            var p1   = "John Doe";
            var p2   = "Jane Smith";
            var psql = new ParameterizedSql<DefaultAccessEntity>(_db.DefaultAccessEntities.Where(x => x.Name == p1), _logger);
            var upd  = psql.ToUpdate(x => new DefaultAccessEntity() {Name = p2});
            Assert.True(psql.IsSelect);
            Assert.False(upd.IsSelect);
            Assert.True(upd.IsUpdate);
        }

        [Fact]
        public void ActuallyUpdate()
        {
            var item     = _db.ReadInsertUpdateDeleteEntities.First();
            var itemName = item.Name;
            var qry      = _db.ReadInsertUpdateDeleteEntities.Where(x => x.Name != itemName);
            var cnt      = qry.Count();
            Assert.True(cnt > 0);
            var sql = qry.ToParameterizedSql().ToUpdate(x => new ReadInsertUpdateDeleteEntity() {Name = itemName});
            _output.WriteLine(sql.ToString());
            _output.WriteLine($"Should update {cnt} records.");
            Assert.Equal(cnt, sql.ExecuteNonQuery());
            Assert.Equal(0, qry.Count());
        }

        [Fact]
        public void ActuallyDelete()
        {
            var item     = _db.ReadInsertUpdateDeleteEntities.First();
            var itemName = item.Name;
            var qry      = _db.ReadInsertUpdateDeleteEntities.Where(x => x.Name != itemName);
            var cnt      = qry.Count();
            Assert.True(cnt > 0);
            var sql = qry.ToParameterizedSql().ToDelete();
            _output.WriteLine(sql.ToString());
            _output.WriteLine($"Should delete {cnt} records.");
            Assert.Equal(cnt, sql.ExecuteNonQuery());
            Assert.Equal(1, _db.ReadInsertUpdateDeleteEntities.Count());
        }

        public void Dispose()
        {
            _db?.Dispose();
        }
    }
}
