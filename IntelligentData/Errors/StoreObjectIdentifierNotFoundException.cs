using System;
using Microsoft.EntityFrameworkCore.Metadata;

namespace IntelligentData.Errors
{
    public class StoreObjectIdentifierNotFoundException : InvalidOperationException, IIntelligentDataException
    {
        public IReadOnlyEntityType EntityType { get; }

        public StoreObjectIdentifierNotFoundException(IReadOnlyEntityType entityType)
            : base($"The store object identifier for the entity type {entityType.Name} could not be found.")
        {
            EntityType = entityType;
        }
    }
}
