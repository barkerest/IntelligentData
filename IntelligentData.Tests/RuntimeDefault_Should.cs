using System;
using IntelligentData.Extensions;
using IntelligentData.Tests.Examples;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace IntelligentData.Tests
{
    public class RuntimeDefault_Should
    {
        private ExampleContext    _db;
        private ITestOutputHelper _output;

        public RuntimeDefault_Should(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _db     = ExampleContext.CreateContext(false);
        }
        
        [Fact]
        public void AutomaticallyRegisterFromAttributes()
        {
            var entityType = _db.Model.FindEntityType(typeof(AutoDateExample));
            var property   = entityType.FindProperty(nameof(AutoDateExample.CreatedDate));
            Assert.True(property.HasRuntimeDefault());
            property = entityType.FindProperty(nameof(AutoDateExample.CreatedInstant));
            Assert.True(property.HasRuntimeDefault());
        }

        [Fact]
        public void CreateWithAppropriateValue()
        {
            var item = new AutoDateExample() {SomeValue = 1234};
            var now = DateTime.Now;
            
            Assert.Equal(default, item.CreatedDate);
            Assert.Equal(default, item.CreatedInstant);

            _db.Add(item);
            Assert.Equal(default, item.CreatedDate);
            Assert.Equal(default, item.CreatedInstant);

            Assert.Equal(1,_db.SaveChanges());
            Assert.NotEqual(default, item.CreatedDate);
            Assert.NotEqual(default, item.CreatedInstant);

            Assert.True(item.CreatedInstant >= now);
            Assert.Equal(now.Date, item.CreatedDate);

            var instant = item.CreatedInstant;

            item.SomeValue++;
            _db.Update(item);
            Assert.Equal(1, _db.SaveChanges());
            
            // make sure the created instant does not change.
            Assert.Equal(instant, item.CreatedInstant);
        }

        
        
    }
}
