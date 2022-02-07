using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace IntelligentData.Extensions
{
    public static class EntityTypeExtensions
    {
        /// <summary>
        /// Returns the StoreObjectIdentifier for the EntityType. 
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        /// <remarks>
        /// This method is ripped straight from the EF core source and made public because it only seems right.
        /// The StoreObjectIdentifier really should be stored as part of the EntityType, but that's just my opinion.
        /// Since they can (and likely will) change the way this works as they move forward, here's the link to the
        /// source for this method.
        /// 
        /// https://github.com/linq2db/linq2db.EntityFrameworkCore/blob/8a635356336e5ad894c9f318524982b7ba3f531b/Source/LinqToDB.EntityFrameworkCore/EFCoreMetadataReader.cs#L450
        /// 
        /// </remarks>
        public static StoreObjectIdentifier? GetStoreObjectIdentifier(this IEntityType entityType)
        {
            return entityType.GetTableName() switch
            {
                not null => StoreObjectIdentifier.Create(entityType, StoreObjectType.Table),
                null => StoreObjectIdentifier.Create(entityType, StoreObjectType.View),
            };
        }
    }
}
