using Microsoft.EntityFrameworkCore;

namespace IntelligentData.Delegates
{
    /// <summary>
    /// A value provider.
    /// </summary>
    /// <param name="entity">The entity the value is being provided for.</param>
    /// <param name="currentValue">The current value from the entity.</param>
    /// <param name="context">The context the entity is being saved to.</param>
    public delegate object ValueProviderDelegate(object entity, object currentValue, DbContext context);

    /// <summary>
    /// A string format provider.
    /// </summary>
    /// <param name="entity">The entity the value is being formatted for.</param>
    /// <param name="currentValue">The current value from the entity.</param>
    /// <param name="context">The context the entity is being saved to.</param>
    public delegate string StringFormatProviderDelegate(object entity, string currentValue, DbContext context);

    /// <summary>
    /// An entity initializer.
    /// </summary>
    /// <param name="context">The context initializing the entity.</param>
    /// <param name="entity">The entity being initialized.</param>
    public delegate void IntelligentEntityInitializer(IntelligentDbContext context, object entity);
}
