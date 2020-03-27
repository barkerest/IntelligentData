using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelligentData.Interfaces
{
    /// <summary>
    /// Defines an interface for property customizers.
    /// </summary>
    public interface IPropertyCustomizer
    {
        /// <summary>
        /// Customizes a property during model creation.
        /// </summary>
        /// <param name="builder">The entity type builder being customized.</param>
        /// <param name="property">The property to customize.</param>
        /// <returns>Returns the customized entity type builder.</returns>
        EntityTypeBuilder Customize(EntityTypeBuilder builder, string property);
    }
}
