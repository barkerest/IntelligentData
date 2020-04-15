using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace IntelligentData.Internal
{
    internal class IncludeTemporaryListsOptionsExtension : IDbContextOptionsExtension
    {
        private class InfoClass : DbContextOptionsExtensionInfo
        {
            public InfoClass(IncludeTemporaryListsOptionsExtension extension)
                : base(extension)
            {
            }

            public override long GetServiceProviderHashCode() => 0x706d65545f636e49L;
            
            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            {
                
            }

            public override bool IsDatabaseProvider { get; } = false;

            public override string LogFragment { get; } = "INC-TEMP";
        }

        public IncludeTemporaryListsOptionsExtension()
        {
            Info = new InfoClass(this);
        }

        public void ApplyServices(IServiceCollection services)
        {
            
        }

        public void Validate(IDbContextOptions options)
        {
            
        }

        public DbContextOptionsExtensionInfo Info { get; }
    }
}
