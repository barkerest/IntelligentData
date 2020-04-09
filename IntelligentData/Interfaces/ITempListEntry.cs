namespace IntelligentData.Interfaces
{
    /// <summary>
    /// A generic interface for a temporary list backing item.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface ITempListEntry<T>
    {
        /// <summary>
        /// The list ID for the value.
        /// </summary>
        int ListId { get; set; }
        
        /// <summary>
        /// The value.
        /// </summary>
        T EntryValue { get; set; }
    }
}
