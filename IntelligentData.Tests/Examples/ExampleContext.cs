﻿using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

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

        public ExampleContext(DbContextOptions options)
            : base(options)
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

        public static ExampleContext CreateContext(bool seed = false)
        {
            var options = CreateOptions();
            using (var context = new ExampleContext(options))
            {
                context.Database.EnsureCreated();
            }

            var ret = new ExampleContext(options);
            if (seed) ret.Seed();
            return ret;
        }

        public static ExampleContext CreateContext(out ExampleContext secondaryContext, bool seed = false)
        {
            var options = CreateOptions();
            using (var context = new ExampleContext(options))
            {
                context.Database.EnsureCreated();
            }

            var ret = new ExampleContext(options);
            if (seed) ret.Seed();
            secondaryContext = new ExampleContext(options);
            return ret;
        }

        #endregion
    }
}
