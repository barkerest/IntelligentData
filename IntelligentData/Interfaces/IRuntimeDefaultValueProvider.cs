namespace IntelligentData.Interfaces
{
	/// <summary>
	/// Defines a provider for a runtime default value for a property.
	/// </summary>
	public interface IRuntimeDefaultValueProvider
	{
		/// <summary>
		/// Gets the value for the property.
		/// </summary>
		/// <param name="entity">The entity being added.</param>
		/// <param name="currentValue">The current value of the property.</param>
		/// <param name="context">The context the insertion is occurring within.</param>
		/// <returns>Returns the current value or default value for the property if the current value is not set.</returns>
		object ValueOrDefault(object entity, object currentValue, IntelligentDbContext context);
	}
}
