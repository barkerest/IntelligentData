using System;

namespace IntelligentData.Internal
{
    /// <summary>
    /// Conversions for SQL Server.
    /// </summary>
    public class MsSqlTypeProvider : AnsiSqlTypeProvider
    {
        public MsSqlTypeProvider()
        {
            KnownTypes[typeof(bool)] = "BIT";
            KnownTypes[typeof(Guid)] = "UNIQUEIDENTIFIER";
        }
    }
}
