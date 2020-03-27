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
    public class ParameterizedSql_Should
    {
        private readonly ITestOutputHelper _output;
        private readonly ILogger           _logger;
        private readonly ExampleContext    _db;

        public ParameterizedSql_Should(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _logger = new TestOutputLogger(output);
            _db     = ExampleContext.CreateContext();
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
            var psql = new ParameterizedSql<DefaultAccessEntity>(_db.DefaultAccessEntities.Where(x => x.Name == param), _logger);
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
            var del = psql.ToDelete();
            Assert.True(psql.IsSelect);
            Assert.False(del.IsSelect);
            Assert.True(del.IsDelete);
        }

        [Fact]
        public void ConvertToUpdate()
        {
            var p1 = "John Doe";
            var p2 = "Jane Smith";
            var psql = new ParameterizedSql<DefaultAccessEntity>(_db.DefaultAccessEntities.Where(x => x.Name == p1), _logger);
            var upd = psql.ToUpdate(x => new DefaultAccessEntity() {Name = p2});
            Assert.True(psql.IsSelect);
            Assert.False(upd.IsSelect);
            Assert.True(upd.IsUpdate);
        }
        
    }
}
