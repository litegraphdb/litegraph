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
    }
}
