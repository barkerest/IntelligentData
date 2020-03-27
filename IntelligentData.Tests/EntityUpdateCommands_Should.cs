using System;
using System.Linq;
using IntelligentData.Enums;
using IntelligentData.Tests.Examples;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace IntelligentData.Tests
{
    public class EntityUpdateCommands_Should
    {
        private readonly ITestOutputHelper _output;
        private readonly ILogger _logger;
        private readonly ExampleContext    _db;

        public EntityUpdateCommands_Should(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _logger = new TestOutputLogger(output);
            _db     = ExampleContext.CreateContext();
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
    }
}
