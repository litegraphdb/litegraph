namespace LiteGraph.GraphRepositories.Postgresql.Queries
{
    using System;

    internal static class ChatFeedbackQueries
    {
        internal static string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        internal static string Insert(ChatFeedback feedback)
        {
            string ret =
                "INSERT INTO 'chatfeedback' "
                + "(guid, tenantguid, threadguid, turnguid, userguid, rating, feedbacktext, createdutc) "
                + "VALUES ("
                + SqlString(feedback.GUID.ToString()) + ","
                + SqlString(feedback.TenantGUID.ToString()) + ","
                + SqlString(feedback.ThreadGUID.ToString()) + ","
                + SqlString(feedback.TurnGUID.ToString()) + ","
                + SqlString(feedback.UserGUID.ToString()) + ","
                + SqlString(feedback.Rating.ToString()) + ","
                + SqlString(feedback.FeedbackText) + ","
                + SqlString(feedback.CreatedUtc.ToString(TimestampFormat))
                + ") "
                + "RETURNING *;";

            return ret;
        }

        internal static string SelectByGuid(Guid tenantGuid, Guid guid)
        {
            return "SELECT * FROM 'chatfeedback' WHERE tenantguid = '" + tenantGuid + "' AND guid = '" + guid + "';";
        }

        internal static string SelectAllInTenant(
            Guid tenantGuid,
            ChatFeedbackRatingEnum? rating,
            Guid? threadGuid,
            int batchSize = 100,
            int skip = 0,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            string ret = "SELECT * FROM 'chatfeedback' WHERE tenantguid = '" + tenantGuid + "' ";

            if (rating != null)
                ret += "AND rating = '" + rating.Value.ToString() + "' ";

            if (threadGuid != null)
                ret += "AND threadguid = '" + threadGuid.Value.ToString() + "' ";

            ret +=
                "ORDER BY " + Converters.EnumerationOrderToClause(order) + " "
                + "LIMIT " + batchSize + " OFFSET " + skip + ";";
            return ret;
        }

        internal static string GetRecordPage(
            Guid? tenantGuid,
            ChatFeedbackRatingEnum? rating = null,
            Guid? threadGuid = null,
            int batchSize = 100,
            int skip = 0,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            ChatFeedback marker = null)
        {
            string ret = "SELECT * FROM 'chatfeedback' WHERE guid IS NOT NULL ";

            if (tenantGuid != null)
                ret += "AND tenantguid = '" + tenantGuid.Value.ToString() + "' ";

            if (rating != null)
                ret += "AND rating = '" + rating.Value.ToString() + "' ";

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
            ChatFeedbackRatingEnum? rating = null,
            Guid? threadGuid = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            ChatFeedback marker = null)
        {
            string ret = "SELECT COUNT(*) AS record_count FROM 'chatfeedback' WHERE guid IS NOT NULL ";

            if (tenantGuid != null)
                ret += "AND tenantguid = '" + tenantGuid.Value.ToString() + "' ";

            if (rating != null)
                ret += "AND rating = '" + rating.Value.ToString() + "' ";

            if (threadGuid != null)
                ret += "AND threadguid = '" + threadGuid.Value.ToString() + "' ";

            if (marker != null)
                ret += "AND " + MarkerWhereClause(order, marker);

            return ret;
        }

        internal static string Delete(Guid tenantGuid, Guid guid)
        {
            return "DELETE FROM 'chatfeedback' WHERE tenantguid = '" + tenantGuid + "' AND guid = '" + guid + "';";
        }

        internal static string DeleteByThread(Guid tenantGuid, Guid threadGuid)
        {
            return "DELETE FROM 'chatfeedback' WHERE tenantguid = '" + tenantGuid + "' AND threadguid = '" + threadGuid + "';";
        }

        internal static string DeleteAllInTenant(Guid tenantGuid)
        {
            return "DELETE FROM 'chatfeedback' WHERE tenantguid = '" + tenantGuid + "';";
        }

        private static string OrderByClause(EnumerationOrderEnum order)
        {
            switch (order)
            {
                case EnumerationOrderEnum.CreatedAscending:
                    return KeysetOrdering.OrderBy(false, "createdutc", "guid");
                case EnumerationOrderEnum.GuidAscending:
                    return KeysetOrdering.OrderBy(false, "guid");
                case EnumerationOrderEnum.GuidDescending:
                    return KeysetOrdering.OrderBy(true, "guid");
                default:
                    return KeysetOrdering.OrderBy(true, "createdutc", "guid");
            }
        }

        private static string MarkerWhereClause(EnumerationOrderEnum order, ChatFeedback marker)
        {
            switch (order)
            {
                case EnumerationOrderEnum.CreatedAscending:
                    return KeysetOrdering.After(false, new string[] { "createdutc", "guid" }, new string[] { "'" + marker.CreatedUtc.ToString(TimestampFormat) + "'", "'" + marker.GUID + "'" });
                case EnumerationOrderEnum.GuidAscending:
                    return KeysetOrdering.After(false, new string[] { "guid" }, new string[] { "'" + marker.GUID + "'" });
                case EnumerationOrderEnum.GuidDescending:
                    return KeysetOrdering.After(true, new string[] { "guid" }, new string[] { "'" + marker.GUID + "'" });
                default:
                    return KeysetOrdering.After(true, new string[] { "createdutc", "guid" }, new string[] { "'" + marker.CreatedUtc.ToString(TimestampFormat) + "'", "'" + marker.GUID + "'" });
            }
        }

        private static string SqlString(string val)
        {
            if (String.IsNullOrEmpty(val)) return "NULL";
            return "'" + Sanitizer.Sanitize(val) + "'";
        }
    }
}
