using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using IntelligentData.Interfaces;
using IntelligentData.Internal;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;

namespace IntelligentData.Extensions
{
    public static class ValidationExtensions
    {
        private static readonly IDictionary<Type, Type> EntityDbContextMap = new Dictionary<Type, Type>();

        /// <summary>
        /// Tries to get the DbContext for the ObjectInstance from the ValidationContext.
        /// </summary>
        /// <param name="validationContext"></param>
        /// <param name="dbContext"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public static bool TryGetDbContext(this ValidationContext validationContext, out DbContext dbContext)
        {
            if (validationContext is null) throw new ArgumentNullException(nameof(validationContext));
            if (validationContext.ObjectType is null) throw new ArgumentException("ObjectType cannot be null.");

            Type contextType = null;

            dbContext = null;

            lock (EntityDbContextMap)
            {
                if (EntityDbContextMap.ContainsKey(validationContext.ObjectType))
                {
                    contextType = EntityDbContextMap[validationContext.ObjectType];
                }
            }

            if (contextType != null)
            {
                dbContext = (DbContext) validationContext.GetRequiredService(contextType);
                return (dbContext != null);
            }

            if (validationContext.ObjectType.IsGenericType &&
                validationContext.ObjectType.GetGenericTypeDefinition() == typeof(IntelligentEntity<>))
            {
                // intelligent entities link to a single context type.
                contextType = validationContext.ObjectType.GetGenericArguments()[0];
            }
            else
            {
                // otherwise we need to search for a DbContext with a DbSet<> for our ObjectType.
                var baseType = typeof(DbContext);

                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (asm.IsDynamic ||
                        asm.ReflectionOnly) continue;

                    foreach (var type in asm.GetTypes().Where(x => baseType.IsAssignableFrom(x)))
                    {
                        if (type.IsAbstract) continue;

                        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                        if (props.Any(
                            x => x.PropertyType.IsGenericType &&
                                 x.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>) &&
                                 x.PropertyType.GetGenericArguments()[0] == validationContext.ObjectType
                        ))
                        {
                            contextType = type;
                            break;
                        }
                    }

                    if (contextType != null) break;
                }
            }

            if (contextType != null)
            {
                lock (EntityDbContextMap)
                {
                    EntityDbContextMap[validationContext.ObjectType] = contextType;
                }

                dbContext = (DbContext) validationContext.GetRequiredService(contextType);
                return (dbContext != null);
            }

            return false;
        }

        /// <summary>
        /// Creates a COUNT(*) query based on the object being validated.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="excludeCurrentItem"></param>
        /// <param name="matchingProperties"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public static ICustomCommand CreateFilteredCountQuery(this ValidationContext context, bool excludeCurrentItem, params string[] matchingProperties)
        {
            if (context is null) throw new ArgumentNullException(nameof(context));
            if (matchingProperties is null) throw new ArgumentNullException(nameof(matchingProperties));
            if (matchingProperties.Length < 1) throw new ArgumentException("At least one matching property must be supplied.");
            if (!context.TryGetDbContext(out var ctx)) throw new ArgumentException("No DB context is available to construct the SQL.");
            if (!(ctx.Model.FindEntityType(context.ObjectType) is IEntityType entityType)) throw new ArgumentException($"The {context.ObjectType} type is not part of the {ctx.GetType()} model.");

            var primaryKey = entityType.FindPrimaryKey();
            var properties = matchingProperties
                             .Select(x => new {Name = x, Property = entityType.FindProperty(x)})
                             .ToArray();

            var mismatch = properties.Where(x => x.Property is null).ToArray();
            if (mismatch.Any())
            {
                throw new ArgumentException("The following types do not exist in the entity type: " + string.Join(", ", mismatch.Select(x => x.Name)));
            }

            if (excludeCurrentItem && primaryKey is null)
            {
                throw new ArgumentException($"The {context.ObjectType} type does not have a primary key to exclude the current item.");
            }

            var tableName = entityType.GetTableName();
            var knowledge = SqlKnowledge.For(ctx.Database.ProviderName)
                            ?? throw new ArgumentException("No connection available from the DB context.");

            var sql  = new StringBuilder();
            var args = new List<Func<object, object>>();

            sql.Append("SELECT COUNT(*) FROM ")
               .Append(knowledge.QuoteObjectName(tableName))
               .Append(" WHERE (");

            var first = true;

            if (excludeCurrentItem)
            {
                foreach (var keyProp in primaryKey.Properties)
                {
                    if (!first) sql.Append(" OR ");
                    first = false;
                    sql.Append('(')
                       .Append(knowledge.QuoteObjectName(keyProp.GetColumnName()))
                       .Append(" <> @p_")
                       .Append(args.Count)
                       .Append(')');
                    if (keyProp.PropertyInfo != null)
                    {
                        args.Add(x => keyProp.PropertyInfo.GetValue(x));
                    }
                    else
                    {
                        args.Add(x => keyProp.FieldInfo.GetValue(x));
                    }
                }
            }

            foreach (var prop in properties.Select(x => x.Property))
            {
                if (!first) sql.Append(") AND (");
                first = false;
                
                sql.Append(knowledge.QuoteObjectName(prop.GetColumnName()))
                   .Append(" = @p_")
                   .Append(args.Count);

                if (prop.PropertyInfo != null)
                {
                    args.Add(x => prop.PropertyInfo.GetValue(x));
                }
                else
                {
                    args.Add(x => prop.FieldInfo.GetValue(x));
                }
            }

            sql.Append(')');

            return new CustomCommand(context.ObjectType, sql.ToString(), args.ToArray());
        }
    }
}
