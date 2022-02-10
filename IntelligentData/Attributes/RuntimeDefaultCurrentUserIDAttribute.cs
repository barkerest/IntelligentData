using System;
using IntelligentData.Interfaces;
using IntelligentData.Internal;
using Microsoft.EntityFrameworkCore;

namespace IntelligentData.Attributes
{
    /// <summary>
    /// Uses the current user ID from the context as the default value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class RuntimeDefaultCurrentUserIDAttribute : Attribute, IRuntimeDefaultValueProvider
    {
        private readonly Func<IUserInformationProvider, object> _getUserId;

        public RuntimeDefaultCurrentUserIDAttribute(Type userIdType)
        {
            if (userIdType is null) throw new ArgumentNullException(nameof(userIdType));
            if (!AutoUpdateToCurrentUserIDAttribute.UserIdAccessors.ContainsKey(userIdType)) throw new ArgumentException("Type is not supported for user ID.");
            UserIdType = userIdType;
            _getUserId = AutoUpdateToCurrentUserIDAttribute.UserIdAccessors[userIdType];
        }

        /// <summary>
        /// The data type to return for the user ID.
        /// </summary>
        public Type UserIdType { get; }

        /// <inheritdoc />
        public object? ValueOrDefault(object entity, object? currentValue, DbContext context)
        {
            var provider = (context as IntelligentDbContext)?.CurrentUserProvider ?? Nobody.Instance;

            if (currentValue is null) return _getUserId(provider);

            if (UserIdType.IsValueType)
            {
                var defVal = Activator.CreateInstance(UserIdType)!;
                return defVal.Equals(currentValue) ? _getUserId(provider) : currentValue;
            }

            if (UserIdType == typeof(string) &&
                currentValue is string s)
            {
                return string.IsNullOrWhiteSpace(s) ? _getUserId(provider) : s;
            }

            return _getUserId(provider);
        }
    }
}
