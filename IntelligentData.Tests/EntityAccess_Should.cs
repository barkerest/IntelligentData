using System;
using System.Linq;
using IntelligentData.Enums;
using IntelligentData.Tests.Examples;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace IntelligentData.Tests
{
    public class EntityAccess_Should
    {
        private ExampleContext    _db;
        private ITestOutputHelper _output;

        public EntityAccess_Should(ITestOutputHelper output)
        {
            _db     = ExampleContext.CreateContext(true);
            _output = output ?? throw new ArgumentNullException(nameof(output));
        }

        [Fact]
        public void AllowInsertInSeedDataAction()
        {
            _db.SeedData(
                () =>
                {
                    Assert.Equal(AccessLevel.Insert, _db.AccessForEntity(new ReadOnlyEntity()));
                    Assert.Equal(AccessLevel.Insert, _db.AccessForEntity(new ReadInsertEntity()));
                    Assert.Equal(AccessLevel.Insert, _db.AccessForEntity(new ReadUpdateEntity()));
                    Assert.Equal(AccessLevel.Insert, _db.AccessForEntity(new ReadDeleteEntity()));
                    Assert.Equal(AccessLevel.Insert, _db.AccessForEntity(new ReadInsertUpdateEntity()));
                    Assert.Equal(AccessLevel.Insert, _db.AccessForEntity(new ReadInsertDeleteEntity()));
                    Assert.Equal(AccessLevel.Insert, _db.AccessForEntity(new ReadUpdateDeleteEntity()));
                    Assert.Equal(AccessLevel.Insert, _db.AccessForEntity(new ReadInsertUpdateDeleteEntity()));
                    Assert.Equal(AccessLevel.Insert, _db.AccessForEntity(new DynamicAccessEntity()));
                }
            );
        }

        [Theory]
        [InlineData(AccessLevel.ReadOnly, typeof(ReadOnlyEntity))]
        [InlineData(AccessLevel.Insert, typeof(ReadInsertEntity))]
        [InlineData(AccessLevel.Update, typeof(ReadUpdateEntity))]
        [InlineData(AccessLevel.Delete, typeof(ReadDeleteEntity))]
        [InlineData(AccessLevel.Insert | AccessLevel.Update, typeof(ReadInsertUpdateEntity))]
        [InlineData(AccessLevel.Insert | AccessLevel.Delete, typeof(ReadInsertDeleteEntity))]
        [InlineData(AccessLevel.Update | AccessLevel.Delete, typeof(ReadUpdateDeleteEntity))]
        [InlineData(AccessLevel.FullAccess, typeof(ReadInsertUpdateDeleteEntity))]
        public void BeAppropriateForEntity(AccessLevel level, Type entityType)
        {
            var entity = Activator.CreateInstance(entityType);
            Assert.Equal(level, _db.AccessForEntity(entity));
        }

        [Fact]
        public void BeDynamicForDynamicAccessEntities()
        {
            foreach (var entity in _db.DynamicAccessEntities)
            {
                var expected = (AccessLevel) (entity.ID % 8); // 0 - 7
                _output.WriteLine($"Entity ID={entity.ID}, ExpectedAccess=({expected})");
                Assert.Equal(expected, entity.EntityAccessLevel);
                Assert.Equal(expected, _db.AccessForEntity(entity));
            }
        }

        [Theory]
        [InlineData(false, typeof(ReadOnlyEntity))]
        [InlineData(true, typeof(ReadInsertEntity))]
        [InlineData(false, typeof(ReadUpdateEntity))]
        [InlineData(false, typeof(ReadDeleteEntity))]
        [InlineData(true, typeof(ReadInsertUpdateEntity))]
        [InlineData(true, typeof(ReadInsertDeleteEntity))]
        [InlineData(false, typeof(ReadUpdateDeleteEntity))]
        [InlineData(true, typeof(ReadInsertUpdateDeleteEntity))]
        public void AllowInsertOnlyWhenPermitted(bool permitted, Type entityType)
        {
            var setType = typeof(DbSet<>).MakeGenericType(entityType);
            var set = (IQueryable<IExampleEntity>) _db.GetType()
                                                      .GetProperties()
                                                      .FirstOrDefault(x => x.PropertyType == setType)
                                                      ?.GetValue(_db)
                      ?? throw new InvalidOperationException("Could not locate DbSet.");

            if (set.Any(x => x.Name == ExampleContext.NewName)) throw new InvalidOperationException("The test value already exists!");

            _output.WriteLine($"Adding entity of type {entityType}...");
            var entity = (IExampleEntity) Activator.CreateInstance(entityType);
            entity.Name = ExampleContext.NewName;
            _db.Add(entity);
            Assert.Equal(
                EntityState.Added, _db.Entry(entity)
                                      .State
            );

            var changes = _db.SaveChanges();

            if (permitted)
            {
                _output.WriteLine("Checking that new record was added.");
                Assert.Equal(1, changes);
                Assert.True(set.Any(x => x.Name == ExampleContext.NewName));
            }
            else
            {
                _output.WriteLine("Checking that new record was blocked.");
                Assert.Equal(0, changes);
                Assert.False(set.Any(x => x.Name == ExampleContext.NewName));
            }
        }

        [Theory]
        [InlineData(false, typeof(ReadOnlyEntity))]
        [InlineData(false, typeof(ReadInsertEntity))]
        [InlineData(true, typeof(ReadUpdateEntity))]
        [InlineData(false, typeof(ReadDeleteEntity))]
        [InlineData(true, typeof(ReadInsertUpdateEntity))]
        [InlineData(false, typeof(ReadInsertDeleteEntity))]
        [InlineData(true, typeof(ReadUpdateDeleteEntity))]
        [InlineData(true, typeof(ReadInsertUpdateDeleteEntity))]
        public void AllowUpdateOnlyWhenPermitted(bool permitted, Type entityType)
        {
            var setType = typeof(DbSet<>).MakeGenericType(entityType);
            var set = (IQueryable<IExampleEntity>) _db.GetType()
                                                      .GetProperties()
                                                      .FirstOrDefault(x => x.PropertyType == setType)
                                                      ?.GetValue(_db)
                      ?? throw new InvalidOperationException("Could not locate DbSet.");

            if (set.Any(x => x.Name == ExampleContext.NewName)) throw new InvalidOperationException("The test value already exists!");

            var entity       = set.First();
            var originalName = entity.Name;
            entity.Name = ExampleContext.NewName;
            _output.WriteLine($"Changing entity name from '{originalName}' to '{entity.Name}'...");
            _db.Update(entity);
            Assert.Equal(
                EntityState.Modified, _db.Entry(entity)
                                         .State
            );

            var changes = _db.SaveChanges();

            if (permitted)
            {
                _output.WriteLine("Checking that record was modified.");
                Assert.Equal(1, changes);
                Assert.True(set.Any(x => x.Name == ExampleContext.NewName));
                Assert.False(set.Any(x => x.Name == originalName));
            }
            else
            {
                _output.WriteLine("Checking that change was blocked.");
                Assert.Equal(0, changes);
                Assert.False(set.Any(x => x.Name == ExampleContext.NewName));
                Assert.True(set.Any(x => x.Name == originalName));
            }
        }

        [Theory]
        [InlineData(false, typeof(ReadOnlyEntity))]
        [InlineData(false, typeof(ReadInsertEntity))]
        [InlineData(false, typeof(ReadUpdateEntity))]
        [InlineData(true, typeof(ReadDeleteEntity))]
        [InlineData(false, typeof(ReadInsertUpdateEntity))]
        [InlineData(true, typeof(ReadInsertDeleteEntity))]
        [InlineData(true, typeof(ReadUpdateDeleteEntity))]
        [InlineData(true, typeof(ReadInsertUpdateDeleteEntity))]
        public void AllowDeleteOnlyWhenPermitted(bool permitted, Type entityType)
        {
            var setType = typeof(DbSet<>).MakeGenericType(entityType);
            var set = (IQueryable<IExampleEntity>) _db.GetType()
                                                      .GetProperties()
                                                      .FirstOrDefault(x => x.PropertyType == setType)
                                                      ?.GetValue(_db)
                      ?? throw new InvalidOperationException("Could not locate DbSet.");

            var entity = set.First();
            _output.WriteLine($"Removing entity named '{entity.Name}'...");
            _db.Remove(entity);
            Assert.Equal(
                EntityState.Deleted, _db.Entry(entity)
                                        .State
            );

            var changes = _db.SaveChanges();

            if (permitted)
            {
                _output.WriteLine("Checking that record was deleted.");
                Assert.Equal(1, changes);
                Assert.False(set.Any(x => x.ID == entity.ID));
            }
            else
            {
                _output.WriteLine("Checking that delete was blocked.");
                Assert.Equal(0, changes);
                Assert.True(set.Any(x => x.ID == entity.ID));
            }
        }
    }
}
