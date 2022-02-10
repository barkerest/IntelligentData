using System;
using IntelligentData.Interfaces;

namespace IntelligentData.Internal
{
    
    /// <summary>
    /// A user information provider for nobody.
    /// </summary>
    public sealed class Nobody : IUserInformationProviderInt32,
                                 IUserInformationProviderInt64,
                                 IUserInformationProviderGuid,
                                 IUserInformationProviderString
    {
        /// <inheritdoc />
        public string GetUserName() => "{nobody}";

        /// <inheritdoc />
        Guid IUserInformationProviderGuid.GetUserID() => Guid.Empty;

        /// <inheritdoc />
        long IUserInformationProviderInt64.GetUserID() => 0L;

        /// <inheritdoc />
        int IUserInformationProviderInt32.GetUserID() => 0;

        /// <inheritdoc />
        string IUserInformationProviderString.GetUserID() => string.Empty;

        private Nobody()
        {
            
        }
        
        /// <summary>
        /// Gets the instance of Nobody.
        /// </summary>
        public static readonly Nobody Instance = new();
    }
}
