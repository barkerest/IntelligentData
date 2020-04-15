using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using IntelligentData.Interfaces;
using IntelligentData.Internal;

namespace IntelligentData
{
    /// <summary>
    /// SQL knowledge object.
    /// </summary>
    public class SqlKnowledge : ISqlKnowledge
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
        public Func<string, string> UnescapeObjectName { get; } = null;

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

        private readonly Regex _provTypePattern;
        private readonly Regex _connTypePattern;

        /// <inheritdoc />
        public bool RelevantForProvider(string providerName)
        {
            if (string.IsNullOrEmpty(providerName)) throw new ArgumentNullException(nameof(providerName));

            return _provTypePattern.IsMatch(providerName);
        }

        /// <inheritdoc />
        public bool RelevantForConnection(IDbConnection connection)
        {
            if (connection is null) throw new ArgumentNullException(nameof(connection));

            return _connTypePattern.IsMatch(connection.GetType().FullName ?? "");
        }

        private readonly ISqlTypeNameProvider _typeNameProvider;

        /// <inheritdoc />
        public string GetValueTypeName(Type type, int maxLength = 0, int precision = 0, int scale = 0)
            => _typeNameProvider.GetValueTypeName(type, maxLength, precision, scale);

        private readonly string _tempTableNamePrefix;

        /// <inheritdoc />
        public string CreateTemporaryTableName(string tableName)
        {
            if (string.IsNullOrEmpty(_tempTableNamePrefix)) return tableName;
            return _tempTableNamePrefix + tableName;
        }

        private readonly string _createTableGuard;
        private readonly string _createTempTableGuard;

        /// <inheritdoc />
        public string GetGuardedCreateTableCommand(string tableName, string body)
            => string.Format(_createTableGuard, tableName, body);

        /// <inheritdoc />
        public string GetCreateTemporaryTableCommand(string tableName, string body)
            => string.Format(_createTempTableGuard, tableName, body);
        
        private SqlKnowledge(string name, string provPattern, string connPattern, string open, string close, string insertId, bool deleteAlias, bool updateAlias, bool updateFrom, string concatOp = null, string concatFunc = null, ISqlTypeNameProvider typeNameProvider = null, string tempTableNamePrefix = null, string guardedCreateTable = null, string tempCreateTable = null)
        {
            EngineName                 = name;
            _provTypePattern           = new Regex(provPattern, RegexOptions.IgnoreCase);
            _connTypePattern           = new Regex(connPattern, RegexOptions.IgnoreCase);
            ObjectOpenQuote            = open;
            ObjectCloseQuote           = close;
            GetLastInsertedIdCommand   = insertId;
            DeleteSupportsTableAliases = deleteAlias;
            UpdateSupportsFromClause   = updateFrom;
            UpdateSupportsTableAliases = updateAlias;

            _createTableGuard = string.IsNullOrEmpty(guardedCreateTable)
                                    ? "CREATE TABLE IF NOT EXISTS {0} {1}"
                                    : guardedCreateTable;

            _createTempTableGuard = string.IsNullOrEmpty(tempCreateTable)
                                        ? "CREATE TEMPORARY TABLE IF NOT EXISTS {0} {1}"
                                        : tempCreateTable;

            _tempTableNamePrefix = tempTableNamePrefix;

            _typeNameProvider = typeNameProvider ?? new AnsiSqlTypeProvider();

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

        private static readonly List<ISqlKnowledge> Known = new List<ISqlKnowledge>()
        {
            new SqlKnowledge(
                "Generic MySQL",
                @"\.(mysql|mariadb)$",
                "(mysql|mariadb)",
                "`",
                "`",
                "SELECT LAST_INSERT_ID()",
                true,
                true,
                false,
                concatFunc: "CONCAT",
                typeNameProvider: new MySqlTypeProvider()
            ),
            new SqlKnowledge(
                "Generic SQL Server",
                @"\.(sqlserver)$",
                @"^(system|microsoft)\.data\.sqlclient",
                "[",
                "]",
                "SELECT SCOPE_IDENTITY()",
                true,
                true,
                true,
                concatOp: "+",
                tempTableNamePrefix: "#",
                guardedCreateTable: "IF OBJECT_ID('{0}') IS NULL BEGIN CREATE TABLE {0} {1} END",
                tempCreateTable: "IF OBJECT_ID('tempdb..{0}') IS NULL BEGIN CREATE TABLE {0} {1} END",
                typeNameProvider: new MsSqlTypeProvider()
            ),
            new SqlKnowledge(
                "Generic SQLite",
                @"\.(sqlite3?)$",
                "(sqlite)",
                "\"",
                "\"",
                "SELECT last_insert_rowid()",
                false,
                false,
                false,
                concatOp: "||"
            ),
        };

        /// <summary>
        /// Gets the SQL knowledge for the supplied provider.
        /// </summary>
        /// <param name="providerName"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static ISqlKnowledge For(string providerName)
        {
            if (string.IsNullOrEmpty(providerName)) throw new ArgumentNullException(nameof(providerName));
            ISqlKnowledge[] known;
            lock (Known)
            {
                known = Known.ToArray();
            }

            return known.FirstOrDefault(x => x.RelevantForProvider(providerName));
        }

        /// <summary>
        /// Gets the SQL knowledge for the supplied connection.
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        public static ISqlKnowledge For(IDbConnection connection)
        {
            if (connection is null) throw new ArgumentNullException(nameof(connection));
            ISqlKnowledge[] known;
            lock (Known)
            {
                known = Known.ToArray();
            }

            return known.FirstOrDefault(x => x.RelevantForConnection(connection));
        }

        /// <summary>
        /// Registers SQL knowledge.
        /// </summary>
        /// <param name="knowledge"></param>
        /// <remarks>
        /// The EngineName property is used to uniquely identify a set of knowledge.
        /// </remarks>
        /// <exception cref="ArgumentNullException"></exception>
        public static void Register(ISqlKnowledge knowledge)
        {
            if (knowledge is null) throw new ArgumentNullException(nameof(knowledge));
            lock (Known)
            {
                if (Known.All(x => x.EngineName != knowledge.EngineName))
                {
                    Known.Insert(0, knowledge);
                }
            }
        }
    }
}
