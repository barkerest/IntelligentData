using IntelligentData.Enums;
using IntelligentData.Extensions;
using IntelligentData.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
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

        public override string TableNamePrefix { get; } = "EX";

        public ExampleContext(DbContextOptions options, IUserInformationProvider currentUserProvider, ILogger logger)
            : base(options, currentUserProvider, logger)
        {
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

        private static DbContextOptions<ExampleContext> CreateOptions()
        {
            var defaultConn = new SqliteConnection("DataSource=:memory:");
            defaultConn.Open();
            var builder = new DbContextOptionsBuilder<ExampleContext>();
            builder.UseSqlite(defaultConn);
            return builder.Options;
        }

        public static ExampleContext CreateContext(bool seed = false, IUserInformationProvider currentUserProvider = null, ITestOutputHelper outputHelper = null)
        {
            var options = CreateOptions();
            using (var context = new ExampleContext(options, currentUserProvider, new ExampleLogger()))
            {
                context.Database.EnsureCreated();
            }

            var ret = new ExampleContext(options, currentUserProvider, new ExampleLogger(outputHelper));
            if (seed) ret.Seed();
            return ret;
        }

        public static ExampleContext CreateContext(out ExampleContext secondaryContext, bool seed = false, IUserInformationProvider currentUserProvider = null, ITestOutputHelper outputHelper = null)
        {
            var options = CreateOptions();
            using (var context = new ExampleContext(options, currentUserProvider, new ExampleLogger()))
            {
                context.Database.EnsureCreated();
            }

            var ret = new ExampleContext(options, currentUserProvider, new ExampleLogger(outputHelper));
            if (seed) ret.Seed();
            secondaryContext = new ExampleContext(options, currentUserProvider, new ExampleLogger(outputHelper));
            return ret;
        }

        #endregion
    }
}
