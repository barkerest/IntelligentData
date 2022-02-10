using System;
using Microsoft.EntityFrameworkCore.Metadata;

namespace IntelligentData.Errors
{
    public class PropertyWithoutAccessorException : InvalidOperationException, IIntelligentDataException
    {
        public IReadOnlyProperty Property { get; }
    
        public bool Writable { get; }

        private static string AbleTo(bool write) => write ? "writable" : "readable";
    
        internal PropertyWithoutAccessorException(IReadOnlyProperty property, bool write = false)
            : base($"The property {property.Name} does not have a {AbleTo(write)} property or field accessor.")

        {
            Property = property;
            Writable = write;
        }
    }
}
