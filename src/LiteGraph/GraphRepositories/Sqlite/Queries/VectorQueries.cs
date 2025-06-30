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

    internal static class VectorQueries
    {
        internal static string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        internal static Serializer Serializer = new Serializer();

        internal static string Insert(VectorMetadata vector)
        {
            string ret =
                "INSERT INTO 'vectors' "
                + "VALUES ("
                + "'" + vector.GUID + "',"
                + "'" + vector.TenantGUID + "',"
                + "'" + vector.GraphGUID + "',"
                + (vector.NodeGUID != null ? "'" + vector.NodeGUID.Value + "'" : "NULL") + ","
                + (vector.EdgeGUID != null ? "'" + vector.EdgeGUID.Value + "'" : "NULL") + ","
                + "'" + Sanitizer.Sanitize(vector.Model) + "',"
                + vector.Dimensionality + ","
                + "'" + Sanitizer.Sanitize(vector.Content) + "',"
                + "'" + Serializer.SerializeJson(vector.Vectors, false) + "',"
                + "'" + Sanitizer.Sanitize(vector.CreatedUtc.ToString(TimestampFormat)) + "',"
                + "'" + Sanitizer.Sanitize(vector.LastUpdateUtc.ToString(TimestampFormat)) + "'"
                + ") "
                + "RETURNING *;";

            return ret;
        }

        internal static string InsertMany(Guid tenantGuid, List<VectorMetadata> vectors)
        {
            string ret =
                "INSERT INTO 'vectors' "
                + "VALUES ";

            for (int i = 0; i < vectors.Count; i++)
            {
                if (i > 0) ret += ",";
                ret += "(";
                ret += "'" + vectors[i].GUID + "',"
                    + "'" + tenantGuid + "',"
                    + "'" + vectors[i].GraphGUID + "',"
                    + (vectors[i].NodeGUID != null ? "'" + vectors[i].NodeGUID.Value + "'," : "NULL,")
                    + (vectors[i].EdgeGUID != null ? "'" + vectors[i].EdgeGUID.Value + "'," : "NULL,")
                    + "'" + Sanitizer.Sanitize(vectors[i].Model) + "',"
                    + vectors[i].Dimensionality + ","
                    + "'" + Sanitizer.Sanitize(vectors[i].Content) + "',"
                    + "'" + Serializer.SerializeJson(vectors[i].Vectors, false) + "',"
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
            string ret = "SELECT * FROM 'nodes' WHERE tenantguid = '" + tenantGuid + "' ";
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
                "SELECT * FROM 'nodes' WHERE tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' ";

            ret +=
                "ORDER BY " + Converters.EnumerationOrderToClause(order) + " "
                + "LIMIT " + batchSize + " OFFSET " + skip + ";";
            return ret;
        }

        internal static string SelectMany(Guid tenantGuid, List<Guid> guids)
        {
            string ret = "SELECT * FROM 'vectors' WHERE tenantguid = '" + tenantGuid + "' AND guid IN (";

            for (int i = 0; i < guids.Count; i++)
            {
                if (i > 0) ret += ",";
                ret += "'" + Sanitizer.Sanitize(guids[i].ToString()) + "'";
            }

            ret += ");";
            return ret;
        }

        internal static string SelectByGuid(Guid tenantGuid, Guid guid)
        {
            return "SELECT * FROM 'vectors' WHERE tenantguid = '" + tenantGuid + "' AND guid = '" + guid + "';";
        }

        internal static string SelectByGuids(Guid tenantGuid, List<Guid> guids)
        {
            return
                "SELECT * FROM 'vectors' " +
                "WHERE tenantguid = '" + tenantGuid + "' " +
                "AND guid IN (" +
                string.Join(", ", guids.Select(g => "'" + g + "'")) +
                ");";
        }

        internal static string SelectTenant(
            Guid tenantGuid,
            int batchSize = 100,
            int skip = 0,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            string ret =
                "SELECT * FROM 'vectors' WHERE guid IS NOT NULL " +
                "AND tenantguid = '" + tenantGuid.ToString() + "' ";

            ret +=
                "ORDER BY " + Converters.EnumerationOrderToClause(order) + " "
                + "LIMIT " + batchSize + " OFFSET " + skip + ";";

            return ret;
        }

        internal static string SelectGraph(
            Guid tenantGuid,
            Guid graphGuid,
            int batchSize = 100,
            int skip = 0,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            string ret =
                "SELECT * FROM 'vectors' WHERE guid IS NOT NULL " +
                "AND tenantguid = '" + tenantGuid.ToString() + "' " +
                "AND graphguid = '" + graphGuid.ToString() + "' " +
                "AND nodeguid IS NULL " +
                "AND edgeguid IS NULL ";

            ret +=
                "ORDER BY " + Converters.EnumerationOrderToClause(order) + " "
                + "LIMIT " + batchSize + " OFFSET " + skip + ";";

            return ret;
        }

        internal static string SelectNode(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            int batchSize = 100,
            int skip = 0,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            string ret =
                "SELECT * FROM 'vectors' WHERE guid IS NOT NULL " +
                "AND tenantguid = '" + tenantGuid.ToString() + "' " +
                "AND graphguid = '" + graphGuid.ToString() + "' " +
                "AND nodeguid = '" + nodeGuid.ToString() + "' " +
                "AND edgeguid IS NULL ";

            ret +=
                "ORDER BY " + Converters.EnumerationOrderToClause(order) + " "
                + "LIMIT " + batchSize + " OFFSET " + skip + ";";

            return ret;
        }

        internal static string SelectEdge(
            Guid tenantGuid,
            Guid graphGuid,
            Guid edgeGuid,
            int batchSize = 100,
            int skip = 0,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            string ret =
                "SELECT * FROM 'vectors' WHERE guid IS NOT NULL " +
                "AND tenantguid = '" + tenantGuid.ToString() + "' " +
                "AND graphguid = '" + graphGuid.ToString() + "' " +
                "AND nodeguid IS NULL " +
                "AND edgeguid = '" + edgeGuid.ToString() + "' ";

            ret +=
                "ORDER BY " + Converters.EnumerationOrderToClause(order) + " "
                + "LIMIT " + batchSize + " OFFSET " + skip + ";";

            return ret;
        }

        internal static string GetRecordPage(
            Guid? tenantGuid,
            Guid? graphGuid,
            int batchSize = 100,
            int skip = 0,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            VectorMetadata marker = null)
        {
            string ret = "SELECT * FROM 'vectors' WHERE guid IS NOT NULL ";

            if (tenantGuid != null)
                ret += "AND tenantguid = '" + tenantGuid.Value.ToString() + "' ";

            if (graphGuid != null)
                ret += "AND graphguid = '" + graphGuid.Value.ToString() + "' ";

            if (marker != null)
            {
                ret += "AND " + MarkerWhereClause(order, marker);
            }

            ret += OrderByClause(order);
            ret += "LIMIT " + batchSize + " OFFSET " + skip + ";";
            return ret;
        }

        internal static string GetRecordCount(
            Guid? tenantGuid,
            Guid? graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            VectorMetadata marker = null)
        {
            string ret = "SELECT COUNT(*) AS record_count FROM 'vectors' WHERE guid IS NOT NULL ";

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

        internal static string Update(VectorMetadata vector)
        {
            return
                "UPDATE 'vectors' SET "
                + "lastupdateutc = '" + DateTime.UtcNow.ToString(TimestampFormat) + "',"
                + "nodeguid = " + (vector.NodeGUID != null ? ("'" + vector.NodeGUID.Value + "'") : "NULL") + ","
                + "edgeguid = " + (vector.EdgeGUID != null ? ("'" + vector.EdgeGUID.Value + "'") : "NULL") + ","
                + "model = '" + Sanitizer.Sanitize(vector.Model) + "',"
                + "dimensionality = " + vector.Dimensionality + ","
                + "content = '" + Sanitizer.Sanitize(vector.Content) + "',"
                + "embeddings = '" + Serializer.SerializeJson(vector.Vectors, false) + "' "
                + "WHERE guid = '" + vector.GUID + "' "
                + "RETURNING *;";
        }

        internal static string Delete(Guid tenantGuid, Guid guid)
        {
            return "DELETE FROM 'vectors' WHERE tenantguid = '" + tenantGuid + "' AND guid = '" + guid + "';";
        }

        internal static string DeleteMany(Guid tenantGuid, Guid? graphGuid, List<Guid> nodeGuids, List<Guid> edgeGuids)
        {
            string ret = "DELETE FROM 'vectors' WHERE tenantguid = '" + tenantGuid + "' ";

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
            string ret = "DELETE FROM 'vectors' WHERE tenantguid = '" + tenantGuid + "' "
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
                "DELETE FROM 'vectors' WHERE " +
                "tenantguid = '" + tenantGuid + "';";
            return ret;
        }

        internal static string DeleteAllInGraph(Guid tenantGuid, Guid graphGuid)
        {
            string ret =
                "DELETE FROM 'vectors' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "';";
            return ret;
        }

        internal static string DeleteGraph(Guid tenantGuid, Guid graphGuid)
        {
            string ret =
                "DELETE FROM 'vectors' WHERE " +
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

        private static string MarkerWhereClause(EnumerationOrderEnum order, VectorMetadata marker)
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
