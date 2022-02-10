using System;
using System.Collections.Generic;
using IntelligentData.Extensions;
using IntelligentData.Internal;
using Microsoft.EntityFrameworkCore.Metadata;

namespace IntelligentData.Errors
{
    public class EntityTypeMissingPropertyException : InvalidOperationException, IIntelligentDataException
    {
        public IReadOnlyEntityType EntityType { get; }

        public IEnumerable<string> MissingProperty { get; }

        public EntityTypeMissingPropertyException(IReadOnlyEntityType entityType, params string[] missingProperty)
            : base(
                missingProperty.Length == 1 
                    ? $"The entity {entityType.Name} does not contain a property named {missingProperty[0]}."
                    : $"The entity {entityType.Name} does not contain properties named {missingProperty.JoinOr()}.")
        {
            EntityType      = entityType;
            MissingProperty = missingProperty;
        }
    
    
    
    }
}
