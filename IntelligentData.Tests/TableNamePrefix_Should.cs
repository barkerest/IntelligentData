using System;
using IntelligentData.Interfaces;
using IntelligentData.Tests.Examples;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace IntelligentData.Tests
{
    [Collection("Database Instance")]

    public class TableNamePrefix_Should
    {
        public class NullExampleContext : ExampleContext
        {
            public NullExampleContext(DbContextOptions options, IUserInformationProvider currentUserProvider, ILogger logger)
                : base(options, currentUserProvider, logger, null)
            {
            }
        }
        
        public class EmptyExampleContext : ExampleContext
        {
            public EmptyExampleContext(DbContextOptions options, IUserInformationProvider currentUserProvider, ILogger logger)
                : base(options, currentUserProvider, logger, "")
            {
            }
        }
        
        public class BlankExampleContext : ExampleContext
        {
            public BlankExampleContext(DbContextOptions options, IUserInformationProvider currentUserProvider, ILogger logger)
                : base(options, currentUserProvider, logger, "   ")
            {
            }
        }
        
        
        private ITestOutputHelper _output;

        public TableNamePrefix_Should(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
        }

        [Fact]
        public void AllowNullPrefix()
        {
            using var db = ExampleContext.CreateContext<NullExampleContext>(_output, false);
            Assert.Null(db.TableNamePrefix);
            var et   = db.Model.FindEntityType(typeof(ReadInsertEntity));
            var name = nameof(db.ReadInsertEntities);
            Assert.Equal(name, et.GetTableName());
            et   = db.Model.FindEntityType(typeof(ReadUpdateDeleteEntity));
            name = nameof(db.ReadUpdateDeleteEntities);
            Assert.Equal(name, et.GetTableName());
        }

        [Fact]
        public void AllowEmptyPrefix()
        {
            using var db = ExampleContext.CreateContext<EmptyExampleContext>(_output, false);
            Assert.Equal("", db.TableNamePrefix);
            var et   = db.Model.FindEntityType(typeof(ReadInsertEntity));
            var name = nameof(db.ReadInsertEntities);
            Assert.Equal(name, et.GetTableName());
            et   = db.Model.FindEntityType(typeof(ReadUpdateDeleteEntity));
            name = nameof(db.ReadUpdateDeleteEntities);
            Assert.Equal(name, et.GetTableName());
        }

        [Fact]
        public void AllowBlankPrefix()
        {
            using var db = ExampleContext.CreateContext<BlankExampleContext>(_output, false);
            Assert.Equal("   ", db.TableNamePrefix);
            var et   = db.Model.FindEntityType(typeof(ReadInsertEntity));
            var name = nameof(db.ReadInsertEntities);
            Assert.Equal(name, et.GetTableName());
            et   = db.Model.FindEntityType(typeof(ReadUpdateDeleteEntity));
            name = nameof(db.ReadUpdateDeleteEntities);
            Assert.Equal(name, et.GetTableName());
        }

        [Fact]
        public void PrefixNameToTables()
        {
            using var db   = ExampleContext.CreateContext(_output, false);
            var et   = db.Model.FindEntityType(typeof(ReadInsertEntity));
            var name = $"{db.TableNamePrefix}_{nameof(db.ReadInsertEntities)}";
            Assert.Equal(name, et.GetTableName());
            et   = db.Model.FindEntityType(typeof(ReadUpdateDeleteEntity));
            name = $"{db.TableNamePrefix}_{nameof(db.ReadUpdateDeleteEntities)}";
            Assert.Equal(name, et.GetTableName());
        }

        [Fact]
        public void NotModifyExplicitlyPrefixedTableNames()
        {
            using var db   = ExampleContext.CreateContext(_output, false);
            var et   = db.Model.FindEntityType(typeof(ReadOnlyEntity));
            var name = "EX__ReadOnly";
            Assert.Equal(name, et.GetTableName());
        }
    }
}
