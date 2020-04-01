using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
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
    }
}
