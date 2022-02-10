using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using IntelligentData.Extensions;
using IntelligentData.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelligentData.Attributes
{
    /// <summary>
    /// Defines a multi-property index.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class CompositeIndexAttribute : ValidationAttribute, IEntityCustomizer
    {
        /// <summary>
        /// The properties in this index.
        /// </summary>
        public string[] Properties { get; }

        /// <summary>
        /// Is this a unique index.
        /// </summary>
        public bool Unique { get; set; }

        public CompositeIndexAttribute(params string[] properties)
        {
            Properties = properties;
        }

        /// <inheritdoc />
        public EntityTypeBuilder Customize(EntityTypeBuilder builder)
        {
            var ib = builder.HasIndex(Properties);
            if (Unique) ib.IsUnique();
            return builder;
        }

        public override bool RequiresValidationContext
            => true;

        private static readonly IDictionary<Type, ICustomCommand> Queries
            = new Dictionary<Type, ICustomCommand>();

        /// <inheritdoc />
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (!Unique) return ValidationResult.Success;
            if (value is null) return ValidationResult.Success;

            if (validationContext.TryGetDbContext(out var context))
            {
                var             conn          = context.Database.GetDbConnection();
                ICustomCommand? customCommand = null;

                lock (Queries)
                {
                    if (Queries.ContainsKey(validationContext.ObjectType))
                    {
                        customCommand = Queries[validationContext.ObjectType];
                    }
                }

                if (customCommand is null)
                {
                    customCommand = validationContext.CreateFilteredCountQuery(true, Properties);
                    lock (Queries)
                    {
                        Queries[validationContext.ObjectType] = customCommand;
                    }
                }

                if (string.IsNullOrEmpty(customCommand.SqlStatement)) return ValidationResult.Success;

                if (conn.State is ConnectionState.Broken or ConnectionState.Closed) conn.Open();

                var cmd = customCommand.CreateCommand(conn, validationContext.ObjectInstance);

                var cnt = Convert.ToInt32(cmd.ExecuteScalar());

                if (cnt > 0)
                {
                    return new ValidationResult("has already been used", Properties);
                }
            }

            return ValidationResult.Success;
        }
    }
}
