using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IntelligentData.Attributes;
using IntelligentData.Enums;

namespace IntelligentData.Tests.Examples
{
    [Access(AccessLevel.Update)]
    public class ReadUpdateEntity : IExampleEntity
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; }
    }
}
