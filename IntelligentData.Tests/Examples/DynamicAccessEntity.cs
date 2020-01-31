using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using IntelligentData.Enums;
using IntelligentData.Interfaces;

namespace IntelligentData.Tests.Examples
{
	public class DynamicAccessEntity : IEntityWithAccess, IExampleEntity
	{
		[Key]
		[DatabaseGenerated(DatabaseGeneratedOption.Identity)]
		public int ID { get; set; }
		
		[Required]
		[StringLength(100)]
		public string Name { get; set; }

		public AccessLevel EntityAccessLevel => (AccessLevel) (ID % 8);
	}
}
