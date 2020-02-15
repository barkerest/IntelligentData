using System;
using IntelligentData.Interfaces;

namespace IntelligentData
{
    /// <summary>
    /// Common SQL knowledge objects.
    /// </summary>
    public class CommonSqlKnowledge : ISqlKnowledge
    {
        /// <inheritdoc />
        public string EngineName { get; }

        /// <inheritdoc />
        public string ObjectOpenQuote { get; }

        /// <inheritdoc />
        public string ObjectCloseQuote { get; }

        /// <inheritdoc />
        public Func<string, string> EscapeObjectName { get; } = null;

        /// <inheritdoc />
        public string ConcatStringBefore { get; }

        /// <inheritdoc />
        public string ConcatStringMid { get; }

        /// <inheritdoc />
        public string ConcatStringAfter { get; }

        /// <inheritdoc />
        public string GetLastInsertedIdCommand { get; }

        /// <inheritdoc />
        public bool DeleteSupportsTableAliases { get; }

        /// <inheritdoc />
        public bool UpdateSupportsTableAliases { get; }

        /// <inheritdoc />
        public bool UpdateSupportsFromClause { get; }

        /// <inheritdoc />
        public override string ToString()
            => EngineName;

        private CommonSqlKnowledge(string name, string open, string close, string insertId, bool deleteAlias, bool updateAlias, bool updateFrom, string concatOp = null, string concatFunc = null)
        {
            EngineName                 = name;
            ObjectOpenQuote            = open;
            ObjectCloseQuote           = close;
            GetLastInsertedIdCommand   = insertId;
            DeleteSupportsTableAliases = deleteAlias;
            UpdateSupportsFromClause   = updateFrom;
            UpdateSupportsTableAliases = updateAlias;
            if (string.IsNullOrEmpty(concatFunc))
            {
                ConcatStringBefore = "";
                ConcatStringAfter  = "";
                ConcatStringMid    = concatOp;
            }
            else
            {
                ConcatStringBefore = concatFunc + "(";
                ConcatStringAfter  = ")";
                ConcatStringMid    = ",";
            }
        }

        /// <summary>
        /// Knowledge for MySQL & MariaDB.
        /// </summary>
        public static readonly CommonSqlKnowledge MySql = new CommonSqlKnowledge(
            "MySQL",
            "`",
            "`",
            "SELECT LAST_INSERT_ID()",
            true,
            true,
            false,
            concatFunc: "CONCAT"
        );

        /// <summary>
        /// Knowledge for SQL server.
        /// </summary>
        public static readonly CommonSqlKnowledge SqlServer = new CommonSqlKnowledge(
            "SQL Server",
            "[",
            "]",
            "SELECT SCOPE_IDENTITY()",
            true,
            true,
            true,
            concatOp: "+"
        );

        /// <summary>
        /// Knowledge for SQLite.
        /// </summary>
        public static readonly CommonSqlKnowledge Sqlite = new CommonSqlKnowledge(
            "SQLite",
            "\"",
            "\"",
            "SELECT last_insert_rowid()",
            false,
            false,
            false,
            concatOp: "||"
        );
    }
}
