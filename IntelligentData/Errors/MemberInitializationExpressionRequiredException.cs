using System.Linq.Expressions;

namespace IntelligentData.Errors
{
    public class MemberInitializationExpressionRequiredException : SpecificExpressionRequiredException
    {
    
        public MemberInitializationExpressionRequiredException(Expression invalidExpression, string message = "A member initialization expression is required.")
            : base(invalidExpression, null, message)
        {
        }
    }
}
