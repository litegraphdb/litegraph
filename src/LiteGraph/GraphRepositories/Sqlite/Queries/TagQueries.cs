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

    internal static class TagQueries
    {
        internal static string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        internal static Serializer Serializer = new Serializer();

        internal static string Insert(TagMetadata tag)
        {
            string ret =
                "INSERT INTO 'tags' "
                + "VALUES ("
                + "'" + tag.GUID + "',"
                + "'" + tag.TenantGUID + "',"
                + "'" + tag.GraphGUID + "',"
                + (tag.NodeGUID != null ? "'" + tag.NodeGUID.Value + "'" : "NULL") + ","
                + (tag.EdgeGUID != null ? "'" + tag.EdgeGUID.Value + "'" : "NULL") + ","
                + "'" + Sanitizer.Sanitize(tag.Key) + "',"
                + (!String.IsNullOrEmpty(tag.Value) ? ("'" + Sanitizer.Sanitize(tag.Value) + "'") : "NULL") + ","
                + "'" + Sanitizer.Sanitize(tag.CreatedUtc.ToString(TimestampFormat)) + "',"
                + "'" + Sanitizer.Sanitize(tag.LastUpdateUtc.ToString(TimestampFormat)) + "'"
                + ") "
                + "RETURNING *;";

            return ret;
        }

        internal static string InsertMany(Guid tenantGuid, List<TagMetadata> tags)
        {
            string ret =
                "INSERT INTO 'tags' "
                + "VALUES ";

            for (int i = 0; i < tags.Count; i++)
            {
                if (i > 0) ret += ",";
                ret += "(";
                ret += "'" + tags[i].GUID + "',"
                    + "'" + tenantGuid + "',"
                    + "'" + tags[i].GraphGUID + "',"
                    + (tags[i].NodeGUID != null ? "'" + tags[i].NodeGUID.Value + "'," : "NULL,")
                    + (tags[i].EdgeGUID != null ? "'" + tags[i].EdgeGUID.Value + "'," : "NULL,")
                    + "'" + Sanitizer.Sanitize(tags[i].Key) + "',"
                    + (!String.IsNullOrEmpty(tags[i].Value) ? "'" + Sanitizer.Sanitize(tags[i].Value) + "'," : "NULL,")
                    + "'" + DateTime.UtcNow.ToString(TimestampFormat) + "',"
                    + "'" + DateTime.UtcNow.ToString(TimestampFormat) + "'";
                ret += ")";
            }

            ret += ";";
            return ret;
        }

        internal static string SelectAllInTenant(
            Guid tenantGuid, 
            int batchSize = 100, 
            int skip = 0,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            string ret = "SELECT * FROM 'tags' WHERE tenantguid = '" + tenantGuid + "' ";
            ret +=
                "ORDER BY " + Converters.EnumerationOrderToClause(order) + " "
                + "LIMIT " + batchSize + " OFFSET " + skip + ";";
            return ret;
        }

        internal static string SelectAllInGraph(
            Guid tenantGuid, 
            Guid graphGuid,
            int batchSize = 100, 
            int skip = 0, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            string ret =
                "SELECT * FROM 'tags' WHERE tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' ";

            ret +=
                "ORDER BY " + Converters.EnumerationOrderToClause(order) + " "
                + "LIMIT " + batchSize + " OFFSET " + skip + ";";
            return ret;
        }

        internal static string SelectMany(Guid tenantGuid, List<Guid> guids)
        {
            string ret = "SELECT * FROM 'tags' WHERE tenantguid = '" + tenantGuid + "' AND guid IN (";

            for (int i = 0; i < guids.Count; i++)
            {
                if (i > 0) ret += ",";
                ret += "'" + Sanitizer.Sanitize(guids[i].ToString()) + "'";
            }

            ret += ");";
            return ret;
        }

        internal static string Select(Guid tenantGuid, Guid guid)
        {
            return "SELECT * FROM 'tags' WHERE tenantguid = '" + tenantGuid + "' AND guid = '" + guid + "';";
        }

        internal static string SelectTenant(
            Guid tenantGuid,
            string key,
            string val,
            int batchSize = 100,
            int skip = 0,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            string ret =
                "SELECT * FROM 'tags' WHERE guid IS NOT NULL " +
                "AND tenantguid = '" + tenantGuid.ToString() + "' ";

            if (!String.IsNullOrEmpty(key))
                ret += "AND tagkey = '" + Sanitizer.Sanitize(key) + "' ";

            if (!String.IsNullOrEmpty(val))
                ret += "AND tagvalue = '" + Sanitizer.Sanitize(val) + "' ";

            ret +=
                "ORDER BY " + Converters.EnumerationOrderToClause(order) + " "
                + "LIMIT " + batchSize + " OFFSET " + skip + ";";

            return ret;
        }

        internal static string SelectGraph(
            Guid tenantGuid,
            Guid graphGuid,
            string key,
            string val,
            int batchSize = 100,
            int skip = 0,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            string ret =
                "SELECT * FROM 'tags' WHERE guid IS NOT NULL " +
                "AND tenantguid = '" + tenantGuid.ToString() + "' " +
                "AND graphguid = '" + graphGuid.ToString() + "' " +
                "AND nodeguid IS NULL " +
                "AND edgeguid IS NULL ";

            if (!String.IsNullOrEmpty(key))
                ret += "AND tagkey = '" + Sanitizer.Sanitize(key) + "' ";

            if (!String.IsNullOrEmpty(val))
                ret += "AND tagvalue = '" + Sanitizer.Sanitize(val) + "' ";

            ret +=
                "ORDER BY " + Converters.EnumerationOrderToClause(order) + " "
                + "LIMIT " + batchSize + " OFFSET " + skip + ";";

            return ret;
        }

        internal static string SelectNode(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            string key,
            string val,
            int batchSize = 100,
            int skip = 0,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            string ret =
                "SELECT * FROM 'tags' WHERE guid IS NOT NULL " +
                "AND tenantguid = '" + tenantGuid.ToString() + "' " +
                "AND graphguid = '" + graphGuid.ToString() + "' " + 
                "AND nodeguid = '" + nodeGuid.ToString() + "' " + 
                "AND edgeguid IS NULL ";

            if (!String.IsNullOrEmpty(key))
                ret += "AND tagkey = '" + Sanitizer.Sanitize(key) + "' ";

            if (!String.IsNullOrEmpty(val))
                ret += "AND tagvalue = '" + Sanitizer.Sanitize(val) + "' ";

            ret +=
                "ORDER BY " + Converters.EnumerationOrderToClause(order) + " "
                + "LIMIT " + batchSize + " OFFSET " + skip + ";";

            return ret;
        }

        internal static string SelectEdge(
            Guid tenantGuid,
            Guid graphGuid,
            Guid edgeGuid,
            string key,
            string val,
            int batchSize = 100,
            int skip = 0,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            string ret =
                "SELECT * FROM 'tags' WHERE guid IS NOT NULL " +
                "AND tenantguid = '" + tenantGuid.ToString() + "' " + 
                "AND graphguid = '" + graphGuid.ToString() + "' " + 
                "AND nodeguid IS NULL " + 
                "AND edgeguid = '" + edgeGuid.ToString() + "' ";

            if (!String.IsNullOrEmpty(key))
                ret += "AND tagkey = '" + Sanitizer.Sanitize(key) + "' ";

            if (!String.IsNullOrEmpty(val))
                ret += "AND tagvalue = '" + Sanitizer.Sanitize(val) + "' ";

            ret +=
                "ORDER BY " + Converters.EnumerationOrderToClause(order) + " "
                + "LIMIT " + batchSize + " OFFSET " + skip + ";";

            return ret;
        }

        internal static string GetRecordPage(
            Guid? tenantGuid,
            Guid? graphGuid,
            int batchSize = 100,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            TagMetadata marker = null)
        {
            string ret = "SELECT * FROM 'tags' WHERE guid IS NOT NULL ";

            if (tenantGuid != null)
                ret += "AND tenantguid = '" + tenantGuid.Value.ToString() + "' ";

            if (graphGuid != null)
                ret += "AND graphguid = '" + graphGuid.Value.ToString() + "' ";

            if (marker != null)
            {
                ret += "AND " + MarkerWhereClause(order, marker);
            }

            ret += OrderByClause(order);
            ret += "LIMIT " + batchSize + ";";
            return ret;
        }

        internal static string GetRecordCount(
            Guid? tenantGuid,
            Guid? graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            TagMetadata marker = null)
        {
            string ret = "SELECT COUNT(*) AS record_count FROM 'tags' WHERE guid IS NOT NULL ";

            if (tenantGuid != null)
                ret += "AND tenantguid = '" + tenantGuid.Value.ToString() + "' ";

            if (graphGuid != null)
                ret += "AND graphguid = '" + graphGuid.Value.ToString() + "' ";

            if (marker != null)
            {
                ret += "AND " + MarkerWhereClause(order, marker);
            }

            return ret;
        }

        internal static string Update(TagMetadata tag)
        {
            return
                "UPDATE 'tags' SET "
                + "lastupdateutc = '" + DateTime.UtcNow.ToString(TimestampFormat) + "',"
                + "nodeguid = " + (tag.NodeGUID != null ? ("'" + tag.NodeGUID.Value + "'") : "NULL") + ","
                + "edgeguid = " + (tag.EdgeGUID != null ? ("'" + tag.EdgeGUID.Value + "'") : "NULL") + ","
                + "tagkey = '" + Sanitizer.Sanitize(tag.Key) + "',"
                + "tagvalue = " + (!String.IsNullOrEmpty(tag.Value) ? ("'" + Sanitizer.Sanitize(tag.Value) + "'") : "NULL") + " "
                + "WHERE guid = '" + tag.GUID + "' "
                + "RETURNING *;";
        }

        internal static string Delete(Guid tenantGuid, Guid guid)
        {
            return "DELETE FROM 'tags' WHERE tenantguid = '" + tenantGuid + "' AND guid = '" + guid + "';";
        }

        internal static string DeleteMany(Guid tenantGuid, Guid? graphGuid, List<Guid> nodeGuids, List<Guid> edgeGuids)
        {
            string ret = "DELETE FROM 'tags' WHERE tenantguid = '" + tenantGuid + "' ";

            if (graphGuid != null) ret += "AND graphguid = '" + graphGuid + "' ";

            if (nodeGuids != null && nodeGuids.Count > 0)
            {
                string nodeGuidsStr = string.Join(",", nodeGuids.Select(g => $"'{g}'"));
                ret += "AND nodeguid IN (" + nodeGuidsStr + ") ";
            }

            if (edgeGuids != null && edgeGuids.Count > 0)
            {
                string edgeGuidsStr = string.Join(",", edgeGuids.Select(g => $"'{g}'"));
                ret += "AND edgeguid IN (" + edgeGuidsStr + ") ";
            }

            return ret;
        }

        internal static string DeleteMany(Guid tenantGuid, List<Guid> guids)
        {
            string ret = "DELETE FROM 'tags' WHERE tenantguid = '" + tenantGuid + "' "
                + "AND guid IN (";

            int added = 0;
            foreach (Guid guid in guids)
            {
                if (added > 0) ret += ",";
                ret += "'" + guid + "'";
                added++;
            }

            ret += ");";
            return ret;
        }

        internal static string DeleteAllInTenant(Guid tenantGuid)
        {
            string ret =
                "DELETE FROM 'tags' WHERE " +
                "tenantguid = '" + tenantGuid + "';";
            return ret;
        }

        internal static string DeleteAllInGraph(Guid tenantGuid, Guid graphGuid)
        {
            string ret =
                "DELETE FROM 'tags' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "';";
            return ret;
        }

        internal static string DeleteGraph(Guid tenantGuid, Guid graphGuid)
        {
            string ret =
                "DELETE FROM 'tags' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND nodeguid IS NULL " +
                "AND edgeguid IS NULL;";
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
                case EnumerationOrderEnum.NameAscending:
                case EnumerationOrderEnum.NameDescending:
                case EnumerationOrderEnum.CreatedDescending:
                    return "ORDER BY createdutc DESC ";
                case EnumerationOrderEnum.CreatedAscending:
                    return "ORDER BY createdutc ASC ";
                case EnumerationOrderEnum.GuidAscending:
                    return "ORDER BY guid ASC ";
                case EnumerationOrderEnum.GuidDescending:
                    return "ORDER BY guid DESC ";
                default:
                    return "ORDER BY createdutc DESC ";
            }
        }

        private static string MarkerWhereClause(EnumerationOrderEnum order, TagMetadata marker)
        {
            switch (order)
            {
                case EnumerationOrderEnum.CostAscending:
                case EnumerationOrderEnum.CostDescending:
                case EnumerationOrderEnum.LeastConnected:
                case EnumerationOrderEnum.MostConnected:
                case EnumerationOrderEnum.NameAscending:
                case EnumerationOrderEnum.NameDescending:
                    return "createdutc < '" + marker.CreatedUtc.ToString(TimestampFormat) + "' ";
                case EnumerationOrderEnum.CreatedAscending:
                    return "createdutc > '" + marker.CreatedUtc.ToString(TimestampFormat) + "' ";
                case EnumerationOrderEnum.CreatedDescending:
                    return "createdutc < '" + marker.CreatedUtc.ToString(TimestampFormat) + "' ";
                case EnumerationOrderEnum.GuidAscending:
                    return "guid > '" + marker.GUID + "' ";
                case EnumerationOrderEnum.GuidDescending:
                    return "guid < '" + marker.GUID + "' ";
                default:
                    return "guid IS NOT NULL ";
            }
        }
    }
}
