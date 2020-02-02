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
        /// <exception cref="InvalidCastException">Throws an invalid cast if the ID type cannot be converted to the desired return type.</exception>
        object GetUserID(Type ofType);
        
        /// <summary>
        /// Gets the current user name.
        /// </summary>
        /// <returns>Returns the user name of the current user.</returns>
        string GetUserName();
    }
}
