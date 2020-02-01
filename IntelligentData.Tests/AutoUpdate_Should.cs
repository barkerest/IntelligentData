﻿using System;
using IntelligentData.Extensions;
using IntelligentData.Tests.Examples;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace IntelligentData.Tests
{
    public class AutoUpdate_Should
    {
        private ExampleContext    _db;
        private ITestOutputHelper _output;

        public AutoUpdate_Should(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _db     = ExampleContext.CreateContext(false);
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
        public void CreateWithAppropriateValue()
        {
            var item = new AutoDateExample() {SomeValue = 1234};
            var now  = DateTime.Now;
            
            Assert.Equal(default, item.UpdatedDate);
            Assert.Equal(default, item.UpdatedInstant);

            _db.Add(item);
            Assert.Equal(default, item.UpdatedDate);
            Assert.Equal(default, item.UpdatedInstant);

            Assert.Equal(1,_db.SaveChanges());
            Assert.NotEqual(default, item.UpdatedDate);
            Assert.NotEqual(default, item.UpdatedInstant);

            Assert.True(item.UpdatedInstant > now);
            Assert.Equal(now.Date, item.UpdatedDate);

            var instant = item.UpdatedInstant;

            item.SomeValue++;
            _db.Update(item);
            Assert.Equal(1, _db.SaveChanges());
            
            // make sure the updated instant does change.
            Assert.NotEqual(instant, item.UpdatedInstant);
            Assert.True(item.UpdatedInstant > instant);
        }

        
        
    }
}
