using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IntelligentData.Enums;
using IntelligentData.Interfaces;

namespace IntelligentData.Tests.Examples
{
    public class VersionedEntity : IVersionedEntity, IEntityAccessProvider
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        
        public string Name { get; set; }
        
        public long? RowVersion { get; set; }
        AccessLevel IEntityAccessProvider.EntityAccessLevel { get; } = AccessLevel.FullAccess;
    }
}
