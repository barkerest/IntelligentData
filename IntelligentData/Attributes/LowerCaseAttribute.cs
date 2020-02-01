using System;
using IntelligentData.Interfaces;

namespace IntelligentData.Attributes
{
    /// <summary>
    /// Stores strings in lower case.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class LowerCaseAttribute : Attribute, IStringFormatProvider
    {
        /// <inheritdoc />
        public string FormatValue(object entity, string currentValue, IntelligentDbContext context)
        {
            return string.IsNullOrEmpty(currentValue) ? "" : currentValue.ToLower();
        }
    }
}
