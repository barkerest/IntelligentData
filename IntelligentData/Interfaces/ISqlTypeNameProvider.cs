using System;

namespace IntelligentData.Interfaces
{
    public interface ISqlTypeNameProvider
    {
        /// <summary>
        /// Gets the appropriate type name for the supplied type.
        /// </summary>
        /// <param name="type">The CLR type.</param>
        /// <param name="maxLength">The maximum length for strings or bytes.</param>
        /// <param name="precision">The precision for decimal types.</param>
        /// <param name="scale">The scale for decimal types.</param>
        /// <returns></returns>
        string GetValueTypeName(Type type, int maxLength = 0, int precision = 0, int scale = 0);
    }
}
