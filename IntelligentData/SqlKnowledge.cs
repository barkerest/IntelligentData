using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using IntelligentData.Interfaces;

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

        private readonly Regex _connTypePattern;

        /// <inheritdoc />
        public bool RelevantForConnection(IDbConnection connection)
        {
            if (connection is null) throw new ArgumentNullException(nameof(connection));

            return _connTypePattern.IsMatch(connection.GetType().FullName ?? "");
        }

        private SqlKnowledge(string name, string pattern, string open, string close, string insertId, bool deleteAlias, bool updateAlias, bool updateFrom, string concatOp = null, string concatFunc = null)
        {
            EngineName                 = name;
            _connTypePattern           = new Regex(pattern, RegexOptions.IgnoreCase);
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

        private static readonly List<ISqlKnowledge> Known = new List<ISqlKnowledge>()
        {
            new SqlKnowledge(
                "Generic MySQL",
                "(mysql|mariadb)",
                "`",
                "`",
                "SELECT LAST_INSERT_ID()",
                true,
                true,
                false,
                concatFunc: "CONCAT"
            ),
            new SqlKnowledge(
                "Generic SQL Server",
                @"^(system|microsoft)\.data\.sqlclient",
                "[",
                "]",
                "SELECT SCOPE_IDENTITY()",
                true,
                true,
                true,
                concatOp: "+"
            ),
            new SqlKnowledge(
                "Generic SQLite",
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
