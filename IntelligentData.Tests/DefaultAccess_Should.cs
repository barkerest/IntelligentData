using System;
using IntelligentData.Enums;
using IntelligentData.Tests.Examples;
using Xunit;
using Xunit.Abstractions;

namespace IntelligentData.Tests
{
    public class DefaultAccess_Should
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

    }
}
