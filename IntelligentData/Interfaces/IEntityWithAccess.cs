using IntelligentData.Enums;

namespace IntelligentData.Interfaces
{
	/// <summary>
	/// Marks an entity type as providing an access level per instance.
	/// </summary>
	public interface IEntityWithAccess
	{
		/// <summary>
		/// Gets the access level for this instance.
		/// </summary>
		AccessLevel EntityAccessLevel { get; }
	}
}
