namespace IntelligentData.Interfaces
{
    /// <summary>
    /// Defines the provider for a string format.
    /// </summary>
    public interface IStringFormatProvider
    {
        /// <summary>
        /// Formats a string value for storage in a property.
        /// </summary>
        /// <param name="entity">The entity being saved.</param>
        /// <param name="currentValue">The current value of the property.</param>
        /// <param name="context">The context the entity is being saved into.</param>
        /// <returns>Returns the formatted string value for the property.</returns>
        string FormatValue(object entity, string currentValue, IntelligentDbContext context);
    }
}
