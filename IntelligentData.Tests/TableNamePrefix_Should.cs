using System;
using IntelligentData.Tests.Examples;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace IntelligentData.Tests
{
    public class TableNamePrefix_Should
    {
        private ExampleContext    _db;
        private ITestOutputHelper _output;

        public TableNamePrefix_Should(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _db     = ExampleContext.CreateContext(false);
        }

        
        [Fact]
        public void PrefixNameToTables()
        {
            var et = _db.Model.FindEntityType(typeof(ReadInsertEntity));
            var name = $"{_db.TableNamePrefix}_{nameof(_db.ReadInsertEntities)}";
            Assert.Equal(name, et.GetTableName());
            et   = _db.Model.FindEntityType(typeof(ReadUpdateDeleteEntity));
            name = $"{_db.TableNamePrefix}_{nameof(_db.ReadUpdateDeleteEntities)}";
            Assert.Equal(name, et.GetTableName());
        }

        [Fact]
        public void NotModifyExplicitlyPrefixedTableNames()
        {
            var et = _db.Model.FindEntityType(typeof(ReadOnlyEntity));
            var name = "EX__ReadOnly";
            Assert.Equal(name, et.GetTableName());
        }
        
    }
}
