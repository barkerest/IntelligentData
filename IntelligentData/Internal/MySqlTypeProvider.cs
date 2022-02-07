namespace IntelligentData.Internal
{
    /// <summary>
    /// Conversions for MySQL.
    /// </summary>
    public class MySqlTypeProvider : AnsiSqlTypeProvider
    {
        public MySqlTypeProvider()
        {
            KnownTypes[typeof(bool)]  = "TINYINT";
            KnownTypes[typeof(byte)]  = "TINYINT";
            KnownTypes[typeof(sbyte)] = "TINYINT";
        }
    }
}
