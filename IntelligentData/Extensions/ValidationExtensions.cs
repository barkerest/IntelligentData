using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using IntelligentData.Errors;
using IntelligentData.Interfaces;
using IntelligentData.Internal;
using Microsoft.EntityFrameworkCore;

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
        public static bool TryGetDbContext(this ValidationContext validationContext, [NotNullWhen(true)] out DbContext? dbContext)
        {
            Type? contextType = null;

            dbContext = null;

            lock (EntityDbContextMap)
            {
                if (EntityDbContextMap.ContainsKey(validationContext.ObjectType))
                {
                    contextType = EntityDbContextMap[validationContext.ObjectType];
                }
            }

            if (contextType is not null)
            {
                dbContext = validationContext.GetService(contextType) as DbContext;
                return (dbContext is not null);
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

                    if (contextType is not null) break;
                }
            }

            if (contextType is not null)
            {
                lock (EntityDbContextMap)
                {
                    EntityDbContextMap[validationContext.ObjectType] = contextType;
                }

                dbContext = validationContext.GetService(contextType) as DbContext;
                return (dbContext is not null);
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
            if (matchingProperties.Length < 1) throw new ArgumentException("At least one matching property must be supplied.");
            if (!context.TryGetDbContext(out var ctx)) throw new ArgumentException("No DB context is available to construct the SQL.");
            if (ctx.Model.FindEntityType(context.ObjectType) is not { } entityType) throw new EntityMissingFromModelException(context.ObjectType);

            var primaryKey = entityType.FindPrimaryKey();
            var properties = matchingProperties
                             .Select(x => new {Name = x, Property = entityType.FindProperty(x)})
                             .ToArray();

            var mismatch = properties.Where(x => x.Property is null).ToArray();
            if (mismatch.Any())
            {
                throw new EntityTypeMissingPropertyException(entityType, mismatch.Select(x => x.Name).ToArray());
            }

            if (excludeCurrentItem && primaryKey is null)
            {
                throw new ArgumentException("The entity type must have a primary key defined to exclude the current item.", nameof(excludeCurrentItem));
            }

            var tableName = entityType.GetTableName()
                            ?? throw new EntityTypeWithoutTableNameException(entityType);
            var knowledge = SqlKnowledge.For(ctx.Database.ProviderName ?? throw new UnnamedDatabaseProviderException())
                            ?? throw new UnknownSqlProviderException(ctx.Database.ProviderName);

            var sql  = new StringBuilder();
            var args = new List<Func<object, object>>();
            var storeId = entityType.GetStoreObjectIdentifier()
                          ?? throw new StoreObjectIdentifierNotFoundException(entityType);
            
            sql.Append("SELECT COUNT(*) FROM ")
               .Append(knowledge.QuoteObjectName(tableName))
               .Append(" WHERE (");

            var first = true;

            if (excludeCurrentItem)
            {
                foreach (var keyProp in primaryKey!.Properties)
                {
                    if (!first) sql.Append(" OR ");
                    first = false;
                    sql.Append('(')
                       .Append(knowledge.QuoteObjectName(keyProp.GetColumnName(storeId) ?? throw new PropertyWithoutColumnNameException(keyProp)))
                       .Append(" <> @p_")
                       .Append(args.Count)
                       .Append(')');
                    
                    args.Add(x => keyProp.GetValue(x) ?? DBNull.Value);
                }
            }

            foreach (var prop in properties.Select(x => x.Property!))
            {
                if (!first) sql.Append(") AND (");
                first = false;
                
                sql.Append(knowledge.QuoteObjectName(prop.GetColumnName(storeId) ?? throw new PropertyWithoutColumnNameException(prop)))
                   .Append(" = @p_")
                   .Append(args.Count);

                args.Add(x => prop.GetValue(x) ?? DBNull.Value);
            }

            sql.Append(')');

            return new CustomCommand(context.ObjectType, sql.ToString(), args.ToArray());
        }
    }
}
