using System;
using System.ComponentModel.DataAnnotations;
using IntelligentData.Interfaces;
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

        /// <inheritdoc />
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            return base.IsValid(value, validationContext);
        }
    }
}
