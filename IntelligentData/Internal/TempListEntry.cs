using System;
using IntelligentData.Interfaces;

namespace IntelligentData.Internal
{
    /// <summary>
    /// The backing type for the default implementation of temporary lists.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class TempListEntry<T> : ITempListEntry<T>
    {
        private static T? _blankValue;

        private static T BlankValue
        {
            get
            {
                if (_blankValue is null)
                {
                    if (typeof(T) == typeof(string))
                    {
                        _blankValue = (T)(object)string.Empty;
                    }
                    else
                    {
                        _blankValue = Activator.CreateInstance<T>();
                    }
                }

                return _blankValue;
            }
        }

        public TempListEntry()
        {
            EntryValue = BlankValue;
        }

        /// <inheritdoc />
        public int ListId { get; set; }

        /// <inheritdoc />
        public T EntryValue { get; set; }
    }
}
