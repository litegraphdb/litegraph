namespace LiteGraph.GraphRepositories.Sqlite.Queries
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

        internal static string Select(string name)
        {
            return "SELECT * FROM 'tenants' WHERE name = '" + Sanitizer.Sanitize(name) + "';";
        }

        internal static string Select(Guid guid)
        {
            return "SELECT * FROM 'tenants' WHERE guid = '" + guid.ToString() + "';";
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
            ret += "LIMIT " + batchSize + " OFFSET " + skip + ";";
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
                case EnumerationOrderEnum.CostAscending:
                case EnumerationOrderEnum.CostDescending:
                case EnumerationOrderEnum.LeastConnected:
                case EnumerationOrderEnum.MostConnected:
                case EnumerationOrderEnum.CreatedDescending:
                    return "ORDER BY createdutc DESC ";
                case EnumerationOrderEnum.CreatedAscending:
                    return "ORDER BY createdutc ASC ";
                case EnumerationOrderEnum.GuidAscending:
                    return "ORDER BY guid ASC ";
                case EnumerationOrderEnum.GuidDescending:
                    return "ORDER BY guid DESC ";
                case EnumerationOrderEnum.NameAscending:
                    return "ORDER BY name ASC ";
                case EnumerationOrderEnum.NameDescending:
                    return "ORDER BY name DESC ";
                default:
                    return "ORDER BY createdutc DESC ";
            }
        }

        private static string MarkerWhereClause(EnumerationOrderEnum order, TenantMetadata marker)
        {
            switch (order)
            {
                case EnumerationOrderEnum.CostAscending:
                case EnumerationOrderEnum.CostDescending:
                case EnumerationOrderEnum.LeastConnected:
                case EnumerationOrderEnum.MostConnected:
                    return "createdutc < '" + marker.CreatedUtc.ToString(TimestampFormat) + "' ";
                case EnumerationOrderEnum.CreatedAscending:
                    return "createdutc > '" + marker.CreatedUtc.ToString(TimestampFormat) + "' ";
                case EnumerationOrderEnum.CreatedDescending:
                    return "createdutc < '" + marker.CreatedUtc.ToString(TimestampFormat) + "' ";
                case EnumerationOrderEnum.GuidAscending:
                    return "guid > '" + marker.GUID + "' ";
                case EnumerationOrderEnum.GuidDescending:
                    return "guid < '" + marker.GUID + "' ";
                case EnumerationOrderEnum.NameAscending:
                    return "name > '" + marker.Name + "' ";
                case EnumerationOrderEnum.NameDescending:
                    return "name < '" + marker.Name + "' ";
                default:
                    return "guid IS NOT NULL ";
            }
        }
    }
}
