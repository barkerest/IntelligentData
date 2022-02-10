using System;
using System.Linq.Expressions;

namespace IntelligentData.Errors
{
    public abstract class SpecificExpressionRequiredException : ArgumentException, IIntelligentDataException
    {
        public Expression? InvalidExpression { get; }
    
        public Expression? ParentExpression { get; }

        protected SpecificExpressionRequiredException(Expression? invalidExpression, Expression? parentExpression, string message)
            : base(message)
        {
            InvalidExpression = invalidExpression;
            ParentExpression  = parentExpression;
        }
    }
}
