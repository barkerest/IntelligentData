using System;
using Microsoft.EntityFrameworkCore.Metadata;

namespace IntelligentData.Errors
{
    public class EntityTypeWithoutTableNameException : InvalidOperationException, IIntelligentDataException
    {
        public IReadOnlyEntityType EntityType { get; }

        public EntityTypeWithoutTableNameException(IReadOnlyEntityType entityType)
            : base($"The entity type {entityType.Name} does not reference a named table or view.")
        {
            EntityType = entityType;
        }
    }
}
