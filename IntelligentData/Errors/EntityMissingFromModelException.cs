using System;

namespace IntelligentData.Errors
{
    public class EntityMissingFromModelException : InvalidOperationException, IIntelligentDataException
    {
        public Type Type { get; }

        public EntityMissingFromModelException(Type type)
            : base($"The entity type {type} is missing from the data model.")
        {
            Type = type;
        }
    
    }
}
