namespace LiteGraph.GraphRepositories.Sqlite.Queries
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using ExpressionTree;
    using LiteGraph.Serialization;

    internal static class EdgeQueries
    {
        internal static string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        internal static Serializer Serializer = new Serializer();

        internal static string Insert(Edge edge)
        {
            string ret = string.Empty;

            string data = null;
            if (edge.Data != null) data = Sanitizer.SanitizeJson(Serializer.SerializeJson(edge.Data, false));

            ret +=
                "INSERT INTO 'edges' " +
                "(guid, tenantguid, graphguid, name, fromguid, toguid, cost, data, createdutc, lastupdateutc) VALUES " +
                "('" + edge.GUID + "', " +
                "'" + edge.TenantGUID + "', " +
                "'" + edge.GraphGUID + "', " +
                "'" + Sanitizer.Sanitize(edge.Name) + "', " +
                "'" + edge.From + "', " +
                "'" + edge.To + "', " +
                edge.Cost + ", " +
                (!String.IsNullOrEmpty(data) ? "'" + data + "', " : "NULL, ") +
                "'" + edge.CreatedUtc.ToString(TimestampFormat) + "', " +
                "'" + edge.LastUpdateUtc.ToString(TimestampFormat) + "'); ";

            if (edge.Labels != null && edge.Labels.Count > 0)
            {
                List<LabelMetadata> labels = LabelMetadata.FromListString(
                    edge.TenantGUID,
                    edge.GraphGUID,
                    null,
                    edge.GUID,
                    edge.Labels);

                foreach (LabelMetadata label in labels)
                {
                    ret +=
                        "INSERT INTO 'labels' " +
                        "(guid, tenantguid, graphguid, nodeguid, edgeguid, label, createdutc, lastupdateutc) VALUES " +
                        "('" + label.GUID + "', " +
                        "'" + edge.TenantGUID + "', " +
                        "'" + edge.GraphGUID + "', " +
                        (label.NodeGUID.HasValue ? "'" + label.NodeGUID + "'" : "NULL") + ", " +
                        "'" + edge.GUID + "', " +
                        "'" + Sanitizer.Sanitize(label.Label) + "', " +
                        "'" + label.CreatedUtc.ToString(TimestampFormat) + "', " +
                        "'" + label.LastUpdateUtc.ToString(TimestampFormat) + "'); ";
                }
            }

            if (edge.Tags != null && edge.Tags.Count > 0)
            {
                List<TagMetadata> tags = TagMetadata.FromNameValueCollection(
                    edge.TenantGUID,
                    edge.GraphGUID,
                    null,
                    edge.GUID,
                    edge.Tags);

                foreach (TagMetadata tag in tags)
                {
                    ret +=
                        "INSERT INTO 'tags' " +
                        "(guid, tenantguid, graphguid, nodeguid, edgeguid, tagkey, tagvalue, createdutc, lastupdateutc) VALUES " +
                        "('" + tag.GUID + "', " +
                        "'" + edge.TenantGUID + "', " +
                        "'" + edge.GraphGUID + "', " +
                        (tag.NodeGUID.HasValue ? "'" + tag.NodeGUID + "'" : "NULL") + ", " +
                        "'" + edge.GUID + "', " +
                        "'" + Sanitizer.Sanitize(tag.Key) + "', " +
                        "'" + Sanitizer.Sanitize(tag.Value) + "', " +
                        "'" + tag.CreatedUtc.ToString(TimestampFormat) + "', " +
                        "'" + tag.LastUpdateUtc.ToString(TimestampFormat) + "'); ";
                }
            }

            if (edge.Vectors != null && edge.Vectors.Count > 0)
            {
                foreach (VectorMetadata vector in edge.Vectors)
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
                        "'" + edge.TenantGUID + "', " +
                        "'" + edge.GraphGUID + "', " +
                        (vector.NodeGUID.HasValue ? "'" + vector.NodeGUID + "'" : "NULL") + ", " +
                        "'" + edge.GUID + "', " +
                        "'" + Sanitizer.Sanitize(vector.Model) + "', " +
                        vector.Dimensionality + ", " +
                        "'" + Sanitizer.Sanitize(vector.Content) + "', " +
                        "'" + vectorsString + "', " +
                        "'" + vector.CreatedUtc.ToString("yyyy-MM-dd HH:mm:ss.fffffff") + "', " +
                        "'" + vector.LastUpdateUtc.ToString("yyyy-MM-dd HH:mm:ss.fffffff") + "'); ";
                }
            }
            ret +=
                "SELECT * FROM 'edges' WHERE "
                + "graphguid = '" + edge.GraphGUID + "' "
                + "AND tenantguid = '" + edge.TenantGUID + "' "
                + "AND guid = '" + edge.GUID + "';";

            return ret;
        }

        internal static string InsertMany(Guid tenantGuid, List<Edge> edges)
        {
            if (edges == null || edges.Count == 0) return string.Empty;

            string ret = string.Empty;

            foreach (Edge edge in edges)
            {
                if (edge.TenantGUID != tenantGuid) edge.TenantGUID = tenantGuid;

                string data = null;
                if (edge.Data != null) data = Sanitizer.SanitizeJson(Serializer.SerializeJson(edge.Data, false));

                ret +=
                    "INSERT INTO 'edges' " +
                    "(guid, tenantguid, graphguid, name, fromguid, toguid, cost, data, createdutc, lastupdateutc) VALUES " +
                    "('" + edge.GUID + "', " +
                    "'" + edge.TenantGUID + "', " +
                    "'" + edge.GraphGUID + "', " +
                    "'" + Sanitizer.Sanitize(edge.Name) + "', " +
                    "'" + edge.From + "', " +
                    "'" + edge.To + "', " +
                    edge.Cost + ", " +
                    (!String.IsNullOrEmpty(data) ? "'" + data + "', " : "NULL, ") +
                    "'" + edge.CreatedUtc.ToString(TimestampFormat) + "', " +
                    "'" + edge.LastUpdateUtc.ToString(TimestampFormat) + "'); ";

                if (edge.Labels != null && edge.Labels.Count > 0)
                {
                    List<LabelMetadata> labels = LabelMetadata.FromListString(
                        edge.TenantGUID,
                        edge.GraphGUID,
                        null,
                        edge.GUID,
                        edge.Labels);

                    foreach (LabelMetadata label in labels)
                    {
                        ret +=
                            "INSERT INTO 'labels' " +
                            "(guid, tenantguid, graphguid, nodeguid, edgeguid, label, createdutc, lastupdateutc) VALUES " +
                            "('" + label.GUID + "', " +
                            "'" + edge.TenantGUID + "', " +
                            "'" + edge.GraphGUID + "', " +
                            (label.NodeGUID.HasValue ? "'" + label.NodeGUID + "'" : "NULL") + ", " +
                            "'" + edge.GUID + "', " +
                            "'" + Sanitizer.Sanitize(label.Label) + "', " +
                            "'" + label.CreatedUtc.ToString(TimestampFormat) + "', " +
                            "'" + label.LastUpdateUtc.ToString(TimestampFormat) + "'); ";
                    }
                }

                if (edge.Tags != null && edge.Tags.Count > 0)
                {
                    List<TagMetadata> tags = TagMetadata.FromNameValueCollection(
                        edge.TenantGUID,
                        edge.GraphGUID,
                        null,
                        edge.GUID,
                        edge.Tags);

                    foreach (TagMetadata tag in tags)
                    {
                        ret +=
                            "INSERT INTO 'tags' " +
                            "(guid, tenantguid, graphguid, nodeguid, edgeguid, tagkey, tagvalue, createdutc, lastupdateutc) VALUES " +
                            "('" + tag.GUID + "', " +
                            "'" + edge.TenantGUID + "', " +
                            "'" + edge.GraphGUID + "', " +
                            (tag.NodeGUID.HasValue ? "'" + tag.NodeGUID + "'" : "NULL") + ", " +
                            "'" + edge.GUID + "', " +
                            "'" + Sanitizer.Sanitize(tag.Key) + "', " +
                            "'" + Sanitizer.Sanitize(tag.Value) + "', " +
                            "'" + tag.CreatedUtc.ToString(TimestampFormat) + "', " +
                            "'" + tag.LastUpdateUtc.ToString(TimestampFormat) + "'); ";
                    }
                }

                if (edge.Vectors != null && edge.Vectors.Count > 0)
                {
                    foreach (VectorMetadata vector in edge.Vectors)
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
                            "'" + edge.TenantGUID + "', " +
                            "'" + edge.GraphGUID + "', " +
                            (vector.NodeGUID.HasValue ? "'" + vector.NodeGUID + "'" : "NULL") + ", " +
                            "'" + edge.GUID + "', " +
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
            string ret = "SELECT * FROM 'edges' WHERE tenantguid = '" + tenantGuid + "' ";
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
                "SELECT * FROM 'edges' WHERE tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' ";

            ret +=
                "ORDER BY " + Converters.EnumerationOrderToClause(order) + " "
                + "LIMIT " + batchSize + " OFFSET " + skip + ";";
            return ret;
        }

        internal static string SelectMany(Guid tenantGuid, List<Guid> guids)
        {
            string ret = "SELECT * FROM 'edges' WHERE tenantguid = '" + tenantGuid + "' AND guid IN (";

            for (int i = 0; i < guids.Count; i++)
            {
                if (i > 0) ret += ",";
                ret += "'" + Sanitizer.Sanitize(guids[i].ToString()) + "'";
            }

            ret += ");";
            return ret;
        }

        internal static string Select(Guid tenantGuid, Guid graphGuid, Guid edgeGuid)
        {
            return
                "SELECT * FROM 'edges' WHERE "
                + "graphguid = '" + graphGuid + "' "
                + "AND tenantguid = '" + tenantGuid + "' "
                + "AND guid = '" + edgeGuid + "';";
        }

        internal static string SelectMany(
            Guid tenantGuid,
            Guid graphGuid,
            List<string> labels,
            NameValueCollection tags,
            Expr edgeFilter = null,
            int batchSize = 100,
            int skip = 0,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            string ret =
                "SELECT * FROM 'edges' ";

            if (labels != null && labels.Count > 0)
                ret += "INNER JOIN 'labels' "
                    + "ON edges.guid = labels.edgeguid "
                    + "AND edges.graphguid = labels.graphguid "
                    + "AND edges.tenantguid = labels.tenantguid ";

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

            ret += "WHERE "
                + "edges.graphguid = '" + graphGuid + "' "
                + "AND edges.tenantguid = '" + tenantGuid + "' ";

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
                if (!String.IsNullOrEmpty(filterClause)) ret += "AND " + filterClause;
            }

            if (labels != null && labels.Count > 0)
            {
                ret += "GROUP BY edges.guid ";

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

        internal static string SelectConnected(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            List<string> labels,
            NameValueCollection tags,
            Expr edgeFilter = null,
            int batchSize = 100,
            int skip = 0,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            string ret = "SELECT * FROM 'edges' ";

            if (labels != null && labels.Count > 0)
                ret += "INNER JOIN 'labels' "
                    + "ON edges.guid = labels.edgeguid "
                    + "AND edges.graphguid = labels.graphguid "
                    + "AND edges.tenantguid = labels.tenantguid ";

            if (tags != null && tags.Count > 0)
                ret += "INNER JOIN 'tags' "
                    + "ON edges.guid = tags.edgeguid "
                    + "AND edges.graphguid = tags.graphguid "
                    + "AND edges.tenantguid = tags.tenantguid ";

            ret += "WHERE "
                + "edges.tenantguid = '" + tenantGuid + "' AND "
                + "edges.graphguid = '" + graphGuid + "' AND "
                + "("
                + "edges.fromguid = '" + nodeGuid + "' "
                + "OR edges.toguid = '" + nodeGuid + "'"
                + ") ";

            if (labels != null && labels.Count > 0)
            {
                foreach (string label in labels)
                {
                    ret += "AND labels.label = '" + Sanitizer.Sanitize(label) + "' ";
                }
            }

            if (tags != null && tags.Count > 0)
            {
                foreach (string key in tags.AllKeys)
                {
                    string val = tags.Get(key);
                    ret += "AND tags.tagkey = '" + Sanitizer.Sanitize(key) + "' ";
                    if (!String.IsNullOrEmpty(val)) ret += "AND tags.tagvalue = '" + Sanitizer.Sanitize(val) + "' ";
                    else ret += "AND tags.tagvalue IS NULL ";
                }
            }

            if (edgeFilter != null) ret += "AND " + Converters.ExpressionToWhereClause("edges", edgeFilter);

            ret +=
                "ORDER BY " + Converters.EnumerationOrderToClause(order) + " "
                + "LIMIT " + batchSize + " OFFSET " + skip + ";";

            return ret;
        }

        internal static string SelectEdgesFrom(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            List<string> labels,
            NameValueCollection tags,
            Expr edgeFilter = null,
            int batchSize = 100,
            int skip = 0,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            string ret =
                "SELECT * FROM 'edges' ";

            if (labels != null && labels.Count > 0)
                ret += "INNER JOIN 'labels' "
                    + "ON edges.guid = labels.edgeguid "
                    + "AND edges.graphguid = labels.graphguid "
                    + "AND edges.tenantguid = labels.tenantguid ";

            if (tags != null && tags.Count > 0)
                ret += "INNER JOIN 'tags' "
                    + "ON edges.guid = tags.edgeguid "
                    + "AND edges.graphguid = tags.graphguid "
                    + "AND edges.tenantguid = tags.tenantguid ";

            ret += "WHERE "
                + "edges.graphguid = '" + graphGuid + "' "
                + "AND edges.tenantguid = '" + tenantGuid + "' "
                + "AND edges.fromguid = '" + nodeGuid + "' ";

            if (labels != null && labels.Count > 0)
            {
                foreach (string label in labels)
                {
                    ret += "AND labels.label = '" + Sanitizer.Sanitize(label) + "' ";
                }
            }

            if (tags != null && tags.Count > 0)
            {
                foreach (string key in tags.AllKeys)
                {
                    string val = tags.Get(key);
                    ret += "AND tags.tagkey = '" + Sanitizer.Sanitize(key) + "' ";
                    if (!String.IsNullOrEmpty(val)) ret += "AND tags.tagvalue = '" + Sanitizer.Sanitize(val) + "' ";
                    else ret += "AND tags.tagvalue IS NULL ";
                }
            }

            if (edgeFilter != null) ret += "AND " + Converters.ExpressionToWhereClause("edges", edgeFilter) + " ";

            ret +=
                "ORDER BY " + Converters.EnumerationOrderToClause(order) + " "
                + "LIMIT " + batchSize + " OFFSET " + skip + ";";

            return ret;
        }

        internal static string SelectEdgesTo(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            List<string> labels,
            NameValueCollection tags,
            Expr edgeFilter = null,
            int batchSize = 100,
            int skip = 0,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            string ret =
                "SELECT * FROM 'edges' ";

            if (labels != null && labels.Count > 0)
                ret += "INNER JOIN 'labels' "
                    + "ON edges.guid = labels.edgeguid "
                    + "AND edges.graphguid = labels.graphguid "
                    + "AND edges.tenantguid = labels.tenantguid ";

            if (tags != null && tags.Count > 0)
                ret += "INNER JOIN 'tags' "
                    + "ON edges.guid = tags.edgeguid "
                    + "AND edges.graphguid = tags.graphguid "
                    + "AND edges.tenantguid = tags.tenantguid ";

            ret += "WHERE "
                + "edges.graphguid = '" + graphGuid + "' "
                + "AND edges.tenantguid = '" + tenantGuid + "' "
                + "AND edges.toguid = '" + nodeGuid + "' ";

            if (labels != null && labels.Count > 0)
            {
                foreach (string label in labels)
                {
                    ret += "AND labels.label = '" + Sanitizer.Sanitize(label) + "' ";
                }
            }

            if (tags != null && tags.Count > 0)
            {
                foreach (string key in tags)
                {
                    string val = tags.Get(key);
                    ret += "AND tags.tagkey = '" + Sanitizer.Sanitize(key) + "' ";
                    if (!String.IsNullOrEmpty(val)) ret += "AND tags.tagvalue = '" + Sanitizer.Sanitize(val) + "' ";
                    else ret += "AND tags.tagvalue IS NULL ";
                }
            }

            if (edgeFilter != null) ret += "AND " + Converters.ExpressionToWhereClause("edges", edgeFilter) + " ";

            ret +=
                "ORDER BY " + Converters.EnumerationOrderToClause(order) + " "
                + "LIMIT " + batchSize + " OFFSET " + skip + ";";

            return ret;
        }

        internal static string SelectEdgesBetween(
            Guid tenantGuid,
            Guid graphGuid,
            Guid from,
            Guid to,
            List<string> labels,
            NameValueCollection tags,
            Expr edgeFilter = null,
            int batchSize = 0,
            int skip = 0,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            string ret =
                "SELECT * FROM 'edges' ";

            if (labels != null && labels.Count > 0)
                ret += "INNER JOIN 'labels' "
                    + "ON edges.guid = labels.edgeguid "
                    + "AND edges.graphguid = labels.graphguid "
                    + "AND edges.tenantguid = labels.tenantguid ";

            if (tags != null && tags.Count > 0)
                ret += "INNER JOIN 'tags' "
                    + "ON edges.guid = tags.edgeguid "
                    + "AND edges.graphguid = tags.graphguid "
                    + "AND edges.tenantguid = tags.tenantguid ";

            ret += "WHERE "
                + "edges.graphguid = '" + graphGuid + "' "
                + "AND edges.tenantguid = '" + tenantGuid + "' "
                + "AND edges.fromguid = '" + from + "' "
                + "AND edges.toguid = '" + to + "' ";

            if (labels != null && labels.Count > 0)
            {
                foreach (string label in labels)
                {
                    ret += "AND labels.label = '" + Sanitizer.Sanitize(label) + "' ";
                }
            }

            if (tags != null && tags.Count > 0)
            {
                foreach (string key in tags)
                {
                    string val = tags.Get(key);
                    ret += "AND tags.tagkey = '" + Sanitizer.Sanitize(key) + "' ";
                    if (!String.IsNullOrEmpty(val)) ret += "AND tags.tagvalue = '" + Sanitizer.Sanitize(val) + "' ";
                    else ret += "AND tags.tagvalue IS NULL ";
                }
            }

            if (edgeFilter != null) ret += "AND " + Converters.ExpressionToWhereClause("edges", edgeFilter) + " ";

            ret +=
                "ORDER BY " + Converters.EnumerationOrderToClause(order) + " "
                + "LIMIT " + batchSize + " OFFSET " + skip + ";";

            return ret;
        }

        internal static string Update(Edge edge)
        {
            string ret = string.Empty;

            string data = null;
            if (edge.Data != null) data = Sanitizer.SanitizeJson(Serializer.SerializeJson(edge.Data, false));

            ret +=
                "UPDATE 'edges' SET " +
                "name = '" + Sanitizer.Sanitize(edge.Name) + "', " +
                "fromguid = '" + edge.From + "', " +
                "toguid = '" + edge.To + "', " +
                "cost = " + edge.Cost + ", " +
                "data = " + (!String.IsNullOrEmpty(data) ? "'" + data + "', " : "NULL, ") +
                "lastupdateutc = '" + DateTime.UtcNow.ToString(TimestampFormat) + "' " +
                "WHERE tenantguid = '" + edge.TenantGUID + "' " +
                "AND graphguid = '" + edge.GraphGUID + "' " +
                "AND guid = '" + edge.GUID + "'; ";

            ret +=
                "DELETE FROM 'labels' WHERE " +
                "tenantguid = '" + edge.TenantGUID + "' " +
                "AND graphguid = '" + edge.GraphGUID + "' " +
                "AND edgeguid = '" + edge.GUID + "'; ";

            ret +=
                "DELETE FROM 'tags' WHERE " +
                "tenantguid = '" + edge.TenantGUID + "' " +
                "AND graphguid = '" + edge.GraphGUID + "' " +
                "AND edgeguid = '" + edge.GUID + "'; ";

            ret +=
                "DELETE FROM 'vectors' WHERE " +
                "tenantguid = '" + edge.TenantGUID + "' " +
                "AND graphguid = '" + edge.GraphGUID + "' " +
                "AND edgeguid = '" + edge.GUID + "'; ";

            if (edge.Labels != null && edge.Labels.Count > 0)
            {
                List<LabelMetadata> labels = LabelMetadata.FromListString(
                    edge.TenantGUID,
                    edge.GraphGUID,
                    null,
                    edge.GUID,
                    edge.Labels);

                foreach (LabelMetadata label in labels)
                {
                    ret +=
                        "INSERT INTO 'labels' " +
                        "(guid, tenantguid, graphguid, nodeguid, edgeguid, label, createdutc, lastupdateutc) VALUES " +
                        "('" + label.GUID + "', " +
                        "'" + edge.TenantGUID + "', " +
                        "'" + edge.GraphGUID + "', " +
                        (label.NodeGUID.HasValue ? "'" + label.NodeGUID + "'" : "NULL") + ", " +
                        "'" + edge.GUID + "', " +
                        "'" + Sanitizer.Sanitize(label.Label) + "', " +
                        "'" + label.CreatedUtc.ToString(TimestampFormat) + "', " +
                        "'" + DateTime.UtcNow.ToString(TimestampFormat) + "'); ";
                }
            }

            if (edge.Tags != null && edge.Tags.Count > 0)
            {
                List<TagMetadata> tags = TagMetadata.FromNameValueCollection(
                    edge.TenantGUID,
                    edge.GraphGUID,
                    null,
                    edge.GUID,
                    edge.Tags);

                foreach (TagMetadata tag in tags)
                {
                    ret +=
                        "INSERT INTO 'tags' " +
                        "(guid, tenantguid, graphguid, nodeguid, edgeguid, tagkey, tagvalue, createdutc, lastupdateutc) VALUES " +
                        "('" + tag.GUID + "', " +
                        "'" + edge.TenantGUID + "', " +
                        "'" + edge.GraphGUID + "', " +
                        (tag.NodeGUID.HasValue ? "'" + tag.NodeGUID + "'" : "NULL") + ", " +
                        "'" + edge.GUID + "', " +
                        "'" + Sanitizer.Sanitize(tag.Key) + "', " +
                        "'" + Sanitizer.Sanitize(tag.Value) + "', " +
                        "'" + tag.CreatedUtc.ToString(TimestampFormat) + "', " +
                        "'" + DateTime.UtcNow.ToString(TimestampFormat) + "'); ";
                }
            }

            if (edge.Vectors != null && edge.Vectors.Count > 0)
            {
                foreach (VectorMetadata vector in edge.Vectors)
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
                        "'" + edge.TenantGUID + "', " +
                        "'" + edge.GraphGUID + "', " +
                        (vector.NodeGUID.HasValue ? "'" + vector.NodeGUID + "'" : "NULL") + ", " +
                        "'" + edge.GUID + "', " +
                        "'" + Sanitizer.Sanitize(vector.Model) + "', " +
                        vector.Dimensionality + ", " +
                        "'" + Sanitizer.Sanitize(vector.Content) + "', " +
                        "'" + vectorsString + "', " +
                        "'" + vector.CreatedUtc.ToString("yyyy-MM-dd HH:mm:ss.fffffff") + "', " +
                        "'" + DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fffffff") + "'); ";
                }
            }

            ret +=
                "SELECT * FROM 'edges' WHERE "
                + "graphguid = '" + edge.GraphGUID + "' "
                + "AND tenantguid = '" + edge.TenantGUID + "' "
                + "AND guid = '" + edge.GUID + "';";

            return ret;
        }

        internal static string Delete(Guid tenantGuid, Guid graphGuid, Guid edgeGuid)
        {
            string ret = 
                "DELETE FROM 'edges' WHERE "
                + "graphguid = '" + graphGuid + "' "
                + "AND tenantguid = '" + tenantGuid + "' "
                + "AND guid = '" + edgeGuid + "'; ";

            ret +=
                "DELETE FROM 'labels' WHERE "
                + "graphguid = '" + graphGuid + "' "
                + "AND tenantguid = '" + tenantGuid + "' "
                + "AND edgeguid = '" + edgeGuid + "'; ";

            ret +=
                "DELETE FROM 'tags' WHERE "
                + "graphguid = '" + graphGuid + "' "
                + "AND tenantguid = '" + tenantGuid + "' "
                + "AND edgeguid = '" + edgeGuid + "'; ";

            ret +=
                "DELETE FROM 'vectors' WHERE "
                + "graphguid = '" + graphGuid + "' "
                + "AND tenantguid = '" + tenantGuid + "' "
                + "AND edgeguid = '" + edgeGuid + "'; ";

            return ret;
        }

        internal static string DeleteAllInTenant(Guid tenantGuid)
        {
            string ret =
                "DELETE FROM 'edges' WHERE "
                + "tenantguid = '" + tenantGuid + "'; ";

            ret +=
                "DELETE FROM 'labels' WHERE "
                + "tenantguid = '" + tenantGuid + "'; ";

            ret +=
                "DELETE FROM 'tags' WHERE "
                + "tenantguid = '" + tenantGuid + "'; ";

            ret +=
                "DELETE FROM 'vectors' WHERE "
                + "tenantguid = '" + tenantGuid + "'; ";

            return ret;
        }

        internal static string DeleteAllInGraph(Guid tenantGuid, Guid graphGuid)
        {
            string ret =
                "DELETE FROM 'edges' WHERE "
                + "graphguid = '" + graphGuid + "' "
                + "AND tenantguid = '" + tenantGuid + "'; ";

            ret +=
                "DELETE FROM 'labels' WHERE "
                + "graphguid = '" + graphGuid + "' "
                + "AND tenantguid = '" + tenantGuid + "'; ";

            ret +=
                "DELETE FROM 'tags' WHERE "
                + "graphguid = '" + graphGuid + "' "
                + "AND tenantguid = '" + tenantGuid + "'; ";

            ret +=
                "DELETE FROM 'vectors' WHERE "
                + "graphguid = '" + graphGuid + "' "
                + "AND tenantguid = '" + tenantGuid + "'; ";

            return ret;
        }

        internal static string DeleteNodeEdges(Guid tenantGuid, Guid graphGuid, List<Guid> nodeGuids)
        {
            string guidList = string.Join(",", nodeGuids.Select(guid => "'" + guid + "'"));

            string findEdgesQuery =
                "SELECT guid FROM 'edges' " +
                "WHERE tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND (toguid IN (" + guidList + ") " +
                "OR fromguid IN (" + guidList + "));";

            string ret =
                "DELETE FROM 'labels' " +
                "WHERE tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND edgeguid IN (" + findEdgesQuery.Replace(";", "") + "); ";

            ret +=
                "DELETE FROM 'tags' " +
                "WHERE tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND edgeguid IN (" + findEdgesQuery.Replace(";", "") + "); ";

            ret +=
                "DELETE FROM 'vectors' " +
                "WHERE tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND edgeguid IN (" + findEdgesQuery.Replace(";", "") + "); ";

            ret +=
                "DELETE FROM 'edges' " +
                "WHERE tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND (toguid IN (" + guidList + ") " +
                "OR fromguid IN (" + guidList + "));";

            return ret;
        }

        internal static string DeleteMany(Guid tenantGuid, Guid graphGuid, List<Guid> edgeGuids)
        {
            string guidList = string.Join(",", edgeGuids.Select(guid => "'" + guid + "'"));

            string ret =
                "DELETE FROM 'labels' " +
                "WHERE tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND edgeguid IN (" + guidList + "); ";

            ret +=
                "DELETE FROM 'tags' " +
                "WHERE tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND edgeguid IN (" + guidList + "); ";

            ret +=
                "DELETE FROM 'vectors' " +
                "WHERE tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND edgeguid IN (" + guidList + "); ";

            ret +=
                "DELETE FROM 'edges' " +
                "WHERE tenantguid = '" + tenantGuid + "' " +
                "AND graphguid = '" + graphGuid + "' " +
                "AND guid IN (" + guidList + ");";

            return ret;
        }

        internal static string BatchExists(Guid tenantGuid, Guid graphGuid, List<Guid> edgeGuids)
        {
            string query = "WITH temp(guid) AS (VALUES ";

            for (int i = 0; i < edgeGuids.Count; i++)
            {
                if (i > 0) query += ",";
                query += "('" + edgeGuids[i].ToString() + "')";
            }

            query +=
                ") "
                + "SELECT temp.guid, CASE WHEN edges.guid IS NOT NULL THEN 1 ELSE 0 END as \"exists\" "
                + "FROM temp "
                + "LEFT JOIN edges ON temp.guid = edges.guid "
                + "AND edges.graphguid = '" + graphGuid + "' "
                + "AND edges.tenantguid = '" + tenantGuid + "';";

            return query;
        }

        internal static string BatchExistsBetween(Guid tenantGuid, Guid graphGuid, List<EdgeBetween> edgesBetween)
        {
            string query = "WITH temp(fromguid, toguid) AS (VALUES ";

            for (int i = 0; i < edgesBetween.Count; i++)
            {
                EdgeBetween curr = edgesBetween[i];
                if (i > 0) query += ",";
                query += "('" + curr.From.ToString() + "','" + curr.To.ToString() + "')";
            }

            query +=
                ") "
                + "SELECT temp.fromguid, temp.toguid, CASE WHEN edges.fromguid IS NOT NULL THEN 1 ELSE 0 END AS \"exists\" "
                + "FROM temp "
                + "LEFT JOIN edges ON temp.fromguid = edges.fromguid "
                + "AND temp.toguid = edges.toguid "
                + "AND edges.graphguid = '" + graphGuid + "' "
                + "AND edges.tenantguid = '" + tenantGuid + "';";

            return query;
        }
    }
}
