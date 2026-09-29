namespace LiteGraph.GraphRepositories.Postgresql.Queries
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using ExpressionTree;
    using LiteGraph.Serialization;

    internal static class CredentialQueries
    {
        internal static string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        internal static Serializer Serializer = new Serializer();

        internal static string Insert(Credential cred)
        {
            string ret =
                "INSERT INTO 'creds' "
                + "(guid, tenantguid, userguid, name, bearertoken, active, scopes, graphguids, createdutc, lastupdateutc) "
                + "VALUES ("
                + SqlString(cred.GUID.ToString()) + ","
                + SqlString(cred.TenantGUID.ToString()) + ","
                + SqlString(cred.UserGUID.ToString()) + ","
                + SqlString(cred.Name) + ","
                + SqlString(cred.BearerToken) + ","
                + (cred.Active ? "1" : "0") + ","
                + SqlJson(Serializer.SerializeJson(cred.Scopes, false)) + ","
                + SqlJson(Serializer.SerializeJson(cred.GraphGUIDs, false)) + ","
                + SqlString(cred.CreatedUtc.ToString(TimestampFormat)) + ","
                + SqlString(cred.LastUpdateUtc.ToString(TimestampFormat))
                + ") "
                + "RETURNING *;";

            return ret;
        }

        internal static string SelectByToken(string bearerToken)
        {
            return "SELECT * FROM 'creds' WHERE bearertoken = '" + Sanitizer.Sanitize(bearerToken) + "';";
        }

        internal static string SelectByGuid(Guid tenantGuid, Guid guid)
        {
            return "SELECT * FROM 'creds' WHERE tenantguid = '" + tenantGuid + "' AND guid = '" + guid + "';";
        }

        internal static string SelectByGuids(Guid tenantGuid, List<Guid> guids)
        {
            return
                "SELECT * FROM 'creds' " +
                "WHERE tenantguid = '" + tenantGuid + "' " +
                "AND guid IN (" +
                string.Join(", ", guids.Select(g => "'" + g + "'")) + 
                ");";
        }

        internal static string SelectAllInTenant(
            Guid tenantGuid, 
            int batchSize = 100, 
            int skip = 0, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            string ret = "SELECT * FROM 'creds' WHERE tenantguid = '" + tenantGuid + "' ";
            ret +=
                "ORDER BY " + Converters.EnumerationOrderToClause(order) + " "
                + "LIMIT " + batchSize + " OFFSET " + skip + ";";
            return ret;
        }

        internal static string Select(
            Guid? tenantGuid,
            Guid? userGuid,
            string bearerToken,
            int batchSize = 100,
            int skip = 0,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            string ret =
                "SELECT * FROM 'creds' WHERE guid IS NOT NULL ";

            if (tenantGuid != null)
                ret += "AND tenantguid = '" + tenantGuid.Value.ToString() + "' ";

            if (userGuid != null)
                ret += "AND userGuid = '" + userGuid.Value.ToString() + "' ";

            if (!String.IsNullOrEmpty(bearerToken))
                ret += "AND bearertoken = '" + Sanitizer.Sanitize(bearerToken) + "' ";

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
            Credential marker = null)
        {
            string ret = "SELECT * FROM 'creds' WHERE guid IS NOT NULL ";

            if (tenantGuid != null)
                ret += "AND tenantguid = '" + tenantGuid.Value.ToString() + "' ";

            if (userGuid != null)
                ret += "AND userguid = '" + userGuid.Value.ToString() + "' ";

            if (marker != null)
            {
                ret += "AND " + MarkerWhereClause(order, marker);
            }

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
            Credential marker = null)
        {
            string ret = "SELECT COUNT(*) AS record_count FROM 'creds' WHERE guid IS NOT NULL ";

            if (tenantGuid != null)
                ret += "AND tenantguid = '" + tenantGuid.Value.ToString() + "' ";

            if (userGuid != null)
                ret += "AND userguid = '" + userGuid.Value.ToString() + "' ";

            if (marker != null)
            {
                ret += "AND " + MarkerWhereClause(order, marker);
            }

            return ret;
        }

        internal static string Update(Credential cred)
        {
            return
                "UPDATE 'creds' SET "
                + "lastupdateutc = '" + DateTime.UtcNow.ToString(TimestampFormat) + "',"
                + "name = '" + Sanitizer.Sanitize(cred.Name) + "',"
                + "active = " + (cred.Active ? "1" : "0") + ","
                + "scopes = " + SqlJson(Serializer.SerializeJson(cred.Scopes, false)) + ","
                + "graphguids = " + SqlJson(Serializer.SerializeJson(cred.GraphGUIDs, false)) + " "
                + "WHERE guid = '" + cred.GUID + "' "
                + "RETURNING *;";
        }

        internal static string Delete(Guid tenantGuid, Guid guid)
        {
            return "DELETE FROM 'creds' WHERE tenantguid = '" + tenantGuid + "' AND guid = '" + guid + "';";
        }

        internal static string DeleteUser(Guid tenantGuid, Guid userGuid)
        {
            return "DELETE FROM 'creds' WHERE tenantguid = '" + tenantGuid + "' AND userguid = '" + userGuid + "';";
        }

        internal static string DeleteAllInTenant(Guid tenantGuid)
        {
            return "DELETE FROM 'creds' WHERE tenantguid = '" + tenantGuid + "';";
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
                case EnumerationOrderEnum.NameAscending:
                    return KeysetOrdering.OrderBy(false, "name", "guid");
                case EnumerationOrderEnum.NameDescending:
                    return KeysetOrdering.OrderBy(true, "name", "guid");
                default:
                    return KeysetOrdering.OrderBy(true, "createdutc", "guid");
            }
        }

        private static string MarkerWhereClause(EnumerationOrderEnum order, Credential marker)
        {
            switch (order)
            {
                case EnumerationOrderEnum.CreatedAscending:
                    return KeysetOrdering.After(false, new string[] { "createdutc", "guid" }, new string[] { "'" + marker.CreatedUtc.ToString(TimestampFormat) + "'", "'" + marker.GUID + "'" });
                case EnumerationOrderEnum.GuidAscending:
                    return KeysetOrdering.After(false, new string[] { "guid" }, new string[] { "'" + marker.GUID + "'" });
                case EnumerationOrderEnum.GuidDescending:
                    return KeysetOrdering.After(true, new string[] { "guid" }, new string[] { "'" + marker.GUID + "'" });
                case EnumerationOrderEnum.NameAscending:
                    return KeysetOrdering.After(false, new string[] { "name", "guid" }, new string[] { "'" + Sanitizer.Sanitize(marker.Name) + "'", "'" + marker.GUID + "'" });
                case EnumerationOrderEnum.NameDescending:
                    return KeysetOrdering.After(true, new string[] { "name", "guid" }, new string[] { "'" + Sanitizer.Sanitize(marker.Name) + "'", "'" + marker.GUID + "'" });
                default:
                    return KeysetOrdering.After(true, new string[] { "createdutc", "guid" }, new string[] { "'" + marker.CreatedUtc.ToString(TimestampFormat) + "'", "'" + marker.GUID + "'" });
            }
        }

        private static string SqlString(string val)
        {
            if (String.IsNullOrEmpty(val)) return "NULL";
            return "'" + Sanitizer.Sanitize(val) + "'";
        }

        private static string SqlJson(string json)
        {
            if (String.IsNullOrEmpty(json)) return "NULL";
            return "'" + Sanitizer.SanitizeJson(json) + "'";
        }
    }
}



