using System;
using IntelligentData.Tests.Examples;
using Xunit;
using Xunit.Abstractions;

namespace IntelligentData.Tests
{
    public class IndexAttribute_Should
    {
        private readonly ITestOutputHelper _output;
        private readonly ExampleContext    _db;

        public IndexAttribute_Should(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _db     = ExampleContext.CreateContext();
        }
        
        [Fact]
        public void PreventDuplicatesWhenUnique()
        {
            var name = "John Doe";
            var item = new UniqueEntity(){Name = name};

            _db.UniqueEntities.Add(item);
            Assert.Equal(1, _db.SaveChanges());

            item = new UniqueEntity(){Name = name};
            
            _db.UniqueEntities.Add(item);
            Assert.Equal(0, _db.SaveChanges());


        }
    }
}
