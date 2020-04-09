using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace IntelligentData.Internal
{
    internal class SkipTemporaryListsOptionsExtension : IDbContextOptionsExtension
    {
        private class InfoClass : DbContextOptionsExtensionInfo
        {
            public InfoClass(SkipTemporaryListsOptionsExtension extension)
                : base(extension)
            {
            }

            public override long GetServiceProviderHashCode() => 0x706d655470696b53L;
            
            public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
            {
                
            }

            public override bool IsDatabaseProvider { get; } = false;

            public override string LogFragment { get; } = "SKIP-TEMP";
        }

        public SkipTemporaryListsOptionsExtension()
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
