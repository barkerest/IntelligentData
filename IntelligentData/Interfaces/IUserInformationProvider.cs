using System;

namespace IntelligentData.Interfaces
{
    /// <summary>
    /// A generic interface for user information providers.
    /// </summary>
    public interface IUserInformationProvider
    {
        /// <summary>
        /// Gets the current user name.
        /// </summary>
        /// <returns>Returns the user name of the current user.</returns>
        string GetUserName();

        /// <summary>
        /// Gets the maximum length for user names.
        /// </summary>
        int MaxLengthForUserName { get; }
    }

    /// <summary>
    /// A user information provider with 32-bit integer IDs.
    /// </summary>
    public interface IUserInformationProviderInt32 : IUserInformationProvider
    {
        /// <summary>
        /// Gets the current user ID.
        /// </summary>
        /// <returns>Returns the user ID or 0.</returns>
        int GetUserID();
    }

    /// <summary>
    /// A user information provider with 64-bit integer IDs.
    /// </summary>
    public interface IUserInformationProviderInt64 : IUserInformationProvider
    {
        /// <summary>
        /// Gets the current user ID.
        /// </summary>
        /// <returns>Returns the user ID or 0.</returns>
        long GetUserID();
    }
    
    /// <summary>
    /// A user information provider with GUID IDs.
    /// </summary>
    public interface IUserInformationProviderGuid : IUserInformationProvider
    {
        /// <summary>
        /// Gets the current user ID.
        /// </summary>
        /// <returns>Returns the user ID or an empty GUID.</returns>
        Guid GetUserID();
    }

    /// <summary>
    /// A user information provider with string IDs.
    /// </summary>
    public interface IUserInformationProviderString : IUserInformationProvider
    {
        /// <summary>
        /// Gets the current user ID.
        /// </summary>
        /// <returns>Returns the user ID or null.</returns>
        string GetUserID();

        /// <summary>
        /// Gets the maximum length for user IDs.
        /// </summary>
        int MaxLengthForUserID { get; }
    }
}
