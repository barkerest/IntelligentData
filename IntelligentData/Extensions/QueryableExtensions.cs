using System;
using System.Collections;
using System.Collections.Generic;
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

            setter.Invoke(obj, new object[] {value});
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

        #region Patching IN expressions

        private static void InPatch_Expression(RelationalQueryContext queryContext, SqlExpression expression)
        {
            if (expression is null) return;

            switch (expression)
            {
                case InExpression inExpression:
                    ProcessInExpression(queryContext, inExpression);
                    break;

                case SqlUnaryExpression sqlUnaryExpression:
                    InPatch_Expression(queryContext, sqlUnaryExpression.Operand);
                    break;

                case CaseExpression caseExpression:
                    foreach (var whenClause in caseExpression.WhenClauses)
                    {
                        InPatch_Expression(queryContext, whenClause.Result);
                        InPatch_Expression(queryContext, whenClause.Test);
                    }

                    break;

                case ExistsExpression existsExpression:
                    InPatch_Select(queryContext, existsExpression.Subquery);
                    break;

                case LikeExpression likeExpression:
                    InPatch_Expression(queryContext, likeExpression.Match);
                    InPatch_Expression(queryContext, likeExpression.Pattern);
                    InPatch_Expression(queryContext, likeExpression.EscapeChar);
                    break;

                case RowNumberExpression rowNumberExpression:
                    if (rowNumberExpression.Orderings != null)
                    {
                        foreach (var order in rowNumberExpression.Orderings)
                        {
                            InPatch_Expression(queryContext, order.Expression);
                        }
                    }

                    if (rowNumberExpression.Partitions != null)
                    {
                        foreach (var partition in rowNumberExpression.Partitions)
                        {
                            InPatch_Expression(queryContext, partition);
                        }
                    }

                    break;

                case ScalarSubqueryExpression scalarSubqueryExpression:
                    InPatch_Select(queryContext, scalarSubqueryExpression.Subquery);
                    break;

                case SqlBinaryExpression sqlBinaryExpression:
                    InPatch_Expression(queryContext, sqlBinaryExpression.Left);
                    InPatch_Expression(queryContext, sqlBinaryExpression.Right);
                    break;

                case SqlFunctionExpression sqlFunctionExpression:
                    if (sqlFunctionExpression.Arguments != null)
                    {
                        foreach (var argument in sqlFunctionExpression.Arguments)
                        {
                            InPatch_Expression(queryContext, argument);
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

        private static void ProcessInExpression(RelationalQueryContext queryContext, InExpression expression)
        {
            InPatch_Expression(queryContext, expression.Item);
            
            if (expression.Subquery != null)
            {
                InPatch_Select(queryContext, expression.Subquery);
            }

            if (expression.Values is null) return;

            // The version of VisitIn in EF Core 3.1.1 has two requirements.
            // 1) The Values must be from a SqlConstantExpression
            // 2) The Value from the SqlConstantExpression must be castable to IEnumerable<object>
            
            // To do this, we'll modify the InExpression Values property directly.
            var type = expression.GetType();
            var prop = type.GetProperty("Values")
                       ?? throw new InvalidOperationException("Missing Values property.");    // should never happen.
            
            // Try getting the setter from the property if available.
            var setter = prop.GetSetMethod(true);
            
            // If there is no setter, check for the backing field.
            FieldInfo fld = null;
            if (setter is null)
            {
                fld = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                              .FirstOrDefault(f => f.Name.Contains($"<{prop.Name}>", StringComparison.OrdinalIgnoreCase));
            }
            
            // If we don't have a setter or a backing field, we can't continue.
            if (setter is null && fld is null) 
                throw new InvalidOperationException("Values is missing setter and no backing field found.");
            
            var currentValue = expression.Values;

            switch (currentValue)
            {
                case SqlParameterExpression paramEx:
                {
                    // Fix issue 1 & 2 by grabbing the parameter and converting to a constant IEnumerable<object>.
                    var value = queryContext.ParameterValues[paramEx.Name];
                    var newVal = (value as IEnumerable)?.Cast<object>().ToArray() ?? new object[0];
                    var newEx = new SqlConstantExpression(Expression.Constant(newVal), paramEx.TypeMapping);
                    if (fld != null)
                    {
                        fld.SetValue(expression, newEx);
                    }
                    else
                    {
                        setter.Invoke(expression, new object[] {newEx});
                    }

                    break;
                }
                
                case SqlConstantExpression sqlConstEx:
                {
                    // Fix issue 2, castable to IEnumerable<object>
                    var constEx = sqlConstEx.PrivateField<ConstantExpression>("_constantExpression");
                    var newVal  = ((IEnumerable) constEx.Value).Cast<object>().ToArray();
                    var newEx = new SqlConstantExpression(Expression.Constant(newVal), sqlConstEx.TypeMapping);
                    if (fld != null)
                    {
                        fld.SetValue(expression, newEx);
                    }
                    else
                    {
                        setter.Invoke(expression, new object[] {newEx});
                    }
                    break;
                }
                    
                default:
                    throw new InvalidOperationException($"Don't know how to convert {currentValue.GetType()} to SqlConstantExpression.");
            }
        }

        private static void InPatch_Select(RelationalQueryContext queryContext, SelectExpression expression)
        {
            InPatch_Expression(queryContext, expression.Having);
            InPatch_Expression(queryContext, expression.Limit);
            InPatch_Expression(queryContext, expression.Offset);
            InPatch_Expression(queryContext, expression.Predicate);

            if (expression.Orderings != null)
            {
                foreach (var order in expression.Orderings)
                {
                    InPatch_Expression(queryContext, order.Expression);
                }
            }

            if (expression.Projection != null)
            {
                foreach (var projection in expression.Projection)
                {
                    InPatch_Expression(queryContext, projection.Expression);
                }
            }

            if (expression.Tables != null)
            {
                foreach (var table in expression.Tables.OfType<SelectExpression>())
                {
                    InPatch_Select(queryContext, table);
                }
            }

            if (expression.GroupBy != null)
            {
                foreach (var groupBy in expression.GroupBy)
                {
                    InPatch_Expression(queryContext, groupBy);
                }
            }
        }

        #endregion

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

            // ReSharper disable once EF1001
            var enumerator = provider.Execute<IEnumerable>(query.Expression).GetEnumerator();

            var commandCache = enumerator.PrivateField<RelationalCommandCache>("_relationalCommandCache");

            if (commandCache is null)
            {
                value = "Does not appear to be a relational command.";
                return false;
            }

            var queryContext = enumerator.PrivateField<RelationalQueryContext>("_relationalQueryContext");
            if (queryContext is null)
            {
                value = "Does not provide a query context.";
                return false;
            }

            var selectExpression = commandCache.PrivateField<SelectExpression>("_selectExpression");
            if (selectExpression is null)
            {
                value = "Does not provide a select expression.";
                return false;
            }

            InPatch_Select(queryContext, selectExpression);

            var factory = commandCache.PrivateField<IQuerySqlGeneratorFactory>("_querySqlGeneratorFactory");
            if (factory is null)
            {
                value = "Does not provide a SQL generator factory.";
                return false;
            }

            var generator = factory.Create();
            var command   = generator.GetCommand(selectExpression);
            value = command.CommandText;

            return true;
        }
    }
}
