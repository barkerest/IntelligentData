using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelligentData.Interfaces
{
    /// <summary>
    /// Defines an interface for entity customizers.
    /// </summary>
    public interface IEntityCustomizer
    {
        /// <summary>
        /// Customizes an entity during model creation.
        /// </summary>
        /// <param name="builder">The entity type builder.</param>
        /// <returns>Returns the customized entity type builder.</returns>
        EntityTypeBuilder Customize(EntityTypeBuilder builder);
    }
}
