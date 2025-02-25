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

    internal static class Vectors
    {
        internal static string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        internal static SerializationHelper Serializer = new SerializationHelper();

        #region Vectors

        internal static string InsertVectorQuery(VectorMetadata vector)
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

        internal static string InsertMultipleVectorsQuery(Guid tenantGuid, Guid graphGuid, List<VectorMetadata> vectors)
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

        internal static string SelectMultipleVectorsQuery(Guid tenantGuid, List<Guid> guids)
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

        internal static string SelectVectorQuery(Guid tenantGuid, Guid guid)
        {
            return "SELECT * FROM 'vectors' WHERE tenantguid = '" + tenantGuid + "' AND guid = '" + guid + "';";
        }

        internal static string SelectTenantVectorsQuery(
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

        internal static string SelectGraphVectorsQuery(
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

        internal static string SelectNodeVectorsQuery(
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

        internal static string SelectEdgeVectorsQuery(
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

        internal static string UpdateVectorQuery(VectorMetadata vector)
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

        internal static string DeleteVectorQuery(Guid tenantGuid, Guid guid)
        {
            return "DELETE FROM 'vectors' WHERE tenantguid = '" + tenantGuid + "' AND guid = '" + guid + "';";
        }

        internal static string DeleteVectorsQuery(Guid tenantGuid, Guid? graphGuid, List<Guid> nodeGuids, List<Guid> edgeGuids)
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

        internal static string DeleteAllTenantVectors(Guid tenantGuid, Guid graphGuid)
        {
            string ret =
                "DELETE FROM 'vectors' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "';";
            return ret;
        }

        internal static string DeleteAllGraphVectors(Guid tenantGuid, Guid graphGuid)
        {
            string ret =
                "DELETE FROM 'vectors' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "';";
            return ret;
        }

        internal static string DeleteGraphVectors(Guid tenantGuid, Guid graphGuid)
        {
            string ret =
                "DELETE FROM 'vectors' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND nodeguid IS NULL " +
                "AND edgeguid IS NULL;";
            return ret;
        }

        internal static string DeleteNodeVectors(Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
        {
            string ret =
                "DELETE FROM 'vectors' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND nodeguid = '" + nodeGuid + "' " +
                "AND edgeguid IS NULL;";
            return ret;
        }

        internal static string DeleteEdgeVectors(Guid tenantGuid, Guid graphGuid, Guid edgeGuid)
        {
            string ret =
                "DELETE FROM 'vectors' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND nodeguid IS NULL " +
                "AND edgeguid = '" + edgeGuid + "';";
            return ret;
        }

        #endregion
    }
}
