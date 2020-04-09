using System;
using System.Collections.Generic;
using IntelligentData.Interfaces;

namespace IntelligentData.Internal
{
    /// <summary>
    /// Generic type conversions that should usually be valid.
    /// </summary>
    public class AnsiSqlTypeProvider : ISqlTypeNameProvider
    {
        /// <summary>
        /// The known value types.
        /// </summary>
        protected Dictionary<Type, string> KnownTypes { get; } = new Dictionary<Type, string>()
        {
            {typeof(bool), "BOOLEAN"},
            {typeof(byte), "SMALLINT"},
            {typeof(sbyte), "SMALLINT"},
            {typeof(short), "SMALLINT"},
            {typeof(ushort), "SMALLINT"},
            {typeof(int), "INTEGER"},
            {typeof(uint), "INTEGER"},
            {typeof(long), "BIGINT"},
            {typeof(ulong), "BIGINT"},
            {typeof(decimal), "DECIMAL({1}, {2})"},
            {typeof(float), "REAL"},
            {typeof(double), "FLOAT"},
            {typeof(DateTime), "DATE"},
            {typeof(string), "VARCHAR({0})"},
            {typeof(byte[]), "VARBINARY({0})"},
            {typeof(ReadOnlySpan<byte>), "VARBINARY({0})"},
            {typeof(Guid), "CHAR(36)"},
        };

        /// <inheritdoc />
        public string GetValueTypeName(Type type, int maxLength = 0, int precision = 0, int scale = 0)
        {
            if (!KnownTypes.ContainsKey(type)) throw new InvalidCastException();
            return string.Format(KnownTypes[type], maxLength, precision, scale);
        }
    }
}
