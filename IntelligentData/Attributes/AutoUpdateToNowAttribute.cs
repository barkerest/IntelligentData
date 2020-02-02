using System;
using IntelligentData.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IntelligentData.Attributes
{
    /// <summary>
    /// Uses the current date/time for the new value when saving.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public sealed class AutoUpdateToNowAttribute : Attribute, IAutoUpdateValueProvider
    {
        /// <summary>
        /// Determines if the default value will include time or just the date.
        /// </summary>
        public bool IncludeTime { get; set; } = true;

        /// <inheritdoc />
        public object NewValue(object entity, object currentValue, DbContext context)
        {
            return IncludeTime ? DateTime.Now : DateTime.Today;
        }
    }
}
