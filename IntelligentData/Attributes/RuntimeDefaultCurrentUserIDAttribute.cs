using System;
using System.Reflection;
using IntelligentData.Interfaces;
using IntelligentData.Internal;
using Microsoft.EntityFrameworkCore;

namespace IntelligentData.Attributes
{
    /// <summary>
    /// Uses the current user ID from the context as the default value.
    /// </summary>
    public class RuntimeDefaultCurrentUserIDAttribute : Attribute, IRuntimeDefaultValueProvider
    {
        /// <summary>
        /// The data type to return for the user ID.
        /// </summary>
        public Type UserIdType { get; set; } = null;
        
        /// <inheritdoc />
        public object ValueOrDefault(object entity, object currentValue, DbContext context)
        {
            var provider = (context as IntelligentDbContext)?.CurrentUserProvider ?? new Nobody();

            if (currentValue is null) return provider.GetUserID(UserIdType);

            var t = UserIdType ?? currentValue.GetType();
            
            if (t.IsValueType &&
                Activator.CreateInstance(t)
                         .Equals(currentValue))
                return provider.GetUserID(t);

            if (t == typeof(string) &&
                "".Equals(currentValue))
                return provider.GetUserID(t);
            
            return currentValue;
        }
    }
}
