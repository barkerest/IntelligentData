using System;
using IntelligentData.Interfaces;
using IntelligentData.Internal;
using Microsoft.EntityFrameworkCore;

namespace IntelligentData.Attributes
{
    /// <summary>
    /// Uses the current user ID from the context for the new value when saving.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class AutoUpdateToCurrentUserNameAttribute : Attribute, IAutoUpdateValueProvider
    {
        /// <inheritdoc />
        public object? NewValue(object entity, object? currentValue, DbContext context)
        {
            var provider = (context as IntelligentDbContext)?.CurrentUserProvider ?? Nobody.Instance;
            return provider.GetUserName();
        }
    }
}
