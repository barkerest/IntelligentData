using System;
using System.Linq;
using IntelligentData.Extensions;
using IntelligentData.Internal;
using IntelligentData.Tests.Examples;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace IntelligentData.Tests
{
    public class TemporaryLists_Should 
    {
        private readonly ITestOutputHelper _output;

        public TemporaryLists_Should(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
        }
        
        [Fact]
        public void BeCreatedByDefault()
        {
            using var db = ExampleContext.CreateContext(withTempTables: true);
            Assert.True(db.HasTempList<int>());
            Assert.True(db.HasTempList<long>());
            Assert.True(db.HasTempList<string>());
            Assert.True(db.HasTempList<Guid>());
            var t = db.Model.FindEntityType(typeof(TempListEntry<int>));
            Assert.NotNull(t);
            _output.WriteLine(t.GetTableName());
            t = db.Model.FindEntityType(typeof(TempListEntry<long>));
            Assert.NotNull(t);
            _output.WriteLine(t.GetTableName());
            t = db.Model.FindEntityType(typeof(TempListEntry<string>));
            Assert.NotNull(t);
            _output.WriteLine(t.GetTableName());
            t = db.Model.FindEntityType(typeof(TempListEntry<Guid>));
            Assert.NotNull(t);
            _output.WriteLine(t.GetTableName());
        }

        [Fact]
        public void NotBeCreatedWhenSkipped()
        {
            using var db = ExampleContext.CreateContext(withTempTables: false);
            Assert.False(db.HasTempList<int>());
            Assert.False(db.HasTempList<long>());
            Assert.False(db.HasTempList<string>());
            Assert.False(db.HasTempList<Guid>());
            Assert.Null(db.Model.FindEntityType(typeof(TempListEntry<int>)));
            Assert.Null(db.Model.FindEntityType(typeof(TempListEntry<long>)));
            Assert.Null(db.Model.FindEntityType(typeof(TempListEntry<string>)));
            Assert.Null(db.Model.FindEntityType(typeof(TempListEntry<Guid>)));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void CreateTempLists(bool withTempTables)
        {
            var items = new int[] {1, 3, 5, 7, 9};
            var otherItems = new int[] {2, 4, 6, 8, 10};
            
            using var db = ExampleContext.CreateContext(withTempTables: withTempTables);
            Assert.Equal(withTempTables, db.HasTempList<int>());

            // have data in before our first test
            db.ToTemporaryList(42, otherItems);
            
            var list = db.ToTemporaryList(items);
            Assert.Equal(withTempTables, list is IQueryable<int>);

            Assert.Equal(items.Length, list.Count());
            Assert.True(items.SequenceEqual(list.OrderBy(x => x)));
            
            // call up the first temporary list.
            list = db.TemporaryList<int>(42);
            Assert.Equal(withTempTables, list is IQueryable<int>);

            Assert.Equal(otherItems.Length, list.Count());
            Assert.True(otherItems.SequenceEqual(list.OrderBy(x => x)));
            
            // check out a query using a temporary list.
            var query = db.TrackedEntities.Where(x => list.Contains(x.ID));
            _output.WriteLine(query.GetSqlString());
        }
    }
}
