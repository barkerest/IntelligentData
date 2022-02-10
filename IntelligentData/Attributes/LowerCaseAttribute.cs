using System;
using IntelligentData.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IntelligentData.Attributes
{
    /// <summary>
    /// Stores strings in lower case.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class LowerCaseAttribute : Attribute, IStringFormatProvider
    {
        /// <inheritdoc />
        public string? FormatValue(object entity, string? currentValue, DbContext context)
        {
            return string.IsNullOrEmpty(currentValue) ? "" : currentValue.ToLower();
        }
    }
}
