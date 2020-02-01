using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IntelligentData.Attributes;
using IntelligentData.Enums;
using IntelligentData.Interfaces;

namespace IntelligentData.Tests.Examples
{
    public class StringFormatExample : IEntityAccessProvider
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        
        [StringLength(100)]
        [LowerCase]
        public string LowerCaseString { get; set; }
        
        [StringLength(100)]
        [UpperCase]
        public string UpperCaseString { get; set; }
        
        AccessLevel IEntityAccessProvider.EntityAccessLevel { get; } = AccessLevel.FullAccess;
    }
}
