namespace LiteGraph.GraphRepositories.Postgresql.Queries
{
    using System;
    using System.Globalization;

    internal static class ChatTurnQueries
    {
        internal static string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        internal static string Insert(ChatTurn turn)
        {
            string ret =
                "INSERT INTO 'chatturns' "
                + "(guid, tenantguid, threadguid, sequence, usermessage, assistantresponse, reasoning, "
                + "tooltranscript, telemetry, traceid, completionendpointguid, embeddingendpointguid, "
                + "provider, model, embeddingdurationms, retrievaldurationms, retrievedchunkcount, "
                + "toolloopiterations, toolcallcount, limiterwaitms, inferenceconnectionms, "
                + "timetofirsttokenms, timetolasttokenms, totaldurationms, prompttokens, completiontokens, "
                + "tokspersecoverall, tokspersecgeneration, retrycount, success, httpstatus, error, createdutc) "
                + "VALUES ("
                + SqlString(turn.GUID.ToString()) + ","
                + SqlString(turn.TenantGUID.ToString()) + ","
                + SqlString(turn.ThreadGUID.ToString()) + ","
                + turn.Sequence + ","
                + SqlString(turn.UserMessage) + ","
                + SqlString(turn.AssistantResponse) + ","
                + SqlString(turn.Reasoning) + ","
                + SqlString(turn.ToolTranscriptJson) + ","
                + SqlString(turn.TelemetryJson) + ","
                + SqlString(turn.TraceId) + ","
                + SqlString(turn.CompletionEndpointGUID != null ? turn.CompletionEndpointGUID.Value.ToString() : null) + ","
                + SqlString(turn.EmbeddingEndpointGUID != null ? turn.EmbeddingEndpointGUID.Value.ToString() : null) + ","
                + SqlString(turn.Provider.ToString()) + ","
                + SqlString(turn.Model) + ","
                + SqlDouble(turn.EmbeddingDurationMs) + ","
                + SqlDouble(turn.RetrievalDurationMs) + ","
                + turn.RetrievedChunkCount + ","
                + turn.ToolLoopIterations + ","
                + turn.ToolCallCount + ","
                + SqlDouble(turn.LimiterWaitMs) + ","
                + SqlDouble(turn.InferenceConnectionMs) + ","
                + SqlDouble(turn.TimeToFirstTokenMs) + ","
                + SqlDouble(turn.TimeToLastTokenMs) + ","
                + turn.TotalDurationMs.ToString(CultureInfo.InvariantCulture) + ","
                + SqlInt(turn.PromptTokens) + ","
                + SqlInt(turn.CompletionTokens) + ","
                + SqlDouble(turn.TokensPerSecondOverall) + ","
                + SqlDouble(turn.TokensPerSecondGeneration) + ","
                + turn.RetryCount + ","
                + (turn.Success ? "1" : "0") + ","
                + SqlInt(turn.HttpStatus) + ","
                + SqlString(turn.Error) + ","
                + SqlString(turn.CreatedUtc.ToString(TimestampFormat))
                + ") "
                + "RETURNING *;";

            return ret;
        }

        internal static string SelectByGuid(Guid tenantGuid, Guid guid)
        {
            return "SELECT * FROM 'chatturns' WHERE tenantguid = '" + tenantGuid + "' AND guid = '" + guid + "';";
        }

        internal static string SelectByThread(
            Guid tenantGuid,
            Guid threadGuid,
            bool ascending,
            int batchSize = 100,
            int skip = 0)
        {
            return
                "SELECT * FROM 'chatturns' "
                + "WHERE tenantguid = '" + tenantGuid + "' AND threadguid = '" + threadGuid + "' "
                + "ORDER BY sequence " + (ascending ? "ASC" : "DESC") + " "
                + "LIMIT " + batchSize + " OFFSET " + skip + ";";
        }

        internal static string SelectAllInTenant(
            Guid tenantGuid,
            int batchSize = 100,
            int skip = 0,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            string ret = "SELECT * FROM 'chatturns' WHERE tenantguid = '" + tenantGuid + "' ";
            ret +=
                "ORDER BY " + Converters.EnumerationOrderToClause(order) + " "
                + "LIMIT " + batchSize + " OFFSET " + skip + ";";
            return ret;
        }

        internal static string GetRecordPage(
            Guid? tenantGuid,
            Guid? threadGuid,
            int batchSize = 100,
            int skip = 0,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            ChatTurn marker = null)
        {
            string ret = "SELECT * FROM 'chatturns' WHERE guid IS NOT NULL ";

            if (tenantGuid != null)
                ret += "AND tenantguid = '" + tenantGuid.Value.ToString() + "' ";

            if (threadGuid != null)
                ret += "AND threadguid = '" + threadGuid.Value.ToString() + "' ";

            if (marker != null)
                ret += "AND " + MarkerWhereClause(order, marker);

            ret += OrderByClause(order);
            ret += "LIMIT " + batchSize;
            if (marker == null && skip > 0) ret += " OFFSET " + skip;
            ret += ";";
            return ret;
        }

        internal static string GetRecordCount(
            Guid? tenantGuid,
            Guid? threadGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            ChatTurn marker = null)
        {
            string ret = "SELECT COUNT(*) AS record_count FROM 'chatturns' WHERE guid IS NOT NULL ";

            if (tenantGuid != null)
                ret += "AND tenantguid = '" + tenantGuid.Value.ToString() + "' ";

            if (threadGuid != null)
                ret += "AND threadguid = '" + threadGuid.Value.ToString() + "' ";

            if (marker != null)
                ret += "AND " + MarkerWhereClause(order, marker);

            return ret;
        }

        internal static string GetCountByThread(Guid tenantGuid, Guid threadGuid)
        {
            return
                "SELECT COUNT(*) AS record_count FROM 'chatturns' "
                + "WHERE tenantguid = '" + tenantGuid + "' AND threadguid = '" + threadGuid + "';";
        }

        internal static string GetMaxSequence(Guid tenantGuid, Guid threadGuid)
        {
            return
                "SELECT MAX(sequence) AS max_sequence FROM 'chatturns' "
                + "WHERE tenantguid = '" + tenantGuid + "' AND threadguid = '" + threadGuid + "';";
        }

        internal static string Delete(Guid tenantGuid, Guid guid)
        {
            return "DELETE FROM 'chatturns' WHERE tenantguid = '" + tenantGuid + "' AND guid = '" + guid + "';";
        }

        internal static string DeleteByThread(Guid tenantGuid, Guid threadGuid)
        {
            return "DELETE FROM 'chatturns' WHERE tenantguid = '" + tenantGuid + "' AND threadguid = '" + threadGuid + "';";
        }

        internal static string DeleteAllInTenant(Guid tenantGuid)
        {
            return "DELETE FROM 'chatturns' WHERE tenantguid = '" + tenantGuid + "';";
        }

        internal static string DeleteOlderThan(Guid tenantGuid, DateTime olderThanUtc)
        {
            return
                "DELETE FROM 'chatturns' "
                + "WHERE tenantguid = '" + tenantGuid + "' "
                + "AND createdutc < '" + olderThanUtc.ToString(TimestampFormat) + "';";
        }

        private static string OrderByClause(EnumerationOrderEnum order)
        {
            switch (order)
            {
                case EnumerationOrderEnum.CreatedAscending:
                    return "ORDER BY sequence ASC, createdutc ASC ";
                case EnumerationOrderEnum.GuidAscending:
                    return "ORDER BY guid ASC ";
                case EnumerationOrderEnum.GuidDescending:
                    return "ORDER BY guid DESC ";
                default:
                    return "ORDER BY sequence DESC, createdutc DESC ";
            }
        }

        private static string MarkerWhereClause(EnumerationOrderEnum order, ChatTurn marker)
        {
            switch (order)
            {
                case EnumerationOrderEnum.CreatedAscending:
                    return "sequence > " + marker.Sequence + " ";
                case EnumerationOrderEnum.GuidAscending:
                    return "guid > '" + marker.GUID + "' ";
                case EnumerationOrderEnum.GuidDescending:
                    return "guid < '" + marker.GUID + "' ";
                default:
                    return "sequence < " + marker.Sequence + " ";
            }
        }

        private static string SqlString(string val)
        {
            if (String.IsNullOrEmpty(val)) return "NULL";
            return "'" + Sanitizer.Sanitize(val) + "'";
        }

        private static string SqlDouble(double? val)
        {
            if (val == null) return "NULL";
            return val.Value.ToString(CultureInfo.InvariantCulture);
        }

        private static string SqlInt(int? val)
        {
            if (val == null) return "NULL";
            return val.Value.ToString(CultureInfo.InvariantCulture);
        }
    }
}
