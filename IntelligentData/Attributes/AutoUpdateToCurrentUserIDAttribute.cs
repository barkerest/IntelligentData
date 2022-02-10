using System;
using System.Collections.Generic;
using IntelligentData.Interfaces;
using IntelligentData.Internal;
using Microsoft.EntityFrameworkCore;

namespace IntelligentData.Attributes
{
    /// <summary>
    /// Uses the current user ID from the context for the new value when saving.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class AutoUpdateToCurrentUserIDAttribute : Attribute, IAutoUpdateValueProvider
    {
        internal static readonly Dictionary<Type, Func<IUserInformationProvider, object>> UserIdAccessors
            = new()
            {
                {
                    typeof(int),
                    p => (p as IUserInformationProviderInt32)?.GetUserID()
                         ?? 0
                },
                {
                    typeof(long),
                    p => p switch
                    {
                        IUserInformationProviderInt64 i64 => i64.GetUserID(),
                        IUserInformationProviderInt32 i32 => i32.GetUserID(),
                        _                                 => 0
                    }
                },
                {
                    typeof(Guid),
                    p => (p as IUserInformationProviderGuid)?.GetUserID()
                         ?? Guid.Empty
                },
                {
                    typeof(string),
                    p => p switch
                    {
                        IUserInformationProviderString s  => s.GetUserID(),
                        IUserInformationProviderGuid g    => g.GetUserID().ToString(),
                        IUserInformationProviderInt64 i64 => i64.GetUserID().ToString(),
                        IUserInformationProviderInt32 i32 => i32.GetUserID().ToString(),
                        _                                 => string.Empty
                    }
                }
            };

        private readonly Func<IUserInformationProvider, object> _getUserId;

        public AutoUpdateToCurrentUserIDAttribute(Type userIdType)
        {
            if (userIdType is null) throw new ArgumentNullException(nameof(userIdType));
            if (!UserIdAccessors.ContainsKey(userIdType)) throw new ArgumentException("Type is not supported for user ID.");
            UserIdType = userIdType;
            _getUserId = UserIdAccessors[userIdType];
        }

        /// <summary>
        /// The data type to return for the user ID.
        /// </summary>
        public Type UserIdType { get; }

        /// <inheritdoc />
        public object? NewValue(object entity, object? currentValue, DbContext context)
        {
            var provider = (context as IntelligentDbContext)?.CurrentUserProvider ?? Nobody.Instance;
            return _getUserId(provider);
        }
    }
}
