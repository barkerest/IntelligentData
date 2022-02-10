using IntelligentData.Enums;

namespace IntelligentData.Extensions
{
    public static class UpdateResultExtension
    {
        /// <summary>
        /// Is the result successful?
        /// </summary>
        /// <param name="result"></param>
        /// <returns>Returns true if the result is successful.</returns>
        public static bool Successful(this UpdateResult result)
            => result is UpdateResult.Success or UpdateResult.SuccessNoChanges;
        
    }
}