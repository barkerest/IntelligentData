using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text;
using IntelligentData.Extensions;
using IntelligentData.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IntelligentData.Attributes
{
    /// <summary>
    /// Defines a single property index.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class IndexAttribute : ValidationAttribute, IPropertyCustomizer
    {
        /// <summary>
        /// Is this a unique index?
        /// </summary>
        public bool Unique { get; set; }

        /// <inheritdoc />
        public EntityTypeBuilder Customize(EntityTypeBuilder builder, string property)
        {
            var ib = builder.HasIndex(property);
            if (Unique) ib.IsUnique();
            return builder;
        }

        private static readonly IDictionary<Type, (string, List<Func<object,object>>)> Queries
            = new Dictionary<Type, (string, List<Func<object, object>>)>();

        public override bool RequiresValidationContext { get; } = true;

        /// <inheritdoc />
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is null) return ValidationResult.Success;
            if (validationContext.ObjectInstance is null) return ValidationResult.Success;
            
            if (validationContext.TryGetDbContext(out var context))
            {
                var conn = context.Database.GetDbConnection();
                string query = null;
                List<Func<object, object>> args = null;

                lock (Queries)
                {
                    if (Queries.ContainsKey(validationContext.ObjectType))
                    {
                        (query, args) = Queries[validationContext.ObjectType];
                    }
                }
                
                if (string.IsNullOrEmpty(query) &&
                    context.Model.FindEntityType(validationContext.ObjectType) is IEntityType entityType &&
                    entityType.FindPrimaryKey() is IKey primaryKey &&
                    entityType.FindProperty(validationContext.MemberName) is IProperty member)
                {
                    var tableName = entityType.GetTableName();
                    var knowledge = SqlKnowledge.For(conn);
                    var sql       = new StringBuilder();
                    args      = new List<Func<object, object>>();

                    sql.Append("SELECT COUNT(*) FROM ");
                    sql.Append(knowledge.QuoteObjectName(tableName));
                    sql.Append(" WHERE (");

                    if (validationContext.ObjectInstance != null)
                    {
                        var first = true;
                        foreach (var prop in primaryKey.Properties)
                        {
                            if (!first) sql.Append(" OR ");
                            first = false;
                            sql.Append(knowledge.QuoteObjectName(prop.GetColumnName()));
                            sql.Append(" <> @p_").Append(args.Count);
                            if (prop.PropertyInfo != null)
                            {
                                args.Add(x => prop.PropertyInfo.GetValue(x));
                            }
                            else
                            {
                                args.Add(x => prop.FieldInfo.GetValue(x));
                            }
                        }

                        sql.Append(") AND (");
                    }

                    sql.Append(knowledge.QuoteObjectName(member.Name))
                       .Append(" = @p_")
                       .Append(args.Count);

                    if (member.PropertyInfo != null)
                    {
                        args.Add(x => member.PropertyInfo.GetValue(x));
                    }
                    else
                    {
                        args.Add(x => member.FieldInfo.GetValue(x));
                    }

                    sql.Append(')');
                    query = sql.ToString();

                    if (!string.IsNullOrEmpty(query))
                    {
                        lock (Queries)
                        {
                            Queries[validationContext.ObjectType] = (query, args);
                        }
                    }
                }
                
                if (string.IsNullOrEmpty(query)) return ValidationResult.Success;
                
                if (conn.State == ConnectionState.Broken ||
                    conn.State == ConnectionState.Closed) conn.Open();

                var cmd = conn.CreateCommand();
                cmd.CommandText = query;
                for (var i = 0; i < args.Count; i++)
                {
                    var arg = args[i];
                    var p = cmd.CreateParameter();
                    p.ParameterName = $"@p_{i}";
                    p.Value = arg(validationContext.ObjectInstance);
                    cmd.Parameters.Add(p);
                }

                var cnt = (int) cmd.ExecuteScalar();

                if (cnt > 0)
                {
                    return new ValidationResult("has already been used", new[] {validationContext.MemberName});
                }
            }
            
            return ValidationResult.Success;
        }
        
    }
}
