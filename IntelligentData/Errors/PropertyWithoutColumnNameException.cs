using System;
using Microsoft.EntityFrameworkCore.Metadata;

namespace IntelligentData.Errors
{
    public class PropertyWithoutColumnNameException : InvalidOperationException, IIntelligentDataException
    {
        public IReadOnlyProperty Property { get; }

        public PropertyWithoutColumnNameException(IReadOnlyProperty property)
            : base($"The property {property.Name} does not have a column name.")
        {
            Property = property;
        }
    }
}
