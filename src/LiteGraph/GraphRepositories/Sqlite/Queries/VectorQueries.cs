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
                + Converters.BytesToHex(Converters.VectorToBlob(vector.Vectors)) + ","
                + "'" + Sanitizer.Sanitize(vector.CreatedUtc.ToString(TimestampFormat)) + "',"
                + "'" + Sanitizer.Sanitize(vector.LastUpdateUtc.ToString(TimestampFormat)) + "'"
                + ") "
                + "RETURNING *;";
            return ret;
        }

        internal static string InsertMany(Guid tenantGuid, List<VectorMetadata> vectors)
        {
            if (vectors == null || vectors.Count == 0) return string.Empty;

            StringBuilder ret = new StringBuilder();
            ret.Append("BEGIN TRANSACTION; ");
            ret.Append("INSERT INTO 'vectors' (guid, tenantguid, graphguid, nodeguid, edgeguid, model, dimensionality, content, embeddings, createdutc, lastupdateutc) VALUES ");

            List<string> values = new List<string>();
            foreach (var vector in vectors)
            {
                string vectorsString = "NULL";
                if (vector.Vectors != null && vector.Vectors.Count > 0)
                {
                    vectorsString = Converters.BytesToHex(Converters.VectorToBlob(vector.Vectors));
                }

                values.Add(
                    "('" + vector.GUID + "', " +
                    "'" + tenantGuid + "', " +
                    "'" + vector.GraphGUID + "', " +
                    (vector.NodeGUID != null ? "'" + vector.NodeGUID.Value + "'" : "NULL") + ", " +
                    (vector.EdgeGUID != null ? "'" + vector.EdgeGUID.Value + "'" : "NULL") + ", " +
                    "'" + Sanitizer.Sanitize(vector.Model) + "', " +
                    vector.Dimensionality + ", " +
                    "'" + Sanitizer.Sanitize(vector.Content) + "', " +
                    vectorsString + ", " +
                    "'" + DateTime.UtcNow.ToString(TimestampFormat) + "', " +
                    "'" + DateTime.UtcNow.ToString(TimestampFormat) + "')");
            }

            ret.Append(string.Join(", ", values));
            ret.Append("; COMMIT;");
            return ret.ToString();
        }

        internal static string SelectAllInTenant(
            Guid tenantGuid,
            int batchSize = 100,
            int skip = 0,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            string ret = "SELECT * FROM 'vectors' WHERE tenantguid = '" + tenantGuid + "' ";
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
                "SELECT * FROM 'vectors' WHERE tenantguid = '" + tenantGuid + "' " +
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

        // New method for searching graph vectors with filters
        internal static string SelectGraphVectorsWithFilters(
            Guid tenantGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr graphFilter = null,
            int batchSize = 100,
            int skip = 0)
        {
            string ret = "SELECT DISTINCT vectors.* FROM 'vectors' ";
            ret += "INNER JOIN 'graphs' ON vectors.graphguid = graphs.guid AND vectors.tenantguid = graphs.tenantguid ";

            if (labels != null && labels.Count > 0)
            {
                ret += "INNER JOIN 'labels' "
                    + "ON graphs.guid = labels.graphguid "
                    + "AND graphs.tenantguid = labels.tenantguid "
                    + "AND labels.nodeguid IS NULL "
                    + "AND labels.edgeguid IS NULL ";
            }

            if (tags != null && tags.Count > 0)
            {
                int added = 1;
                foreach (string key in tags.AllKeys)
                {
                    ret +=
                        "INNER JOIN 'tags' t" + added.ToString() + " " +
                        "ON graphs.guid = t" + added.ToString() + ".graphguid " +
                        "AND graphs.tenantguid = t" + added.ToString() + ".tenantguid " +
                        "AND t" + added.ToString() + ".nodeguid IS NULL " +
                        "AND t" + added.ToString() + ".edgeguid IS NULL ";
                    added++;
                }
            }

            ret += "WHERE vectors.tenantguid = '" + tenantGuid + "' ";
            ret += "AND vectors.nodeguid IS NULL ";
            ret += "AND vectors.edgeguid IS NULL ";

            if (labels != null && labels.Count > 0)
            {
                string labelList = "(";
                int labelsAdded = 0;
                foreach (string label in labels)
                {
                    if (labelsAdded > 0) labelList += ",";
                    labelList += "'" + Sanitizer.Sanitize(label) + "'";
                    labelsAdded++;
                }
                labelList += ")";
                ret += "AND labels.label IN " + labelList + " ";
            }

            if (tags != null && tags.Count > 0)
            {
                int added = 1;
                foreach (string key in tags.AllKeys)
                {
                    string val = tags.Get(key);
                    ret += "AND t" + added.ToString() + ".tagkey = '" + Sanitizer.Sanitize(key) + "' ";
                    if (!String.IsNullOrEmpty(val)) ret += "AND t" + added.ToString() + ".tagvalue = '" + Sanitizer.Sanitize(val) + "' ";
                    else ret += "AND t" + added.ToString() + ".tagvalue IS NULL ";
                    added++;
                }
            }

            if (graphFilter != null)
            {
                string filterClause = Converters.ExpressionToWhereClause("graphs", graphFilter);
                if (!String.IsNullOrEmpty(filterClause)) ret += "AND (" + filterClause + ") ";
            }

            if (labels != null && labels.Count > 0)
            {
                ret += "GROUP BY vectors.guid ";
                int labelsAdded = 0;
                ret += "HAVING ";
                foreach (string label in labels)
                {
                    if (labelsAdded > 0) ret += "AND ";
                    ret += "SUM(CASE WHEN labels.label = '" + Sanitizer.Sanitize(label) + "' THEN 1 ELSE 0 END) > 0 ";
                    labelsAdded++;
                }
            }

            ret += "ORDER BY vectors.createdutc DESC ";
            ret += "LIMIT " + batchSize + " OFFSET " + skip + ";";

            return ret;
        }

        // New method for searching node vectors with filters
        internal static string SelectNodeVectorsWithFilters(
            Guid tenantGuid,
            Guid graphGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr nodeFilter = null,
            int batchSize = 100,
            int skip = 0)
        {
            string ret = "SELECT DISTINCT vectors.* FROM 'vectors' ";
            ret += "INNER JOIN 'nodes' ON vectors.nodeguid = nodes.guid AND vectors.graphguid = nodes.graphguid AND vectors.tenantguid = nodes.tenantguid ";

            if (labels != null && labels.Count > 0)
            {
                ret += "INNER JOIN 'labels' "
                    + "ON nodes.guid = labels.nodeguid "
                    + "AND nodes.graphguid = labels.graphguid "
                    + "AND nodes.tenantguid = labels.tenantguid ";
            }

            if (tags != null && tags.Count > 0)
            {
                int added = 1;
                foreach (string key in tags.AllKeys)
                {
                    ret +=
                        "INNER JOIN 'tags' t" + added.ToString() + " " +
                        "ON nodes.guid = t" + added.ToString() + ".nodeguid " +
                        "AND nodes.graphguid = t" + added.ToString() + ".graphguid " +
                        "AND nodes.tenantguid = t" + added.ToString() + ".tenantguid ";
                    added++;
                }
            }

            ret += "WHERE vectors.tenantguid = '" + tenantGuid + "' ";
            ret += "AND vectors.graphguid = '" + graphGuid + "' ";
            ret += "AND vectors.nodeguid IS NOT NULL ";
            ret += "AND vectors.edgeguid IS NULL ";

            if (labels != null && labels.Count > 0)
            {
                string labelList = "(";
                int labelsAdded = 0;
                foreach (string label in labels)
                {
                    if (labelsAdded > 0) labelList += ",";
                    labelList += "'" + Sanitizer.Sanitize(label) + "'";
                    labelsAdded++;
                }
                labelList += ")";
                ret += "AND labels.label IN " + labelList + " ";
            }

            if (tags != null && tags.Count > 0)
            {
                int added = 1;
                foreach (string key in tags.AllKeys)
                {
                    string val = tags.Get(key);
                    ret += "AND t" + added.ToString() + ".tagkey = '" + Sanitizer.Sanitize(key) + "' ";
                    if (!String.IsNullOrEmpty(val)) ret += "AND t" + added.ToString() + ".tagvalue = '" + Sanitizer.Sanitize(val) + "' ";
                    else ret += "AND t" + added.ToString() + ".tagvalue IS NULL ";
                    added++;
                }
            }

            if (nodeFilter != null)
            {
                string filterClause = Converters.ExpressionToWhereClause("nodes", nodeFilter);
                if (!String.IsNullOrEmpty(filterClause)) ret += "AND (" + filterClause + ") ";
            }

            if (labels != null && labels.Count > 0)
            {
                ret += "GROUP BY vectors.guid ";
                int labelsAdded = 0;
                ret += "HAVING ";
                foreach (string label in labels)
                {
                    if (labelsAdded > 0) ret += "AND ";
                    ret += "SUM(CASE WHEN labels.label = '" + Sanitizer.Sanitize(label) + "' THEN 1 ELSE 0 END) > 0 ";
                    labelsAdded++;
                }
            }

            ret += "ORDER BY vectors.createdutc DESC ";
            ret += "LIMIT " + batchSize + " OFFSET " + skip + ";";

            return ret;
        }

        // New method for searching edge vectors with filters
        internal static string SelectEdgeVectorsWithFilters(
            Guid tenantGuid,
            Guid graphGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            int batchSize = 100,
            int skip = 0)
        {
            string ret = "SELECT DISTINCT vectors.* FROM 'vectors' ";
            ret += "INNER JOIN 'edges' ON vectors.edgeguid = edges.guid AND vectors.graphguid = edges.graphguid AND vectors.tenantguid = edges.tenantguid ";

            if (labels != null && labels.Count > 0)
            {
                ret += "INNER JOIN 'labels' "
                    + "ON edges.guid = labels.edgeguid "
                    + "AND edges.graphguid = labels.graphguid "
                    + "AND edges.tenantguid = labels.tenantguid ";
            }

            if (tags != null && tags.Count > 0)
            {
                int added = 1;
                foreach (string key in tags.AllKeys)
                {
                    ret +=
                        "INNER JOIN 'tags' t" + added.ToString() + " " +
                        "ON edges.guid = t" + added.ToString() + ".edgeguid " +
                        "AND edges.graphguid = t" + added.ToString() + ".graphguid " +
                        "AND edges.tenantguid = t" + added.ToString() + ".tenantguid ";
                    added++;
                }
            }

            ret += "WHERE vectors.tenantguid = '" + tenantGuid + "' ";
            ret += "AND vectors.graphguid = '" + graphGuid + "' ";
            ret += "AND vectors.nodeguid IS NULL ";
            ret += "AND vectors.edgeguid IS NOT NULL ";

            if (labels != null && labels.Count > 0)
            {
                string labelList = "(";
                int labelsAdded = 0;
                foreach (string label in labels)
                {
                    if (labelsAdded > 0) labelList += ",";
                    labelList += "'" + Sanitizer.Sanitize(label) + "'";
                    labelsAdded++;
                }
                labelList += ")";
                ret += "AND labels.label IN " + labelList + " ";
            }

            if (tags != null && tags.Count > 0)
            {
                int added = 1;
                foreach (string key in tags.AllKeys)
                {
                    string val = tags.Get(key);
                    ret += "AND t" + added.ToString() + ".tagkey = '" + Sanitizer.Sanitize(key) + "' ";
                    if (!String.IsNullOrEmpty(val)) ret += "AND t" + added.ToString() + ".tagvalue = '" + Sanitizer.Sanitize(val) + "' ";
                    else ret += "AND t" + added.ToString() + ".tagvalue IS NULL ";
                    added++;
                }
            }

            if (edgeFilter != null)
            {
                string filterClause = Converters.ExpressionToWhereClause("edges", edgeFilter);
                if (!String.IsNullOrEmpty(filterClause)) ret += "AND (" + filterClause + ") ";
            }

            if (labels != null && labels.Count > 0)
            {
                ret += "GROUP BY vectors.guid ";
                int labelsAdded = 0;
                ret += "HAVING ";
                foreach (string label in labels)
                {
                    if (labelsAdded > 0) ret += "AND ";
                    ret += "SUM(CASE WHEN labels.label = '" + Sanitizer.Sanitize(label) + "' THEN 1 ELSE 0 END) > 0 ";
                    labelsAdded++;
                }
            }

            ret += "ORDER BY vectors.createdutc DESC ";
            ret += "LIMIT " + batchSize + " OFFSET " + skip + ";";

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
                + "embeddings = " + Converters.BytesToHex(Converters.VectorToBlob(vector.Vectors)) + " "
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