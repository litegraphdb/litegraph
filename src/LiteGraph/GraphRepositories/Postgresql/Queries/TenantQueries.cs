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

    internal static class TenantQueries
    {
        internal static string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        internal static Serializer Serializer = new Serializer();

        internal static string Insert(TenantMetadata tenant)
        {
            string ret =
                "INSERT INTO 'tenants' "
                + "(guid, name, active, createdutc, lastupdateutc) "
                + "VALUES ("
                + "'" + tenant.GUID + "',"
                + "'" + Sanitizer.Sanitize(tenant.Name) + "',"
                + (tenant.Active ? "1" : "0") + ","
                + "'" + Sanitizer.Sanitize(tenant.CreatedUtc.ToString(TimestampFormat)) + "',"
                + "'" + Sanitizer.Sanitize(tenant.LastUpdateUtc.ToString(TimestampFormat)) + "'"
                + ") "
                + "RETURNING *;";

            return ret;
        }

        internal static string SelectByName(string name)
        {
            return "SELECT * FROM 'tenants' WHERE name = '" + Sanitizer.Sanitize(name) + "';";
        }

        internal static string SelectByGuid(Guid guid)
        {
            return "SELECT * FROM 'tenants' WHERE guid = '" + guid.ToString() + "';";
        }

        internal static string SelectByGuids(List<Guid> guids)
        {
            return
                "SELECT * FROM 'tenants' " +
                "WHERE guid IN (" +
                string.Join(", ", guids.Select(g => "'" + g + "'")) +
                ");";
        }

        internal static string SelectMany(
            int batchSize = 100,
            int skip = 0,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            string ret =
                "SELECT * FROM 'tenants' WHERE guid IS NOT NULL "
                + "ORDER BY " + Converters.EnumerationOrderToClause(order) + " "
                + "LIMIT " + batchSize + " OFFSET " + skip + ";";

            return ret;
        }

        internal static string GetRecordPage(
            int batchSize = 100,
            int skip = 0,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            TenantMetadata marker = null)
        {
            string ret = "SELECT * FROM 'tenants' WHERE guid IS NOT NULL ";

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
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            TenantMetadata marker = null)
        {
            string ret = "SELECT COUNT(*) AS record_count FROM 'tenants' WHERE guid IS NOT NULL ";

            if (marker != null)
            {
                ret += "AND " + MarkerWhereClause(order, marker);
            }

            return ret;
        }

        internal static string Update(TenantMetadata tenant)
        {
            return
                "UPDATE 'tenants' SET "
                + "lastupdateutc = '" + DateTime.UtcNow.ToString(TimestampFormat) + "',"
                + "name = '" + Sanitizer.Sanitize(tenant.Name) + "',"
                + "active = " + (tenant.Active ? "1" : "0") + " "
                + "WHERE guid = '" + tenant.GUID + "' "
                + "RETURNING *;";
        }

        internal static string Delete(Guid tenantGuid)
        {
            string ret = string.Empty;
            ret += "DELETE FROM 'labels' WHERE tenantguid = '" + tenantGuid + "'; ";
            ret += "DELETE FROM 'tags' WHERE tenantguid = '" + tenantGuid + "'; ";
            ret += "DELETE FROM 'vectors' WHERE tenantguid = '" + tenantGuid + "'; ";
            ret += "DELETE FROM 'edges' WHERE tenantguid = '" + tenantGuid + "'; ";
            ret += "DELETE FROM 'nodes' WHERE tenantguid = '" + tenantGuid + "'; ";
            ret += "DELETE FROM 'graphs' WHERE tenantguid = '" + tenantGuid + "'; ";
            ret += "DELETE FROM 'creds' WHERE tenantguid = '" + tenantGuid + "'; ";
            ret += "DELETE FROM 'users' WHERE tenantguid = '" + tenantGuid + "'; ";
            ret += "DELETE FROM 'chatfeedback' WHERE tenantguid = '" + tenantGuid + "'; ";
            ret += "DELETE FROM 'chatturns' WHERE tenantguid = '" + tenantGuid + "'; ";
            ret += "DELETE FROM 'chatthreads' WHERE tenantguid = '" + tenantGuid + "'; ";
            ret += "DELETE FROM 'chatendpoints' WHERE tenantguid = '" + tenantGuid + "'; ";
            ret += "DELETE FROM 'chatsettings' WHERE tenantguid = '" + tenantGuid + "'; ";
            ret += "DELETE FROM 'tenants' WHERE guid = '" + tenantGuid + "'; ";
            return ret;
        }

        internal static string GetStatistics(Guid? tenantGuid = null)
        {
            string ret = "";
            if (tenantGuid == null)
            {
                // Return statistics for all tenants
                ret = "SELECT " +
                    "t.guid, " +
                    "(SELECT COUNT(DISTINCT guid) FROM graphs g WHERE g.tenantguid = t.guid) AS graphs, " +
                    "(SELECT COUNT(DISTINCT guid) FROM nodes n WHERE n.tenantguid = t.guid) AS nodes, " +
                    "(SELECT COUNT(DISTINCT guid) FROM edges e WHERE e.tenantguid = t.guid) AS edges, " +
                    "(SELECT COUNT(DISTINCT guid) FROM labels l WHERE l.tenantguid = t.guid) AS labels, " +
                    "(SELECT COUNT(DISTINCT guid) FROM tags tg WHERE tg.tenantguid = t.guid) AS tags, " +
                    "(SELECT COUNT(DISTINCT guid) FROM vectors v WHERE v.tenantguid = t.guid) AS vectors " +
                    "FROM tenants t " +
                    "ORDER BY t.guid";
            }
            else
            {
                // Return statistics for a specific tenant
                ret = "SELECT " +
                    "t.guid, " +
                    "(SELECT COUNT(DISTINCT guid) FROM graphs g WHERE g.tenantguid = t.guid) AS graphs, " +
                    "(SELECT COUNT(DISTINCT guid) FROM nodes n WHERE n.tenantguid = t.guid) AS nodes, " +
                    "(SELECT COUNT(DISTINCT guid) FROM edges e WHERE e.tenantguid = t.guid) AS edges, " +
                    "(SELECT COUNT(DISTINCT guid) FROM labels l WHERE l.tenantguid = t.guid) AS labels, " +
                    "(SELECT COUNT(DISTINCT guid) FROM tags tg WHERE tg.tenantguid = t.guid) AS tags, " +
                    "(SELECT COUNT(DISTINCT guid) FROM vectors v WHERE v.tenantguid = t.guid) AS vectors " +
                    "FROM tenants t " +
                    "WHERE t.guid = '" + tenantGuid.Value + "'";
            }

            ret += "; ";
            return ret;
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

        private static string MarkerWhereClause(EnumerationOrderEnum order, TenantMetadata marker)
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
    }
}



