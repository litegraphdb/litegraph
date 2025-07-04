﻿namespace LiteGraph.GraphRepositories.Sqlite.Queries
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Reflection.Emit;
    using System.Text;
    using System.Threading.Tasks;
    using ExpressionTree;
    using LiteGraph.Serialization;

    internal static class GraphQueries
    {
        internal static string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        internal static Serializer Serializer = new Serializer();

        internal static string Insert(Graph graph)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));

            string ret = string.Empty;

            ret +=
                "INSERT INTO 'graphs' "
                + "(guid, tenantguid, name, data, createdutc, lastupdateutc) VALUES "
                + "('" + graph.GUID + "',"
                + "'" + graph.TenantGUID + "',"
                + "'" + Sanitizer.Sanitize(graph.Name) + "',";

            if (graph.Data == null) ret += "null,";
            else ret += "'" + Sanitizer.SanitizeJson(Serializer.SerializeJson(graph.Data, false)) + "',";

            ret +=
                "'" + graph.CreatedUtc.ToString(TimestampFormat) + "',"
                + "'" + graph.LastUpdateUtc.ToString(TimestampFormat) + "'"
                + "); ";

            if (graph.Labels != null && graph.Labels.Count > 0)
            {
                List<LabelMetadata> labels = LabelMetadata.FromListString(
                    graph.TenantGUID,
                    graph.GUID,
                    null,
                    null,
                    graph.Labels);

                foreach (LabelMetadata label in labels)
                {
                    ret +=
                        "INSERT INTO 'labels' " +
                        "(guid, tenantguid, graphguid, nodeguid, edgeguid, label, createdutc, lastupdateutc) VALUES " +
                        "('" + label.GUID + "', " +
                        "'" + graph.TenantGUID + "', " +
                        "'" + graph.GUID + "', " +
                        "NULL, " +
                        "NULL, " +
                        "'" + Sanitizer.Sanitize(label.Label) + "', " +
                        "'" + label.CreatedUtc.ToString(TimestampFormat) + "', " +
                        "'" + label.LastUpdateUtc.ToString(TimestampFormat) + "'); ";
                }
            }

            if (graph.Tags != null && graph.Tags.Count > 0)
            {
                List<TagMetadata> tags = TagMetadata.FromNameValueCollection(
                    graph.TenantGUID,
                    graph.GUID,
                    null,
                    null,
                    graph.Tags);

                foreach (TagMetadata tag in tags)
                {
                    ret +=
                        "INSERT INTO 'tags' " +
                        "(guid, tenantguid, graphguid, nodeguid, edgeguid, tagkey, tagvalue, createdutc, lastupdateutc) VALUES " +
                        "('" + tag.GUID + "', " +
                        "'" + graph.TenantGUID + "', " +
                        "'" + graph.GUID + "', " +
                        "NULL, " +
                        "NULL, " +
                        "'" + Sanitizer.Sanitize(tag.Key) + "', " +
                        "'" + Sanitizer.Sanitize(tag.Value) + "', " +
                        "'" + tag.CreatedUtc.ToString(TimestampFormat) + "', " +
                        "'" + tag.LastUpdateUtc.ToString(TimestampFormat) + "'); ";
                }
            }

            if (graph.Vectors != null && graph.Vectors.Count > 0)
            {
                foreach (VectorMetadata vector in graph.Vectors)
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
                        "'" + graph.TenantGUID + "', " +
                        "'" + graph.GUID + "', " +
                        "NULL, " +
                        "NULL, " +
                        "'" + Sanitizer.Sanitize(vector.Model) + "', " +
                        vector.Dimensionality + ", " +
                        "'" + Sanitizer.Sanitize(vector.Content) + "', " +
                        "'" + vectorsString + "', " +
                        "'" + vector.CreatedUtc.ToString(TimestampFormat) + "', " +
                        "'" + vector.LastUpdateUtc.ToString(TimestampFormat) + "'); ";
                }
            }

            ret += "SELECT * FROM 'graphs' WHERE guid = '" + graph.GUID + "' AND tenantguid = '" + graph.TenantGUID + "';";
            return ret;
        }

        internal static string SelectAllInTenant(
            Guid tenantGuid, 
            int batchSize = 100, 
            int skip = 0, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            string ret = "SELECT * FROM 'graphs' WHERE tenantguid = '" + tenantGuid + "' ";
            ret +=
                "ORDER BY " + Converters.EnumerationOrderToClause(order) + " "
                + "LIMIT " + batchSize + " OFFSET " + skip + ";";
            return ret;
        }

        internal static string SelectByGuid(Guid tenantGuid, Guid guid)
        {
            return "SELECT * FROM 'graphs' WHERE tenantguid = '" + tenantGuid + "' AND guid = '" + guid + "';";
        }

        internal static string SelectByGuids(Guid tenantGuid, List<Guid> guids)
        {
            return
                "SELECT * FROM 'graphs' " +
                "WHERE tenantguid = '" + tenantGuid + "' " +
                "AND guid IN (" +
                string.Join(", ", guids.Select(g => "'" + g + "'")) +
                ");";
        }

        internal static string SelectMany(
            Guid tenantGuid,
            string name,
            List<string> labels,
            NameValueCollection tags,
            Expr graphFilter = null,
            int batchSize = 100,
            int skip = 0,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            string ret = "SELECT * FROM 'graphs' ";

            if (labels != null && labels.Count > 0)
                ret += "INNER JOIN 'labels' "
                    + "ON graphs.guid = labels.graphguid "
                    + "AND graphs.tenantguid = labels.tenantguid "
                    + "AND labels.nodeguid IS NULL "
                    + "AND labels.edgeguid IS NULL ";

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

            ret += "WHERE graphs.tenantguid = '" + tenantGuid + "' ";

            if (!String.IsNullOrEmpty(name))
                ret += "AND graphs.name LIKE '%" + Sanitizer.Sanitize(name) + "%' ";

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
                ret += "GROUP BY graphs.guid ";

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
            List<string> labels,
            NameValueCollection tags,
            Expr graphFilter = null,
            int batchSize = 100,
            int skip = 0,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            Graph marker = null)
        {
            string ret = "SELECT graphs.* FROM 'graphs' WHERE graphs.guid IS NOT NULL ";

            if (tenantGuid != null)
                ret += "AND graphs.tenantguid = '" + tenantGuid.Value.ToString() + "' ";

            // Handle labels
            if (labels != null && labels.Count > 0)
            {
                foreach (string label in labels)
                {
                    ret += "AND EXISTS (SELECT 1 FROM 'labels' " +
                           "WHERE labels.graphguid = graphs.guid " +
                           "AND labels.tenantguid = graphs.tenantguid " +
                           "AND labels.nodeguid IS NULL " +
                           "AND labels.edgeguid IS NULL " +
                           "AND labels.label = '" + Sanitizer.Sanitize(label) + "') ";
                }
            }

            // Handle tags
            if (tags != null && tags.Count > 0)
            {
                foreach (string key in tags.AllKeys)
                {
                    string val = tags.Get(key);
                    ret += "AND EXISTS (SELECT 1 FROM 'tags' " +
                           "WHERE tags.graphguid = graphs.guid " +
                           "AND tags.tenantguid = graphs.tenantguid " +
                           "AND tags.nodeguid IS NULL " +
                           "AND tags.edgeguid IS NULL " +
                           "AND tags.tagkey = '" + Sanitizer.Sanitize(key) + "' ";

                    if (!String.IsNullOrEmpty(val))
                        ret += "AND tags.tagvalue = '" + Sanitizer.Sanitize(val) + "' ";
                    else
                        ret += "AND tags.tagvalue IS NULL ";

                    ret += ") ";
                }
            }

            if (graphFilter != null)
            {
                string filterClause = Converters.ExpressionToWhereClause("graphs", graphFilter);
                if (!String.IsNullOrEmpty(filterClause))
                    ret += "AND (" + filterClause + ") ";
            }

            if (marker != null)
            {
                ret += "AND " + MarkerWhereClause(order, marker) + " ";
            }

            ret += OrderByClause(order);
            ret += "LIMIT " + batchSize + " OFFSET " + skip + ";";

            return ret;
        }

        internal static string GetRecordCount(
            Guid? tenantGuid,
            List<string> labels,
            NameValueCollection tags,
            Expr graphFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            Graph marker = null)
        {
            bool needsDistinct = (tags != null && tags.Count > 0);
            string ret = needsDistinct
                ? "SELECT COUNT(DISTINCT graphs.guid) AS record_count FROM 'graphs' "
                : "SELECT COUNT(*) AS record_count FROM 'graphs' ";

            ret += "WHERE graphs.guid IS NOT NULL ";

            if (tenantGuid != null)
                ret += "AND graphs.tenantguid = '" + tenantGuid.Value.ToString() + "' ";

            if (labels != null && labels.Count > 0)
            {
                foreach (string label in labels)
                {
                    ret += "AND EXISTS (SELECT 1 FROM 'labels' " +
                           "WHERE labels.graphguid = graphs.guid " +
                           "AND labels.tenantguid = graphs.tenantguid " +
                           "AND labels.nodeguid IS NULL " +
                           "AND labels.edgeguid IS NULL " +
                           "AND labels.label = '" + Sanitizer.Sanitize(label) + "') ";
                }
            }

            if (tags != null && tags.Count > 0)
            {
                foreach (string key in tags.AllKeys)
                {
                    string val = tags.Get(key);
                    ret += "AND EXISTS (SELECT 1 FROM 'tags' " +
                           "WHERE tags.graphguid = graphs.guid " +
                           "AND tags.tenantguid = graphs.tenantguid " +
                           "AND tags.nodeguid IS NULL " +
                           "AND tags.edgeguid IS NULL " +
                           "AND tags.tagkey = '" + Sanitizer.Sanitize(key) + "' ";

                    if (!String.IsNullOrEmpty(val))
                        ret += "AND tags.tagvalue = '" + Sanitizer.Sanitize(val) + "' ";
                    else
                        ret += "AND tags.tagvalue IS NULL ";

                    ret += ") ";
                }
            }

            if (graphFilter != null)
            {
                string filterClause = Converters.ExpressionToWhereClause("graphs", graphFilter);
                if (!String.IsNullOrEmpty(filterClause))
                    ret += "AND (" + filterClause + ") ";
            }

            if (marker != null)
            {
                ret += "AND " + MarkerWhereClause(order, marker) + " ";
            }

            ret += ";";

            return ret;
        }

        internal static string Update(Graph graph)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));

            string ret = string.Empty;

            ret +=
                "UPDATE 'graphs' SET " +
                "name = '" + Sanitizer.Sanitize(graph.Name) + "', " +
                "lastupdateutc = '" + DateTime.UtcNow.ToString(TimestampFormat) + "',";

            if (graph.Data == null) ret += "data = null ";
            else ret += "data = '" + Sanitizer.SanitizeJson(Serializer.SerializeJson(graph.Data, false)) + "' ";

            ret +=
                "WHERE guid = '" + graph.GUID + "' " +
                "AND tenantguid = '" + graph.TenantGUID + "'; ";

            ret +=
                "DELETE FROM 'labels' WHERE " +
                "tenantguid = '" + graph.TenantGUID + "' " +
                "AND graphguid = '" + graph.GUID + "' " +
                "AND nodeguid IS NULL " +
                "AND edgeguid IS NULL; ";

            ret +=
                "DELETE FROM 'tags' WHERE " +
                "tenantguid = '" + graph.TenantGUID + "' " +
                "AND graphguid = '" + graph.GUID + "' " +
                "AND nodeguid IS NULL " +
                "AND edgeguid IS NULL; ";

            ret +=
                "DELETE FROM 'vectors' WHERE " +
                "tenantguid = '" + graph.TenantGUID + "' " +
                "AND graphguid = '" + graph.GUID + "' " +
                "AND nodeguid IS NULL " +
                "AND edgeguid IS NULL; ";

            if (graph.Labels != null && graph.Labels.Count > 0)
            {
                List<LabelMetadata> labels = LabelMetadata.FromListString(
                    graph.TenantGUID,
                    graph.GUID,
                    null,
                    null,
                    graph.Labels);

                foreach (LabelMetadata label in labels)
                {
                    ret +=
                        "INSERT INTO 'labels' " +
                        "(guid, tenantguid, graphguid, nodeguid, edgeguid, label, createdutc, lastupdateutc) VALUES " +
                        "('" + label.GUID + "', " +
                        "'" + graph.TenantGUID + "', " +
                        "'" + graph.GUID + "', " +
                        "NULL, " +
                        "NULL, " +
                        "'" + Sanitizer.Sanitize(label.Label) + "', " +
                        "'" + label.CreatedUtc.ToString(TimestampFormat) + "', " +
                        "'" + DateTime.UtcNow.ToString(TimestampFormat) + "'); ";
                }
            }

            if (graph.Tags != null && graph.Tags.Count > 0)
            {
                List<TagMetadata> tags = TagMetadata.FromNameValueCollection(
                    graph.TenantGUID,
                    graph.GUID,
                    null,
                    null,
                    graph.Tags);

                foreach (TagMetadata tag in tags)
                {
                    ret +=
                        "INSERT INTO 'tags' " +
                        "(guid, tenantguid, graphguid, nodeguid, edgeguid, tagkey, tagvalue, createdutc, lastupdateutc) VALUES " +
                        "('" + tag.GUID + "', " +
                        "'" + graph.TenantGUID + "', " +
                        "'" + graph.GUID + "', " +
                        "NULL, " +
                        "NULL, " +
                        "'" + Sanitizer.Sanitize(tag.Key) + "', " +
                        "'" + Sanitizer.Sanitize(tag.Value) + "', " +
                        "'" + tag.CreatedUtc.ToString(TimestampFormat) + "', " +
                        "'" + DateTime.UtcNow.ToString(TimestampFormat) + "'); ";
                }
            }

            if (graph.Vectors != null && graph.Vectors.Count > 0)
            {
                foreach (VectorMetadata vector in graph.Vectors)
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
                        "'" + graph.TenantGUID + "', " +
                        "'" + graph.GUID + "', " +
                        "NULL, " +
                        "NULL, " +
                        "'" + Sanitizer.Sanitize(vector.Model) + "', " +
                        vector.Dimensionality + ", " +
                        "'" + Sanitizer.Sanitize(vector.Content) + "', " +
                        "'" + vectorsString + "', " +
                        "'" + vector.CreatedUtc.ToString(TimestampFormat) + "', " +
                        "'" + DateTime.UtcNow.ToString(TimestampFormat) + "'); ";
                }
            }

            ret += "SELECT * FROM 'graphs' WHERE guid = '" + graph.GUID + "' AND tenantguid = '" + graph.TenantGUID + "';";
            return ret;
        }

        internal static string DeleteAllInTenant(Guid tenantGuid)
        {
            string ret = string.Empty;

            // Delete all edge metadata first
            ret += "DELETE FROM 'labels' WHERE tenantguid = '" + tenantGuid + "' AND edgeguid IS NOT NULL; ";
            ret += "DELETE FROM 'tags' WHERE tenantguid = '" + tenantGuid + "' AND edgeguid IS NOT NULL; ";
            ret += "DELETE FROM 'vectors' WHERE tenantguid = '" + tenantGuid + "' AND edgeguid IS NOT NULL; ";

            // Delete all node metadata
            ret += "DELETE FROM 'labels' WHERE tenantguid = '" + tenantGuid + "' AND nodeguid IS NOT NULL; ";
            ret += "DELETE FROM 'tags' WHERE tenantguid = '" + tenantGuid + "' AND nodeguid IS NOT NULL; ";
            ret += "DELETE FROM 'vectors' WHERE tenantguid = '" + tenantGuid + "' AND nodeguid IS NOT NULL; ";

            // Delete all graph metadata
            ret += "DELETE FROM 'labels' WHERE tenantguid = '" + tenantGuid + "' AND graphguid IS NOT NULL AND nodeguid IS NULL AND edgeguid IS NULL; ";
            ret += "DELETE FROM 'tags' WHERE tenantguid = '" + tenantGuid + "' AND graphguid IS NOT NULL AND nodeguid IS NULL AND edgeguid IS NULL; ";
            ret += "DELETE FROM 'vectors' WHERE tenantguid = '" + tenantGuid + "' AND graphguid IS NOT NULL AND nodeguid IS NULL AND edgeguid IS NULL; ";

            // Delete all edges
            ret += "DELETE FROM 'edges' WHERE tenantguid = '" + tenantGuid + "'; ";

            // Delete all nodes
            ret += "DELETE FROM 'nodes' WHERE tenantguid = '" + tenantGuid + "'; ";

            // Finally delete all graphs
            ret += "DELETE FROM 'graphs' WHERE tenantguid = '" + tenantGuid + "'; ";

            return ret;
        }

        internal static string Delete(Guid tenantGuid, Guid graphGuid)
        {
            string ret = string.Empty;

            // Delete all edge metadata first
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

            // Delete graph-specific metadata (not associated with nodes or edges)
            ret +=
                "DELETE FROM 'labels' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND nodeguid IS NULL " +
                "AND edgeguid IS NULL; ";

            ret +=
                "DELETE FROM 'tags' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND nodeguid IS NULL " +
                "AND edgeguid IS NULL; ";

            ret +=
                "DELETE FROM 'vectors' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND nodeguid IS NULL " +
                "AND edgeguid IS NULL; ";

            // Delete all edges in the graph
            ret +=
                "DELETE FROM 'edges' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "'; ";

            // Delete all nodes in the graph
            ret +=
                "DELETE FROM 'nodes' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "'; ";

            // Finally delete the graph itself
            ret +=
                "DELETE FROM 'graphs' WHERE " +
                "tenantguid = '" + tenantGuid + "' " +
                "AND guid = '" + graphGuid + "'; ";

            return ret;
        }

        internal static string GetStatistics(Guid? tenantGuid, Guid? graphGuid)
        {
            string ret = "SELECT " +
                "g.tenantguid, " +
                "g.guid, " +
                "(SELECT COUNT(DISTINCT guid) FROM nodes WHERE tenantguid = g.tenantguid AND graphguid = g.guid) AS nodes, " +
                "(SELECT COUNT(DISTINCT guid) FROM edges WHERE tenantguid = g.tenantguid AND graphguid = g.guid) AS edges, " +
                "(SELECT COUNT(DISTINCT guid) FROM labels WHERE tenantguid = g.tenantguid AND graphguid = g.guid) AS labels, " +
                "(SELECT COUNT(DISTINCT guid) FROM tags WHERE tenantguid = g.tenantguid AND graphguid = g.guid) AS tags, " +
                "(SELECT COUNT(DISTINCT guid) FROM vectors WHERE tenantguid = g.tenantguid AND graphguid = g.guid) AS vectors " +
                "FROM graphs g";

            // Build WHERE clause for graphs table
            List<string> conditions = new List<string>();

            if (tenantGuid != null)
            {
                conditions.Add("g.tenantguid = '" + tenantGuid.Value + "'");
            }

            if (graphGuid != null)
            {
                conditions.Add("g.guid = '" + graphGuid.Value + "'");
            }

            if (conditions.Count > 0)
            {
                ret += " WHERE " + string.Join(" AND ", conditions);
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

        private static string MarkerWhereClause(EnumerationOrderEnum order, Graph marker)
        {
            switch (order)
            {
                case EnumerationOrderEnum.CostAscending:
                case EnumerationOrderEnum.CostDescending:
                case EnumerationOrderEnum.LeastConnected:
                case EnumerationOrderEnum.MostConnected:
                    return "graphs.createdutc < '" + marker.CreatedUtc.ToString(TimestampFormat) + "' ";
                case EnumerationOrderEnum.CreatedAscending:
                    return "graphs.createdutc > '" + marker.CreatedUtc.ToString(TimestampFormat) + "' ";
                case EnumerationOrderEnum.CreatedDescending:
                    return "graphs.createdutc < '" + marker.CreatedUtc.ToString(TimestampFormat) + "' ";
                case EnumerationOrderEnum.GuidAscending:
                    return "graphs.guid > '" + marker.GUID + "' ";
                case EnumerationOrderEnum.GuidDescending:
                    return "graphs.guid < '" + marker.GUID + "' ";
                case EnumerationOrderEnum.NameAscending:
                    return "graphs.name > '" + Sanitizer.Sanitize(marker.Name) + "' ";
                case EnumerationOrderEnum.NameDescending:
                    return "graphs.name < '" + Sanitizer.Sanitize(marker.Name) + "' ";
                default:
                    return "graphs.guid IS NOT NULL ";
            }
        }
    }
}
