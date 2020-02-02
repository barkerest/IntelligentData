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

        [Fact]
        public void SetPropertiesCorrectly()
        {
            var item = new TrackedEntity() {Name = "Hello World"};
            _user.CurrentUser = ExampleUserInformationProvider.Users.JohnSmith;

            var cid = (int) _user.CurrentUser;
            var cnm = _user.CurrentUser.ToString();
            
            Assert.Null(item.CreatedBy);
            Assert.Equal(default, item.CreatedAt);
            Assert.Equal(default, item.CreatedByID);
            Assert.Null(item.LastModifiedBy);
            Assert.Equal(default, item.LastModifiedAt);
            Assert.Equal(default, item.LastModifiedByID);

            _db.Add(item);
            Assert.Equal(1, _db.SaveChanges());
            Assert.NotNull(item.CreatedBy);
            Assert.NotEqual(default, item.CreatedAt);
            Assert.NotEqual(default, item.CreatedByID);
            Assert.NotNull(item.LastModifiedBy);
            Assert.NotEqual(default, item.LastModifiedAt);
            Assert.NotEqual(default, item.LastModifiedByID);

            Assert.Equal(cnm, item.CreatedBy);
            Assert.Equal(cnm, item.LastModifiedBy);
            Assert.Equal(cid, item.CreatedByID);
            Assert.Equal(cid, item.LastModifiedByID);
            var ctm = item.CreatedAt;

            item.Name = "Goodbye";
            _user.CurrentUser = ExampleUserInformationProvider.Users.JaneDoe;
            _db.Update(item);
            Assert.Equal(1, _db.SaveChanges());
            
            Assert.Equal(cnm, item.CreatedBy);
            Assert.Equal(cid, item.CreatedByID);
            Assert.Equal(ctm, item.CreatedAt);
            
            Assert.NotEqual(cnm, item.LastModifiedBy);
            Assert.NotEqual(cid, item.LastModifiedByID);
            Assert.NotEqual(ctm, item.LastModifiedAt);

            var mid = (int) _user.CurrentUser;
            var mnm = _user.CurrentUser.ToString();
            Assert.Equal(mnm, item.LastModifiedBy);
            Assert.Equal(mid, item.LastModifiedByID);
            Assert.True(item.LastModifiedAt > ctm);
        }
        
    }
}
