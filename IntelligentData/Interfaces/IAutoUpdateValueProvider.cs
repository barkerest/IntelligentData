using Microsoft.EntityFrameworkCore;

namespace IntelligentData.Interfaces
{
    /// <summary>
    /// Defines a provider for an automatically updated property.
    /// </summary>
    public interface IAutoUpdateValueProvider
    {
        /// <summary>
        /// Gets the new value for the property.
        /// </summary>
        /// <param name="entity">The entity being modified.</param>
        /// <param name="currentValue">The current value of the property.</param>
        /// <param name="context">The context the modification is occurring within.</param>
        /// <returns>Returns the new value for the property.</returns>
        object? NewValue(object entity, object? currentValue, DbContext context);
    }
}
