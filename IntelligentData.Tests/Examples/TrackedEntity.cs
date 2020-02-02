using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IntelligentData.Interfaces;

namespace IntelligentData.Tests.Examples
{
    public class TrackedEntity : ITrackedEntityWithUserName, ITrackedEntityWithInt32UserID
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        public DateTime CreatedAt { get; set; }
        public DateTime LastModifiedAt { get; set; }
        public string CreatedBy { get; set; }
        public string LastModifiedBy { get; set; }
        public int CreatedByID { get; set; }
        public int LastModifiedByID { get; set; }
    }
}
