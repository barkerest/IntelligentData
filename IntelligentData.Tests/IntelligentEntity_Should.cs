using System;
using System.Linq;
using IntelligentData.Enums;
using IntelligentData.Tests.Examples;
using Xunit;
using Xunit.Abstractions;

namespace IntelligentData.Tests
{
    public class IntelligentEntity_Should
    {
        private ExampleContext    _db;
        private ITestOutputHelper _output;

        public IntelligentEntity_Should(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _db     = ExampleContext.CreateContext(false);
        }

        [Fact]
        public void SaveToDatabase()
        {
            // simple test that the intelligent methods work.
            var item = new SmartEntity(_db)
            {
                Name = "Bob"
            };

            Assert.Empty(_db.SmartEntities);

            Assert.Equal(UpdateResult.Success, item.SaveToDatabase());

            Assert.Single(_db.SmartEntities);
        }

        [Fact]
        public void UpdateInDatabase()
        {
            var item = new SmartEntity(_db)
            {
                Name = "Bob"
            };

            _db.Add(item);
            Assert.Equal(1, _db.SaveChanges());

            item.Name = "Harry";
            Assert.Empty(_db.SmartEntities.Where(x => x.Name == "Harry"));
            Assert.Equal(UpdateResult.Success, item.SaveToDatabase());
            Assert.Single(_db.SmartEntities.Where(x => x.Name == "Harry"));
        }

        [Fact]
        public void DeleteFromDatabase()
        {
            var item = new SmartEntity(_db)
            {
                Name = "Bob"
            };

            _db.Add(item);
            Assert.Equal(1, _db.SaveChanges());

            Assert.Single(_db.SmartEntities);
            Assert.Equal(UpdateResult.Success, item.DeleteFromDatabase());
            Assert.Empty(_db.SmartEntities);
        }

        [Fact]
        public void CanCheckExistence()
        {
            var item = new SmartEntity(_db)
            {
                Name = "Bob"
            };

            Assert.False(item.ExistsInDatabase());

            _db.Add(item);
            Assert.Equal(1, _db.SaveChanges());

            Assert.True(item.ExistsInDatabase());

            _db.Remove(item);
            Assert.Equal(1, _db.SaveChanges());

            Assert.False(item.ExistsInDatabase());
        }

        [Theory]
        [InlineData(true, "Bob", "")]
        [InlineData(false, null, "is required")]
        [InlineData(false, "", "is required")]
        [InlineData(false, "    ", "is required")]
        [InlineData(true, "100 characters......................................................................................", "")]
        [InlineData(false, "101 characters.......................................................................................", "maximum length")]
        [InlineData(false, "George", "cannot be george")]
        public void CanValidateSelf(bool ok, string name, string error)
        {
            var item = new SmartEntity(_db)
            {
                Name = name
            };

            _output.WriteLine($"Name is {(name ?? "").Length} chars.");
            
            var valid = item.IsValidForDatabase(out var errors);
            Assert.Equal(name, item.Name);
            Assert.Equal(ok, valid);

            if (!ok)
            {
                Assert.Contains(
                    errors,
                    x => x.MemberNames.Contains("Name") && x.ErrorMessage.Contains(error, StringComparison.OrdinalIgnoreCase)
                );
            }
        }

        [Fact]
        public void FailsForValidationError()
        {
            var item = new SmartEntity(_db)
            {
                Name = "George"
            };
            
            Assert.Equal(UpdateResult.FailedValidation, item.SaveToDatabase());

            item.Name = "Bob";
            Assert.Equal(UpdateResult.Success, item.SaveToDatabase());
        }

        [Fact]
        public void FailsForInsertDisallowed()
        {
            var item = new SmartEntity(_db)
            {
                Name = "Bob"
            };
            
            item.SetAccessLevel(AccessLevel.ReadOnly);
            
            Assert.Equal(UpdateResult.FailedInsertDisallowed, item.SaveToDatabase());
            
            item.SetAccessLevel(AccessLevel.FullAccess);
            Assert.Equal(UpdateResult.Success, item.SaveToDatabase());
        }

        [Fact]
        public void FailsForUpdateDisallowed()
        {
            var item = new SmartEntity(_db)
            {
                Name = "Bob"
            };

            item.SetAccessLevel(AccessLevel.Insert);
            
            _db.Add(item);
            Assert.Equal(1, _db.SaveChanges());

            item.Name = "Roy";
            Assert.Equal(UpdateResult.FailedUpdateDisallowed, item.SaveToDatabase());
            
            item.SetAccessLevel(AccessLevel.FullAccess);
            Assert.Equal(UpdateResult.Success, item.SaveToDatabase());
        }

        [Fact]
        public void FailsForDeleteDisallowed()
        {
            var item = new SmartEntity(_db)
            {
                Name = "Bob"
            };

            item.SetAccessLevel(AccessLevel.Insert);
            
            _db.Add(item);
            Assert.Equal(1, _db.SaveChanges());

            Assert.Equal(UpdateResult.FailedDeleteDisallowed, item.DeleteFromDatabase());
            
            item.SetAccessLevel(AccessLevel.FullAccess);
            Assert.Equal(UpdateResult.Success, item.DeleteFromDatabase());
        }

        [Fact]
        public void FailsForConcurrentUpdate()
        {
            var item = new SmartEntity(_db)
            {
                Name = "Bob"
            };

            _db.Add(item);
            Assert.Equal(1, _db.SaveChanges());

            var v = item.RowVersion;

            item.Name = "Roy";
            _db.Update(item);
            Assert.Equal(1, _db.SaveChanges());

            item.RowVersion = v;
            item.Name = "Mark";
            
            Assert.Equal(UpdateResult.FailedUpdatedByOther, item.SaveToDatabase());
        }

        [Fact]
        public void FailsForConcurrentDelete()
        {
            var item = new SmartEntity(_db)
            {
                Name = "Bob"
            };

            _db.Add(item);
            Assert.Equal(1, _db.SaveChanges());

            _db.Remove(item);
            Assert.Equal(1, _db.SaveChanges());

            item.Name = "Roy";
            Assert.Equal(UpdateResult.FailedDeletedByOther, item.SaveToDatabase());
        }
        
    }
}
