using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;

namespace IntelligentData.Internal
{
    internal static class InExpressionPatch
    {
        internal static SelectExpression PatchInExpressions(this SelectExpression expression, RelationalQueryContext context, List<string>? usedParams = null)
        {
            return
                expression.Update(
                    expression.Projection.Select(e => e.Update(e.Expression.PatchInExpressions(context, usedParams))).ToArray(),
                    expression.Tables.Select(e => e.PatchInExpressions(context, usedParams)).ToArray(),
                    expression.Predicate?.PatchInExpressions(context, usedParams),
                    expression.GroupBy.Select(e => e.PatchInExpressions(context, usedParams)).ToArray(),
                    expression.Having?.PatchInExpressions(context, usedParams),
                    expression.Orderings.Select(e => e.Update(e.Expression.PatchInExpressions(context, usedParams))).ToArray(),
                    expression.Limit?.PatchInExpressions(context, usedParams),
                    expression.Offset?.PatchInExpressions(context, usedParams)
                );
        }

        private static InExpression PatchInExpressions(this InExpression expression, RelationalQueryContext context, List<string>? usedParams = null)
        {
            var item     = expression.Item.PatchInExpressions(context, usedParams);
            var subquery = expression.Subquery?.PatchInExpressions(context, usedParams);
            
            // VisitIn has two requirements for the Values expression.
            // 1) The Values must castable to a SqlConstantExpression
            // 2) The Value from the SqlConstantExpression must be castable to IEnumerable<object>

            switch (expression.Values)
            {
                case null:
                    return expression.Update(item, null, subquery);
                
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
                    return expression.Update(
                        item,
                        new SqlConstantExpression(Expression.Constant(newVal), paramEx.TypeMapping),
                        subquery
                    );
                }

                case SqlConstantExpression sqlConstEx:
                {
                    // Fix issue 2, castable to IEnumerable<object>
                    
                    var newVal  = (sqlConstEx.Value as IEnumerable)?.Cast<object>().ToArray() ?? Array.Empty<object>();
                    return expression.Update(
                        item,
                        new SqlConstantExpression(Expression.Constant(newVal), sqlConstEx.TypeMapping),
                        subquery
                    );
                }

                default:
                    throw new InvalidOperationException($"Don't know how to convert {expression.Values.GetType()} to SqlConstantExpression.");
            }
        }

        private static TableExpressionBase PatchInExpressions(this TableExpressionBase expression, RelationalQueryContext context, List<string>? usedParams = null)
        {
            switch (expression)
            {
                case TableExpression _:
                case FromSqlExpression _:
                    return expression;

                case SelectExpression selectExpression:
                    return selectExpression.PatchInExpressions(context, usedParams);

                case TableValuedFunctionExpression tableValuedFunctionExpression:
                    return tableValuedFunctionExpression.Update(
                        tableValuedFunctionExpression.Arguments.Select(e => e.PatchInExpressions(context, usedParams)).ToArray()
                    );
                
                case CrossJoinExpression crossJoinExpression:
                    return crossJoinExpression.Update(
                        crossJoinExpression.Table.PatchInExpressions(context, usedParams)
                    );
                    
                case InnerJoinExpression innerJoinExpression:
                    return innerJoinExpression.Update(
                        innerJoinExpression.Table.PatchInExpressions(context, usedParams),
                        innerJoinExpression.JoinPredicate.PatchInExpressions(context, usedParams)
                    );
                    
                case LeftJoinExpression leftJoinExpression:
                    return leftJoinExpression.Update(
                        leftJoinExpression.Table.PatchInExpressions(context, usedParams),
                        leftJoinExpression.JoinPredicate.PatchInExpressions(context, usedParams)
                    );
                
                case OuterApplyExpression outerApplyExpression:
                    return outerApplyExpression.Update(
                        outerApplyExpression.Table.PatchInExpressions(context, usedParams)
                    );

                case CrossApplyExpression crossApplyExpression:
                    return crossApplyExpression.Update(
                        crossApplyExpression.Table.PatchInExpressions(context, usedParams)
                    );
                    
                case ExceptExpression exceptExpression:
                    return exceptExpression.Update(
                        exceptExpression.Source1.PatchInExpressions(context, usedParams),
                        exceptExpression.Source2.PatchInExpressions(context, usedParams)
                    );
                
                case IntersectExpression intersectExpression:
                    return intersectExpression.Update(
                        intersectExpression.Source1.PatchInExpressions(context, usedParams),
                        intersectExpression.Source2.PatchInExpressions(context, usedParams)
                    );
                
                case UnionExpression unionExpression:
                    return unionExpression.Update(
                        unionExpression.Source1.PatchInExpressions(context, usedParams),
                        unionExpression.Source2.PatchInExpressions(context, usedParams)
                    );
                
                default:
                    throw new InvalidOperationException($"Unknown table expression type: {expression.GetType()}");
            }
        }

        private static SqlExpression PatchInExpressions(this SqlExpression expression, RelationalQueryContext context, List<string>? usedParams = null)
        {
            switch (expression)
            {
                case InExpression inExpression:
                    return inExpression.PatchInExpressions(context, usedParams);

                case SqlUnaryExpression sqlUnaryExpression:
                    return sqlUnaryExpression.Update(
                        sqlUnaryExpression.Operand.PatchInExpressions(context, usedParams)
                    );

                case CaseExpression caseExpression:
                    return caseExpression.Update(
                        caseExpression.Operand?.PatchInExpressions(context, usedParams),
                        caseExpression.WhenClauses.Select(
                                          e => new CaseWhenClause(
                                              e.Test.PatchInExpressions(context, usedParams),
                                              e.Result.PatchInExpressions(context, usedParams)
                                          )
                                      )
                                      .ToArray(),
                        caseExpression.ElseResult?.PatchInExpressions(context, usedParams)
                    );

                case ExistsExpression existsExpression:
                    return existsExpression.Update(
                        existsExpression.Subquery.PatchInExpressions(context, usedParams)
                    );

                case LikeExpression likeExpression:
                    return likeExpression.Update(
                        likeExpression.Match.PatchInExpressions(context, usedParams),
                        likeExpression.Pattern.PatchInExpressions(context, usedParams),
                        likeExpression.EscapeChar?.PatchInExpressions(context, usedParams)
                    );

                case RowNumberExpression rowNumberExpression:
                    return rowNumberExpression.Update(
                        rowNumberExpression.Partitions.Select(e => e.PatchInExpressions(context, usedParams)).ToArray(),
                        rowNumberExpression.Orderings.Select(
                                               e => new OrderingExpression(
                                                   e.Expression.PatchInExpressions(context, usedParams),
                                                   e.IsAscending
                                               )
                                           )
                                           .ToArray()
                    );

                case ScalarSubqueryExpression scalarSubqueryExpression:
                    return scalarSubqueryExpression.Update(
                        scalarSubqueryExpression.Subquery.PatchInExpressions(context, usedParams)
                    );

                case SqlBinaryExpression sqlBinaryExpression:
                    return sqlBinaryExpression.Update(
                        sqlBinaryExpression.Left.PatchInExpressions(context, usedParams),
                        sqlBinaryExpression.Right.PatchInExpressions(context, usedParams)
                    );

                case SqlFunctionExpression sqlFunctionExpression:
                    return sqlFunctionExpression.Update(
                        sqlFunctionExpression.Instance?.PatchInExpressions(context, usedParams),
                        sqlFunctionExpression.Arguments?.Select(e => e.PatchInExpressions(context, usedParams)).ToArray()
                    );

                case SqlFragmentExpression _:
                case ColumnExpression _:
                case SqlConstantExpression _:
                case SqlParameterExpression _:
                    return expression;

                default:
                    throw new InvalidOperationException($"Unknown SQL expression type: {expression.GetType()}");
            }
        }
    }
}
