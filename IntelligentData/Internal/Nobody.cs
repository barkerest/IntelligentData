using System;
using IntelligentData.Interfaces;

namespace IntelligentData.Internal
{
    
    /// <summary>
    /// A user information provider for nobody.
    /// </summary>
    public sealed class Nobody : IUserInformationProvider,
                                 IUserInformationProviderInt32,
                                 IUserInformationProviderInt64,
                                 IUserInformationProviderGuid,
                                 IUserInformationProviderString
    {
        /// <summary>
        /// Creates an instance of Nobody.
        /// </summary>
        /// <param name="maxUserNameLength"></param>
        /// <param name="maxUserIdLength"></param>
        public Nobody(int maxUserNameLength = 255, int maxUserIdLength = 255)
        {
            MaxLengthForUserName = maxUserNameLength;
            MaxLengthForUserID = maxUserIdLength;
        }
        
        /// <inheritdoc />
        public string GetUserName() => "{nobody}";

        /// <inheritdoc />
        public int MaxLengthForUserName { get; } = 255;

        /// <inheritdoc />
        Guid IUserInformationProviderGuid.GetUserID() => Guid.Empty;

        /// <inheritdoc />
        public int MaxLengthForUserID { get; } = 255;

        /// <inheritdoc />
        long IUserInformationProviderInt64.GetUserID() => 0L;

        /// <inheritdoc />
        int IUserInformationProviderInt32.GetUserID() => 0;

        /// <inheritdoc />
        string IUserInformationProviderString.GetUserID() => null;
        
    }
}
