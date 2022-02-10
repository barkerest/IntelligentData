using System;
using IntelligentData.Interfaces;
using IntelligentData.Internal;
using Microsoft.EntityFrameworkCore;

namespace IntelligentData.Attributes
{
    /// <summary>
    /// Uses the current user name from the context as the default value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class RuntimeDefaultCurrentUserNameAttribute : Attribute, IRuntimeDefaultValueProvider
    {
        /// <inheritdoc />
        public object? ValueOrDefault(object entity, object? currentValue, DbContext context)
        {
            var provider = (context as IntelligentDbContext)?.CurrentUserProvider ?? Nobody.Instance;

            if (currentValue is null) return provider.GetUserName();

            if (currentValue is string s)
                return string.IsNullOrWhiteSpace(s) ? provider.GetUserName() : s;

            return provider.GetUserName();
        }
    }
}
