using System;
using IntelligentData.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IntelligentData.Attributes
{
    /// <summary>
    /// Stores strings in upper case.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class UpperCaseAttribute : Attribute, IStringFormatProvider
    {
        /// <inheritdoc />
        public string FormatValue(object entity, string currentValue, DbContext context)
        {
            return string.IsNullOrEmpty(currentValue) ? "" : currentValue.ToUpper();
        }
    }
}
