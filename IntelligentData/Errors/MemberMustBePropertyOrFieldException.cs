using System;
using System.Reflection;

namespace IntelligentData.Errors
{
    public class MemberMustBePropertyOrFieldException : ArgumentException, IIntelligentDataException
    {
        public MemberInfo MemberInfo { get; }

        public MemberMustBePropertyOrFieldException(MemberInfo memberInfo)
            : base("The member must be a property or field.")
        {
            MemberInfo = memberInfo;
        }
    }
}
