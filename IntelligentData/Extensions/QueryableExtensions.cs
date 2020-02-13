using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;
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

        private static void PrivateSet(this object obj, string propName, object value)
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
            switch (expression)
            {
                case SqlUnaryExpression sqlUnaryExpression:
                    {
                        if (sqlUnaryExpression.Operand is InExpression inex)
                        {
                                sqlUnaryExpression.PrivateSet("Operand", ProcessInExpression(inex));
                        }
                        else
                        {
                                InPatch_Expression(sqlUnaryExpression.Operand);
                        }
                    }
                    break;
                
                case CaseExpression caseExpression:
                    foreach (var c in caseExpression.WhenClauses)
                    {
                        if (c.Test is InExpression inex)
                        {
                            c.PrivateSet("Test", ProcessInExpression(inex));
                        }
                        else
                        {
                            InPatch_Expression(c.Test);
                        }
                    }

                    break;

                case ExistsExpression existsExpression:
                case LikeExpression likeExpression:
                case RowNumberExpression rowNumberExpression:
                case ScalarSubqueryExpression scalarSubqueryExpression:
                case SqlBinaryExpression sqlBinaryExpression:
                case SqlFragmentExpression sqlFragmentExpression:
                case SqlFunctionExpression sqlFunctionExpression:
                    break;
                
                case ColumnExpression _:
                case SqlConstantExpression _:
                case SqlParameterExpression _:
                    break;
                default:
                    throw new InvalidOperationException($"Unknown expression type: {expression.GetType()}");
            }
        }

        private static InExpression ProcessInExpression(InExpression expression)
        {
            // TODO: Process!!
            return expression;
        }
        
        private static void InPatch_Having(SelectExpression expression)
        {
            if (expression.Having is null) return;

            if (expression.Having is InExpression inex)
            {
                expression.PrivateSet("Having", ProcessInExpression(inex));
            }
            else
            {
                InPatch_Expression(expression.Having);
            }
        }

        private static void InPatch_Predicate(SelectExpression expression)
        {
            if (expression.Predicate is null) return;

            if (expression.Predicate is InExpression inex)
            {
                expression.PrivateSet("Predicate", ProcessInExpression(inex));
            }
            else
            {
                InPatch_Expression(expression.Predicate);
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

            InPatch_Having(selectExpression);
            InPatch_Predicate(selectExpression);
            
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
