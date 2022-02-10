using System;
using System.Linq;
using IntelligentData.Errors;
using IntelligentData.Extensions;
using Microsoft.EntityFrameworkCore.Metadata;

namespace IntelligentData.Internal
{
    internal class EntityInfo
    {
        public Type EntityType { get; }
        
        public IKey? EntityKey { get; }
        
        public IEntityType EntityModel { get; }

        public bool HasPrimaryKey => EntityKey?.Properties.Any() ?? false;

        public bool SimplePrimaryKey => EntityKey?.Properties.Count == 1;

        public object?[] GetPrimaryKey(object entity)
        {
            if (!HasPrimaryKey) return Array.Empty<object>();
            
            if (!EntityType.IsInstanceOfType(entity)) throw new InvalidCastException();

            return EntityKey!.Properties.Select(x => x.GetValue(entity)).ToArray();
        }

        public bool HasDefaultPrimaryKey(object entity)
        {
            if (!HasPrimaryKey) return true;

            foreach (var prop in EntityKey!.Properties)
            {
                var o = prop.GetValue(entity);
                var t = prop.GetValueType();

                if (t == typeof(string))
                {
                    // any blank string or null is considered a default value.
                    if (!string.IsNullOrWhiteSpace(o?.ToString()))
                        return false;
                } 
                else if (t.IsClass)
                {
                    // a null reference for a class is considered the default.
                    if (o is not null)
                        return false;
                }
                else if (t.IsValueType)
                {
                    // default for values is a new instance of the struct.
                    var d = Activator.CreateInstance(t)!;
                    if (!d.Equals(o))
                        return false;
                }
            }

            return true;
        }
        
        public EntityInfo(IntelligentDbContext ctx, Type t)
        {
            EntityType  = t;
            EntityModel = ctx.Model.FindEntityType(t) ?? throw new EntityMissingFromModelException(t);
            EntityKey   = EntityModel.FindPrimaryKey();
        }
        
    }
}
