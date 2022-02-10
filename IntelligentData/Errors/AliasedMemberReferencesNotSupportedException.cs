using System;

namespace IntelligentData.Errors
{
    public class AliasedMemberReferencesNotSupportedException : NotSupportedException, IIntelligentDataException
    {
        public AliasedMemberReferencesNotSupportedException()
            : base("The current SQL provider does not support aliased member references.")
        {
        
        }
    }
}
