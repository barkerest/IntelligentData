using System;
using System.Collections;
using System.Linq;
using System.Linq.Expressions;
using IntelligentData.Extensions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace IntelligentData.Internal
{
    internal static class InExpressionPatch
    {
        internal static void PatchInExpressions(this SelectExpression expression, RelationalQueryContext context)
        {
            if (context is null ||
                expression is null)
            {
                return;
            }

            expression.Having.PatchInExpressions(context);
            expression.Limit.PatchInExpressions(context);
            expression.Offset.PatchInExpressions(context);
            expression.Predicate.PatchInExpressions(context);

            if (expression.Orderings != null)
            {
                foreach (var order in expression.Orderings)
                {
                    order.Expression.PatchInExpressions(context);
                }
            }

            if (expression.Projection != null)
            {
                foreach (var projection in expression.Projection)
                {
                    projection.Expression.PatchInExpressions(context);
                }
            }

            if (expression.Tables != null)
            {
                foreach (var table in expression.Tables)
                {
                    table.PatchInExpressions(context);
                }
            }

            if (expression.GroupBy != null)
            {
                foreach (var groupBy in expression.GroupBy)
                {
                    groupBy.PatchInExpressions(context);
                }
            }
        }

        internal static void PatchInExpressions(this InExpression expression, RelationalQueryContext context)
        {
            if (context is null ||
                expression is null)
            {
                return;
            }

            expression.Item.PatchInExpressions(context);

            expression.Subquery?.PatchInExpressions(context);

            if (expression.Values is null) return;

            // The version of VisitIn in EF Core 3.1.1 has two requirements.
            // 1) The Values must be from a SqlConstantExpression
            // 2) The Value from the SqlConstantExpression must be castable to IEnumerable<object>
            
            var currentValue = expression.Values;

            switch (currentValue)
            {
                case SqlParameterExpression paramEx:
                {
                    // Fix issue 1 & 2 by grabbing the parameter and converting to a constant IEnumerable<object>.
                    var value = context.ParameterValues[paramEx.Name];
                    var newVal = (value as IEnumerable)?.Cast<object>().ToArray() ?? new object[0];
                    var newEx = new SqlConstantExpression(Expression.Constant(newVal), paramEx.TypeMapping);
                    if (!expression.SetNonPublicProperty("Values", newEx))
                    {
                        throw new InvalidOperationException("Could not update Values for InExpression.");
                    }

                    break;
                }
                
                case SqlConstantExpression sqlConstEx:
                {
                    // Fix issue 2, castable to IEnumerable<object>
                    var constEx = sqlConstEx.GetNonPublicField<ConstantExpression>("_constantExpression");
                    var newVal  = ((IEnumerable) constEx.Value).Cast<object>().ToArray();
                    var newEx = new SqlConstantExpression(Expression.Constant(newVal), sqlConstEx.TypeMapping);
                    if (!expression.SetNonPublicProperty("Values", newEx))
                    {
                        throw new InvalidOperationException("Could not update Values for InExpression.");
                    }
                    break;
                }
                    
                default:
                    throw new InvalidOperationException($"Don't know how to convert {currentValue.GetType()} to SqlConstantExpression.");
            }
        }

        internal static void PatchInExpressions(this TableExpressionBase expression, RelationalQueryContext context)
        {
            if (context is null ||
                expression is null)
            {
                return;
            }

            switch (expression)
            {
                case TableExpression _:
                case FromSqlExpression _:
                    break;
                
                case SelectExpression selectExpression:
                    selectExpression.PatchInExpressions(context);
                    break;
                
                case JoinExpressionBase joinExpression:
                    joinExpression.Table.PatchInExpressions(context);
                    break;
                
                case SetOperationBase setOperation:
                    setOperation.Source1.PatchInExpressions(context);
                    setOperation.Source2.PatchInExpressions(context);
                    break;
                
                default:
                    throw new InvalidOperationException($"Unknown table expression type: {expression.GetType()}");
            }
        }

        internal static void PatchInExpressions(this SqlExpression expression, RelationalQueryContext context)
        {
            if (context is null ||
                expression is null)
            {
                return;
            }

            switch (expression)
            {
                case InExpression inExpression:
                    inExpression.PatchInExpressions(context);
                    break;

                case SqlUnaryExpression sqlUnaryExpression:
                    sqlUnaryExpression.Operand.PatchInExpressions(context);
                    break;

                case CaseExpression caseExpression:
                    foreach (var whenClause in caseExpression.WhenClauses)
                    {
                        whenClause.Result.PatchInExpressions(context);
                        whenClause.Test.PatchInExpressions(context);
                    }
                    caseExpression.ElseResult.PatchInExpressions(context);
                    break;

                case ExistsExpression existsExpression:
                    existsExpression.Subquery.PatchInExpressions(context);
                    break;

                case LikeExpression likeExpression:
                    likeExpression.Match.PatchInExpressions(context);
                    likeExpression.Pattern.PatchInExpressions(context);
                    likeExpression.EscapeChar.PatchInExpressions(context);
                    break;

                case RowNumberExpression rowNumberExpression:
                    if (rowNumberExpression.Orderings != null)
                    {
                        foreach (var order in rowNumberExpression.Orderings)
                        {
                            order.Expression.PatchInExpressions(context);
                        }
                    }

                    if (rowNumberExpression.Partitions != null)
                    {
                        foreach (var partition in rowNumberExpression.Partitions)
                        {
                            partition.PatchInExpressions(context);
                        }
                    }

                    break;

                case ScalarSubqueryExpression scalarSubqueryExpression:
                    scalarSubqueryExpression.Subquery.PatchInExpressions(context);
                    break;

                case SqlBinaryExpression sqlBinaryExpression:
                    sqlBinaryExpression.Left.PatchInExpressions(context);
                    sqlBinaryExpression.Right.PatchInExpressions(context);
                    break;

                case SqlFunctionExpression sqlFunctionExpression:
                    if (sqlFunctionExpression.Arguments != null)
                    {
                        foreach (var argument in sqlFunctionExpression.Arguments)
                        {
                            argument.PatchInExpressions(context);
                        }
                    }

                    break;

                case SqlFragmentExpression _:
                case ColumnExpression _:
                case SqlConstantExpression _:
                case SqlParameterExpression _:
                    break;

                default:
                    throw new InvalidOperationException($"Unknown SQL expression type: {expression.GetType()}");
            }
        }
        
    }
}
