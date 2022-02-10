using System;

namespace IntelligentData.Errors
{
    public abstract class CannotConvertToDeleteException : InvalidOperationException, IIntelligentDataException
    {
        protected CannotConvertToDeleteException(string message)
            : base(message)
        {
        
        }
    }
}
