using System;

namespace IntelligentData.Interfaces
{
    /// <summary>
    /// A generic interface for user information providers.
    /// </summary>
    public interface IUserInformationProvider
    {
        /// <summary>
        /// Gets the current user ID.
        /// </summary>
        /// <param name="ofType">The type we want the user ID to be returned as (or null for actual value).</param>
        /// <returns>Returns the ID of the current user.</returns>
        object GetUserID(Type ofType);
        
        /// <summary>
        /// Gets the current user name.
        /// </summary>
        /// <returns>Returns the user name of the current user.</returns>
        string GetUserName();
    }
}
