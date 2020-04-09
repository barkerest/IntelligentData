using System.ComponentModel.DataAnnotations;
using IntelligentData.Interfaces;

namespace IntelligentData.Internal
{
    /// <summary>
    /// The backing type for the default implementation of temporary lists.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TempListEntry<T> : ITempListEntry<T>
    {
        /// <inheritdoc />
        public int ListId { get; set; }

        /// <inheritdoc />
        public T EntryValue { get; set; }
    }
}
