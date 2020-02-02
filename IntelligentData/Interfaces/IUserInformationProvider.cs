using System;

namespace IntelligentData.Interfaces
{
    /// <summary>
    /// A generic interface for user information providers.
    /// </summary>
    public interface IUserInformationProvider<TUserId>
    {
        /// <summary>
        /// Gets the current user ID.
        /// </summary>
        /// <returns>Returns the ID of the current user.</returns>
        TUserId GetUserID();
        
        /// <summary>
        /// Gets the current user name.
        /// </summary>
        /// <returns>Returns the user name of the current user.</returns>
        string GetUserName();
    }
}
