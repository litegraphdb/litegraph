namespace LiteGraph.GraphRepositories.Postgresql.Queries
{
    using System;
    using System.Globalization;

    internal static class ChatEndpointQueries
    {
        internal static string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        internal static string Insert(ChatEndpoint endpoint)
        {
            string ret =
                "INSERT INTO 'chatendpoints' "
                + "(guid, tenantguid, name, endpointtype, provider, endpoint, apikey, model, "
                + "contextwindowtokens, maxoutputtokens, temperature, timeoutms, maxconcurrentrequests, active, "
                + "healthcheckenabled, healthcheckurl, healthcheckmethod, healthcheckintervalms, "
                + "healthchecktimeoutms, healthcheckexpectedstatuscode, healthythreshold, "
                + "unhealthythreshold, healthcheckuseauth, createdutc, lastupdateutc) "
                + "VALUES ("
                + SqlString(endpoint.GUID.ToString()) + ","
                + SqlString(endpoint.TenantGUID.ToString()) + ","
                + SqlString(endpoint.Name) + ","
                + SqlString(endpoint.EndpointType.ToString()) + ","
                + SqlString(endpoint.Provider.ToString()) + ","
                + SqlString(endpoint.Endpoint) + ","
                + SqlString(endpoint.ApiKey) + ","
                + SqlString(endpoint.Model) + ","
                + endpoint.ContextWindowTokens + ","
                + endpoint.MaxOutputTokens + ","
                + endpoint.Temperature.ToString(CultureInfo.InvariantCulture) + ","
                + endpoint.TimeoutMs + ","
                + endpoint.MaxConcurrentRequests + ","
                + (endpoint.Active ? "1" : "0") + ","
                + (endpoint.HealthCheckEnabled ? "1" : "0") + ","
                + SqlString(endpoint.HealthCheckUrl) + ","
                + SqlString(endpoint.HealthCheckMethod) + ","
                + endpoint.HealthCheckIntervalMs + ","
                + endpoint.HealthCheckTimeoutMs + ","
                + endpoint.HealthCheckExpectedStatusCode + ","
                + endpoint.HealthyThreshold + ","
                + endpoint.UnhealthyThreshold + ","
                + (endpoint.HealthCheckUseAuth ? "1" : "0") + ","
                + SqlString(endpoint.CreatedUtc.ToString(TimestampFormat)) + ","
                + SqlString(endpoint.LastUpdateUtc.ToString(TimestampFormat))
                + ") "
                + "RETURNING *;";

            return ret;
        }

        internal static string SelectByGuid(Guid tenantGuid, Guid guid)
        {
            return "SELECT * FROM 'chatendpoints' WHERE tenantguid = '" + tenantGuid + "' AND guid = '" + guid + "';";
        }

        internal static string SelectAllInTenant(
            Guid tenantGuid,
            ChatEndpointTypeEnum? endpointType,
            int batchSize = 100,
            int skip = 0,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            string ret = "SELECT * FROM 'chatendpoints' WHERE tenantguid = '" + tenantGuid + "' ";

            if (endpointType != null)
                ret += "AND endpointtype = '" + endpointType.Value.ToString() + "' ";

            ret +=
                "ORDER BY " + Converters.EnumerationOrderToClause(order) + " "
                + "LIMIT " + batchSize + " OFFSET " + skip + ";";
            return ret;
        }

        internal static string GetRecordPage(
            Guid? tenantGuid,
            ChatEndpointTypeEnum? endpointType = null,
            int batchSize = 100,
            int skip = 0,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            ChatEndpoint marker = null)
        {
            string ret = "SELECT * FROM 'chatendpoints' WHERE guid IS NOT NULL ";

            if (tenantGuid != null)
                ret += "AND tenantguid = '" + tenantGuid.Value.ToString() + "' ";

            if (endpointType != null)
                ret += "AND endpointtype = '" + endpointType.Value.ToString() + "' ";

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
            ChatEndpointTypeEnum? endpointType = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            ChatEndpoint marker = null)
        {
            string ret = "SELECT COUNT(*) AS record_count FROM 'chatendpoints' WHERE guid IS NOT NULL ";

            if (tenantGuid != null)
                ret += "AND tenantguid = '" + tenantGuid.Value.ToString() + "' ";

            if (endpointType != null)
                ret += "AND endpointtype = '" + endpointType.Value.ToString() + "' ";

            if (marker != null)
                ret += "AND " + MarkerWhereClause(order, marker);

            return ret;
        }

        internal static string Update(ChatEndpoint endpoint)
        {
            return
                "UPDATE 'chatendpoints' SET "
                + "lastupdateutc = '" + DateTime.UtcNow.ToString(TimestampFormat) + "',"
                + "name = " + SqlString(endpoint.Name) + ","
                + "endpointtype = " + SqlString(endpoint.EndpointType.ToString()) + ","
                + "provider = " + SqlString(endpoint.Provider.ToString()) + ","
                + "endpoint = " + SqlString(endpoint.Endpoint) + ","
                + "apikey = " + SqlString(endpoint.ApiKey) + ","
                + "model = " + SqlString(endpoint.Model) + ","
                + "contextwindowtokens = " + endpoint.ContextWindowTokens + ","
                + "maxoutputtokens = " + endpoint.MaxOutputTokens + ","
                + "temperature = " + endpoint.Temperature.ToString(CultureInfo.InvariantCulture) + ","
                + "timeoutms = " + endpoint.TimeoutMs + ","
                + "maxconcurrentrequests = " + endpoint.MaxConcurrentRequests + ","
                + "active = " + (endpoint.Active ? "1" : "0") + ","
                + "healthcheckenabled = " + (endpoint.HealthCheckEnabled ? "1" : "0") + ","
                + "healthcheckurl = " + SqlString(endpoint.HealthCheckUrl) + ","
                + "healthcheckmethod = " + SqlString(endpoint.HealthCheckMethod) + ","
                + "healthcheckintervalms = " + endpoint.HealthCheckIntervalMs + ","
                + "healthchecktimeoutms = " + endpoint.HealthCheckTimeoutMs + ","
                + "healthcheckexpectedstatuscode = " + endpoint.HealthCheckExpectedStatusCode + ","
                + "healthythreshold = " + endpoint.HealthyThreshold + ","
                + "unhealthythreshold = " + endpoint.UnhealthyThreshold + ","
                + "healthcheckuseauth = " + (endpoint.HealthCheckUseAuth ? "1" : "0") + " "
                + "WHERE tenantguid = '" + endpoint.TenantGUID + "' AND guid = '" + endpoint.GUID + "' "
                + "RETURNING *;";
        }

        internal static string Delete(Guid tenantGuid, Guid guid)
        {
            return "DELETE FROM 'chatendpoints' WHERE tenantguid = '" + tenantGuid + "' AND guid = '" + guid + "';";
        }

        internal static string DeleteAllInTenant(Guid tenantGuid)
        {
            return "DELETE FROM 'chatendpoints' WHERE tenantguid = '" + tenantGuid + "';";
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

        private static string MarkerWhereClause(EnumerationOrderEnum order, ChatEndpoint marker)
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
    }
}
