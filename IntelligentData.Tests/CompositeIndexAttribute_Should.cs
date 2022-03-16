using System;
using System.ComponentModel.DataAnnotations;
using System.Reflection;
using IntelligentData.Attributes;
using IntelligentData.Tests.Examples;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;

namespace IntelligentData.Tests
{
    [Collection("Database Instance")]

    public class CompositeIndexAttribute_Should : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly IServiceProvider  _sp;
        private readonly ExampleContext    _db;

        public CompositeIndexAttribute_Should(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _sp     = ExampleContext.CreateServiceProvider(outputHelper: output);
            _db     = _sp.GetRequiredService<ExampleContext>();
        }

        [Fact]
        public void PreventDuplicatesWhenUnique()
        {
            var a = 1234;
            var b = 5678;
            var item = new UniqueEntity() {Name = "FirstItem", ValueA = a, ValueB = b};

            _db.UniqueEntities.Add(item);
            Assert.Equal(1, _db.SaveChanges());

            item = new UniqueEntity() {Name = "SecondItem", ValueA = a, ValueB = b};

            var context = new ValidationContext(item, _sp, null);

            var attrib = item.GetType().GetCustomAttribute<CompositeIndexAttribute>()
                         ?? throw new InvalidOperationException("Missing attribute.");
            
            Assert.True(attrib.Unique);
            
            var result = attrib.GetValidationResult(item, context);

            Assert.NotNull(result);
            Assert.NotEqual(ValidationResult.Success, result);
            Assert.Matches(@"already\sbeen\sused", result.ErrorMessage);
            Assert.Contains("ValueA", result.MemberNames);
            Assert.Contains("ValueB", result.MemberNames);
            
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
