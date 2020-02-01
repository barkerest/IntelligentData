using System;
using IntelligentData.Interfaces;

namespace IntelligentData.Attributes
{
    /// <summary>
    /// Stores strings in upper case.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class UpperCaseAttribute : Attribute, IStringFormatProvider
    {
        /// <inheritdoc />
        public string FormatValue(object entity, string currentValue, IntelligentDbContext context)
        {
            return string.IsNullOrEmpty(currentValue) ? "" : currentValue.ToUpper();
        }
    }
}
