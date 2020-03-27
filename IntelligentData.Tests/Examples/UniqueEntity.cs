using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IntelligentData.Attributes;
using IntelligentData.Enums;

namespace IntelligentData.Tests.Examples
{
    [Access(AccessLevel.FullAccess)]
    public class UniqueEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        
        [Required]
        [StringLength(100)]
        [Index(Unique = true)]
        public string Name { get; set; }
        
    }
}
