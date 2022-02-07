using System;
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata;

namespace IntelligentData.Internal
{
    internal class EntityInfo
    {
        public Type EntityType { get; }
        
        public IKey EntityKey { get; }
        
        public IEntityType EntityModel { get; }

        public bool HasPrimaryKey => EntityKey?.Properties?.Any() ?? false;

        public bool SimplePrimaryKey => EntityKey?.Properties?.Count == 1;

        public object[] GetPrimaryKey(object entity)
        {
            if (entity is null) return null;
            
            if (!HasPrimaryKey) return new object[0];
            
            if (!EntityType.IsInstanceOfType(entity)) throw new InvalidCastException();

            return EntityKey.Properties.Select(x => x.PropertyInfo.GetValue(entity)).ToArray();
        }

        public bool HasDefaultPrimaryKey(object entity)
        {
            if (entity is null) return true;
            if (!HasPrimaryKey) return true;

            foreach (var prop in EntityKey.Properties)
            {
                var o = prop.PropertyInfo.GetValue(entity);

                // default for classes is null, even strings.
                if (prop.PropertyInfo.PropertyType.IsClass && !ReferenceEquals(o, null))
                    return false;

                // default for values is a new instance of the struct.
                if (prop.PropertyInfo.PropertyType.IsValueType)
                {
                    var d = Activator.CreateInstance(prop.PropertyInfo.PropertyType);
                    if (!d.Equals(o))
                        return false;
                }
            }

            return true;
        }
        
        public EntityInfo(IntelligentDbContext ctx, Type t)
        {
            EntityType = t;
            EntityModel = ctx.Model.FindEntityType(t) ?? throw new ArgumentException($"Type {t} is not part of the data model for {ctx.GetType()}.");
            EntityKey = EntityModel.FindPrimaryKey();
        }
        
    }
}
