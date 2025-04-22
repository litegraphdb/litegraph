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

    internal static class UserQueries
    {
        internal static string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        internal static Serializer Serializer = new Serializer();

        internal static string Insert(UserMaster user)
        {
            string ret =
                "INSERT INTO 'users' "
                + "VALUES ("
                + "'" + user.GUID + "',"
                + "'" + user.TenantGUID + "',"
                + "'" + Sanitizer.Sanitize(user.FirstName) + "',"
                + "'" + Sanitizer.Sanitize(user.LastName) + "',"
                + "'" + Sanitizer.Sanitize(user.Email) + "',"
                + "'" + Sanitizer.Sanitize(user.Password) + "',"
                + (user.Active ? "1" : "0") + ","
                + "'" + Sanitizer.Sanitize(user.CreatedUtc.ToString(TimestampFormat)) + "',"
                + "'" + Sanitizer.Sanitize(user.LastUpdateUtc.ToString(TimestampFormat)) + "'"
                + ") "
                + "RETURNING *;";

            return ret;
        }

        internal static string SelectAllInTenant(Guid tenantGuid, int batchSize = 100, int skip = 0, EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            string ret = "SELECT * FROM 'users' WHERE tenantguid = '" + tenantGuid + "' ";
            ret +=
                "ORDER BY " + Converters.EnumerationOrderToClause(order) + " "
                + "LIMIT " + batchSize + " OFFSET " + skip + ";";
            return ret;
        }

        internal static string Select(Guid tenantGuid, string email)
        {
            return "SELECT * FROM 'users' WHERE tenantguid = '" + tenantGuid + "' AND email = '" + Sanitizer.Sanitize(email) + "';";
        }

        internal static string Select(Guid tenantGuid, Guid guid)
        {
            return "SELECT * FROM 'users' WHERE tenantguid = '" + tenantGuid + "' AND guid = '" + guid + "';";
        }

        internal static string SelectMany(
            Guid? tenantGuid,
            string email,
            int batchSize = 100,
            int skip = 0,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            string ret =
                "SELECT * FROM 'users' WHERE guid IS NOT NULL ";

            if (tenantGuid != null)
                ret += "AND tenantguid = '" + tenantGuid.Value.ToString() + "' ";

            if (!String.IsNullOrEmpty(email))
                ret += "AND email = '" + Sanitizer.Sanitize(email) + "' ";

            ret +=
                "ORDER BY " + Converters.EnumerationOrderToClause(order) + " "
                + "LIMIT " + batchSize + " OFFSET " + skip + ";";

            return ret;
        }

        internal static string SelectTenantsByEmail(string email)
        {
            string ret =
                "SELECT * FROM 'tenants' WHERE " +
                "guid IN (" +
                "SELECT tenantguid FROM users WHERE email = '" + Sanitizer.Sanitize(email) + "'" +
                ");";
            return ret;
        }

        internal static string Update(UserMaster user)
        {
            return
                "UPDATE 'users' SET "
                + "lastupdateutc = '" + DateTime.UtcNow.ToString(TimestampFormat) + "',"
                + "firstname = '" + Sanitizer.Sanitize(user.FirstName) + "',"
                + "lastname = '" + Sanitizer.Sanitize(user.LastName) + "',"
                + "email = '" + Sanitizer.Sanitize(user.Email) + "',"
                + "password = '" + Sanitizer.Sanitize(user.Password) + "',"
                + "active = " + (user.Active ? "1" : "0") + " "
                + "WHERE guid = '" + user.GUID + "' "
                + "RETURNING *;";
        }

        internal static string Delete(Guid tenantGuid, Guid userGuid)
        {
            string ret = string.Empty;

            // First delete any credentials associated with this user
            ret +=
                "DELETE FROM 'creds' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND userguid = '" + userGuid + "'; ";

            // Then delete the user
            ret +=
                "DELETE FROM 'users' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND guid = '" + userGuid + "'; ";

            return ret;
        }

        internal static string DeleteAllInTenant(Guid tenantGuid)
        {
            string ret = string.Empty;

            // First delete all credentials in the tenant
            ret += "DELETE FROM 'creds' WHERE tenantguid = '" + tenantGuid + "'; ";

            // Then delete all users in the tenant
            ret += "DELETE FROM 'users' WHERE tenantguid = '" + tenantGuid + "'; ";

            return ret;
        }
    }
}
