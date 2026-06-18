namespace LiteGraph.GraphRepositories.Postgresql.Queries
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.Json;
    using LiteGraph.Serialization;

    internal static class RequestHistoryQueries
    {
        internal const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        private static readonly Serializer _Serializer = new Serializer();

        internal static string Insert(RequestHistoryDetail detail)
        {
            string reqHeadersJson = detail.RequestHeaders != null ? _Serializer.SerializeJson(detail.RequestHeaders, false) : null;
            string respHeadersJson = detail.ResponseHeaders != null ? _Serializer.SerializeJson(detail.ResponseHeaders, false) : null;
            string reqBodyB64 = EncodeBody(detail.RequestBody);
            string respBodyB64 = EncodeBody(detail.ResponseBody);

            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO 'requesthistory' (");
            sb.Append("guid, requestid, correlationid, traceid, createdutc, completedutc, method, path, url, sourceip, ");
            sb.Append("tenantguid, userguid, statuscode, success, processingtimems, ");
            sb.Append("requestbodylength, responsebodylength, requestbodytruncated, responsebodytruncated, ");
            sb.Append("requestcontenttype, responsecontenttype, requestheadersjson, requestbodyb64, ");
            sb.Append("responseheadersjson, responsebodyb64, transactiondiagnosticsjson");
            sb.Append(") VALUES (");
            sb.Append("'").Append(detail.GUID).Append("', ");
            sb.Append(StringOrNull(detail.RequestId)).Append(", ");
            sb.Append(StringOrNull(detail.CorrelationId)).Append(", ");
            sb.Append(StringOrNull(detail.TraceId)).Append(", ");
            sb.Append("'").Append(detail.CreatedUtc.ToString(TimestampFormat)).Append("', ");
            sb.Append(detail.CompletedUtc.HasValue ? ("'" + detail.CompletedUtc.Value.ToString(TimestampFormat) + "'") : "NULL").Append(", ");
            sb.Append("'").Append(Sanitizer.Sanitize(detail.Method ?? "")).Append("', ");
            sb.Append("'").Append(EscapeQuotes(detail.Path ?? "")).Append("', ");
            sb.Append("'").Append(EscapeQuotes(detail.Url ?? "")).Append("', ");
            sb.Append(StringOrNull(detail.SourceIp)).Append(", ");
            sb.Append(detail.TenantGUID.HasValue ? ("'" + detail.TenantGUID.Value + "'") : "NULL").Append(", ");
            sb.Append(detail.UserGUID.HasValue ? ("'" + detail.UserGUID.Value + "'") : "NULL").Append(", ");
            sb.Append(detail.StatusCode).Append(", ");
            sb.Append(detail.Success ? "1" : "0").Append(", ");
            sb.Append(detail.ProcessingTimeMs.ToString(System.Globalization.CultureInfo.InvariantCulture)).Append(", ");
            sb.Append(detail.RequestBodyLength).Append(", ");
            sb.Append(detail.ResponseBodyLength).Append(", ");
            sb.Append(detail.RequestBodyTruncated ? "1" : "0").Append(", ");
            sb.Append(detail.ResponseBodyTruncated ? "1" : "0").Append(", ");
            sb.Append(StringOrNull(detail.RequestContentType)).Append(", ");
            sb.Append(StringOrNull(detail.ResponseContentType)).Append(", ");
            sb.Append(JsonOrNull(reqHeadersJson)).Append(", ");
            sb.Append(StringOrNull(reqBodyB64)).Append(", ");
            sb.Append(JsonOrNull(respHeadersJson)).Append(", ");
            sb.Append(StringOrNull(respBodyB64)).Append(", ");
            sb.Append(JsonOrNull(detail.TransactionDiagnosticsJson));
            sb.Append(");");
            return sb.ToString();
        }

        internal static string SelectByGuid(Guid guid)
        {
            return "SELECT * FROM 'requesthistory' WHERE guid = '" + guid + "';";
        }

        internal static string Search(RequestHistorySearchRequest search, bool countOnly)
        {
            StringBuilder sb = new StringBuilder();
            if (countOnly)
                sb.Append("SELECT COUNT(*) AS record_count FROM 'requesthistory' WHERE 1=1 ");
            else
                sb.Append("SELECT guid, requestid, correlationid, traceid, createdutc, completedutc, method, path, url, sourceip, tenantguid, userguid, statuscode, success, processingtimems, requestbodylength, responsebodylength, requestbodytruncated, responsebodytruncated, requestcontenttype, responsecontenttype, transactiondiagnosticsjson FROM 'requesthistory' WHERE 1=1 ");

            AppendFilters(sb, search);

            if (!countOnly)
            {
                sb.Append("ORDER BY createdutc DESC ");
                sb.Append("LIMIT ").Append(search.PageSize).Append(" OFFSET ").Append(search.Page * search.PageSize).Append(";");
            }
            else
            {
                sb.Append(";");
            }

            return sb.ToString();
        }

        internal static string GetSummary(Guid? tenantGuid, string interval, DateTime startUtc, DateTime endUtc)
        {
            string bucketExpr = BucketExpression(interval);

            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT ");
            sb.Append(bucketExpr).Append(" AS bucketutc, ");
            sb.Append("SUM(CASE WHEN success = 1 THEN 1 ELSE 0 END) AS successcount, ");
            sb.Append("SUM(CASE WHEN success = 0 THEN 1 ELSE 0 END) AS failurecount ");
            sb.Append("FROM 'requesthistory' WHERE 1=1 ");
            sb.Append("AND createdutc >= '").Append(startUtc.ToString(TimestampFormat)).Append("' ");
            sb.Append("AND createdutc < '").Append(endUtc.ToString(TimestampFormat)).Append("' ");

            if (tenantGuid.HasValue)
                sb.Append("AND tenantguid = '").Append(tenantGuid.Value).Append("' ");

            sb.Append("GROUP BY ").Append(bucketExpr).Append(" ");
            sb.Append("ORDER BY ").Append(bucketExpr).Append(" ASC;");

            return sb.ToString();
        }

        internal static string Delete(Guid guid)
        {
            return "DELETE FROM 'requesthistory' WHERE guid = '" + guid + "';";
        }

        internal static string DeleteMany(RequestHistorySearchRequest search)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("DELETE FROM 'requesthistory' WHERE 1=1 ");
            AppendFilters(sb, search);
            sb.Append(";");
            return sb.ToString();
        }

        internal static string DeleteOlderThan(DateTime cutoffUtc)
        {
            return "DELETE FROM 'requesthistory' WHERE createdutc < '" + cutoffUtc.ToString(TimestampFormat) + "';";
        }

        internal static string Count()
        {
            return "SELECT changes() AS record_count;";
        }

        private static void AppendFilters(StringBuilder sb, RequestHistorySearchRequest search)
        {
            if (search.TenantGUID.HasValue)
                sb.Append("AND tenantguid = '").Append(search.TenantGUID.Value).Append("' ");

            if (!string.IsNullOrEmpty(search.RequestId))
                sb.Append("AND requestid = '").Append(EscapeQuotes(search.RequestId)).Append("' ");

            if (!string.IsNullOrEmpty(search.CorrelationId))
                sb.Append("AND correlationid = '").Append(EscapeQuotes(search.CorrelationId)).Append("' ");

            if (!string.IsNullOrEmpty(search.TraceId))
                sb.Append("AND traceid = '").Append(EscapeQuotes(search.TraceId)).Append("' ");

            if (!string.IsNullOrEmpty(search.Method))
                sb.Append("AND method = '").Append(Sanitizer.Sanitize(search.Method)).Append("' ");

            if (search.StatusCode.HasValue)
                sb.Append("AND statuscode = ").Append(search.StatusCode.Value).Append(" ");

            if (search.Success.HasValue)
                sb.Append("AND success = ").Append(search.Success.Value ? "1" : "0").Append(" ");

            if (!string.IsNullOrEmpty(search.Path))
                sb.Append("AND path LIKE '%").Append(EscapeQuotes(search.Path)).Append("%' ");

            if (!string.IsNullOrEmpty(search.SourceIp))
                sb.Append("AND sourceip = '").Append(Sanitizer.Sanitize(search.SourceIp)).Append("' ");

            if (search.HasTransactionDiagnostics.HasValue)
            {
                if (search.HasTransactionDiagnostics.Value)
                    sb.Append("AND transactiondiagnosticsjson IS NOT NULL AND transactiondiagnosticsjson <> '' ");
                else
                    sb.Append("AND (transactiondiagnosticsjson IS NULL OR transactiondiagnosticsjson = '') ");
            }

            if (!string.IsNullOrEmpty(search.TransactionId))
                sb.Append("AND transactiondiagnosticsjson LIKE '%").Append(EscapeQuotes(search.TransactionId)).Append("%' ");

            if (search.FromUtc.HasValue)
                sb.Append("AND createdutc >= '").Append(search.FromUtc.Value.ToString(TimestampFormat)).Append("' ");

            if (search.ToUtc.HasValue)
                sb.Append("AND createdutc < '").Append(search.ToUtc.Value.ToString(TimestampFormat)).Append("' ");
        }

        private static string BucketExpression(string interval)
        {
            const string timestampExpr = "CAST(createdutc AS timestamp)";

            switch ((interval ?? "hour").ToLowerInvariant())
            {
                case "minute":
                    return "to_char(date_trunc('minute', " + timestampExpr + "), 'YYYY-MM-DD HH24:MI:00')";
                case "15minute":
                    return "to_char(date_trunc('hour', " + timestampExpr + ") + (floor(extract(minute from " + timestampExpr + ") / 15)::int * interval '15 minute'), 'YYYY-MM-DD HH24:MI:00')";
                case "6hour":
                    return "to_char(date_trunc('day', " + timestampExpr + ") + (floor(extract(hour from " + timestampExpr + ") / 6)::int * interval '6 hour'), 'YYYY-MM-DD HH24:00:00')";
                case "day":
                    return "to_char(date_trunc('day', " + timestampExpr + "), 'YYYY-MM-DD 00:00:00')";
                case "hour":
                default:
                    return "to_char(date_trunc('hour', " + timestampExpr + "), 'YYYY-MM-DD HH24:00:00')";
            }
        }

        private static string EncodeBody(string body)
        {
            if (string.IsNullOrEmpty(body)) return null;
            byte[] bytes = Encoding.UTF8.GetBytes(body);
            return Convert.ToBase64String(bytes);
        }

        internal static string DecodeBody(string b64)
        {
            if (string.IsNullOrEmpty(b64)) return null;
            try
            {
                byte[] bytes = Convert.FromBase64String(b64);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return b64;
            }
        }

        private static string EscapeQuotes(string value)
        {
            if (value == null) return "";
            return value.Replace("'", "''");
        }

        private static string StringOrNull(string value)
        {
            if (value == null) return "NULL";
            return "'" + EscapeQuotes(value) + "'";
        }

        private static string JsonOrNull(string json)
        {
            if (string.IsNullOrEmpty(json)) return "NULL";
            return "'" + json.Replace("'", "''") + "'";
        }
    }
}

