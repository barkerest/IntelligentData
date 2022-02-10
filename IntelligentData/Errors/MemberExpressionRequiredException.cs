using System.Linq.Expressions;

namespace IntelligentData.Errors
{
    public class MemberExpressionRequiredException : SpecificExpressionRequiredException
    {
        public MemberExpressionRequiredException(Expression? invalidExpression, Expression? parentExpression = null, string message = "A member expression is required.")
            : base(invalidExpression, parentExpression, message)
        {
         
        }
    }
}