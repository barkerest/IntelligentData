using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IntelligentData.Extensions;
using IntelligentData.Interfaces;
using IntelligentData.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.Extensions.Logging;

namespace IntelligentData
{
    /// <summary>
    /// Represents a parameterized SQL statement generated from an IQueryable object.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public class ParameterizedSql<TEntity>
    {
        private static readonly string NotKeyword =
            @"(?:
  (?:
    (?:
      (?!FROM|WHERE|GROUP\s+BY|HAVING|ORDER\s+BY)
      [^`'""\[\(]
    )*
  )|
  (?:`[^`]*`)|
  (?:'[^']*')|
  (?:""[^""]*"")|
  (?:\[
    (?:[^\]]|\]\])*
  \])|
  (((?<POpen>\()[^\(\)]*)+((?<PClose-POpen>\))[^\(\)]*)+)*(?(POpen)(?!))
)"; // note that contents of parens are not further processed and may break if a paren appears within a string inside a paren block.

        private static readonly Regex SelectRipper = new Regex(
            $@"\A\s*
(?<WITH>WITH\s+\{{[^}}]*\}})?
SELECT
\s+
(?<SELECTION>{NotKeyword}*)
\s*
(?<FROM>FROM\s+{NotKeyword}*)?
\s*
(?<WHERE>WHERE\s+{NotKeyword}*)?
\s*
(?<GROUPBY>GROUP\s+BY\s+{NotKeyword}*)?
\s*
(?<HAVING>HAVING\s+{NotKeyword}*)?
\s*
(?<ORDERBY>ORDER\s+BY\s+{NotKeyword}*)?
\s*
\z",
            RegexOptions.IgnorePatternWhitespace | RegexOptions.IgnoreCase
        );

        /// <summary>
        /// The SQL text.
        /// </summary>
        public string SqlText { get; }

        private readonly string _action;
        private readonly string _tableAlias;
        private readonly string _tableName;
        private readonly string _fromClause;
        private readonly string _whereClause;

        /// <summary>
        /// Gets the DB context this parameterized SQL is configured against. 
        /// </summary>
        public DbContext DbContext { get; }

        private readonly ISqlKnowledge              _knowledge;
        private readonly ILogger                    _logger;
        private readonly Dictionary<string, object> _parameterValues;
        private readonly IQueryable<TEntity>        _original;
        private readonly QueryInfo                  _info;
        public           bool                       ReadOnly { get; }


        /// <summary>
        /// The parameters for the SQL text.
        /// </summary>
        public IReadOnlyDictionary<string, object> ParameterValues => _parameterValues;

        private ParameterizedSql(
            ParameterizedSql<TEntity>           source,
            string                              action,
            string                              sql,
            IReadOnlyDictionary<string, object> parameterValues
        )
        {
            SqlText           = sql.Trim();
            _parameterValues  = parameterValues.ToDictionary(x => x.Key, x => x.Value);
            _tableAlias       = source._tableAlias;
            _tableName        = source._tableName;
            _fromClause       = source._fromClause;
            _whereClause      = source._whereClause;
            IsGrouped         = source.IsGrouped;
            HasWithExpression = source.HasWithExpression;
            _action           = action.ToUpper();
            DbContext         = source.DbContext;
            _knowledge        = source._knowledge;
            _logger           = source._logger;
            _original         = source._original;
            _info             = source._info;
            ReadOnly          = source.ReadOnly;
        }

        /// <summary>
        /// Creates a parameterized SQL string from the supplied query.
        /// </summary>
        /// <param name="query"></param>
        /// <param name="logger"></param>
        public ParameterizedSql(IQueryable<TEntity> query, ILogger logger = null)
        {
            _original = query;
            _info     = new QueryInfo(query);
            DbContext = _info.Context.Context;
            _logger = logger
                      ?? ((IInfrastructure<IServiceProvider>) DbContext)
                         .Instance
                         .GetService(typeof(ILogger<ParameterizedSql<TEntity>>)) as ILogger<ParameterizedSql<TEntity>>;

            if (DbContext.Model.FindEntityType(typeof(TEntity)) is null)
            {
                _logger?.LogInformation($"The type {typeof(TEntity)} is not part of the data model, creating read-only SQL.");
                ReadOnly = true;
            }

            _knowledge = SqlKnowledge.For(DbContext.Database.GetDbConnection());

            _logger?.LogDebug("Generating SQL...");

            var cmd = _info.Command;
            SqlText = cmd.CommandText;
            var paramNames = cmd.Parameters.Select(x => x.InvariantName).ToArray();
            _parameterValues = _info.Context
                                    .ParameterValues
                                    .Where(v => paramNames.Contains(v.Key))
                                    .ToDictionary(
                                        v => "@" + v.Key.TrimStart('@'),
                                        v => v.Value
                                    );
            _logger?.LogDebug(SqlText);
            _logger?.LogDebug("Params: " + string.Join(", ", _parameterValues.Select(x => x.Key)));

            var rip = SelectRipper.Match(SqlText);
            if (!rip.Success)
                throw new ArgumentException(
                    "The generated SQL does not match the extraction regular expression.\n" + SqlText
                );

            _action           = "SELECT";
            _fromClause       = rip.Groups["FROM"].Value.Trim();
            _whereClause      = rip.Groups["WHERE"].Value.Trim();
            IsGrouped         = !string.IsNullOrWhiteSpace(rip.Groups["GROUPBY"].Value);
            HasWithExpression = !string.IsNullOrWhiteSpace(rip.Groups["WITH"].Value);

            var sel        = rip.Groups["SELECTION"].Value.Trim();
            var aliasSplit = sel.IndexOf('.');
            if (aliasSplit <= 0)
            {
                _tableAlias = "";
            }
            else
            {
                var tbl = sel.Substring(0, aliasSplit);
                _tableAlias = _knowledge.UnquoteObjectName(tbl);
            }

            if (!ReadOnly)
            {
                if (string.IsNullOrEmpty(_tableAlias))
                {
                    _tableAlias = _info.Expression.Tables.First().Alias;
                }

                var tex = _info.Expression.Tables.FirstOrDefault(x => x.Alias == _tableAlias);
                if (tex is null)
                {
                    ReadOnly = true;
                    _logger?.LogInformation($"Failed to locate record set named {_tableAlias}.");
                }
                else if (tex is TableExpression tt)
                {
                    if (!string.IsNullOrEmpty(tt.Schema) &&
                        tt.Schema != "dbo")
                    {
                        _tableName = _knowledge.QuoteObjectName(tt.Schema) + '.' + _knowledge.QuoteObjectName(tt.Name);
                    }
                    else
                    {
                        _tableName = _knowledge.QuoteObjectName(tt.Name);
                    }
                }
                else
                {
                    ReadOnly = true;
                    _logger?.LogInformation($"Failed to locate a table expression named {_tableAlias}.");
                }
            }
        }

        /// <summary>
        /// Is this a SELECT statement.
        /// </summary>
        public bool IsSelect => _action.Equals("SELECT");

        /// <summary>
        /// Is this select statement grouping?
        /// </summary>
        public bool IsGrouped { get; }

        /// <summary>
        /// Is this select statement prefixed with a WITH expression?
        /// </summary>
        public bool HasWithExpression { get; }

        /// <summary>
        /// Is this a DELETE statement.
        /// </summary>
        public bool IsDelete => _action.Equals("DELETE");

        /// <summary>
        /// Is this a UPDATE statement.
        /// </summary>
        public bool IsUpdate => _action.Equals("UPDATE");

        private string GenerateUnAliasedWhereClause()
        {
            if (string.IsNullOrWhiteSpace(_whereClause)) return "";

            var ctx = DbContext ?? throw new InvalidOperationException("No current context.");
            var model = ctx.Model.FindEntityType(typeof(TEntity)) ??
                        throw new InvalidOperationException("Entity missing from context model.");

            var result = new StringBuilder();

            result.Append("WHERE ");

            var keys = model.FindPrimaryKey().Properties.Select(x => _knowledge.QuoteObjectName(x.GetColumnName())).ToArray();
            if (keys.Length == 1)
            {
                result.Append(keys[0]);
            }
            else
            {
                result.Append(_knowledge.ConcatValues(keys));
            }

            var subAlias = _knowledge.QuoteObjectName(_tableAlias) + ".";

            result.Append(" IN (SELECT ");

            if (keys.Length == 1)
            {
                result.Append(subAlias).Append(keys[0]);
            }
            else
            {
                result.Append(_knowledge.ConcatValues(keys.Select(x => subAlias + x).ToArray()));
            }

            result.Append(' ').Append(_fromClause).Append(' ').Append(_whereClause).Append(')');

            return result.ToString();
        }

        /// <summary>
        /// Converts the SQL into a DELETE statement.
        /// </summary>
        /// <returns></returns>
        public ParameterizedSql<TEntity> ToDelete()
        {
            if (ReadOnly)
                throw new InvalidOperationException("The DELETE SQL cannot be automatically generated from this statement.");
            if (!IsSelect)
                throw new InvalidOperationException("Only SELECT statements can be converted to DELETE statements.");
            if (IsGrouped)
                throw new InvalidOperationException(
                    "An aggregate SELECT statement cannot be converted to a DELETE statement."
                );
            if (HasWithExpression)
                throw new InvalidOperationException(
                    "A SELECT statement with a WITH clause cannot be converted to a DELETE statement."
                );

            string sql;

            if (_knowledge.DeleteSupportsTableAliases)
            {
                sql = $"DELETE {_knowledge.QuoteObjectName(_tableAlias)} {_fromClause} {_whereClause}".Trim();
            }
            else
            {
                sql = $"DELETE FROM {_tableName} {GenerateUnAliasedWhereClause()}".Trim();
            }

            _logger?.LogDebug("Generating DELETE SQL");
            _logger?.LogDebug(sql);

            return new ParameterizedSql<TEntity>(
                this,
                "DELETE",
                sql,
                ParameterValues
            );
        }

        private string ColumnNameFor(IProperty prop)
        {
            return _knowledge.QuoteObjectName(prop.GetColumnName());
        }

        /// <summary>
        /// Converts the SQL into an UPDATE statement.
        /// </summary>
        /// <param name="update">The field or fields to update.</param>
        /// <returns></returns>
        public ParameterizedSql<TEntity> ToUpdate(Expression<Func<TEntity, TEntity>> update)
        {
            if (ReadOnly)
                throw new InvalidOperationException("The UPDATE SQL cannot be automatically generated from this statement.");
            if (!IsSelect)
                throw new InvalidOperationException("Only SELECT statements can be converted to UPDATE statements.");
            if (IsGrouped)
                throw new InvalidOperationException("An aggregate SELECT statement cannot be converted to an UPDATE statement.");
            if (HasWithExpression)
                throw new InvalidOperationException("A SELECT statement with a WITH clause cannot be converted to an UPDATE statement.");

            if (!(update.Body is MemberInitExpression initExpression))
                throw new InvalidOperationException("The update expression must be a member initialization expression.");

            var paramValues = ParameterValues.ToDictionary(x => x.Key, x => x.Value);
            var builder     = new StringBuilder();

            _logger?.LogDebug("Generating UPDATE SQL");

            var context = DbContext ??
                          throw new InvalidOperationException("No current DbContext available.");

            var entityType = context.Model.FindEntityType(typeof(TEntity)) ??
                             throw new InvalidOperationException("Failed to locate entity type.");

            builder.Append("UPDATE ");

            string subAlias;
            if (_knowledge.UpdateSupportsTableAliases)
            {
                if (_knowledge.UpdateSupportsFromClause)
                {
                    // UPDATE supports aliases and a FROM clause.
                    builder.Append(_knowledge.QuoteObjectName(_tableAlias)).Append(" SET ");
                }
                else
                {
                    // UPDATE supports aliases, but not the from clause (MySQL, MariaDB)
                    // The tables and aliases are specified immediately following the UPDATE keyword, so we can
                    // trim off the FROM keyword and just append the rest of the from clause.
                    builder.Append(_fromClause.Substring(5)).Append(" SET ");
                }

                subAlias = _knowledge.QuoteObjectName(_tableAlias) + ".";
            }
            else
            {
                builder.Append(_tableName).Append(" SET ");
                subAlias = "";
            }

            bool first = true;
            foreach (var binding in initExpression.Bindings)
            {
                if (!(binding is MemberAssignment assignment))
                    throw new InvalidOperationException("Only simple assignment bindings are supported in the update expression.");

                if (!first) builder.Append(", ");
                first = false;

                var prop = entityType.FindProperty(assignment.Member.Name) ??
                           throw new InvalidOperationException(
                               $"The entity does not contain a {assignment.Member.Name} property."
                           );

                builder.Append(subAlias);

                builder.Append(ColumnNameFor(prop)).Append(" = ");

                AddColumnValue(assignment.Expression, builder, paramValues, context, entityType);
            }

            if (_knowledge.UpdateSupportsTableAliases)
            {
                if (_knowledge.UpdateSupportsFromClause)
                {
                    builder.Append(' ').Append(_fromClause);
                }

                builder.Append(' ').Append(_whereClause);
            }
            else
            {
                builder.Append(' ').Append(GenerateUnAliasedWhereClause());
            }

            var sql = builder.ToString().Trim();
            _logger?.LogDebug(sql);

            return new ParameterizedSql<TEntity>(
                this,
                "UPDATE",
                sql,
                paramValues
            );
        }

        private static bool IsModelMemberExpression(MemberExpression expr)
        {
            while (true)
            {
                switch (expr.Expression)
                {
                    case ParameterExpression _:
                        return true;
                    case MemberExpression m:
                        expr = m;
                        continue;
                }

                return false;
            }
        }

        private string ComputeColumnName(DbContext context, IEntityType entityType, MemberExpression expr)
        {
            if (expr.Expression is ParameterExpression parameterExpression)
            {
                var prop = entityType.FindProperty(expr.Member.Name) ??
                           throw new InvalidOperationException($"The model does not contain a property named {expr.Member.Name}.");
                return _knowledge.UpdateSupportsTableAliases
                           ? $"{_knowledge.QuoteObjectName(_tableAlias)}.{ColumnNameFor(prop)}"
                           : ColumnNameFor(prop);
            }

            if (!_knowledge.UpdateSupportsTableAliases) throw new InvalidOperationException("Current engine does not support aliased member references.");

            var colEx = expr;

            var alias = new StringBuilder();

            while (!(expr.Expression is ParameterExpression))
            {
                expr = expr.Expression as MemberExpression ??
                       throw new InvalidOperationException("Expected member expression.");

                if (alias.Length > 0) alias.Insert(0, '.');
                alias.Insert(0, expr.Member.Name);
            }

            alias.Insert(0, '.');
            alias.Insert(0, _tableAlias);

            var entity = context.Model.FindEntityType(colEx.Type) ??
                         throw new InvalidOperationException($"The {colEx.Type} model is not part of this data model.");
            var entityProp = entity.FindProperty(colEx.Member.Name) ??
                             throw new InvalidOperationException(
                                 $"The {colEx.Member.Name} property is not part of the {colEx.Type} model."
                             );

            return $"{_knowledge.QuoteObjectName(_tableAlias)}.{ColumnNameFor(entityProp)}";
        }

        private object ComputeValue(MemberExpression expr)
        {
            PropertyInfo propInfo  = expr.Member as PropertyInfo;
            FieldInfo    fieldInfo = expr.Member as FieldInfo;

            if (propInfo is null &&
                fieldInfo is null)
                throw new InvalidOperationException("Member is not a field or property.");

            if (expr.Expression is null)
            {
                if (propInfo is null) return fieldInfo.GetValue(null);
                return propInfo.GetValue(null);
            }

            if (expr.Expression is ConstantExpression constantExpression)
            {
                var value = constantExpression.Value;
                if (value is null) return null;
                if (propInfo is null) return fieldInfo.GetValue(value);
                return propInfo.GetValue(value);
            }

            if (expr.Expression is MemberExpression memberExpression)
            {
                var value = ComputeValue(memberExpression);
                if (value is null) return null;
                if (propInfo is null) return fieldInfo.GetValue(value);
                return propInfo.GetValue(value);
            }

            throw new InvalidOperationException($"Cannot compute value of {expr.GetType()} expression.");
        }

        private void AddColumnValue(
            Expression  expr, StringBuilder builder, IDictionary<string, object> paramValues, DbContext context,
            IEntityType entityType
        )
        {
            string nextParamName;

            switch (expr)
            {
                case ConstantExpression constantExpression:
                    var constantValue = constantExpression.Value;

                    if (constantValue is null)
                    {
                        builder.Append("NULL");
                    }
                    else if (constantValue is bool constantBool)
                    {
                        builder.Append(constantBool ? '1' : '0');
                    }
                    else if (constantValue is string constantString)
                    {
                        builder.Append('\'').Append(constantString.Replace("'", "''")).Append('\'');
                    }
                    else if (constantValue.GetType().IsPrimitive)
                    {
                        builder.Append(constantValue);
                    }
                    else
                    {
                        nextParamName = $"@__constant_{paramValues.Count}";
                        paramValues.Add(nextParamName, constantExpression.Value);
                        builder.Append(nextParamName);
                    }

                    break;
                case UnaryExpression unaryExpression:
                    switch (unaryExpression.NodeType)
                    {
                        case ExpressionType.Not:
                            builder.Append("(~");
                            AddColumnValue(unaryExpression.Operand, builder, paramValues, context, entityType);
                            builder.Append(')');
                            break;
                        case ExpressionType.Negate:
                            builder.Append("(-");
                            AddColumnValue(unaryExpression.Operand, builder, paramValues, context, entityType);
                            builder.Append(')');
                            break;
                        case ExpressionType.Convert:
                            AddColumnValue(unaryExpression.Operand, builder, paramValues, context, entityType);
                            break;
                        default:
                            throw new InvalidOperationException($"Invalid unary operation: {unaryExpression.NodeType}");
                    }

                    break;
                case BinaryExpression binaryExpression:
                    if (binaryExpression.Type == typeof(string) &&
                        (binaryExpression.NodeType == ExpressionType.Add ||
                         binaryExpression.NodeType == ExpressionType.AddChecked))
                    {
                        // concatenation.
                        if (string.IsNullOrEmpty(_knowledge.ConcatStringBefore))
                        {
                            builder.Append('(');
                        }
                        else
                        {
                            builder.Append(_knowledge.ConcatStringBefore);
                        }

                        AddColumnValue(binaryExpression.Left, builder, paramValues, context, entityType);

                        if (_knowledge.ConcatStringMid == ",")
                        {
                            builder.Append(", ");
                        }
                        else
                        {
                            builder.Append(' ').Append(_knowledge.ConcatStringMid).Append(' ');
                        }

                        AddColumnValue(binaryExpression.Right, builder, paramValues, context, entityType);

                        if (string.IsNullOrEmpty(_knowledge.ConcatStringBefore))
                        {
                            builder.Append(')');
                        }
                        else
                        {
                            builder.Append(_knowledge.ConcatStringAfter);
                        }
                    }
                    else
                    {
                        builder.Append('(');
                        AddColumnValue(binaryExpression.Left, builder, paramValues, context, entityType);
                        switch (binaryExpression.NodeType)
                        {
                            case ExpressionType.Add:
                            case ExpressionType.AddChecked:
                                // math.
                                builder.Append(" + ");
                                break;

                            case ExpressionType.Subtract:
                            case ExpressionType.SubtractChecked:
                                builder.Append(" - ");
                                break;

                            case ExpressionType.Multiply:
                            case ExpressionType.MultiplyChecked:
                                builder.Append(" * ");
                                break;

                            case ExpressionType.Divide:
                                builder.Append(" / ");
                                break;

                            case ExpressionType.Modulo:
                                builder.Append(" % ");
                                break;

                            case ExpressionType.LeftShift:
                                builder.Append(" << ");
                                break;

                            case ExpressionType.RightShift:
                                builder.Append(" >> ");
                                break;

                            case ExpressionType.And:
                                builder.Append(" & ");
                                break;

                            case ExpressionType.Or:
                                builder.Append(" | ");
                                break;

                            case ExpressionType.ExclusiveOr:
                                builder.Append(" ^ ");
                                break;

                            default:
                                throw new InvalidOperationException(
                                    $"Invalid unary operation: {binaryExpression.NodeType}"
                                );
                        }

                        AddColumnValue(binaryExpression.Right, builder, paramValues, context, entityType);
                        builder.Append(')');
                    }

                    break;

                case MemberExpression memberExpression:
                    if (IsModelMemberExpression(memberExpression))
                    {
                        // reference from the existing table structure.
                        builder.Append(ComputeColumnName(context, entityType, memberExpression));
                    }
                    else
                    {
                        // it's a parameter.
                        nextParamName = $"@__{memberExpression.Member.Name}_{paramValues.Count}";
                        paramValues.Add(nextParamName, ComputeValue(memberExpression));
                        builder.Append(nextParamName);
                    }

                    break;
            }
        }

        /// <summary>
        /// Executes the SQL and returns the number of rows affected.
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public int ExecuteNonQuery(DbTransaction transaction = null)
        {
            var context = DbContext ?? throw new InvalidOperationException("No current DbContext.");

            var conn = context.Database.GetDbConnection();
            if (conn.State == ConnectionState.Broken ||
                conn.State == ConnectionState.Closed) conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = SqlText;
            if (transaction != null)
            {
                cmd.Transaction = transaction;
            }

            foreach (var pv in ParameterValues)
            {
                var p = cmd.CreateParameter();
                p.ParameterName = pv.Key;
                p.Value         = pv.Value;
                cmd.Parameters.Add(p);
            }

            _logger?.LogInformation("Executing Command:\r\n" + SqlText);

            return cmd.ExecuteNonQuery();
        }


        /// <summary>
        /// Executes the SQL and returns the number of rows affected.
        /// </summary>
        /// <param name="transaction"></param>
        /// <returns></returns>
        public Task<int> ExecuteNonQueryAsync(DbTransaction transaction = null)
        {
            var context = DbContext ?? throw new InvalidOperationException("No current DbContext.");

            var conn = context.Database.GetDbConnection();
            if (conn.State == ConnectionState.Broken ||
                conn.State == ConnectionState.Closed) conn.Open();

            var cmd = conn.CreateCommand();
            cmd.CommandText = SqlText;
            if (transaction != null)
            {
                cmd.Transaction = transaction;
            }

            foreach (var pv in ParameterValues)
            {
                var p = cmd.CreateParameter();
                p.ParameterName = pv.Key;
                p.Value         = pv.Value;
                cmd.Parameters.Add(p);
            }

            _logger?.LogInformation("Executing Command:\r\n" + SqlText);

            return cmd.ExecuteNonQueryAsync();
        }

        /// <summary>
        /// Creates a formattable string from this parameterized SQL. 
        /// </summary>
        /// <returns></returns>
        public FormattableString ToFormattableString()
        {
            var r = new Regex(@"@[A-Z_][A-Z0-9_]+", RegexOptions.IgnoreCase);

            var list = new List<object>();
            var fmt = r.Replace(
                SqlText, (m) =>
                {
                    var placeholder = $"{{{list.Count}}}";
                    if (ParameterValues.ContainsKey(m.Value))
                    {
                        list.Add(ParameterValues[m.Value]);
                        return placeholder;
                    }

                    if (ParameterValues.ContainsKey(m.Value.Substring(1)))
                    {
                        list.Add(ParameterValues[m.Value.Substring(1)]);
                        return placeholder;
                    }

                    return m.Value;
                }
            );

            return FormattableStringFactory.Create(fmt, list.ToArray());
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return SqlText;
        }
    }
}
