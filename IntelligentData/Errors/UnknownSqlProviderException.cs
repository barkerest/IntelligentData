using System;

namespace IntelligentData.Errors
{
    public class UnknownSqlProviderException : ArgumentException, IIntelligentDataException
    {
        public UnknownSqlProviderException(string? providerName)
            : base($"There is no knowledge available for the {providerName} provider.")
        {
        
        }
    }
}
