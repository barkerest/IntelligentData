using System;
using IntelligentData.Interfaces;

namespace IntelligentData.Internal
{
    
    /// <summary>
    /// A user information provider for nobody.
    /// </summary>
    public sealed class Nobody : IUserInformationProvider
    {
        /// <inheritdoc />
        public object GetUserID(Type ofType)
        {
            if (ofType is null) return null;
            return ofType.IsValueType ? Activator.CreateInstance(ofType) : null;
        }

        /// <inheritdoc />
        public string GetUserName() => "{nobody}";
    }
}
