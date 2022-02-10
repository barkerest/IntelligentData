using System;

namespace IntelligentData.Errors
{
    public class UnnamedDatabaseProviderException : InvalidOperationException, IIntelligentDataException
    {
        public UnnamedDatabaseProviderException()
            : base("The database has an unnamed provider.")
        {
        }
    }
}
