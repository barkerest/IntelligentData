using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using IntelligentData.Extensions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace IntelligentData.Internal
{
    internal static class InExpressionPatch
    {
        internal static void PatchInExpressions(this SelectExpression expression, RelationalQueryContext context, List<string>? usedParams = null)
        {
            expression.Having?.PatchInExpressions(context, usedParams);
            expression.Limit?.PatchInExpressions(context, usedParams);
            expression.Offset?.PatchInExpressions(context, usedParams);
            expression.Predicate?.PatchInExpressions(context, usedParams);

            foreach (var order in expression.Orderings)
            {
                order.Expression.PatchInExpressions(context, usedParams);
            }
        
            foreach (var projection in expression.Projection)
            {
                projection.Expression.PatchInExpressions(context, usedParams);
            }
        
            foreach (var table in expression.Tables)
            {
                table.PatchInExpressions(context, usedParams);
            }
        
            foreach (var groupBy in expression.GroupBy)
            {
                groupBy.PatchInExpressions(context, usedParams);
            }
        }

        private static void PatchInExpressions(this InExpression expression, RelationalQueryContext context, List<string>? usedParams = null)
        {
            expression.Item.PatchInExpressions(context, usedParams);

            expression.Subquery?.PatchInExpressions(context, usedParams);

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
                    if (usedParams is not null &&
                        !usedParams.Contains(paramEx.Name))
                    {
                        usedParams.Add(paramEx.Name);
                    }
                    
                    var newVal = (value as IEnumerable)?.Cast<object>().ToArray() ?? Array.Empty<object>();
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
                    var newVal  = (constEx?.Value as IEnumerable)?.Cast<object>().ToArray() ?? Array.Empty<object>();
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

        private static void PatchInExpressions(this TableExpressionBase expression, RelationalQueryContext context, List<string>? usedParams = null)
        {
            switch (expression)
            {
                case TableExpression _:
                case FromSqlExpression _:
                    break;
                
                case SelectExpression selectExpression:
                    selectExpression.PatchInExpressions(context, usedParams);
                    break;
                
                case JoinExpressionBase joinExpression:
                    joinExpression.Table.PatchInExpressions(context, usedParams);
                    break;
                
                case SetOperationBase setOperation:
                    setOperation.Source1.PatchInExpressions(context, usedParams);
                    setOperation.Source2.PatchInExpressions(context, usedParams);
                    break;
                
                default:
                    throw new InvalidOperationException($"Unknown table expression type: {expression.GetType()}");
            }
        }

        private static void PatchInExpressions(this SqlExpression expression, RelationalQueryContext context, List<string>? usedParams = null)
        {
            switch (expression)
            {
                case InExpression inExpression:
                    inExpression.PatchInExpressions(context, usedParams);
                    break;

                case SqlUnaryExpression sqlUnaryExpression:
                    sqlUnaryExpression.Operand.PatchInExpressions(context, usedParams);
                    break;

                case CaseExpression caseExpression:
                    foreach (var whenClause in caseExpression.WhenClauses)
                    {
                        whenClause.Result.PatchInExpressions(context, usedParams);
                        whenClause.Test.PatchInExpressions(context, usedParams);
                    }
                    caseExpression.ElseResult?.PatchInExpressions(context, usedParams);
                    break;

                case ExistsExpression existsExpression:
                    existsExpression.Subquery.PatchInExpressions(context, usedParams);
                    break;

                case LikeExpression likeExpression:
                    likeExpression.Match.PatchInExpressions(context, usedParams);
                    likeExpression.Pattern.PatchInExpressions(context, usedParams);
                    likeExpression.EscapeChar?.PatchInExpressions(context, usedParams);
                    break;

                case RowNumberExpression rowNumberExpression:
                    foreach (var order in rowNumberExpression.Orderings)
                    {
                        order.Expression.PatchInExpressions(context, usedParams);
                    }
                    
                    foreach (var partition in rowNumberExpression.Partitions)
                    {
                        partition.PatchInExpressions(context, usedParams);
                    }
                    break;

                case ScalarSubqueryExpression scalarSubqueryExpression:
                    scalarSubqueryExpression.Subquery.PatchInExpressions(context, usedParams);
                    break;

                case SqlBinaryExpression sqlBinaryExpression:
                    sqlBinaryExpression.Left.PatchInExpressions(context, usedParams);
                    sqlBinaryExpression.Right.PatchInExpressions(context, usedParams);
                    break;

                case SqlFunctionExpression sqlFunctionExpression:
                    if (sqlFunctionExpression.Arguments is not null)
                    {
                        foreach (var argument in sqlFunctionExpression.Arguments)
                        {
                            argument.PatchInExpressions(context, usedParams);
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
