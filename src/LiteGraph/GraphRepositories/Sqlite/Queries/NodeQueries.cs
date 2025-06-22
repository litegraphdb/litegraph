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
    using SQLitePCL;

    internal static class NodeQueries
    {
        internal static string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        internal static Serializer Serializer = new Serializer();

        internal static string Insert(Node node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            string ret = string.Empty;

            string data = null;
            if (node.Data != null) data = Sanitizer.SanitizeJson(Serializer.SerializeJson(node.Data, false));

            ret +=
                "INSERT INTO 'nodes' " +
                "(guid, tenantguid, graphguid, name, data, createdutc, lastupdateutc) VALUES " +
                "('" + node.GUID + "', " +
                "'" + node.TenantGUID + "', " +
                "'" + node.GraphGUID + "', " +
                "'" + Sanitizer.Sanitize(node.Name) + "', " +
                (!String.IsNullOrEmpty(data) ? "'" + data + "', " : "NULL, ") +
                "'" + node.CreatedUtc.ToString(TimestampFormat) + "', " +
                "'" + node.LastUpdateUtc.ToString(TimestampFormat) + "'); ";

            if (node.Labels != null && node.Labels.Count > 0)
            {
                List<LabelMetadata> labels = LabelMetadata.FromListString(
                    node.TenantGUID,
                    node.GraphGUID,
                    node.GUID,
                    null,
                    node.Labels);

                foreach (LabelMetadata label in labels)
                {
                    ret +=
                        "INSERT INTO 'labels' " +
                        "(guid, tenantguid, graphguid, nodeguid, edgeguid, label, createdutc, lastupdateutc) VALUES " +
                        "('" + label.GUID + "', " +
                        "'" + node.TenantGUID + "', " +
                        "'" + node.GraphGUID + "', " +
                        "'" + node.GUID + "', " +
                        (label.EdgeGUID.HasValue ? "'" + label.EdgeGUID + "'" : "NULL") + ", " +
                        "'" + Sanitizer.Sanitize(label.Label) + "', " +
                        "'" + label.CreatedUtc.ToString(TimestampFormat) + "', " +
                        "'" + label.LastUpdateUtc.ToString(TimestampFormat) + "'); ";
                }
            }

            if (node.Tags != null && node.Tags.Count > 0)
            {
                List<TagMetadata> tags = TagMetadata.FromNameValueCollection(
                    node.TenantGUID,
                    node.GraphGUID,
                    node.GUID,
                    null,
                    node.Tags);

                foreach (TagMetadata tag in tags)
                {
                    ret +=
                        "INSERT INTO 'tags' " +
                        "(guid, tenantguid, graphguid, nodeguid, edgeguid, tagkey, tagvalue, createdutc, lastupdateutc) VALUES " +
                        "('" + tag.GUID + "', " +
                        "'" + node.TenantGUID + "', " +
                        "'" + node.GraphGUID + "', " +
                        "'" + node.GUID + "', " +
                        (tag.EdgeGUID.HasValue ? "'" + tag.EdgeGUID + "'" : "NULL") + ", " +
                        "'" + Sanitizer.Sanitize(tag.Key) + "', " +
                        "'" + Sanitizer.Sanitize(tag.Value) + "', " +
                        "'" + tag.CreatedUtc.ToString("yyyy-MM-dd HH:mm:ss.fffffff") + "', " +
                        "'" + tag.LastUpdateUtc.ToString("yyyy-MM-dd HH:mm:ss.fffffff") + "'); ";
                }
            }

            if (node.Vectors != null && node.Vectors.Count > 0)
            {
                foreach (VectorMetadata vector in node.Vectors)
                {
                    string vectorsString = string.Empty;
                    if (vector.Vectors != null && vector.Vectors.Count > 0)
                    {
                        vectorsString = Serializer.SerializeJson(vector.Vectors, false);
                    }

                    ret +=
                        "INSERT INTO 'vectors' " +
                        "(guid, tenantguid, graphguid, nodeguid, edgeguid, model, dimensionality, content, embeddings, createdutc, lastupdateutc) VALUES " +
                        "('" + vector.GUID + "', " +
                        "'" + node.TenantGUID + "', " +
                        "'" + node.GraphGUID + "', " +
                        "'" + node.GUID + "', " +
                        (vector.EdgeGUID.HasValue ? "'" + vector.EdgeGUID + "'" : "NULL") + ", " +
                        "'" + Sanitizer.Sanitize(vector.Model) + "', " +
                        vector.Dimensionality + ", " +
                        "'" + Sanitizer.Sanitize(vector.Content) + "', " +
                        "'" + vectorsString + "', " +
                        "'" + vector.CreatedUtc.ToString("yyyy-MM-dd HH:mm:ss.fffffff") + "', " +
                        "'" + vector.LastUpdateUtc.ToString("yyyy-MM-dd HH:mm:ss.fffffff") + "'); ";
                }
            }

            ret +=
                "SELECT * FROM 'nodes' WHERE "
                + "guid = '" + node.GUID + "' "
                + "AND tenantguid = '" + node.TenantGUID + "' "
                + "AND graphguid = '" + node.GraphGUID + "';";

            return ret;
        }

        internal static string InsertMany(Guid tenantGuid, List<Node> nodes)
        {
            if (nodes == null || nodes.Count == 0) return string.Empty;

            string ret = string.Empty;

            foreach (Node node in nodes)
            {
                if (node.TenantGUID != tenantGuid) node.TenantGUID = tenantGuid;

                string data = null;
                if (node.Data != null) data = Sanitizer.SanitizeJson(Serializer.SerializeJson(node.Data, false));

                ret +=
                    "INSERT INTO 'nodes' " +
                    "(guid, tenantguid, graphguid, name, data, createdutc, lastupdateutc) VALUES " +
                    "('" + node.GUID + "', " +
                    "'" + node.TenantGUID + "', " +
                    "'" + node.GraphGUID + "', " +
                    "'" + Sanitizer.Sanitize(node.Name) + "', " +
                    (!String.IsNullOrEmpty(data) ? "'" + data + "', " : "NULL, ") +
                    "'" + node.CreatedUtc.ToString(TimestampFormat) + "', " +
                    "'" + node.LastUpdateUtc.ToString(TimestampFormat) + "'); ";

                if (node.Labels != null && node.Labels.Count > 0)
                {
                    List<LabelMetadata> labels = LabelMetadata.FromListString(
                        node.TenantGUID,
                        node.GraphGUID,
                        node.GUID,
                        null,
                        node.Labels);

                    foreach (LabelMetadata label in labels)
                    {
                        ret +=
                            "INSERT INTO 'labels' " +
                            "(guid, tenantguid, graphguid, nodeguid, edgeguid, label, createdutc, lastupdateutc) VALUES " +
                            "('" + label.GUID + "', " +
                            "'" + node.TenantGUID + "', " +
                            "'" + node.GraphGUID + "', " +
                            "'" + node.GUID + "', " +
                            (label.EdgeGUID.HasValue ? "'" + label.EdgeGUID + "'" : "NULL") + ", " +
                            "'" + Sanitizer.Sanitize(label.Label) + "', " +
                            "'" + label.CreatedUtc.ToString(TimestampFormat) + "', " +
                            "'" + label.LastUpdateUtc.ToString(TimestampFormat) + "'); ";
                    }
                }

                if (node.Tags != null && node.Tags.Count > 0)
                {
                    List<TagMetadata> tags = TagMetadata.FromNameValueCollection(
                        node.TenantGUID,
                        node.GraphGUID,
                        node.GUID,
                        null,
                        node.Tags);

                    foreach (TagMetadata tag in tags)
                    {
                        ret +=
                            "INSERT INTO 'tags' " +
                            "(guid, tenantguid, graphguid, nodeguid, edgeguid, tagkey, tagvalue, createdutc, lastupdateutc) VALUES " +
                            "('" + tag.GUID + "', " +
                            "'" + node.TenantGUID + "', " +
                            "'" + node.GraphGUID + "', " +
                            "'" + node.GUID + "', " +
                            (tag.EdgeGUID.HasValue ? "'" + tag.EdgeGUID + "'" : "NULL") + ", " +
                            "'" + Sanitizer.Sanitize(tag.Key) + "', " +
                            "'" + Sanitizer.Sanitize(tag.Value) + "', " +
                            "'" + tag.CreatedUtc.ToString(TimestampFormat) + "', " +
                            "'" + tag.LastUpdateUtc.ToString(TimestampFormat) + "'); ";
                    }
                }

                // Insert vectors if any
                if (node.Vectors != null && node.Vectors.Count > 0)
                {
                    foreach (VectorMetadata vector in node.Vectors)
                    {
                        string vectorsString = string.Empty;
                        if (vector.Vectors != null && vector.Vectors.Count > 0)
                        {
                            vectorsString = Serializer.SerializeJson(vector.Vectors, false);
                        }

                        ret +=
                            "INSERT INTO 'vectors' " +
                            "(guid, tenantguid, graphguid, nodeguid, edgeguid, model, dimensionality, content, embeddings, createdutc, lastupdateutc) VALUES " +
                            "('" + vector.GUID + "', " +
                            "'" + node.TenantGUID + "', " +
                            "'" + node.GraphGUID + "', " +
                            "'" + node.GUID + "', " +
                            (vector.EdgeGUID.HasValue ? "'" + vector.EdgeGUID + "'" : "NULL") + ", " +
                            "'" + Sanitizer.Sanitize(vector.Model) + "', " +
                            vector.Dimensionality + ", " +
                            "'" + Sanitizer.Sanitize(vector.Content) + "', " +
                            "'" + vectorsString + "', " +
                            "'" + vector.CreatedUtc.ToString(TimestampFormat) + "', " +
                            "'" + vector.LastUpdateUtc.ToString(TimestampFormat) + "'); ";
                    }
                }
            }

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
            string ret = "SELECT * FROM 'nodes' WHERE tenantguid = '" + tenantGuid + "' AND guid IN (";

            for (int i = 0; i < guids.Count; i++)
            {
                if (i > 0) ret += ",";
                ret += "'" + Sanitizer.Sanitize(guids[i].ToString()) + "'";
            }

            ret += ");";
            return ret;
        }

        internal static string Select(Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
        {
            return "SELECT * FROM 'nodes' WHERE "
                + "guid = '" + nodeGuid + "' "
                + "AND tenantguid = '" + tenantGuid + "' "
                + "AND graphguid = '" + graphGuid + "';";
        }

        internal static string SelectMostConnected(
            Guid tenantGuid,
            Guid graphGuid,
            List<string> labels,
            NameValueCollection tags,
            Expr nodeFilter = null,
            int batchSize = 100,
            int skip = 0)
        {
            string ret = 
                "WITH edge_counts AS ( " +
                "SELECT " +
                    "n.guid, " +
                    "(SELECT COUNT(*) FROM edges WHERE toguid = n.guid) AS edges_in, " +
                    "(SELECT COUNT(*) FROM edges WHERE fromguid = n.guid) AS edges_out " +
                "FROM nodes n " +
                "WHERE n.tenantguid = '" + tenantGuid + "' " +
                "AND n.graphguid = '" + graphGuid + "' " +
                ") ";

            ret += 
                "SELECT nodes.*, " +
                "COALESCE(edge_counts.edges_in, 0) AS edges_in, " +
                "COALESCE(edge_counts.edges_out, 0) AS edges_out, " +
                "COALESCE(edge_counts.edges_in, 0) + COALESCE(edge_counts.edges_out, 0) AS edges_total " +
                "FROM 'nodes' " +
                "LEFT JOIN edge_counts ON nodes.guid = edge_counts.guid ";

            if (labels != null && labels.Count > 0)
                ret += 
                    "INNER JOIN 'labels' " +
                    "ON nodes.guid = labels.nodeguid " +
                    "AND nodes.graphguid = labels.graphguid " +
                    "AND nodes.tenantguid = labels.tenantguid ";

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

            ret += 
                "WHERE " +
                "nodes.tenantguid = '" + tenantGuid + "' " +
                "AND nodes.graphguid = '" + graphGuid + "' ";

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
                ret += "GROUP BY nodes.guid ";
                int labelsAdded = 0;
                ret += "HAVING ";
                foreach (string label in labels)
                {
                    if (labelsAdded > 0) ret += "AND ";
                    ret += "SUM(CASE WHEN labels.label = '" + Sanitizer.Sanitize(label) + "' THEN 1 ELSE 0 END) > 0 ";
                    labelsAdded++;
                }
            }

            ret += "ORDER BY edges_total DESC ";
            ret += "LIMIT " + batchSize + " OFFSET " + skip + ";";

            return ret;
        }

        internal static string SelectLeastConnected(
            Guid tenantGuid,
            Guid graphGuid,
            List<string> labels,
            NameValueCollection tags,
            Expr nodeFilter = null,
            int batchSize = 100,
            int skip = 0)
        {
            string ret =
                "WITH edge_counts AS ( " +
                "SELECT " +
                    "n.guid, " +
                    "(SELECT COUNT(*) FROM edges WHERE toguid = n.guid) AS edges_in, " +
                    "(SELECT COUNT(*) FROM edges WHERE fromguid = n.guid) AS edges_out " +
                "FROM nodes n " +
                "WHERE n.tenantguid = '" + tenantGuid + "' " +
                "AND n.graphguid = '" + graphGuid + "' " +
                ") ";

            ret +=
                "SELECT nodes.*, " +
                "COALESCE(edge_counts.edges_in, 0) AS edges_in, " +
                "COALESCE(edge_counts.edges_out, 0) AS edges_out, " +
                "COALESCE(edge_counts.edges_in, 0) + COALESCE(edge_counts.edges_out, 0) AS edges_total " +
                "FROM 'nodes' " +
                "LEFT JOIN edge_counts ON nodes.guid = edge_counts.guid ";

            if (labels != null && labels.Count > 0)
                ret +=
                    "INNER JOIN 'labels' " +
                    "ON nodes.guid = labels.nodeguid " +
                    "AND nodes.graphguid = labels.graphguid " +
                    "AND nodes.tenantguid = labels.tenantguid ";

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

            ret +=
                "WHERE " +
                "nodes.tenantguid = '" + tenantGuid + "' " +
                "AND nodes.graphguid = '" + graphGuid + "' ";

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
                ret += "GROUP BY nodes.guid ";
                int labelsAdded = 0;
                ret += "HAVING ";
                foreach (string label in labels)
                {
                    if (labelsAdded > 0) ret += "AND ";
                    ret += "SUM(CASE WHEN labels.label = '" + Sanitizer.Sanitize(label) + "' THEN 1 ELSE 0 END) > 0 ";
                    labelsAdded++;
                }
            }

            ret += "ORDER BY edges_total ASC ";
            ret += "LIMIT " + batchSize + " OFFSET " + skip + ";";

            return ret;
        }

        internal static string SelectMany(
            Guid tenantGuid,
            Guid graphGuid,
            List<string> labels,
            NameValueCollection tags,
            Expr nodeFilter = null,
            int batchSize = 100,
            int skip = 0,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            string ret = "SELECT * FROM 'nodes' ";

            if (labels != null && labels.Count > 0)
                ret += "INNER JOIN 'labels' "
                    + "ON nodes.guid = labels.nodeguid "
                    + "AND nodes.graphguid = labels.graphguid "
                    + "AND nodes.tenantguid = labels.tenantguid ";

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

            ret += "WHERE "
                + "nodes.tenantguid = '" + tenantGuid + "' "
                + "AND nodes.graphguid = '" + graphGuid + "' ";

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
                ret += "GROUP BY nodes.guid ";

                int labelsAdded = 0;
                ret += "HAVING ";
                foreach (string label in labels)
                {
                    if (labelsAdded > 0) ret += "AND ";
                    ret += "SUM(CASE WHEN labels.label = '" + Sanitizer.Sanitize(label) + "' THEN 1 ELSE 0 END) > 0 ";
                    labelsAdded++;
                }
            }

            ret +=
                "ORDER BY " + Converters.EnumerationOrderToClause(order) + " "
                + "LIMIT " + batchSize + " OFFSET " + skip + ";";

            return ret;
        }

        internal static string GetRecordPage(
            Guid? tenantGuid,
            Guid? graphGuid,
            List<string> labels,
            NameValueCollection tags,
            Expr nodeFilter = null,
            int batchSize = 100,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            Node marker = null)
        {
            string ret;

            // If we have labels, we need a subquery to handle the GROUP BY/HAVING logic
            if (labels != null && labels.Count > 0)
            {
                ret = "SELECT * FROM (SELECT nodes.* FROM 'nodes' ";

                ret += "INNER JOIN 'labels' "
                    + "ON nodes.guid = labels.nodeguid "
                    + "AND nodes.graphguid = labels.graphguid "
                    + "AND nodes.tenantguid = labels.tenantguid ";

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

                ret += "WHERE nodes.guid IS NOT NULL ";

                if (tenantGuid != null)
                    ret += "AND nodes.tenantguid = '" + tenantGuid.Value.ToString() + "' ";

                if (graphGuid != null)
                    ret += "AND nodes.graphguid = '" + graphGuid.Value.ToString() + "' ";

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

                if (marker != null)
                {
                    ret += "AND " + MarkerWhereClause(order, marker) + " ";
                }

                ret += "GROUP BY nodes.guid ";

                labelsAdded = 0;
                ret += "HAVING ";
                foreach (string label in labels)
                {
                    if (labelsAdded > 0) ret += "AND ";
                    ret += "SUM(CASE WHEN labels.label = '" + Sanitizer.Sanitize(label) + "' THEN 1 ELSE 0 END) > 0 ";
                    labelsAdded++;
                }

                ret += ") AS filtered_nodes ";
                ret += OrderByClause(order);
                ret += "LIMIT " + batchSize + ";";
            }
            else
            {
                // No labels - simpler query
                ret = "SELECT nodes.* FROM 'nodes' ";

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

                ret += "WHERE nodes.guid IS NOT NULL ";

                if (tenantGuid != null)
                    ret += "AND nodes.tenantguid = '" + tenantGuid.Value.ToString() + "' ";

                if (graphGuid != null)
                    ret += "AND nodes.graphguid = '" + graphGuid.Value.ToString() + "' ";

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

                if (marker != null)
                {
                    ret += "AND " + MarkerWhereClause(order, marker) + " ";
                }

                ret += OrderByClause(order);
                ret += "LIMIT " + batchSize + ";";
            }

            return ret;
        }

        internal static string GetRecordCount(
            Guid? tenantGuid,
            Guid? graphGuid,
            List<string> labels,
            NameValueCollection tags,
            Expr nodeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            Node marker = null)
        {
            string ret;

            // If we have labels, we need a subquery to handle the GROUP BY/HAVING logic
            if (labels != null && labels.Count > 0)
            {
                ret = "SELECT COUNT(*) AS record_count FROM (SELECT nodes.guid FROM 'nodes' ";

                ret += "INNER JOIN 'labels' "
                    + "ON nodes.guid = labels.nodeguid "
                    + "AND nodes.graphguid = labels.graphguid "
                    + "AND nodes.tenantguid = labels.tenantguid ";

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

                ret += "WHERE nodes.guid IS NOT NULL ";

                if (tenantGuid != null)
                    ret += "AND nodes.tenantguid = '" + tenantGuid.Value.ToString() + "' ";

                if (graphGuid != null)
                    ret += "AND nodes.graphguid = '" + graphGuid.Value.ToString() + "' ";

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

                if (marker != null)
                {
                    ret += "AND " + MarkerWhereClause(order, marker) + " ";
                }

                ret += "GROUP BY nodes.guid ";

                labelsAdded = 0;
                ret += "HAVING ";
                foreach (string label in labels)
                {
                    if (labelsAdded > 0) ret += "AND ";
                    ret += "SUM(CASE WHEN labels.label = '" + Sanitizer.Sanitize(label) + "' THEN 1 ELSE 0 END) > 0 ";
                    labelsAdded++;
                }

                ret += ") AS filtered_nodes AS record_count;";
            }
            else
            {
                // No labels - check if we need DISTINCT due to tags
                if (tags != null && tags.Count > 0)
                {
                    ret = "SELECT COUNT(DISTINCT nodes.guid) AS record_count FROM 'nodes' ";
                }
                else
                {
                    ret = "SELECT COUNT(*) AS record_count FROM 'nodes' ";
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

                ret += "WHERE nodes.guid IS NOT NULL ";

                if (tenantGuid != null)
                    ret += "AND nodes.tenantguid = '" + tenantGuid.Value.ToString() + "' ";

                if (graphGuid != null)
                    ret += "AND nodes.graphguid = '" + graphGuid.Value.ToString() + "' ";

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

                if (marker != null)
                {
                    ret += "AND " + MarkerWhereClause(order, marker) + " ";
                }

                ret += ";";
            }

            return ret;
        }

        internal static string Update(Node node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            string ret = string.Empty;

            string data = null;
            if (node.Data != null) data = Sanitizer.SanitizeJson(Serializer.SerializeJson(node.Data, false));

            ret +=
                "UPDATE 'nodes' SET " +
                "name = '" + Sanitizer.Sanitize(node.Name) + "', " +
                "data = " + (!String.IsNullOrEmpty(data) ? "'" + data + "', " : "NULL, ") +
                "lastupdateutc = '" + DateTime.UtcNow.ToString(TimestampFormat) + "' " +
                "WHERE tenantguid = '" + node.TenantGUID + "' " +
                "AND graphguid = '" + node.GraphGUID + "' " +
                "AND guid = '" + node.GUID + "'; ";

            ret +=
                "DELETE FROM 'labels' WHERE " +
                "tenantguid = '" + node.TenantGUID + "' " +
                "AND graphguid = '" + node.GraphGUID + "' " +
                "AND nodeguid = '" + node.GUID + "'; ";

            ret +=
                "DELETE FROM 'tags' WHERE " +
                "tenantguid = '" + node.TenantGUID + "' " +
                "AND graphguid = '" + node.GraphGUID + "' " +
                "AND nodeguid = '" + node.GUID + "'; ";

            ret +=
                "DELETE FROM 'vectors' WHERE " +
                "tenantguid = '" + node.TenantGUID + "' " +
                "AND graphguid = '" + node.GraphGUID + "' " +
                "AND nodeguid = '" + node.GUID + "'; ";

            if (node.Labels != null && node.Labels.Count > 0)
            {
                List<LabelMetadata> labels = LabelMetadata.FromListString(
                    node.TenantGUID,
                    node.GraphGUID,
                    node.GUID,
                    null,
                    node.Labels);

                foreach (LabelMetadata label in labels)
                {
                    ret +=
                        "INSERT INTO 'labels' " +
                        "(guid, tenantguid, graphguid, nodeguid, edgeguid, label, createdutc, lastupdateutc) VALUES " +
                        "('" + label.GUID + "', " +
                        "'" + node.TenantGUID + "', " +
                        "'" + node.GraphGUID + "', " +
                        "'" + node.GUID + "', " +
                        (label.EdgeGUID.HasValue ? "'" + label.EdgeGUID + "'" : "NULL") + ", " +
                        "'" + Sanitizer.Sanitize(label.Label) + "', " +
                        "'" + label.CreatedUtc.ToString(TimestampFormat) + "', " +
                        "'" + DateTime.UtcNow.ToString(TimestampFormat) + "'); ";
                }
            }

            if (node.Tags != null && node.Tags.Count > 0)
            {
                List<TagMetadata> tags = TagMetadata.FromNameValueCollection(
                    node.TenantGUID,
                    node.GraphGUID,
                    node.GUID,
                    null,
                    node.Tags);

                foreach (TagMetadata tag in tags)
                {
                    ret +=
                        "INSERT INTO 'tags' " +
                        "(guid, tenantguid, graphguid, nodeguid, edgeguid, tagkey, tagvalue, createdutc, lastupdateutc) VALUES " +
                        "('" + tag.GUID + "', " +
                        "'" + node.TenantGUID + "', " +
                        "'" + node.GraphGUID + "', " +
                        "'" + node.GUID + "', " +
                        (tag.EdgeGUID.HasValue ? "'" + tag.EdgeGUID + "'" : "NULL") + ", " +
                        "'" + Sanitizer.Sanitize(tag.Key) + "', " +
                        "'" + Sanitizer.Sanitize(tag.Value) + "', " +
                        "'" + tag.CreatedUtc.ToString(TimestampFormat) + "', " +
                        "'" + DateTime.UtcNow.ToString(TimestampFormat) + "'); ";
                }
            }

            if (node.Vectors != null && node.Vectors.Count > 0)
            {
                foreach (VectorMetadata vector in node.Vectors)
                {
                    string vectorsString = string.Empty;
                    if (vector.Vectors != null && vector.Vectors.Count > 0)
                    {
                        vectorsString = Serializer.SerializeJson(vector.Vectors, false);
                    }

                    ret +=
                        "INSERT INTO 'vectors' " +
                        "(guid, tenantguid, graphguid, nodeguid, edgeguid, model, dimensionality, content, embeddings, createdutc, lastupdateutc) VALUES " +
                        "('" + vector.GUID + "', " +
                        "'" + node.TenantGUID + "', " +
                        "'" + node.GraphGUID + "', " +
                        "'" + node.GUID + "', " +
                        (vector.EdgeGUID.HasValue ? "'" + vector.EdgeGUID + "'" : "NULL") + ", " +
                        "'" + Sanitizer.Sanitize(vector.Model) + "', " +
                        vector.Dimensionality + ", " +
                        "'" + Sanitizer.Sanitize(vector.Content) + "', " +
                        "'" + vectorsString + "', " +
                        "'" + vector.CreatedUtc.ToString(TimestampFormat) + "', " +
                        "'" + DateTime.UtcNow.ToString(TimestampFormat) + "'); ";
                }
            }

            ret +=
                "SELECT * FROM 'nodes' WHERE "
                + "guid = '" + node.GUID + "' "
                + "AND tenantguid = '" + node.TenantGUID + "' "
                + "AND graphguid = '" + node.GraphGUID + "';";

            return ret;
        }

        internal static string Delete(Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
        {
            string ret = string.Empty;

            // First delete associated edges and their related data
            ret +=
                // Find and delete all edge metadata related to this node
                "DELETE FROM 'labels' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND edgeguid IN (SELECT guid FROM 'edges' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND (fromguid = '" + nodeGuid + "' OR toguid = '" + nodeGuid + "')); ";

            ret +=
                "DELETE FROM 'tags' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND edgeguid IN (SELECT guid FROM 'edges' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND (fromguid = '" + nodeGuid + "' OR toguid = '" + nodeGuid + "')); ";

            ret +=
                "DELETE FROM 'vectors' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND edgeguid IN (SELECT guid FROM 'edges' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND (fromguid = '" + nodeGuid + "' OR toguid = '" + nodeGuid + "')); ";

            // Delete the edges themselves
            ret +=
                "DELETE FROM 'edges' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND (fromguid = '" + nodeGuid + "' OR toguid = '" + nodeGuid + "'); ";

            // Now delete the node's own metadata
            ret +=
                "DELETE FROM 'labels' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND nodeguid = '" + nodeGuid + "'; ";

            ret +=
                "DELETE FROM 'tags' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND nodeguid = '" + nodeGuid + "'; ";

            ret +=
                "DELETE FROM 'vectors' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND nodeguid = '" + nodeGuid + "'; ";

            // Finally delete the node itself
            ret +=
                "DELETE FROM 'nodes' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND guid = '" + nodeGuid + "'; ";

            return ret;
        }

        internal static string DeleteAllInTenant(Guid tenantGuid)
        {
            string ret = string.Empty;

            // Delete all edge metadata
            ret += "DELETE FROM 'labels' WHERE tenantguid = '" + tenantGuid + "' AND edgeguid IS NOT NULL; ";
            ret += "DELETE FROM 'tags' WHERE tenantguid = '" + tenantGuid + "' AND edgeguid IS NOT NULL; ";
            ret += "DELETE FROM 'vectors' WHERE tenantguid = '" + tenantGuid + "' AND edgeguid IS NOT NULL; ";

            // Delete all edges
            ret += "DELETE FROM 'edges' WHERE tenantguid = '" + tenantGuid + "'; ";

            // Delete all node metadata
            ret += "DELETE FROM 'labels' WHERE tenantguid = '" + tenantGuid + "' AND nodeguid IS NOT NULL; ";
            ret += "DELETE FROM 'tags' WHERE tenantguid = '" + tenantGuid + "' AND nodeguid IS NOT NULL; ";
            ret += "DELETE FROM 'vectors' WHERE tenantguid = '" + tenantGuid + "' AND nodeguid IS NOT NULL; ";

            // Delete all nodes
            ret += "DELETE FROM 'nodes' WHERE tenantguid = '" + tenantGuid + "'; ";

            return ret;
        }

        internal static string DeleteAllInGraph(Guid tenantGuid, Guid graphGuid)
        {
            string ret = string.Empty;

            // Delete all edge metadata
            ret +=
                "DELETE FROM 'labels' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND edgeguid IS NOT NULL; ";

            ret +=
                "DELETE FROM 'tags' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND edgeguid IS NOT NULL; ";

            ret +=
                "DELETE FROM 'vectors' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND edgeguid IS NOT NULL; ";

            // Delete all edges
            ret +=
                "DELETE FROM 'edges' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "'; ";

            // Delete all node metadata
            ret +=
                "DELETE FROM 'labels' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND nodeguid IS NOT NULL; ";

            ret +=
                "DELETE FROM 'tags' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND nodeguid IS NOT NULL; ";

            ret +=
                "DELETE FROM 'vectors' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND nodeguid IS NOT NULL; ";

            // Delete all nodes
            ret +=
                "DELETE FROM 'nodes' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "'; ";

            return ret;
        }

        internal static string DeleteMany(Guid tenantGuid, Guid graphGuid, List<Guid> nodeGuids)
        {
            if (nodeGuids == null || nodeGuids.Count == 0) return string.Empty;

            string guidList = string.Join(",", nodeGuids.Select(guid => "'" + guid + "'"));

            string ret = string.Empty;

            // Delete associated edges and their metadata
            ret +=
                // Delete edge labels
                "DELETE FROM 'labels' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND edgeguid IN (SELECT guid FROM 'edges' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND (fromguid IN (" + guidList + ") OR toguid IN (" + guidList + "))); ";

            ret +=
                // Delete edge tags
                "DELETE FROM 'tags' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND edgeguid IN (SELECT guid FROM 'edges' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND (fromguid IN (" + guidList + ") OR toguid IN (" + guidList + "))); ";

            ret +=
                // Delete edge vectors
                "DELETE FROM 'vectors' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND edgeguid IN (SELECT guid FROM 'edges' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND (fromguid IN (" + guidList + ") OR toguid IN (" + guidList + "))); ";

            // Delete the edges
            ret +=
                "DELETE FROM 'edges' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND (fromguid IN (" + guidList + ") OR toguid IN (" + guidList + ")); ";

            // Now delete the node metadata
            ret +=
                "DELETE FROM 'labels' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND nodeguid IN (" + guidList + "); ";

            ret +=
                "DELETE FROM 'tags' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND nodeguid IN (" + guidList + "); ";

            ret +=
                "DELETE FROM 'vectors' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND nodeguid IN (" + guidList + "); ";

            // Finally delete the nodes
            ret +=
                "DELETE FROM 'nodes' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND guid IN (" + guidList + "); ";

            return ret;
        }

        internal static string BatchExists(Guid tenantGuid, Guid graphGuid, List<Guid> nodeGuids)
        {
            string query = "WITH temp(guid) AS (VALUES ";

            for (int i = 0; i < nodeGuids.Count; i++)
            {
                if (i > 0) query += ",";
                query += "('" + nodeGuids[i].ToString() + "')";
            }

            query +=
                ") "
                + "SELECT temp.guid, CASE WHEN nodes.guid IS NOT NULL THEN 1 ELSE 0 END as \"exists\" "
                + "FROM temp "
                + "LEFT JOIN nodes ON temp.guid = nodes.guid "
                + "AND nodes.graphguid = '" + graphGuid + "' "
                + "AND nodes.tenantguid = '" + tenantGuid + "';";

            return query;
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

        private static string MarkerWhereClause(EnumerationOrderEnum order, Node marker)
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
