using System;

namespace IntelligentData.Errors
{
    public abstract class CannotConvertToUpdateException : InvalidOperationException, IIntelligentDataException
    {
        protected CannotConvertToUpdateException(string message)
            : base(message)
        {
        
        }
    }
}
