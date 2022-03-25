using System;
using System.Linq;
using IntelligentData.Enums;
using IntelligentData.Tests.Examples;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace IntelligentData.Tests
{
    [Collection("Database Instance")]

    public class EntityUpdateCommands_Should : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly ILogger _logger;
        private readonly ExampleContext    _db;

        public EntityUpdateCommands_Should(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _logger = new TestOutputLogger(output);
            _db     = ExampleContext.CreateContext(output);
            _db.SetDefaultAccessLevel(AccessLevel.FullAccess);
        }

        [Fact]
        public void AllowInsertingUpdatingAndDeleting()
        {
            _output.WriteLine("Creating command set...");
            var commands = new EntityUpdateCommands<DefaultAccessEntity>(_db, _logger);
            
            Assert.Equal(0, _db.DefaultAccessEntities.Count(x => x.Name == "John Doe"));
            Assert.Equal(0, _db.DefaultAccessEntities.Count(x => x.Name == "Jane Smith"));

            var entity = new DefaultAccessEntity(){ Name = "John Doe"};
            commands.Insert(entity, null);
            Assert.Equal(1, _db.DefaultAccessEntities.Count(x => x.Name == "John Doe"));
            Assert.Equal(0, _db.DefaultAccessEntities.Count(x => x.Name == "Jane Smith"));

            entity.Name = "Jane Smith";
            commands.Update(entity, null);
            Assert.Equal(0, _db.DefaultAccessEntities.Count(x => x.Name == "John Doe"));
            Assert.Equal(1, _db.DefaultAccessEntities.Count(x => x.Name == "Jane Smith"));
            
            commands.Remove(entity, null);
            Assert.Equal(0, _db.DefaultAccessEntities.Count(x => x.Name == "John Doe"));
            Assert.Equal(0, _db.DefaultAccessEntities.Count(x => x.Name == "Jane Smith"));
        }
        
        [Fact]
        public void AllowInsertingUpdatingAndDeletingWithinTransaction()
        {
            _output.WriteLine("Creating command set...");
            var commands = new EntityUpdateCommands<DefaultAccessEntity>(_db, _logger);
            using (var transaction = _db.Database.BeginTransaction())
            {
                var dbTran = transaction.GetDbTransaction();
                
                Assert.Equal(0, _db.DefaultAccessEntities.Count(x => x.Name == "John Doe"));
                Assert.Equal(0, _db.DefaultAccessEntities.Count(x => x.Name == "Jane Smith"));

                var entity = new DefaultAccessEntity() { Name = "John Doe" };
                commands.Insert(entity, dbTran);
                Assert.Equal(1, _db.DefaultAccessEntities.Count(x => x.Name == "John Doe"));
                Assert.Equal(0, _db.DefaultAccessEntities.Count(x => x.Name == "Jane Smith"));

                entity.Name = "Jane Smith";
                commands.Update(entity, dbTran);
                Assert.Equal(0, _db.DefaultAccessEntities.Count(x => x.Name == "John Doe"));
                Assert.Equal(1, _db.DefaultAccessEntities.Count(x => x.Name == "Jane Smith"));

                commands.Remove(entity, dbTran);
                Assert.Equal(0, _db.DefaultAccessEntities.Count(x => x.Name == "John Doe"));
                Assert.Equal(0, _db.DefaultAccessEntities.Count(x => x.Name == "Jane Smith"));
            }
        }

        public void Dispose()
        {
            _db?.Dispose();
        }
    }
}
