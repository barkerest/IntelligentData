using System;
using System.Data;
using IntelligentData.Interfaces;
using IntelligentData.Tests.Examples;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace IntelligentData.Tests
{
    public class VersionedEntity_Should : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly ExampleContext _db;

        public VersionedEntity_Should(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _db = ExampleContext.CreateContext();
        }

        [Fact]
        public void ConfigureVersionedFieldsProperly()
        {
            var et = _db.Model.FindEntityType(typeof(VersionedEntity));

            var prop = et.FindProperty(nameof(IVersionedEntity.RowVersion));
            Assert.True(prop.IsConcurrencyToken);

            et = _db.Model.FindEntityType(typeof(TimestampedEntity));
            prop = et.FindProperty(nameof(ITimestampedEntity.RowVersion));
            Assert.True(prop.IsConcurrencyToken);
        }

        [Fact]
        public void VersionedModelShouldIncrement()
        {
            var item = new VersionedEntity() { Name = "Hello World" };
            Assert.Null(item.RowVersion);
            _db.Add(item);
            Assert.Equal(1, _db.SaveChanges());
            Assert.Equal(1, item.RowVersion);
            
            // change, save, increment
            item.Name = "Goodbye";
            _db.Update(item);
            Assert.Equal(1, _db.SaveChanges());
            Assert.Equal(2, item.RowVersion);
            
            // fake concurrency conflict.
            item.RowVersion = 1;
            item.Name = "Hello Again";
            _db.Update(item);
            Assert.Throws<DbUpdateConcurrencyException>(() => _db.SaveChanges());
        }

        [Fact]
        public void TimestampedModelShouldUpdate()
        {
            var item = new TimestampedEntity() {Name = "Hello World"};
            Assert.Null(item.RowVersion);
            _db.Add(item);
            var ts = DateTime.Now.Ticks;
            Assert.Equal(1, _db.SaveChanges());
            Assert.NotNull(item.RowVersion);
            Assert.True(item.RowVersion > ts);

            ts = item.RowVersion.Value;
            
            // change, save, test
            item.Name = "Goodbye";
            _db.Update(item);
            Assert.Equal(1, _db.SaveChanges());
            Assert.NotNull(item.RowVersion);
            Assert.True(item.RowVersion > ts);
            
            // fake concurrency conflict.
            item.RowVersion = ts;
            item.Name = "Hello Again";
            _db.Update(item);
            Assert.Throws<DbUpdateConcurrencyException>(() => _db.SaveChanges());
        }

        public void Dispose()
        {
            _db?.Dispose();
        }
    }
}
