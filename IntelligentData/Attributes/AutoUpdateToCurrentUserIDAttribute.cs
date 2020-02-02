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
    public class AutoUpdateToCurrentUserIDAttribute : Attribute, IAutoUpdateValueProvider
    {
        internal static readonly Dictionary<Type, Func<IUserInformationProvider, object>> UserIdAccessors
            = new Dictionary<Type, Func<IUserInformationProvider, object>>()
            {
                {
                    typeof(int),
                    p => (p as IUserInformationProviderInt32)?.GetUserID()
                         ?? 0
                },
                {
                    typeof(long),
                    p =>
                    {
                        if (p is IUserInformationProviderInt64 i64) return i64.GetUserID();
                        if (p is IUserInformationProviderInt32 i32) return i32.GetUserID();
                        return 0;
                    }
                },
                {
                    typeof(Guid),
                    p => (p as IUserInformationProviderGuid)?.GetUserID()
                         ?? Guid.Empty
                },
                {
                    typeof(string),
                    p =>
                    {
                        if (p is IUserInformationProviderString s) return s.GetUserID();
                        if (p is IUserInformationProviderGuid g) return g.GetUserID().ToString();
                        if (p is IUserInformationProviderInt64 i64) return i64.GetUserID().ToString();
                        if (p is IUserInformationProviderInt32 i32) return i32.GetUserID().ToString();
                        return null;
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
        public object NewValue(object entity, object currentValue, DbContext context)
        {
            var provider = (context as IntelligentDbContext)?.CurrentUserProvider ?? Nobody.Instance;
            return _getUserId(provider);
        }
    }
}
