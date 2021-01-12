using System;
using IntelligentData.Enums;
using IntelligentData.Tests.Examples;
using Xunit;
using Xunit.Abstractions;

namespace IntelligentData.Tests
{
    public class DefaultAccess_Should : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly ExampleContext    _db;

        public DefaultAccess_Should(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _db     = ExampleContext.CreateContext();
        }

        [Theory]
        [InlineData(AccessLevel.ReadOnly)]
        [InlineData(AccessLevel.FullAccess)]
        [InlineData(AccessLevel.Insert | AccessLevel.Update)]
        [InlineData(AccessLevel.Insert | AccessLevel.Delete)]
        public void FollowContextDefault(AccessLevel level)
        {
            _db.SetDefaultAccessLevel(level);
            
            Assert.Equal(level, _db.DefaultAccessLevel);
            
            Assert.Equal(level, _db.AccessForEntity(new DefaultAccessEntity()));
        }

        [Theory]
        [InlineData(AccessLevel.ReadOnly)]
        [InlineData(AccessLevel.FullAccess)]
        [InlineData(AccessLevel.Insert | AccessLevel.Update)]
        [InlineData(AccessLevel.Insert | AccessLevel.Delete)]
        public void NotInterfereWithExplicitAccess(AccessLevel level)
        {
            _db.SetDefaultAccessLevel(level);
            
            Assert.Equal(level, _db.DefaultAccessLevel);

            _output.WriteLine($"Default is {level}.");
            
            foreach (var (expected,type) in EntityAccess_Should.ExpectedEntityAccessLevels)
            {
                var entity = Activator.CreateInstance(type);
                var actual = _db.AccessForEntity(entity);
                _output.WriteLine($"Type {type}; expected: {expected}; actual: {actual}");
                Assert.Equal(expected, actual);
            }
        }

        public void Dispose()
        {
            _db?.Dispose();
        }
    }
}
