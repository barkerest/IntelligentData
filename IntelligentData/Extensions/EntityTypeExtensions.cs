using Microsoft.EntityFrameworkCore.Metadata;

namespace IntelligentData.Extensions
{
    public static class EntityTypeExtensions
    {
        private static string? AnnotationValue(IReadOnlyEntityType entityType, string annotationName)
        {
            if (entityType.FindAnnotation(annotationName) is { Value: string value } &&
                !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            return null;
        }

        /// <summary>
        /// Returns the StoreObjectIdentifier for the EntityType. 
        /// </summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        /// <remarks>
        /// To determine the object type we look at the annotations EF added to the entity type.
        /// If we find one set, we return the appropriate StoreObjectIdentifier, otherwise we return null.
        ///
        /// This is just my opinion, but the EF Core developers should have included a utility method like this.
        /// In fact, the StoreObjectIdentifier.Create() method seems to almost do this as long as the type specified
        /// matches the annotated type.  We just take it a step further and check the annotations first and then
        /// create the appropriate StoreObjectIdentifier from there.
        /// </remarks>
        public static StoreObjectIdentifier? GetStoreObjectIdentifier(this IReadOnlyEntityType entityType)
        {
            if (AnnotationValue(entityType, RelationalAnnotationNames.TableName) is { } tableName)
            {
                return StoreObjectIdentifier.Table(tableName, AnnotationValue(entityType, RelationalAnnotationNames.Schema));
            }

            if (AnnotationValue(entityType, RelationalAnnotationNames.ViewName) is { } viewName)
            {
                return StoreObjectIdentifier.View(viewName, AnnotationValue(entityType, RelationalAnnotationNames.ViewSchema));
            }

            if (AnnotationValue(entityType, RelationalAnnotationNames.FunctionName) is { } funcName)
            {
                return StoreObjectIdentifier.DbFunction(funcName);
            }

            if (AnnotationValue(entityType, RelationalAnnotationNames.SqlQuery) is { } sqlQuery)
            {
                return StoreObjectIdentifier.SqlQuery(sqlQuery);
            }

            return null;
        }
        
    }
}
