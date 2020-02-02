using System;
using IntelligentData.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IntelligentData.Attributes
{
    /// <summary>
    /// Uses the current date/time for the default value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class RuntimeDefaultNowAttribute : Attribute, IRuntimeDefaultValueProvider
    {
        /// <summary>
        /// Determines if the default value will include time or just the date.
        /// </summary>
        public bool IncludeTime { get; set; } = true;
        
        /// <inheritdoc />
        public object ValueOrDefault(object entity, object currentValue, DbContext context)
        {
            var def = IncludeTime ? DateTime.Now : DateTime.Today;
            
            if (currentValue is null) return def;
            
            if (currentValue is DateTime dt &&
                dt == default) return def;
            
            return currentValue;
        }
    }
}
