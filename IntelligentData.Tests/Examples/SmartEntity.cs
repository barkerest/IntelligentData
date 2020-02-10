using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IntelligentData.Attributes;
using IntelligentData.Enums;
using IntelligentData.Interfaces;

namespace IntelligentData.Tests.Examples
{
    public class SmartEntity : IntelligentEntity<ExampleContext>, IVersionedEntity, IValidatableObject, IEntityAccessProvider
    {
        public SmartEntity(ExampleContext dbContext)
            : base(dbContext)
        {
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        public long? RowVersion { get; set; }
        
        IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext)
        {
            if (string.IsNullOrEmpty(Name)) yield break;
            if (Name.ToUpper().Equals("GEORGE")) yield return new ValidationResult("cannot be George", new []{nameof(Name)});
        }

        private AccessLevel _accessLevel = AccessLevel.FullAccess;

        public void SetAccessLevel(AccessLevel level) => _accessLevel = level;
        
        AccessLevel IEntityAccessProvider.EntityAccessLevel => _accessLevel;
        
    }
}
