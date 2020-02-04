namespace IntelligentData.Interfaces
{
    /// <summary>
    /// A common entity with a 64-bit row version property.
    /// </summary>
    public interface IVersionedEntity
    {
        /// <summary>
        /// Gets the current row version for the entity.
        /// </summary>
        /// <remarks>
        /// This will be automatically incremented during concurrent update requests.
        /// </remarks>
        long? RowVersion { get; set; }
    }
}
