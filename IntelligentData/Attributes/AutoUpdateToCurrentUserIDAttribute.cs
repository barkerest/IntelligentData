using System;
using IntelligentData.Interfaces;
using IntelligentData.Internal;
using Microsoft.EntityFrameworkCore;

namespace IntelligentData.Attributes
{
    /// <summary>
    /// Uses the current user ID from the context for the new value when saving.
    /// </summary>
    public class AutoUpdateToCurrentUserIDAttribute : Attribute, IAutoUpdateValueProvider
    {
        /// <summary>
        /// The data type to return for the user ID.
        /// </summary>
        public Type UserIdType { get; set; } = null;

        /// <inheritdoc />
        public object NewValue(object entity, object currentValue, DbContext context)
        {
            var provider = (context as IntelligentDbContext)?.CurrentUserProvider ?? new Nobody();
            return provider.GetUserID(UserIdType ?? currentValue?.GetType());
        }
    }
}
