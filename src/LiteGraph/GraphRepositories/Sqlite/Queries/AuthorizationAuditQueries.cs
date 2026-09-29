namespace LiteGraph.GraphRepositories.Sqlite.Queries
{
    using System;
    using System.Text;
    using LiteGraph.Serialization;

    internal static class AuthorizationAuditQueries
    {
        internal const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        internal static string Insert(AuthorizationAuditEntry entry)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO 'authorizationaudit' (");
            sb.Append("guid, createdutc, requestid, correlationid, traceid, tenantguid, graphguid, userguid, credentialguid, ");
            sb.Append("requesttype, method, path, sourceip, authenticationresult, authorizationresult, reason, requiredscope, isadmin, statuscode, description");
            sb.Append(") VALUES (");
            sb.Append("'").Append(entry.GUID).Append("', ");
            sb.Append("'").Append(entry.CreatedUtc.ToString(TimestampFormat)).Append("', ");
            sb.Append(StringOrNull(entry.RequestId)).Append(", ");
            sb.Append(StringOrNull(entry.CorrelationId)).Append(", ");
            sb.Append(StringOrNull(entry.TraceId)).Append(", ");
            sb.Append(GuidOrNull(entry.TenantGUID)).Append(", ");
            sb.Append(GuidOrNull(entry.GraphGUID)).Append(", ");
            sb.Append(GuidOrNull(entry.UserGUID)).Append(", ");
            sb.Append(GuidOrNull(entry.CredentialGUID)).Append(", ");
            sb.Append(StringOrNull(entry.RequestType)).Append(", ");
            sb.Append(StringOrNull(entry.Method)).Append(", ");
            sb.Append(StringOrNull(entry.Path)).Append(", ");
            sb.Append(StringOrNull(entry.SourceIp)).Append(", ");
            sb.Append(StringOrNull(entry.AuthenticationResult)).Append(", ");
            sb.Append(StringOrNull(entry.AuthorizationResult)).Append(", ");
            sb.Append(StringOrNull(entry.Reason)).Append(", ");
            sb.Append(StringOrNull(entry.RequiredScope)).Append(", ");
            sb.Append(entry.IsAdmin ? "1" : "0").Append(", ");
            sb.Append(entry.StatusCode).Append(", ");
            sb.Append(StringOrNull(entry.Description));
            sb.Append(");");
            return sb.ToString();
        }

        internal static string SelectByGuid(Guid guid)
        {
            return "SELECT * FROM 'authorizationaudit' WHERE guid = '" + guid + "';";
        }

        internal static string Search(AuthorizationAuditSearchRequest search, bool countOnly)
        {
            StringBuilder sb = new StringBuilder();
            if (countOnly)
                sb.Append("SELECT COUNT(*) AS record_count FROM 'authorizationaudit' WHERE 1=1 ");
            else
                sb.Append("SELECT * FROM 'authorizationaudit' WHERE 1=1 ");

            AppendFilters(sb, search);

            if (!countOnly)
            {
                sb.Append("ORDER BY createdutc DESC, guid DESC ");
                sb.Append("LIMIT ").Append(search.PageSize).Append(" OFFSET ").Append(search.Page * search.PageSize).Append(";");
            }
            else
            {
                sb.Append(";");
            }

            return sb.ToString();
        }

        internal static string Delete(Guid guid)
        {
            return "DELETE FROM 'authorizationaudit' WHERE guid = '" + guid + "';";
        }

        internal static string DeleteMany(AuthorizationAuditSearchRequest search)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("DELETE FROM 'authorizationaudit' WHERE 1=1 ");
            AppendFilters(sb, search);
            sb.Append(";");
            return sb.ToString();
        }

        private static void AppendFilters(StringBuilder sb, AuthorizationAuditSearchRequest search)
        {
            if (search.TenantGUID.HasValue)
                sb.Append("AND tenantguid = '").Append(search.TenantGUID.Value).Append("' ");

            if (search.GraphGUID.HasValue)
                sb.Append("AND graphguid = '").Append(search.GraphGUID.Value).Append("' ");

            if (search.UserGUID.HasValue)
                sb.Append("AND userguid = '").Append(search.UserGUID.Value).Append("' ");

            if (search.CredentialGUID.HasValue)
                sb.Append("AND credentialguid = '").Append(search.CredentialGUID.Value).Append("' ");

            if (!String.IsNullOrEmpty(search.RequestId))
                sb.Append("AND requestid = '").Append(EscapeQuotes(search.RequestId)).Append("' ");

            if (!String.IsNullOrEmpty(search.CorrelationId))
                sb.Append("AND correlationid = '").Append(EscapeQuotes(search.CorrelationId)).Append("' ");

            if (!String.IsNullOrEmpty(search.TraceId))
                sb.Append("AND traceid = '").Append(EscapeQuotes(search.TraceId)).Append("' ");

            if (!String.IsNullOrEmpty(search.RequestType))
                sb.Append("AND requesttype = '").Append(EscapeQuotes(search.RequestType)).Append("' ");

            if (!String.IsNullOrEmpty(search.Reason))
                sb.Append("AND reason = '").Append(EscapeQuotes(search.Reason)).Append("' ");

            if (!String.IsNullOrEmpty(search.RequiredScope))
                sb.Append("AND requiredscope = '").Append(EscapeQuotes(search.RequiredScope)).Append("' ");

            if (search.FromUtc.HasValue)
                sb.Append("AND createdutc >= '").Append(search.FromUtc.Value.ToString(TimestampFormat)).Append("' ");

            if (search.ToUtc.HasValue)
                sb.Append("AND createdutc < '").Append(search.ToUtc.Value.ToString(TimestampFormat)).Append("' ");
        }

        private static string GuidOrNull(Guid? guid)
        {
            return guid.HasValue ? ("'" + guid.Value + "'") : "NULL";
        }

        private static string StringOrNull(string value)
        {
            if (value == null) return "NULL";
            return "'" + EscapeQuotes(value) + "'";
        }

        private static string EscapeQuotes(string value)
        {
            if (value == null) return "";
            return Sanitizer.Sanitize(value).Replace("'", "''");
        }
    }
}
