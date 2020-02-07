namespace IntelligentData.Tests.Examples
{
    // This entity does not provide an Access attribute or IEntityAccessProvider implementation
    // so the access level for this entity will be the default for the context.
    public class DefaultAccessEntity
    {
        public int ID { get; set; }
        
        public string Name { get; set; }
    }
}
