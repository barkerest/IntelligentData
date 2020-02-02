using IntelligentData.Interfaces;

namespace IntelligentData.Tests.Examples
{
    public class ExampleUserInformationProvider : IUserInformationProviderInt32
    {
        public enum Users
        {
            JohnSmith = 6543,
            JaneDoe = 9876
        }

        public Users CurrentUser { get; set; } = Users.JohnSmith;
        
        public string GetUserName() => CurrentUser.ToString();
        
        public int MaxLengthForUserName { get; } = 64;

        public int GetUserID() => (int)CurrentUser;
    }
}
