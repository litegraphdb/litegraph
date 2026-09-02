namespace LiteGraph.GraphRepositories.Postgresql
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Translates LiteGraph's SQLite-shaped provider SQL into PostgreSQL dialect SQL.
    /// All single-quoted string literals are masked before any rewriting is applied and restored
    /// afterward, so user-supplied values (tag values, label values, names, JSON data) are never
    /// altered by the translation even when they contain SQL keywords, table names, or column
    /// names such as the word 'data'.  Thread safety: this class is stateless and safe for
    /// concurrent use.
    /// </summary>
    public static class PostgresqlSqlTranslator
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private const char _SentinelChar = '\uE000';

        private static readonly string[] _Tables =
        {
            "tenants",
            "users",
            "creds",
            "authorizationroles",
            "userroleassignments",
            "credentialscopeassignments",
            "labels",
            "tags",
            "vectors",
            "graphs",
            "nodes",
            "edges",
            "requesthistory",
            "authorizationaudit",
            "chatendpoints",
            "chatthreads",
            "chatturns",
            "chatfeedback",
            "chatsettings"
        };

        private static readonly string[] _QuotedColumns =
        {
            "createdutc",
            "lastupdateutc",
            "data"
        };

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        /// <summary>
        /// Translate provider SQL into PostgreSQL dialect SQL for the supplied schema.
        /// String literal contents are preserved byte-for-byte; only SQL syntax outside of
        /// single-quoted literals (plus quoted identifiers in known syntactic positions) is rewritten.
        /// </summary>
        /// <param name="sql">SQL text.  Null or whitespace input is returned unchanged.</param>
        /// <param name="schema">PostgreSQL schema.</param>
        /// <returns>Translated SQL.</returns>
        public static string Translate(string sql, string schema)
        {
            if (String.IsNullOrWhiteSpace(sql)) return sql;

            string quotedSchema = PostgresqlGraphRepository.QuoteIdentifier(schema);

            List<string> literals = new List<string>();
            string sentinel = BuildSentinel(sql);
            string placeholderPattern = Regex.Escape(sentinel) + @"(?<lit>\d+)" + Regex.Escape(sentinel);

            string ret = MaskStringLiterals(sql, sentinel, literals);

            ret = Regex.Replace(ret, @"\bBEGIN\s+TRANSACTION\b", "BEGIN", RegexOptions.IgnoreCase);
            ret = Regex.Replace(ret, @"\bEND\s+TRANSACTION\b", "COMMIT", RegexOptions.IgnoreCase);
            ret = TranslateHexLiterals(ret, placeholderPattern, literals);
            ret = Regex.Replace(ret, @"\bBLOB\b", "BYTEA", RegexOptions.IgnoreCase);
            ret = Regex.Replace(ret, @"\bREAL\b", "DOUBLE PRECISION", RegexOptions.IgnoreCase);
            ret = Regex.Replace(ret, @"guid\s+VARCHAR\(64\)\s+NOT\s+NULL\s+UNIQUE", "guid VARCHAR(64) PRIMARY KEY", RegexOptions.IgnoreCase);

            ret = TranslateCreateIndexNames(ret, placeholderPattern, literals);
            ret = TranslateDropIndexNames(ret, placeholderPattern, literals, quotedSchema);
            ret = PrefixKnownTables(ret, placeholderPattern, literals, quotedSchema);
            ret = TranslateQuotedColumns(ret, placeholderPattern, literals);
            ret = TranslateJsonExtract(ret, placeholderPattern, literals);
            ret = TranslateJsonComparisons(ret);

            return UnmaskStringLiterals(ret, placeholderPattern, literals);
        }

        #endregion

        #region Private-Methods

        private static string BuildSentinel(string sql)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_SentinelChar);
            while (sql.Contains(sb.ToString())) sb.Append(_SentinelChar);
            return sb.ToString();
        }

        private static string MaskStringLiterals(string sql, string sentinel, List<string> literals)
        {
            StringBuilder masked = new StringBuilder(sql.Length);
            int i = 0;

            while (i < sql.Length)
            {
                char c = sql[i];
                if (c == '\'')
                {
                    int end = FindLiteralEnd(sql, i);
                    string literal = (end >= 0) ? sql.Substring(i, end - i + 1) : sql.Substring(i);
                    masked.Append(sentinel).Append(literals.Count).Append(sentinel);
                    literals.Add(literal);
                    i = (end >= 0) ? (end + 1) : sql.Length;
                }
                else
                {
                    masked.Append(c);
                    i++;
                }
            }

            return masked.ToString();
        }

        private static int FindLiteralEnd(string sql, int start)
        {
            int i = start + 1;

            while (i < sql.Length)
            {
                if (sql[i] == '\'')
                {
                    if ((i + 1) < sql.Length && sql[i + 1] == '\'')
                    {
                        i += 2;
                        continue;
                    }

                    return i;
                }

                i++;
            }

            return -1;
        }

        private static string UnmaskStringLiterals(string sql, string placeholderPattern, List<string> literals)
        {
            return Regex.Replace(
                sql,
                placeholderPattern,
                match => literals[Int32.Parse(match.Groups["lit"].Value)]);
        }

        private static string GetLiteralContent(List<string> literals, Match match)
        {
            string literal = literals[Int32.Parse(match.Groups["lit"].Value)];
            if (literal.Length < 2) return null;
            if (literal[0] != '\'' || literal[literal.Length - 1] != '\'') return null;
            return literal.Substring(1, literal.Length - 2).Replace("''", "'");
        }

        private static string TranslateHexLiterals(string sql, string placeholderPattern, List<string> literals)
        {
            return Regex.Replace(
                sql,
                "X" + placeholderPattern,
                match =>
                {
                    string content = GetLiteralContent(literals, match);
                    if (content == null || !Regex.IsMatch(content, "^[0-9A-Fa-f]*$")) return match.Value;
                    return "decode('" + content + "', 'hex')";
                });
        }

        private static string TranslateCreateIndexNames(string sql, string placeholderPattern, List<string> literals)
        {
            return Regex.Replace(
                sql,
                @"(?i)\b(INDEX\s+IF\s+NOT\s+EXISTS)\s+" + placeholderPattern,
                match =>
                {
                    string content = GetLiteralContent(literals, match);
                    if (content == null) return match.Value;
                    return match.Groups[1].Value + " " + PostgresqlGraphRepository.QuoteIdentifier(content);
                });
        }

        private static string TranslateDropIndexNames(string sql, string placeholderPattern, List<string> literals, string quotedSchema)
        {
            return Regex.Replace(
                sql,
                @"(?i)\b(DROP\s+INDEX\s+IF\s+EXISTS)\s+" + placeholderPattern,
                match =>
                {
                    string content = GetLiteralContent(literals, match);
                    if (content == null) return match.Value;
                    return match.Groups[1].Value + " " + quotedSchema + "." + PostgresqlGraphRepository.QuoteIdentifier(content);
                });
        }

        private static string PrefixKnownTables(string sql, string placeholderPattern, List<string> literals, string quotedSchema)
        {
            string ret = Regex.Replace(
                sql,
                @"(?i)\b(TABLE\s+IF\s+NOT\s+EXISTS|INTO|UPDATE|FROM|JOIN|ON)\s+" + placeholderPattern,
                match =>
                {
                    string content = GetLiteralContent(literals, match);
                    if (content == null) return match.Value;

                    string canonical = _Tables.FirstOrDefault(t => t.Equals(content, StringComparison.OrdinalIgnoreCase));
                    if (canonical == null) return match.Value;

                    return match.Groups[1].Value + " " + quotedSchema + "." + PostgresqlGraphRepository.QuoteIdentifier(canonical);
                });

            foreach (string table in _Tables)
            {
                string quotedTable = quotedSchema + "." + PostgresqlGraphRepository.QuoteIdentifier(table);

                ret = Regex.Replace(
                    ret,
                    @"(?i)\b(INTO|UPDATE|FROM|JOIN)\s+" + table + @"\b",
                    match => match.Groups[1].Value + " " + quotedTable);

                ret = Regex.Replace(
                    ret,
                    @"(?i)\bON\s+" + table + @"\b",
                    match => match.Groups[0].Value.Substring(0, 2) + " " + quotedTable);
            }

            return ret;
        }

        private static string TranslateQuotedColumns(string sql, string placeholderPattern, List<string> literals)
        {
            Regex placeholderRegex = new Regex(placeholderPattern);

            return Regex.Replace(
                sql,
                @"(?i)\bCREATE\s+(?:UNIQUE\s+)?INDEX\b[^(;]*\([^);]*\)",
                stmtMatch => placeholderRegex.Replace(
                    stmtMatch.Value,
                    phMatch =>
                    {
                        string content = GetLiteralContent(literals, phMatch);
                        if (content == null || !_QuotedColumns.Contains(content, StringComparer.Ordinal)) return phMatch.Value;
                        return PostgresqlGraphRepository.QuoteIdentifier(content);
                    }));
        }

        private static string TranslateJsonExtract(string sql, string placeholderPattern, List<string> literals)
        {
            return Regex.Replace(
                sql,
                @"(?i)json_extract\((?<target>[A-Za-z_][A-Za-z0-9_]*\.data),\s*" + placeholderPattern + @"\)",
                match =>
                {
                    string content = GetLiteralContent(literals, match);
                    if (content == null
                        || content.Length <= 2
                        || !content.StartsWith("$.", StringComparison.Ordinal)
                        || content.IndexOf('\'') >= 0)
                    {
                        return match.Value;
                    }

                    string target = match.Groups["target"].Value;
                    IEnumerable<string> parts = content.Substring(2)
                        .Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Replace("\"", "\"\"").Replace("'", "''"));
                    return "(" + target + "::jsonb #>> '{" + String.Join(",", parts) + "}')";
                });
        }

        private static string TranslateJsonComparisons(string sql)
        {
            string ret = Regex.Replace(
                sql,
                @"\((?<json>[A-Za-z_][A-Za-z0-9_]*\.data::jsonb\s+#>>\s+'\{[^']+\}')\)\s*(?<op>>=|<=|<>|=|>|<)\s*(?<num>-?\d+(?:\.\d+)?)",
                match => "((" + match.Groups["json"].Value + ")::DOUBLE PRECISION) "
                    + match.Groups["op"].Value + " "
                    + match.Groups["num"].Value,
                RegexOptions.IgnoreCase);

            ret = Regex.Replace(
                ret,
                @"\((?<json>[A-Za-z_][A-Za-z0-9_]*\.data::jsonb\s+#>>\s+'\{[^']+\}')\)\s*(?<op><>|=)\s*(?<bool>true|false)",
                match => "((" + match.Groups["json"].Value + ")::BOOLEAN) "
                    + match.Groups["op"].Value + " "
                    + match.Groups["bool"].Value.ToLowerInvariant(),
                RegexOptions.IgnoreCase);

            return ret;
        }

        #endregion
    }
}
