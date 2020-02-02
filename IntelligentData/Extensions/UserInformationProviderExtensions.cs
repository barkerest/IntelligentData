using System;
using IntelligentData.Interfaces;

namespace IntelligentData.Extensions
{
    /// <summary>
    /// Extension methods that can be used with any user information provider.
    /// </summary>
    public static class UserInformationProviderExtensions
    {
        /// <summary>
        /// Gets the user ID as a 32-bit integer.
        /// </summary>
        /// <param name="provider"></param>
        /// <returns>Returns the user ID or 0.</returns>
        public static int GetInt32UserID(this IUserInformationProvider provider)
        {
            try
            {
                return (int) provider.GetUserID(typeof(int));
            }
            catch (InvalidCastException)
            {
                return default;
            }
        }

        /// <summary>
        /// Gets the user ID as a 64-bit integer.
        /// </summary>
        /// <param name="provider"></param>
        /// <returns>Returns the user ID or 0.</returns>
        public static long GetInt64UserID(this IUserInformationProvider provider)
        {
            try
            {
                return (long) provider.GetUserID(typeof(long));
            }
            catch (InvalidCastException)
            {
                return default;
            }
        }

        /// <summary>
        /// Gets the user ID as a GUID.
        /// </summary>
        /// <param name="provider"></param>
        /// <returns>Returns the user ID or an empty GUID.</returns>
        public static Guid GetGuidUserID(this IUserInformationProvider provider)
        {
            try
            {
                return (Guid) provider.GetUserID(typeof(Guid));
            }
            catch (InvalidCastException)
            {
                return default;
            }
        }

        /// <summary>
        /// Gets the user ID as a string.
        /// </summary>
        /// <param name="provider"></param>
        /// <returns>Returns the user ID or null.</returns>
        public static string GetStringUserID(this IUserInformationProvider provider)
        {
            try
            {
                return (string) provider.GetUserID(typeof(string));
            }
            catch (InvalidCastException)
            {
                return null;
            }
        }
    }
}
