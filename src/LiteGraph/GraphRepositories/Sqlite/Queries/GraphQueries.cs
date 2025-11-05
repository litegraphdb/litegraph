namespace LiteGraph.GraphRepositories.Sqlite.Queries
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
                + "(guid, tenantguid, name, vectorindextype, vectorindexfile, vectorindexthreshold, "
                + "vectordimensionality, vectorindexm, vectorindexef, vectorindexefconstruction, "
                + "data, createdutc, lastupdateutc) VALUES "
                + "('" + graph.GUID + "',"
                + "'" + graph.TenantGUID + "',"
                + "'" + Sanitizer.Sanitize(graph.Name) + "',";

            // Vector index fields
            if (graph.VectorIndexType.HasValue) ret += "'" + graph.VectorIndexType.Value.ToString() + "',";
            else ret += "null,";

            if (!string.IsNullOrEmpty(graph.VectorIndexFile)) ret += "'" + Sanitizer.Sanitize(graph.VectorIndexFile) + "',";
            else ret += "null,";

            if (graph.VectorIndexThreshold.HasValue) ret += graph.VectorIndexThreshold.Value + ",";
            else ret += "null,";

            if (graph.VectorDimensionality.HasValue) ret += graph.VectorDimensionality.Value + ",";
            else ret += "null,";

            if (graph.VectorIndexM.HasValue) ret += graph.VectorIndexM.Value + ",";
            else ret += "null,";

            if (graph.VectorIndexEf.HasValue) ret += graph.VectorIndexEf.Value + ",";
            else ret += "null,";

            if (graph.VectorIndexEfConstruction.HasValue) ret += graph.VectorIndexEfConstruction.Value + ",";
            else ret += "null,";

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
                        vectorsString = Converters.BytesToHex(Converters.VectorToBlob(vector.Vectors));
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
                        vectorsString + ", " +
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
                "lastupdateutc = '" + DateTime.UtcNow.ToString(TimestampFormat) + "', ";

            // Vector index fields
            if (graph.VectorIndexType.HasValue) ret += "vectorindextype = '" + graph.VectorIndexType.Value.ToString() + "', ";
            else ret += "vectorindextype = null, ";

            if (!string.IsNullOrEmpty(graph.VectorIndexFile)) ret += "vectorindexfile = '" + Sanitizer.Sanitize(graph.VectorIndexFile) + "', ";
            else ret += "vectorindexfile = null, ";

            if (graph.VectorIndexThreshold.HasValue) ret += "vectorindexthreshold = " + graph.VectorIndexThreshold.Value + ", ";
            else ret += "vectorindexthreshold = null, ";

            if (graph.VectorDimensionality.HasValue) ret += "vectordimensionality = " + graph.VectorDimensionality.Value + ", ";
            else ret += "vectordimensionality = null, ";

            if (graph.VectorIndexM.HasValue) ret += "vectorindexm = " + graph.VectorIndexM.Value + ", ";
            else ret += "vectorindexm = null, ";

            if (graph.VectorIndexEf.HasValue) ret += "vectorindexef = " + graph.VectorIndexEf.Value + ", ";
            else ret += "vectorindexef = null, ";

            if (graph.VectorIndexEfConstruction.HasValue) ret += "vectorindexefconstruction = " + graph.VectorIndexEfConstruction.Value + ", ";
            else ret += "vectorindexefconstruction = null, ";

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
                        vectorsString = Converters.BytesToHex(Converters.VectorToBlob(vector.Vectors));
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
                        vectorsString + ", " +
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

        internal static string GetSubgraphStatistics(Guid tenantGuid, Guid graphGuid, Guid nodeGuid, int maxDepth = 2, int maxNodes = 0, int maxEdges = 0)
        {
            string ret = "WITH RECURSIVE starting_node AS (";
            ret += "SELECT guid FROM nodes WHERE tenantguid = '" + tenantGuid + "' AND graphguid = '" + graphGuid + "' AND guid = '" + nodeGuid + "' ";
            ret += "), ";

            ret += "subgraph_traversal(nodeguid, edgeguid, depth, visited_path) AS (";

            ret += "SELECT " +
                "sn.guid AS nodeguid, " +
                "NULL AS edgeguid, " +
                "0 AS depth, " +
                "'" + nodeGuid + "' AS visited_path " +
                "FROM starting_node sn ";

            ret += "UNION ALL ";

            // Recursive case: traverse to neighbors (avoiding cycles)
            ret += "SELECT " +
                "CASE WHEN e.fromguid = st.nodeguid THEN e.toguid ELSE e.fromguid END AS nodeguid, " +
                "e.guid AS edgeguid, " +
                "st.depth + 1 AS depth, " +
                "st.visited_path || '|' || CASE WHEN e.fromguid = st.nodeguid THEN e.toguid ELSE e.fromguid END AS visited_path " +
                "FROM subgraph_traversal st " +
                "INNER JOIN edges e ON " +
                "(e.fromguid = st.nodeguid OR e.toguid = st.nodeguid) " +
                "AND e.tenantguid = '" + tenantGuid + "' " +
                "AND e.graphguid = '" + graphGuid + "' " +
                "WHERE st.depth < " + maxDepth + " ";

            // Check if the neighbor GUID appears in the path (either with pipe separator or at start/end)
            ret += "AND (" +
                "INSTR(st.visited_path, '|' || CASE WHEN e.fromguid = st.nodeguid THEN e.toguid ELSE e.fromguid END || '|') = 0 " +
                "AND st.visited_path NOT LIKE (CASE WHEN e.fromguid = st.nodeguid THEN e.toguid ELSE e.fromguid END || '|%') " +
                "AND st.visited_path NOT LIKE ('%|' || CASE WHEN e.fromguid = st.nodeguid THEN e.toguid ELSE e.fromguid END) " +
                "AND st.visited_path != (CASE WHEN e.fromguid = st.nodeguid THEN e.toguid ELSE e.fromguid END)" +
                ") ";

            ret += "), ";

            // Collect distinct nodes and edges in traversal order (by minimum depth encountered)
            ret += "traversal_nodes AS (SELECT DISTINCT nodeguid FROM subgraph_traversal WHERE nodeguid IS NOT NULL), ";
            ret += "traversal_edges AS (SELECT DISTINCT edgeguid FROM subgraph_traversal WHERE edgeguid IS NOT NULL), ";
            ret += "ordered_nodes AS (SELECT nodeguid, MIN(depth) AS first_depth FROM subgraph_traversal WHERE nodeguid IS NOT NULL GROUP BY nodeguid ORDER BY first_depth, nodeguid), ";

            // Ordered traversal edges (by first appearance depth)
            ret += "ordered_edges AS (SELECT edgeguid, MIN(depth) AS first_depth FROM subgraph_traversal WHERE edgeguid IS NOT NULL GROUP BY edgeguid ORDER BY first_depth, edgeguid), ";

            // If maxEdges specified, limit edges and filter nodes to only those connected by the limited edges
            string nodesCTE;
            string edgesCTE;

            if (maxNodes > 0 && maxEdges > 0)
            {
                // Both limits: take first maxNodes nodes, filter edges to those connecting these nodes, then limit to maxEdges
                nodesCTE = "subgraph_nodes AS (SELECT nodeguid FROM ordered_nodes LIMIT " + maxNodes + "), ";
                edgesCTE = "subgraph_edges AS (SELECT DISTINCT e.guid AS edgeguid FROM edges e " +
                    "INNER JOIN subgraph_nodes sn1 ON e.fromguid = sn1.nodeguid " +
                    "INNER JOIN subgraph_nodes sn2 ON e.toguid = sn2.nodeguid " +
                    "WHERE e.tenantguid = '" + tenantGuid + "' AND e.graphguid = '" + graphGuid + "' " +
                    "AND e.guid IN (SELECT edgeguid FROM ordered_edges) " +
                    "ORDER BY (SELECT first_depth FROM ordered_edges WHERE edgeguid = e.guid) " +
                    "LIMIT " + maxEdges + ") ";
            }
            else if (maxNodes > 0)
            {
                // Only maxNodes: limit nodes, then filter edges to only those connecting the limited nodes
                nodesCTE = "subgraph_nodes AS (SELECT nodeguid FROM ordered_nodes LIMIT " + maxNodes + "), ";
                edgesCTE = "subgraph_edges AS (SELECT DISTINCT e.guid AS edgeguid FROM edges e " +
                    "INNER JOIN subgraph_nodes sn1 ON e.fromguid = sn1.nodeguid " +
                    "INNER JOIN subgraph_nodes sn2 ON e.toguid = sn2.nodeguid " +
                    "WHERE e.tenantguid = '" + tenantGuid + "' AND e.graphguid = '" + graphGuid + "' " +
                    "AND e.guid IN (SELECT edgeguid FROM ordered_edges)) ";
            }
            else if (maxEdges > 0)
            {
                // Only maxEdges: limit edges first, then filter nodes to only those connected by the limited edges
                edgesCTE = "subgraph_edges AS (SELECT edgeguid FROM ordered_edges LIMIT " + maxEdges + "), ";
                nodesCTE = "subgraph_nodes AS (SELECT DISTINCT n.guid AS nodeguid FROM nodes n " +
                    "INNER JOIN edges e ON (e.fromguid = n.guid OR e.toguid = n.guid) " +
                    "WHERE e.guid IN (SELECT edgeguid FROM subgraph_edges) " +
                    "AND n.tenantguid = '" + tenantGuid + "' AND n.graphguid = '" + graphGuid + "') ";
            }
            else
            {
                // No limits: use all nodes and edges
                nodesCTE = "subgraph_nodes AS (SELECT nodeguid FROM ordered_nodes), ";
                edgesCTE = "subgraph_edges AS (SELECT edgeguid FROM ordered_edges) ";
            }

            if (maxEdges > 0 && maxNodes == 0)
            {
                ret += edgesCTE + nodesCTE;
            }
            else
            {
                ret += nodesCTE + edgesCTE;
            }

            ret += "SELECT ";

            // Count nodes (using COALESCE to handle empty results)
            ret += "COALESCE((SELECT COUNT(*) FROM subgraph_nodes), 0) AS nodes, ";

            // Count edges (using COALESCE to handle empty results)
            ret += "COALESCE((SELECT COUNT(*) FROM subgraph_edges), 0) AS edges, ";

            // Count labels (for nodes and edges) - using UNION to get distinct, handle empty subgraphs
            ret += "COALESCE((SELECT COUNT(DISTINCT guid) FROM (";
            ret += "SELECT guid FROM labels WHERE tenantguid = '" + tenantGuid + "' AND graphguid = '" + graphGuid + "' " +
                "AND nodeguid IN (SELECT nodeguid FROM subgraph_nodes) ";
            ret += "UNION ";
            ret += "SELECT guid FROM labels WHERE tenantguid = '" + tenantGuid + "' AND graphguid = '" + graphGuid + "' " +
                "AND edgeguid IN (SELECT edgeguid FROM subgraph_edges)";
            ret += ")), 0) AS labels, ";

            // Count tags (for nodes and edges) - using UNION to get distinct, handle empty subgraphs
            ret += "COALESCE((SELECT COUNT(DISTINCT guid) FROM (";
            ret += "SELECT guid FROM tags WHERE tenantguid = '" + tenantGuid + "' AND graphguid = '" + graphGuid + "' " +
                "AND nodeguid IN (SELECT nodeguid FROM subgraph_nodes) ";
            ret += "UNION ";
            ret += "SELECT guid FROM tags WHERE tenantguid = '" + tenantGuid + "' AND graphguid = '" + graphGuid + "' " +
                "AND edgeguid IN (SELECT edgeguid FROM subgraph_edges)";
            ret += ")), 0) AS tags, ";

            // Count vectors (for nodes and edges) - using UNION to get distinct, handle empty subgraphs
            ret += "COALESCE((SELECT COUNT(DISTINCT guid) FROM (";
            ret += "SELECT guid FROM vectors WHERE tenantguid = '" + tenantGuid + "' AND graphguid = '" + graphGuid + "' " +
                "AND nodeguid IN (SELECT nodeguid FROM subgraph_nodes) AND edgeguid IS NULL ";
            ret += "UNION ";
            ret += "SELECT guid FROM vectors WHERE tenantguid = '" + tenantGuid + "' AND graphguid = '" + graphGuid + "' " +
                "AND edgeguid IN (SELECT edgeguid FROM subgraph_edges) AND nodeguid IS NULL";
            ret += ")), 0) AS vectors; ";

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
