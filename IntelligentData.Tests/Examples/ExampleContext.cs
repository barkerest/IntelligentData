using System;
using IntelligentData.Enums;
using IntelligentData.Extensions;
using IntelligentData.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace IntelligentData.Tests.Examples
{
    public class ExampleContext : IntelligentDbContext
    {
        public static readonly string[] ExampleNames =
        {
            "Fred Flintstone",
            "Wilma Flintstone",
            "Barney Rubble",
            "Betty Rubble",
            "Mr. Slate",
            "George Jetson",
            "Jane Jetson",
            "Judy Jetson",
            "Elroy Jetson",
            "Mr. Spacely",
            "Bugs Bunny",
            "Daffy Duck",
            "Elmer Fudd",
            "Yosemite Sam",
            "Foghorn Leghorn",
            "Sylvester",
            "Tweety"
        };

        public static readonly string NewName = "Tasmanian Devil";

        public override  string TableNamePrefix { get; }

        public ExampleContext(DbContextOptions options, IUserInformationProvider currentUserProvider, ILogger logger)
            : this(options, currentUserProvider, logger, "EX")
        {
            
        }
        
        protected ExampleContext(DbContextOptions options, IUserInformationProvider currentUserProvider, ILogger logger, string prefix)
            : base(options, currentUserProvider, logger)
        {
            TableNamePrefix = prefix;
        }

        // for access tests.
        public DbSet<ReadOnlyEntity>               ReadOnlyEntities               { get; set; }
        public DbSet<ReadInsertEntity>             ReadInsertEntities             { get; set; }
        public DbSet<ReadUpdateEntity>             ReadUpdateEntities             { get; set; }
        public DbSet<ReadDeleteEntity>             ReadDeleteEntities             { get; set; }
        public DbSet<ReadInsertUpdateEntity>       ReadInsertUpdateEntities       { get; set; }
        public DbSet<ReadInsertDeleteEntity>       ReadInsertDeleteEntities       { get; set; }
        public DbSet<ReadUpdateDeleteEntity>       ReadUpdateDeleteEntities       { get; set; }
        public DbSet<ReadInsertUpdateDeleteEntity> ReadInsertUpdateDeleteEntities { get; set; }
        public DbSet<DynamicAccessEntity>          DynamicAccessEntities          { get; set; }
        public DbSet<StringFormatExample>          StringFormatExamples           { get; set; }
        public DbSet<AutoDateExample>              AutoDateExamples               { get; set; }
        public DbSet<TrackedEntity>                TrackedEntities                { get; set; }
        public DbSet<VersionedEntity>              VersionedEntities              { get; set; }
        public DbSet<TimestampedEntity>            TimestampedEntities            { get; set; }
        public DbSet<DefaultAccessEntity>          DefaultAccessEntities          { get; set; }
        public DbSet<SmartEntity>                  SmartEntities                  { get; set; }
        public DbSet<UniqueEntity>                 UniqueEntities                 { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder
                .Entity<AutoDateExample>()
                .Property(x => x.SaveCount)
                .HasAutoUpdate(v => (int) v + 1);
        }

        private         AccessLevel _defaultAccessLevel = AccessLevel.ReadOnly;


        public override AccessLevel DefaultAccessLevel => _defaultAccessLevel;

        public void SetDefaultAccessLevel(AccessLevel level)
        {
            _defaultAccessLevel = level;
        }


        #region Create Context

        private void Seed()
        {
            foreach (var name in ExampleNames)
            {
                ReadOnlyEntities.Add(new ReadOnlyEntity() {Name                             = name});
                ReadInsertEntities.Add(new ReadInsertEntity() {Name                         = name});
                ReadUpdateEntities.Add(new ReadUpdateEntity() {Name                         = name});
                ReadDeleteEntities.Add(new ReadDeleteEntity() {Name                         = name});
                ReadInsertUpdateEntities.Add(new ReadInsertUpdateEntity() {Name             = name});
                ReadInsertDeleteEntities.Add(new ReadInsertDeleteEntity() {Name             = name});
                ReadUpdateDeleteEntities.Add(new ReadUpdateDeleteEntity() {Name             = name});
                ReadInsertUpdateDeleteEntities.Add(new ReadInsertUpdateDeleteEntity() {Name = name});
                DynamicAccessEntities.Add(new DynamicAccessEntity() {Name                   = name});
            }

            SeedData(() => SaveChanges());
        }

        public static IServiceProvider CreateServiceProvider<TContext>(ITestOutputHelper outputHelper, IUserInformationProvider currentUserProvider = null, bool withTempTables = true, bool seed = true) where TContext : ExampleContext
        {
            var col = new ServiceCollection();
            
            var logger = new TestOutputLogger(outputHelper);
            
            col.AddSingleton<ILogger>(logger);
            col.AddSingleton<ILoggerProvider>(logger);
            col.AddSingleton<ILoggerFactory>(logger);

            if (currentUserProvider is null) currentUserProvider = new ExampleUserInformationProvider() {CurrentUser = ExampleUserInformationProvider.Users.Maximillian};

            col.AddSingleton<IUserInformationProvider>(currentUserProvider);

            var options = CreateOptions<TContext>(withTempTables);
            
            col.AddSingleton<DbContextOptions<TContext>>(options);
            
            col.AddSingleton<DbContextOptions>(options);

            col.AddDbContext<TContext>();

            var sp = col.BuildServiceProvider();

            using (var scope = sp.CreateScope())
            {
                using (var preContext = scope.ServiceProvider.GetRequiredService<TContext>())
                {
                    preContext.Database.EnsureCreated();
                }
            }

            if (seed)
            {
                using (var scope = sp.CreateScope())
                {
                    using (var preContext = scope.ServiceProvider.GetRequiredService<TContext>())
                    {
                        preContext.Seed();
                    }
                }
            }

            return sp;
        }

        public static IServiceProvider CreateServiceProvider(ITestOutputHelper outputHelper, IUserInformationProvider currentUserProvider = null,  bool withTempTables = true, bool seed = true)
            => CreateServiceProvider<ExampleContext>(outputHelper, currentUserProvider, withTempTables, seed);
        
        private static DbContextOptions<TContext> CreateOptions<TContext>(bool withTempTables = true) where TContext : ExampleContext
        {
            var defaultConn = new SqliteConnection("DataSource=:memory:");
            defaultConn.Open();
            var builder = new DbContextOptionsBuilder<TContext>();
            builder.UseSqlite(defaultConn);
            if (withTempTables)
            {
                builder.WithTemporaryLists();
            }
            return builder.Options;
        }

        private static DbContextOptions<ExampleContext> CreateOptions(bool withTempTables = true) => CreateOptions<ExampleContext>();
        
        public static TContext CreateContext<TContext>(ITestOutputHelper outputHelper, bool seed = false, IUserInformationProvider currentUserProvider = null, bool withTempTables = true) where TContext : ExampleContext
            => CreateServiceProvider<TContext>(outputHelper, currentUserProvider, withTempTables, seed).GetRequiredService<TContext>();

        public static ExampleContext CreateContext(ITestOutputHelper outputHelper, bool seed = false, IUserInformationProvider currentUserProvider = null, bool withTempTables = true)
            => CreateServiceProvider<ExampleContext>(outputHelper, currentUserProvider, withTempTables, seed).GetRequiredService<ExampleContext>();

        public static TContext CreateContext<TContext>(ITestOutputHelper outputHelper, out ExampleContext secondaryContext, bool seed = false, IUserInformationProvider currentUserProvider = null, bool withTempTables = true) where TContext : ExampleContext
        {
            var sp      = CreateServiceProvider<TContext>(outputHelper, currentUserProvider,withTempTables, seed);

            secondaryContext = sp.CreateScope().ServiceProvider.GetRequiredService<TContext>();
            return sp.CreateScope().ServiceProvider.GetRequiredService<TContext>();
        }

        public static ExampleContext CreateContext(ITestOutputHelper outputHelper, out ExampleContext secondaryContext, bool seed = false, IUserInformationProvider currentUserProvider = null, bool withTempTables = true)
            => CreateContext<ExampleContext>(outputHelper, out secondaryContext, seed, currentUserProvider, withTempTables);


        #endregion
    }
}
