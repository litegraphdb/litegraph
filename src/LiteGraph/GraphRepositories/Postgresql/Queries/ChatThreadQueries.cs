namespace LiteGraph.GraphRepositories.Postgresql.Queries
{
    using System;

    internal static class ChatThreadQueries
    {
        internal static string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        internal static string Insert(ChatThread thread)
        {
            string ret =
                "INSERT INTO 'chatthreads' "
                + "(guid, tenantguid, userguid, graphguid, title, createdutc, lastupdateutc) "
                + "VALUES ("
                + SqlString(thread.GUID.ToString()) + ","
                + SqlString(thread.TenantGUID.ToString()) + ","
                + SqlString(thread.UserGUID.ToString()) + ","
                + SqlString(thread.GraphGUID != null ? thread.GraphGUID.Value.ToString() : null) + ","
                + SqlString(thread.Title) + ","
                + SqlString(thread.CreatedUtc.ToString(TimestampFormat)) + ","
                + SqlString(thread.LastUpdateUtc.ToString(TimestampFormat))
                + ") "
                + "RETURNING *;";

            return ret;
        }

        internal static string SelectByGuid(Guid tenantGuid, Guid guid)
        {
            return "SELECT * FROM 'chatthreads' WHERE tenantguid = '" + tenantGuid + "' AND guid = '" + guid + "';";
        }

        internal static string SelectAllInTenant(
            Guid tenantGuid,
            Guid? userGuid,
            int batchSize = 100,
            int skip = 0,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            string ret = "SELECT * FROM 'chatthreads' WHERE tenantguid = '" + tenantGuid + "' ";

            if (userGuid != null)
                ret += "AND userguid = '" + userGuid.Value.ToString() + "' ";

            ret +=
                "ORDER BY " + Converters.EnumerationOrderToClause(order) + " "
                + "LIMIT " + batchSize + " OFFSET " + skip + ";";
            return ret;
        }

        internal static string GetRecordPage(
            Guid? tenantGuid,
            Guid? userGuid,
            int batchSize = 100,
            int skip = 0,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            ChatThread marker = null)
        {
            string ret = "SELECT * FROM 'chatthreads' WHERE guid IS NOT NULL ";

            if (tenantGuid != null)
                ret += "AND tenantguid = '" + tenantGuid.Value.ToString() + "' ";

            if (userGuid != null)
                ret += "AND userguid = '" + userGuid.Value.ToString() + "' ";

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
            Guid? userGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            ChatThread marker = null)
        {
            string ret = "SELECT COUNT(*) AS record_count FROM 'chatthreads' WHERE guid IS NOT NULL ";

            if (tenantGuid != null)
                ret += "AND tenantguid = '" + tenantGuid.Value.ToString() + "' ";

            if (userGuid != null)
                ret += "AND userguid = '" + userGuid.Value.ToString() + "' ";

            if (marker != null)
                ret += "AND " + MarkerWhereClause(order, marker);

            return ret;
        }

        internal static string Update(ChatThread thread)
        {
            return
                "UPDATE 'chatthreads' SET "
                + "lastupdateutc = '" + DateTime.UtcNow.ToString(TimestampFormat) + "',"
                + "graphguid = " + SqlString(thread.GraphGUID != null ? thread.GraphGUID.Value.ToString() : null) + ","
                + "title = " + SqlString(thread.Title) + " "
                + "WHERE tenantguid = '" + thread.TenantGUID + "' AND guid = '" + thread.GUID + "' "
                + "RETURNING *;";
        }

        internal static string Delete(Guid tenantGuid, Guid guid)
        {
            return "DELETE FROM 'chatthreads' WHERE tenantguid = '" + tenantGuid + "' AND guid = '" + guid + "';";
        }

        internal static string DeleteAllInTenant(Guid tenantGuid)
        {
            return "DELETE FROM 'chatthreads' WHERE tenantguid = '" + tenantGuid + "';";
        }

        private static string OrderByClause(EnumerationOrderEnum order)
        {
            switch (order)
            {
                case EnumerationOrderEnum.CreatedAscending:
                    return "ORDER BY createdutc ASC ";
                case EnumerationOrderEnum.GuidAscending:
                    return "ORDER BY guid ASC ";
                case EnumerationOrderEnum.GuidDescending:
                    return "ORDER BY guid DESC ";
                case EnumerationOrderEnum.NameAscending:
                    return "ORDER BY title ASC ";
                case EnumerationOrderEnum.NameDescending:
                    return "ORDER BY title DESC ";
                default:
                    return "ORDER BY createdutc DESC ";
            }
        }

        private static string MarkerWhereClause(EnumerationOrderEnum order, ChatThread marker)
        {
            switch (order)
            {
                case EnumerationOrderEnum.CreatedAscending:
                    return "createdutc > '" + marker.CreatedUtc.ToString(TimestampFormat) + "' ";
                case EnumerationOrderEnum.CreatedDescending:
                    return "createdutc < '" + marker.CreatedUtc.ToString(TimestampFormat) + "' ";
                case EnumerationOrderEnum.GuidAscending:
                    return "guid > '" + marker.GUID + "' ";
                case EnumerationOrderEnum.GuidDescending:
                    return "guid < '" + marker.GUID + "' ";
                case EnumerationOrderEnum.NameAscending:
                    return "title > '" + marker.Title + "' ";
                case EnumerationOrderEnum.NameDescending:
                    return "title < '" + marker.Title + "' ";
                default:
                    return "createdutc < '" + marker.CreatedUtc.ToString(TimestampFormat) + "' ";
            }
        }

        private static string SqlString(string val)
        {
            if (String.IsNullOrEmpty(val)) return "NULL";
            return "'" + Sanitizer.Sanitize(val) + "'";
        }
    }
}
