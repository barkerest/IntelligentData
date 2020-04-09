using System.Linq;
using IntelligentData.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace IntelligentData.Extensions
{
    /// <summary>
    /// Extension methods for DbContextOptions builders.
    /// </summary>
    public static class DbContextOptionsExtensions
    {
        /// <summary>
        /// Instructs the IntelligentDbContext to skip adding the temporary lists to the model.
        /// </summary>
        /// <param name="builder"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static DbContextOptionsBuilder<T> WithoutTemporaryLists<T>(this DbContextOptionsBuilder<T> builder)
            where T : DbContext
        {
            ((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(new SkipTemporaryListsOptionsExtension());
            return builder;
        }

        /// <summary>
        /// Instructs the IntelligentDbContext to skip adding the temporary lists to the model.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static DbContextOptionsBuilder WithoutTemporaryLists(this DbContextOptionsBuilder builder)
        {
            ((IDbContextOptionsBuilderInfrastructure)builder).AddOrUpdateExtension(new SkipTemporaryListsOptionsExtension());
            return builder;
        }

        /// <summary>
        /// Determines if the IntelligentDbContext should skip adding the temporary lists to the model.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public static bool WithoutTemporaryLists(this DbContextOptions options)
            => options.Extensions.Any(x => x is SkipTemporaryListsOptionsExtension);
        
    }
}
