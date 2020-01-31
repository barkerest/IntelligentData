using IntelligentData.Enums;

namespace IntelligentData.Interfaces
{
	/// <summary>
	/// Defines the access level for an entity.
	/// </summary>
	public interface IAccessAttribute
	{
		/// <summary>
		/// The defined access level for an entity.
		/// </summary>
		AccessLevel Level { get; }
	}
}
