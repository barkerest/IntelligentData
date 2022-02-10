using System;
using Microsoft.EntityFrameworkCore.Metadata;

namespace IntelligentData.Errors
{
    public class EntityTypeWithoutPrimaryKeyException : InvalidOperationException, IIntelligentDataException
    {
        public IReadOnlyEntityType EntityType { get; }

        public EntityTypeWithoutPrimaryKeyException(IReadOnlyEntityType entityType)
            : base($"The entity type {entityType.Name} does not have a primary key.")
        {
            EntityType = entityType;
        }
    }
}
