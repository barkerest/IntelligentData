using System;
using IntelligentData.Extensions;
using IntelligentData.Tests.Examples;
using Xunit;
using Xunit.Abstractions;

namespace IntelligentData.Tests
{
    [Collection("Database Instance")]

    public class StringFormat_Should : IDisposable
    {
        private ExampleContext _db;
        private ITestOutputHelper _output;

        public StringFormat_Should(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _db = ExampleContext.CreateContext(output, false);
        }

        [Fact]
        public void AutomaticallyRegisterFromAttributes()
        {
            var entityType = _db.Model.FindEntityType(typeof(StringFormatExample));
            var property = entityType.FindProperty(nameof(StringFormatExample.LowerCaseString));
            Assert.True(property.HasStringFormat());
            property = entityType.FindProperty(nameof(StringFormatExample.UpperCaseString));
            Assert.True(property.HasStringFormat());
        }

        [Theory]
        [InlineData("HelloWorld")]
        [InlineData("HELLO WORLD")]
        public void StoreLowerCasedAsAppropriate(string testValue)
        {
            var expected = testValue.ToLower();
            var item = new StringFormatExample()
            {
                LowerCaseString = testValue
            };
            
            Assert.Equal(testValue, item.LowerCaseString);
            _db.Add(item);
            Assert.Equal(testValue, item.LowerCaseString);
            _db.SaveChanges();
            Assert.NotEqual(testValue, item.LowerCaseString);
            Assert.Equal(expected, item.LowerCaseString);
            item.LowerCaseString = testValue;
            Assert.Equal(testValue, item.LowerCaseString);
            _db.Update(item);
            _db.SaveChanges();
            Assert.NotEqual(testValue, item.LowerCaseString);
            Assert.Equal(expected, item.LowerCaseString);
        }
        
        [Theory]
        [InlineData("HelloWorld")]
        [InlineData("hello world")]
        public void StoreUpperCasedAsAppropriate(string testValue)
        {
            var expected = testValue.ToUpper();
            var item = new StringFormatExample()
            {
                UpperCaseString = testValue
            };
            
            Assert.Equal(testValue, item.UpperCaseString);
            _db.Add(item);
            Assert.Equal(testValue, item.UpperCaseString);
            _db.SaveChanges();
            Assert.NotEqual(testValue, item.UpperCaseString);
            Assert.Equal(expected, item.UpperCaseString);
            item.UpperCaseString = testValue;
            Assert.Equal(testValue, item.UpperCaseString);
            _db.Update(item);
            _db.SaveChanges();
            Assert.NotEqual(testValue, item.UpperCaseString);
            Assert.Equal(expected, item.UpperCaseString);
        }

        public void Dispose()
        {
            _db?.Dispose();
        }
    }
}
