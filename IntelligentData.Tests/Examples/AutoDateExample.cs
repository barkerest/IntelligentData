using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IntelligentData.Attributes;
using IntelligentData.Enums;
using IntelligentData.Interfaces;

namespace IntelligentData.Tests.Examples
{
    public class AutoDateExample : IEntityAccessProvider
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
       
        public int SomeValue { get; set; }
        
        public int SaveCount { get; set; }
        
        [RuntimeDefaultNow]
        public DateTime CreatedInstant { get; set; }
        
        [RuntimeDefaultNow(IncludeTime = false)]
        public DateTime CreatedDate { get; set; }
        
        [AutoUpdateToNow]
        public DateTime UpdatedInstant { get; set; }
        
        [AutoUpdateToNow(IncludeTime = false)]
        public DateTime UpdatedDate { get; set; }
        
        AccessLevel IEntityAccessProvider.EntityAccessLevel { get; } = AccessLevel.FullAccess;
    }
}
