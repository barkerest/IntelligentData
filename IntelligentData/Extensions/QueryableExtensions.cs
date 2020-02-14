using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Storage;

namespace IntelligentData.Extensions
{
    /// <summary>
    /// Extension methods for queryable interfaces.
    /// </summary>
    public static class QueryableExtensions
    {
        private static object PrivateField(this object obj, string fieldName)
            => obj?.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(obj);

        private static T PrivateField<T>(this object obj, string fieldName)
            => (T) PrivateField(obj, fieldName);

        private static void SetPrivateField(this object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"Missing {fieldName} field.");
            field.SetValue(obj, value);
        }
        
        private static void SetPropertyPrivately(this object obj, string propName, object value)
        {
            var type = obj.GetType();
            var prop = type.GetProperty(propName, BindingFlags.Instance | BindingFlags.Public)
                       ?? throw new InvalidOperationException($"Missing public {propName} property.");
            var setter = prop.GetSetMethod(true)
                         ?? throw new InvalidOperationException($"Missing non-public setter on {propName} property.");

            setter.Invoke(obj, new object[]{value});
        }

        /// <summary>
        /// Gets the SQL string from this query.
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static string GetSqlString(this IQueryable query)
        {
            var result = TryGetSqlString(query, out var value);
            if (!result) throw new InvalidOperationException(value);
            return value;
        }

        private static void InPatch_Expression(SqlExpression expression)
        {
            if (expression is null) return;
            
            switch (expression)
            {
                case InExpression inExpression:
                    ProcessInExpression(inExpression);
                    break;
                
                case SqlUnaryExpression sqlUnaryExpression:
                    InPatch_Expression(sqlUnaryExpression.Operand);
                    break;
                
                case CaseExpression caseExpression:
                    foreach (var whenClause in caseExpression.WhenClauses)
                    {
                        InPatch_Expression(whenClause.Result);
                        InPatch_Expression(whenClause.Test);
                    }
                    break;

                case ExistsExpression existsExpression:
                    InPatch_Select(existsExpression.Subquery);
                    break;
                
                case LikeExpression likeExpression:
                    InPatch_Expression(likeExpression.Match);
                    InPatch_Expression(likeExpression.Pattern);
                    InPatch_Expression(likeExpression.EscapeChar);
                    break;
                
                case RowNumberExpression rowNumberExpression:
                    if (rowNumberExpression.Orderings != null)
                    {
                        foreach (var order in rowNumberExpression.Orderings)
                        {
                            InPatch_Expression(order.Expression);
                        }
                    }

                    if (rowNumberExpression.Partitions != null)
                    {
                        foreach (var partition in rowNumberExpression.Partitions)
                        {
                            InPatch_Expression(partition);
                        }
                    }
                    break;
                
                case ScalarSubqueryExpression scalarSubqueryExpression:
                    InPatch_Select(scalarSubqueryExpression.Subquery);
                    break;
                
                case SqlBinaryExpression sqlBinaryExpression:
                    InPatch_Expression(sqlBinaryExpression.Left);
                    InPatch_Expression(sqlBinaryExpression.Right);
                    break;
                
                case SqlFunctionExpression sqlFunctionExpression:
                    if (sqlFunctionExpression.Arguments != null)
                    {
                        foreach (var argument in sqlFunctionExpression.Arguments)
                        {
                            InPatch_Expression(argument);
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

        private static void ProcessInExpression(InExpression expression)
        {
            if (expression.Subquery != null)
            {
                InPatch_Select(expression.Subquery);
            }
            
            if (expression.Values is null) return;

            // The version of VisitIn in EF Core 3.1.1 has two requirements.
            // 1) The Values must be from a SqlConstantExpression
            // 2) The Value from the SqlConstantExpression must be castable to IEnumerable<object>

            var vals = expression.Values;
            switch (vals)
            {
                case SqlParameterExpression paramEx:
                    
                    
                    break;
                case SqlConstantExpression sqlConstEx:
                    // Fix issue 2, castable to IEnumerable<object>
                    var constEx = sqlConstEx.PrivateField<ConstantExpression>("_constantExpression");
                    var newVal = ((IEnumerable) constEx.Value).Cast<object>().ToArray();
                    SetPrivateField(
                        sqlConstEx,
                        "_constantExpression",
                        Expression.Constant(newVal));
                    
                    break;
                default:
                    throw new InvalidOperationException($"Don't know how to convert {vals.GetType()} to SqlConstantExpression.");
            }

        }

        private static void InPatch_Select(SelectExpression expression)
        {
            InPatch_Expression(expression.Having);
            InPatch_Expression(expression.Limit);
            InPatch_Expression(expression.Offset);
            InPatch_Expression(expression.Predicate);
            
            if (expression.Orderings != null)
            {
                foreach (var order in expression.Orderings)
                {
                    InPatch_Expression(order.Expression);
                }
            }

            if (expression.Projection != null)
            {
                foreach (var projection in expression.Projection)
                {
                    InPatch_Expression(projection.Expression);
                }
            }

            if (expression.Tables != null)
            {
                foreach (var table in expression.Tables.OfType<SelectExpression>())
                {
                    InPatch_Select(table);
                }
            }

            if (expression.GroupBy != null)
            {
                foreach (var groupBy in expression.GroupBy)
                {
                    InPatch_Expression(groupBy);
                }
            }
        }
        
        
        /// <summary>
        /// Tries to get the SQL string from this query.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool TryGetSqlString(this IQueryable query, out string value)
        {
            value = "";

            var provider = query.Provider as EntityQueryProvider;
            if (provider is null)
            {
                value = "Does not use an entity query provider.";
                return false;
            }

            var enumerator   = provider.Execute<IEnumerable>(query.Expression).GetEnumerator();
            
            var commandCache = enumerator.PrivateField<RelationalCommandCache>("_relationalCommandCache");
            
            if (commandCache is null)
            {
                value = "Does not appear to be a relational command.";
                return false;
            }

            var selectExpression = commandCache.PrivateField<SelectExpression>("_selectExpression");
            if (selectExpression is null)
            {
                value = "Does not provide a select expression.";
                return false;
            }

            InPatch_Select(selectExpression);

            var factory = commandCache.PrivateField<IQuerySqlGeneratorFactory>("_querySqlGeneratorFactory");
            if (factory is null)
            {
                value = "Does not provide a SQL generator factory.";
                return false;
            }

            var generator = factory.Create();
            var command = generator.GetCommand(selectExpression);
            value = command.CommandText;
            
            return true;
        }
    }
}
