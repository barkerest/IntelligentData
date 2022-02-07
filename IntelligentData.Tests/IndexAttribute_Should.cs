using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using IntelligentData.Tests.Examples;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace IntelligentData.Tests
{
    public class IndexAttribute_Should : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly IServiceProvider  _sp;
        private readonly ExampleContext    _db;

        public IndexAttribute_Should(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _sp     = ExampleContext.CreateServiceProvider(outputHelper: output);
            _db     = _sp.GetRequiredService<ExampleContext>();
        }

        [Fact]
        public void PreventDuplicatesWhenUnique()
        {
            var name = "John Doe";
            var item = new UniqueEntity() {Name = name};

            _db.UniqueEntities.Add(item);
            Assert.Equal(1, _db.SaveChanges());

            item = new UniqueEntity() {Name = name};

            var context = new ValidationContext(item, _sp, null) { MemberName = "Name" };

            var attrib = item.GetType().GetProperty("Name")?.GetCustomAttribute<IntelligentData.Attributes.IndexAttribute>()
                         ?? throw new InvalidOperationException("Missing attribute.");
            
            Assert.True(attrib.Unique);
            
            var result = attrib.GetValidationResult(item.Name, context);

            Assert.NotNull(result);
            Assert.NotEqual(ValidationResult.Success, result);
            Assert.Matches(@"already\sbeen\sused", result.ErrorMessage);
            Assert.Contains("Name", result.MemberNames);
            
            _db.UniqueEntities.Add(item);

            var x = Assert.Throws<DbUpdateException>(
                () => { _db.SaveChanges(); }
            );

            Assert.NotNull(x.InnerException);
            Assert.Matches("unique", x.InnerException.Message.ToLower());
        }

        public void Dispose()
        {
            _db?.Dispose();
        }
    }
}
