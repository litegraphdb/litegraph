namespace LiteGraph.GraphRepositories.Postgresql.Queries
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Globalization;
    using System.Text;
    using ExpressionTree;

    /// <summary>
    /// pgvector search and index SQL for the PostgreSQL repository.
    /// Search SQL is written in the repository's SQLite-shaped dialect so the translator can apply schema
    /// qualification and JSON filter translation; index DDL is native PostgreSQL.
    /// </summary>
    internal static class PgvectorQueries
    {
        /// <summary>
        /// Largest dimensionality pgvector can index with the vector type.
        /// </summary>
        internal const int MaxVectorIndexDimensions = 2000;

        /// <summary>
        /// Largest dimensionality pgvector can index with the halfvec type.
        /// </summary>
        internal const int MaxHalfvecIndexDimensions = 4000;

        /// <summary>
        /// Largest ef_search value pgvector accepts.
        /// </summary>
        internal const int MaxEfSearch = 1000;

        internal static bool IsIndexableDimensionality(int dimensions)
        {
            return dimensions >= 1 && dimensions <= MaxHalfvecIndexDimensions;
        }

        internal static bool IsCosine(VectorSearchTypeEnum searchType)
        {
            return searchType == VectorSearchTypeEnum.CosineDistance || searchType == VectorSearchTypeEnum.CosineSimilarity;
        }

        internal static string IndexName(int dimensions)
        {
            return "idx_vectors_hnsw_cosine_" + dimensions.ToString(CultureInfo.InvariantCulture);
        }

        internal static string IndexedType(int dimensions)
        {
            string d = dimensions.ToString(CultureInfo.InvariantCulture);
            return dimensions <= MaxVectorIndexDimensions ? "vector(" + d + ")" : "halfvec(" + d + ")";
        }

        /// <summary>
        /// CREATE INDEX for the cosine HNSW index of one dimensionality.  The index name must not be schema-qualified;
        /// PostgreSQL always creates an index in its table's schema.
        /// </summary>
        internal static string CreateIndex(string qualifiedVectorsTable, string quotedIndexName, int dimensions, int m, int efConstruction, bool concurrently)
        {
            string type = IndexedType(dimensions);
            string opClass = dimensions <= MaxVectorIndexDimensions ? "vector_cosine_ops" : "halfvec_cosine_ops";
            return "CREATE INDEX " + (concurrently ? "CONCURRENTLY " : "") + "IF NOT EXISTS " + quotedIndexName
                + " ON " + qualifiedVectorsTable
                + " USING hnsw ((embeddings::" + type + ") " + opClass + ")"
                + " WITH (m = " + m.ToString(CultureInfo.InvariantCulture) + ", ef_construction = " + efConstruction.ToString(CultureInfo.InvariantCulture) + ")"
                + " WHERE vector_dims(embeddings) = " + dimensions.ToString(CultureInfo.InvariantCulture) + ";";
        }

        internal static string IndexState(string schema, string indexName)
        {
            return "SELECT i.indisvalid AS valid, pg_relation_size(c.oid) AS sizebytes "
                + "FROM pg_class c "
                + "INNER JOIN pg_namespace n ON n.oid = c.relnamespace "
                + "INNER JOIN pg_index i ON i.indexrelid = c.oid "
                + "WHERE n.nspname = '" + Sanitizer.Sanitize(schema) + "' AND c.relname = '" + Sanitizer.Sanitize(indexName) + "';";
        }

        internal static string CountIndexedGraphsWithDimensions(string qualifiedGraphsTable, int dimensions, Guid? excludeGraphGuid)
        {
            string ret = "SELECT COUNT(*) AS graph_count FROM " + qualifiedGraphsTable
                + " WHERE vectordimensionality = " + dimensions.ToString(CultureInfo.InvariantCulture)
                + " AND vectorindextype IS NOT NULL AND vectorindextype <> 'None'";
            if (excludeGraphGuid != null) ret += " AND guid <> '" + excludeGraphGuid.Value + "'";
            return ret + ";";
        }

        internal static string CountGraphNodeVectors(string qualifiedVectorsTable, Guid tenantGuid, Guid graphGuid, int? dimensions)
        {
            string ret = "SELECT COUNT(*) AS vector_count FROM " + qualifiedVectorsTable
                + " WHERE tenantguid = '" + tenantGuid + "' AND graphguid = '" + graphGuid + "' AND nodeguid IS NOT NULL AND embeddings IS NOT NULL";
            if (dimensions != null) ret += " AND vector_dims(embeddings) = " + dimensions.Value.ToString(CultureInfo.InvariantCulture);
            return ret + ";";
        }

        /// <summary>
        /// Build a search that returns one row per matching object (graph, node, or edge) with its best distance,
        /// ordered best first.  Distances are pgvector operator results: cosine distance for cosine searches,
        /// L2 distance for Euclidean searches, and negative inner product for dot product searches.
        /// </summary>
        internal static string Search(
            string scope,
            Guid tenantGuid,
            Guid? graphGuid,
            List<float> query,
            VectorSearchTypeEnum searchType,
            List<string> labels,
            NameValueCollection tags,
            Expr filter,
            int topK,
            bool useIndex,
            int candidateLimit,
            int efSearch)
        {
            int dims = query.Count;
            string literal = Converters.VectorToText(query);
            string d = dims.ToString(CultureInfo.InvariantCulture);

            string distance;
            if (useIndex)
            {
                string type = IndexedType(dims);
                distance = "(vectors.embeddings::" + type + ") <=> '" + literal + "'::" + type;
            }
            else
            {
                distance = "vectors.embeddings " + Operator(searchType) + " '" + literal + "'::vector";
            }

            string objColumn;
            string scopeClause;
            string entityTable;
            string entityJoin;

            switch (scope)
            {
                case "graph":
                    objColumn = "graphguid";
                    scopeClause = "AND vectors.nodeguid IS NULL AND vectors.edgeguid IS NULL ";
                    entityTable = "graphs";
                    entityJoin = "INNER JOIN 'graphs' ON vectors.graphguid = graphs.guid AND vectors.tenantguid = graphs.tenantguid ";
                    break;
                case "node":
                    objColumn = "nodeguid";
                    scopeClause = "AND vectors.graphguid = '" + graphGuid.Value + "' AND vectors.nodeguid IS NOT NULL AND vectors.edgeguid IS NULL ";
                    entityTable = "nodes";
                    entityJoin = "INNER JOIN 'nodes' ON vectors.nodeguid = nodes.guid AND vectors.graphguid = nodes.graphguid AND vectors.tenantguid = nodes.tenantguid ";
                    break;
                case "edge":
                    objColumn = "edgeguid";
                    scopeClause = "AND vectors.graphguid = '" + graphGuid.Value + "' AND vectors.nodeguid IS NULL AND vectors.edgeguid IS NOT NULL ";
                    entityTable = "edges";
                    entityJoin = "INNER JOIN 'edges' ON vectors.edgeguid = edges.guid AND vectors.graphguid = edges.graphguid AND vectors.tenantguid = edges.tenantguid ";
                    break;
                default:
                    throw new ArgumentException("Unknown vector search scope '" + scope + "'.", nameof(scope));
            }

            string filterClause = null;
            if (filter != null)
            {
                filterClause = Converters.ExpressionToWhereClause(entityTable, filter);
                if (String.IsNullOrEmpty(filterClause)) filterClause = null;
            }

            StringBuilder inner = new StringBuilder();
            inner.Append("SELECT vectors." + objColumn + " AS objguid, " + distance + " AS dist FROM 'vectors' ");
            if (filterClause != null) inner.Append(entityJoin);
            inner.Append("WHERE vectors.tenantguid = '" + tenantGuid + "' ");
            inner.Append(scopeClause);
            inner.Append("AND vectors.embeddings IS NOT NULL ");
            inner.Append("AND vector_dims(vectors.embeddings) = " + d + " ");

            if (labels != null)
            {
                int n = 0;
                foreach (string label in labels)
                {
                    if (String.IsNullOrEmpty(label)) continue;
                    n++;
                    string alias = "lbl" + n.ToString(CultureInfo.InvariantCulture);
                    inner.Append("AND EXISTS (SELECT 1 FROM 'labels' " + alias + " WHERE " + alias + ".tenantguid = vectors.tenantguid ");
                    inner.Append(OwnerMatch(alias, scope));
                    inner.Append("AND " + alias + ".label = '" + Sanitizer.Sanitize(label) + "') ");
                }
            }

            if (tags != null)
            {
                int n = 0;
                foreach (string key in tags.AllKeys)
                {
                    if (key == null) continue;
                    n++;
                    string alias = "tg" + n.ToString(CultureInfo.InvariantCulture);
                    string val = tags.Get(key);
                    inner.Append("AND EXISTS (SELECT 1 FROM 'tags' " + alias + " WHERE " + alias + ".tenantguid = vectors.tenantguid ");
                    inner.Append(OwnerMatch(alias, scope));
                    inner.Append("AND " + alias + ".tagkey = '" + Sanitizer.Sanitize(key) + "' ");
                    if (!String.IsNullOrEmpty(val)) inner.Append("AND " + alias + ".tagvalue = '" + Sanitizer.Sanitize(val) + "') ");
                    else inner.Append("AND " + alias + ".tagvalue IS NULL) ");
                }
            }

            if (filterClause != null) inner.Append("AND (" + filterClause + ") ");

            if (useIndex)
            {
                inner.Append("ORDER BY dist ASC LIMIT " + candidateLimit.ToString(CultureInfo.InvariantCulture) + " ");
            }

            StringBuilder sql = new StringBuilder();
            if (useIndex)
            {
                sql.Append("SET LOCAL hnsw.ef_search = " + efSearch.ToString(CultureInfo.InvariantCulture) + "; ");
                sql.Append("SET LOCAL hnsw.iterative_scan = relaxed_order; ");
            }

            sql.Append("SELECT c.objguid AS objguid, MIN(c.dist) AS dist FROM (");
            sql.Append(inner.ToString());
            sql.Append(") c WHERE c.objguid IS NOT NULL GROUP BY c.objguid ORDER BY dist ASC LIMIT " + topK.ToString(CultureInfo.InvariantCulture) + ";");
            return sql.ToString();
        }

        private static string Operator(VectorSearchTypeEnum searchType)
        {
            switch (searchType)
            {
                case VectorSearchTypeEnum.CosineDistance:
                case VectorSearchTypeEnum.CosineSimilarity:
                    return "<=>";
                case VectorSearchTypeEnum.EuclidianDistance:
                case VectorSearchTypeEnum.EuclidianSimilarity:
                    return "<->";
                case VectorSearchTypeEnum.DotProduct:
                    return "<#>";
                default:
                    throw new ArgumentException("Unknown vector search type " + searchType + ".", nameof(searchType));
            }
        }

        private static string OwnerMatch(string alias, string scope)
        {
            switch (scope)
            {
                case "graph":
                    return "AND " + alias + ".graphguid = vectors.graphguid AND " + alias + ".nodeguid IS NULL AND " + alias + ".edgeguid IS NULL ";
                case "node":
                    return "AND " + alias + ".graphguid = vectors.graphguid AND " + alias + ".nodeguid = vectors.nodeguid ";
                default:
                    return "AND " + alias + ".graphguid = vectors.graphguid AND " + alias + ".edgeguid = vectors.edgeguid ";
            }
        }
    }
}
