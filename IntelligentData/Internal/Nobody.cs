using System;
using IntelligentData.Interfaces;

namespace IntelligentData.Internal
{
    
    /// <summary>
    /// A user information provider for nobody.
    /// </summary>
    public sealed class Nobody<T> : IUserInformationProvider<T>
    {
        /// <inheritdoc />
        public T GetUserID() => default;

        /// <inheritdoc />
        public string GetUserName() => "{nobody}";
    }
}
