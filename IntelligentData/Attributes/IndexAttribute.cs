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
    /// Defines a single property index.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class IndexAttribute : ValidationAttribute, IPropertyCustomizer
    {
        /// <summary>
        /// Is this a unique index?
        /// </summary>
        public bool Unique { get; set; }

        /// <inheritdoc />
        public EntityTypeBuilder Customize(EntityTypeBuilder builder, string property)
        {
            var ib = builder.HasIndex(property);
            if (Unique) ib.IsUnique();
            return builder;
        }

        private static readonly IDictionary<Type, ICustomCommand> Queries
            = new Dictionary<Type, ICustomCommand>();

        public override bool RequiresValidationContext
            => true;

        /// <inheritdoc />
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (!Unique) return ValidationResult.Success;
            if (value is null) return ValidationResult.Success;
            if (string.IsNullOrWhiteSpace(validationContext.MemberName)) return ValidationResult.Success;

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
                    customCommand = validationContext.CreateFilteredCountQuery(true, validationContext.MemberName);
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
                    return new ValidationResult("has already been used", new[] { validationContext.MemberName });
                }
            }

            return ValidationResult.Success;
        }
    }
}
