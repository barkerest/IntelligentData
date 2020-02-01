using IntelligentData.Enums;

namespace IntelligentData.Interfaces
{
    /// <summary>
    /// Provides the access level for an entity.
    /// </summary>
    public interface IEntityAccessProvider
    {
        /// <summary>
        /// Gets the access level for an entity.
        /// </summary>
        AccessLevel EntityAccessLevel { get; }
    }
}
