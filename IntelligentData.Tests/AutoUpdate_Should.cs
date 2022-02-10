using System;
using IntelligentData.Extensions;
using IntelligentData.Tests.Examples;
using Xunit;
using Xunit.Abstractions;

namespace IntelligentData.Tests
{
    public class AutoUpdate_Should : IDisposable
    {
        private ExampleContext    _db;
        private ITestOutputHelper _output;

        public AutoUpdate_Should(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _db     = ExampleContext.CreateContext(output,false);
        }

        [Fact]
        public void AutomaticallyRegisterFromAttributes()
        {
            var entityType = _db.Model.FindEntityType(typeof(AutoDateExample));
            var property   = entityType.FindProperty(nameof(AutoDateExample.UpdatedDate));
            Assert.True(property.HasAutoUpdate());
            property = entityType.FindProperty(nameof(AutoDateExample.UpdatedInstant));
            Assert.True(property.HasAutoUpdate());
        }

        [Fact]
        public void RegisterFromOnModelCreating()
        {
            var entityType = _db.Model.FindEntityType(typeof(AutoDateExample));
            var property   = entityType.FindProperty(nameof(AutoDateExample.SaveCount));
            Assert.True(property.HasAutoUpdate());
        }

        [Fact]
        public void UpdateWithAppropriateValue()
        {
            var item = new AutoDateExample() {SomeValue = 1234};
            var now  = DateTime.Now;

            Assert.Equal(default, item.UpdatedDate);
            Assert.Equal(default, item.UpdatedInstant);
            Assert.Equal(0, item.SaveCount);

            _db.Add(item);
            Assert.Equal(default, item.UpdatedDate);
            Assert.Equal(default, item.UpdatedInstant);
            Assert.Equal(0, item.SaveCount);

            Assert.Equal(1, _db.SaveChanges());
            Assert.NotEqual(default, item.UpdatedDate);
            Assert.NotEqual(default, item.UpdatedInstant);
            Assert.Equal(1, item.SaveCount); // save count incremented as expected?

            Assert.True(item.UpdatedInstant > now);
            Assert.Equal(now.Date, item.UpdatedDate);

            var instant = item.UpdatedInstant;

            item.SomeValue += 1234;
            _db.Update(item);
            Assert.Equal(1, item.SaveCount);
            Assert.Equal(1, _db.SaveChanges());
            Assert.Equal(2, item.SaveCount); // save count incremented as expected?

            // make sure the updated instant does change.
            Assert.NotEqual(instant, item.UpdatedInstant);
            Assert.True(item.UpdatedInstant > instant);
        }

        public void Dispose()
        {
            _db?.Dispose();
        }
    }
}
