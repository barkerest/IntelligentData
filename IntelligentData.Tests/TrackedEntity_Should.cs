using System;
using IntelligentData.Extensions;
using IntelligentData.Tests.Examples;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace IntelligentData.Tests
{
    public class TrackedEntity_Should
    {
        private readonly ITestOutputHelper _output;
        private readonly ExampleContext _db;
        private readonly ExampleUserInformationProvider _user;

        public TrackedEntity_Should(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _user = new ExampleUserInformationProvider();
            _db = ExampleContext.CreateContext(currentUserProvider: _user);
        }

        [Fact]
        public void ConfigureTrackedFieldsProperly()
        {
            var et = _db.Model.FindEntityType(typeof(TrackedEntity));
            
            var prop = et.FindProperty(nameof(TrackedEntity.CreatedAt));
            Assert.True(prop.HasRuntimeDefault());
            
            prop = et.FindProperty(nameof(TrackedEntity.CreatedBy));
            Assert.True(prop.HasRuntimeDefault());

            prop = et.FindProperty(nameof(TrackedEntity.CreatedByID));
            Assert.True(prop.HasRuntimeDefault());

            prop = et.FindProperty(nameof(TrackedEntity.LastModifiedAt));
            Assert.True(prop.HasAutoUpdate());

            prop = et.FindProperty(nameof(TrackedEntity.LastModifiedBy));
            Assert.True(prop.HasAutoUpdate());

            prop = et.FindProperty(nameof(TrackedEntity.LastModifiedByID));
            Assert.True(prop.HasAutoUpdate());
        }
        
        
    }
}
