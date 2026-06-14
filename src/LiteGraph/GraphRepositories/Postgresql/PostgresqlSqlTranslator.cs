namespace LiteGraph.GraphRepositories.Postgresql
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Translates LiteGraph's SQLite-shaped provider SQL into PostgreSQL dialect SQL.
    /// </summary>
    public static class PostgresqlSqlTranslator
    {
        private static readonly string[] Tables =
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
            "authorizationaudit"
        };

        private static readonly string[] QuotedColumns =
        {
            "createdutc",
            "lastupdateutc",
            "data"
        };

        /// <summary>
        /// Translate provider SQL into PostgreSQL dialect SQL for the supplied schema.
        /// </summary>
        /// <param name="sql">SQL text.</param>
        /// <param name="schema">PostgreSQL schema.</param>
        /// <returns>Translated SQL.</returns>
        public static string Translate(string sql, string schema)
        {
            if (String.IsNullOrWhiteSpace(sql)) return sql;

            string ret = sql;
            string quotedSchema = PostgresqlGraphRepository.QuoteIdentifier(schema);

            ret = Regex.Replace(ret, @"\bBEGIN\s+TRANSACTION\b", "BEGIN", RegexOptions.IgnoreCase);
            ret = Regex.Replace(ret, @"\bEND\s+TRANSACTION\b", "COMMIT", RegexOptions.IgnoreCase);
            ret = Regex.Replace(ret, @"X'([0-9A-Fa-f]*)'", "decode('$1', 'hex')");
            ret = Regex.Replace(ret, @"\bBLOB\b", "BYTEA", RegexOptions.IgnoreCase);
            ret = Regex.Replace(ret, @"\bREAL\b", "DOUBLE PRECISION", RegexOptions.IgnoreCase);
            ret = Regex.Replace(ret, @"guid\s+VARCHAR\(64\)\s+NOT\s+NULL\s+UNIQUE", "guid VARCHAR(64) PRIMARY KEY", RegexOptions.IgnoreCase);

            ret = TranslateCreateIndexNames(ret, quotedSchema);
            ret = TranslateDropIndexNames(ret, quotedSchema);
            ret = PrefixKnownTables(ret, quotedSchema);
            ret = TranslateQuotedColumns(ret);
            ret = TranslateJsonExtract(ret);
            ret = TranslateJsonComparisons(ret);

            return ret;
        }

        private static string PrefixKnownTables(string sql, string quotedSchema)
        {
            string ret = sql;
            foreach (string table in Tables)
            {
                string quotedTable = quotedSchema + "." + PostgresqlGraphRepository.QuoteIdentifier(table);

                ret = Regex.Replace(
                    ret,
                    @"(?i)\b(TABLE\s+IF\s+NOT\s+EXISTS|INTO|UPDATE|FROM|JOIN|ON)\s+'" + table + @"'",
                    match => match.Groups[1].Value + " " + quotedTable);

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

        private static string TranslateCreateIndexNames(string sql, string quotedSchema)
        {
            return Regex.Replace(
                sql,
                @"(?i)\b(INDEX\s+IF\s+NOT\s+EXISTS)\s+'([^']+)'",
                match => match.Groups[1].Value + " " + PostgresqlGraphRepository.QuoteIdentifier(match.Groups[2].Value));
        }

        private static string TranslateDropIndexNames(string sql, string quotedSchema)
        {
            return Regex.Replace(
                sql,
                @"(?i)\b(DROP\s+INDEX\s+IF\s+EXISTS)\s+'([^']+)'",
                match => match.Groups[1].Value + " " + quotedSchema + "." + PostgresqlGraphRepository.QuoteIdentifier(match.Groups[2].Value));
        }

        private static string TranslateQuotedColumns(string sql)
        {
            string ret = sql;
            foreach (string column in QuotedColumns)
            {
                ret = ret.Replace("'" + column + "'", PostgresqlGraphRepository.QuoteIdentifier(column));
            }
            return ret;
        }

        private static string TranslateJsonExtract(string sql)
        {
            return Regex.Replace(
                sql,
                @"json_extract\((?<target>[A-Za-z_][A-Za-z0-9_]*\.data),\s*'\$\.(?<path>[^']+)'\)",
                match =>
                {
                    string target = match.Groups["target"].Value;
                    string path = match.Groups["path"].Value;
                    IEnumerable<string> parts = path
                        .Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(p => p.Replace("\"", "\"\"").Replace("'", "''"));
                    return "(" + target + "::jsonb #>> '{" + String.Join(",", parts) + "}')";
                },
                RegexOptions.IgnoreCase);
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
    }
}
