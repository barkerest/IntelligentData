using System;
using System.Linq.Expressions;

namespace IntelligentData.Errors
{
    public class MemberAssignmentBindingRequiredException : ArgumentException, IIntelligentDataException
    {
        public MemberBinding InvalidBinding { get; }
    
        public MemberAssignmentBindingRequiredException(MemberBinding invalidBinding, string message = "A basic member assignment binding is required.")
            : base(message)
        {
            InvalidBinding = invalidBinding;
        }
    }
}
