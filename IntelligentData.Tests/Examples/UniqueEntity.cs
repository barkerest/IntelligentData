using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IntelligentData.Attributes;
using IntelligentData.Enums;

namespace IntelligentData.Tests.Examples
{
    [Access(AccessLevel.FullAccess)]
    [CompositeIndex(nameof(ValueA), nameof(ValueB), Unique = true)]
    public class UniqueEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        
        [Required]
        [StringLength(100)]
        [Index(Unique = true)]
        public string Name { get; set; }
        
        public int? ValueA { get; set; }
        
        public int? ValueB { get; set; }
        
    }
}
