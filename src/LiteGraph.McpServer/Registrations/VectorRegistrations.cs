namespace LiteGraph.McpServer.Registrations
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using LiteGraph.McpServer.Classes;
    using LiteGraph.Sdk;
    using Voltaic;

    /// <summary>
    /// Registration methods for Vector operations.
    /// </summary>
    public static class VectorRegistrations
    {
        #region HTTP-Tools

        /// <summary>
        /// Registers vector tools on HTTP server.
        /// </summary>
        /// <param name="server">HTTP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterHttpTools(McpHttpServer server, LiteGraphSdk sdk)
        {
            server.RegisterTool(
                "vector/create",
                "Creates a new vector in LiteGraph",
                new
                {
                    type = "object",
                    properties = new
                    {
                        vector = new { type = "string", description = "Vector object serialized as JSON string using Serializer" }
                    },
                    required = new[] { "vector" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("vector", out JsonElement vectorProp))
                        throw new ArgumentException("Vector JSON string is required");
                    string vectorJson = vectorProp.GetString() ?? throw new ArgumentException("Vector JSON string cannot be null");
                    VectorMetadata vector = Serializer.DeserializeJson<VectorMetadata>(vectorJson);
                    VectorMetadata created = sdk.Vector.Create(vector).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(created, true);
                });

            server.RegisterTool(
                "vector/get",
                "Reads a vector by GUID",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        vectorGuid = new { type = "string", description = "Vector GUID" }
                    },
                    required = new[] { "tenantGuid", "vectorGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid vectorGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "vectorGuid");

                    VectorMetadata vector = sdk.Vector.ReadByGuid(tenantGuid, vectorGuid).GetAwaiter().GetResult();
                    return vector != null ? Serializer.SerializeJson(vector, true) : "null";
                });

            server.RegisterTool(
                "vector/all",
                "Lists all vectors in a tenant",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        order = new { type = "string", description = "Enumeration order (default: CreatedDescending)" },
                        skip = new { type = "integer", description = "Number of records to skip (default: 0)" }
                    },
                    required = new[] { "tenantGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                        throw new ArgumentException("Tenant GUID is required");

                    Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                    (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                    List<VectorMetadata> vectors = sdk.Vector.ReadMany(tenantGuid, order, skip).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(vectors, true);
                });

            server.RegisterTool(
                "vector/readallintenant",
                "Reads all vectors within a tenant",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" }
                    },
                    required = new[] { "tenantGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    List<VectorMetadata> vectors = sdk.Vector.ReadAllInTenant(tenantGuid).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(vectors, true);
                });

            server.RegisterTool(
                "vector/readallingraph",
                "Reads all vectors within a graph",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" }
                    },
                    required = new[] { "tenantGuid", "graphGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    List<VectorMetadata> vectors = sdk.Vector.ReadAllInGraph(tenantGuid, graphGuid).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(vectors, true);
                });

            server.RegisterTool(
                "vector/readmanygraph",
                "Reads vectors attached to a graph object",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" }
                    },
                    required = new[] { "tenantGuid", "graphGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    List<VectorMetadata> vectors = sdk.Vector.ReadManyGraph(tenantGuid, graphGuid).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(vectors, true);
                });

            server.RegisterTool(
                "vector/readmanynode",
                "Reads vectors attached to a node",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        nodeGuid = new { type = "string", description = "Node GUID" }
                    },
                    required = new[] { "tenantGuid", "graphGuid", "nodeGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                    List<VectorMetadata> vectors = sdk.Vector.ReadManyNode(tenantGuid, graphGuid, nodeGuid).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(vectors, true);
                });

            server.RegisterTool(
                "vector/readmanyedge",
                "Reads vectors attached to an edge",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        edgeGuid = new { type = "string", description = "Edge GUID" }
                    },
                    required = new[] { "tenantGuid", "graphGuid", "edgeGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");
                    List<VectorMetadata> vectors = sdk.Vector.ReadManyEdge(tenantGuid, graphGuid, edgeGuid).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(vectors, true);
                });

            server.RegisterTool(
                "vector/enumerate",
                "Enumerates vectors with pagination and filtering",
                new
                {
                    type = "object",
                    properties = new
                    {
                        query = new { type = "string", description = "Enumeration request serialized as JSON string using Serializer" }
                    },
                    required = new[] { "query" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("query", out JsonElement queryProp))
                        throw new ArgumentException("Enumeration query is required");

                    string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                    EnumerationRequest query = Serializer.DeserializeJson<EnumerationRequest>(queryJson) ?? new EnumerationRequest();
                    if (query.TenantGUID == null)
                        throw new ArgumentException("query.TenantGUID is required.");
                    
                    EnumerationResult<VectorMetadata> result = sdk.Vector.Enumerate(query).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(result, true);
                });

            server.RegisterTool(
                "vector/update",
                "Updates a vector",
                new
                {
                    type = "object",
                    properties = new
                    {
                        vector = new { type = "string", description = "Vector object serialized as JSON string using Serializer" }
                    },
                    required = new[] { "vector" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("vector", out JsonElement vectorProp))
                        throw new ArgumentException("Vector JSON string is required");
                    string vectorJson = vectorProp.GetString() ?? throw new ArgumentException("Vector JSON string cannot be null");
                    VectorMetadata vector = Serializer.DeserializeJson<VectorMetadata>(vectorJson);
                    VectorMetadata updated = sdk.Vector.Update(vector).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(updated, true);
                });

            server.RegisterTool(
                "vector/delete",
                "Deletes a vector by GUID",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        vectorGuid = new { type = "string", description = "Vector GUID" }
                    },
                    required = new[] { "tenantGuid", "vectorGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid vectorGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "vectorGuid");
                    sdk.Vector.DeleteByGuid(tenantGuid, vectorGuid).GetAwaiter().GetResult();
                    return true;
                });

            server.RegisterTool(
                "vector/exists",
                "Checks if a vector exists by GUID",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        vectorGuid = new { type = "string", description = "Vector GUID" }
                    },
                    required = new[] { "tenantGuid", "vectorGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid vectorGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "vectorGuid");
                    bool exists = sdk.Vector.ExistsByGuid(tenantGuid, vectorGuid).GetAwaiter().GetResult();
                    return exists.ToString().ToLower();
                });

            server.RegisterTool(
                "vector/getmany",
                "Reads multiple vectors by their GUIDs",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        vectorGuids = new { type = "array", items = new { type = "string" }, description = "Array of vector GUIDs" }
                    },
                    required = new[] { "tenantGuid", "vectorGuids" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    if (!args.Value.TryGetProperty("vectorGuids", out JsonElement guidsProp))
                        throw new ArgumentException("Vector GUIDs array is required");
                    
                    List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                    List<VectorMetadata> vectors = sdk.Vector.ReadByGuids(tenantGuid, guids).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(vectors, true);
                });

            server.RegisterTool(
                "vector/createmany",
                "Creates multiple vectors",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        vectors = new { type = "string", description = "Array of vector objects serialized as JSON string using Serializer" }
                    },
                    required = new[] { "tenantGuid", "vectors" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    if (!args.Value.TryGetProperty("vectors", out JsonElement vectorsProp))
                        throw new ArgumentException("Vectors array is required");
                    
                    string vectorsJson = vectorsProp.GetString() ?? throw new ArgumentException("Vectors JSON string cannot be null");
                    List<VectorMetadata> vectors = Serializer.DeserializeJson<List<VectorMetadata>>(vectorsJson);
                    List<VectorMetadata> created = sdk.Vector.CreateMany(tenantGuid, vectors).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(created, true);
                });

            server.RegisterTool(
                "vector/deletemany",
                "Deletes multiple vectors by their GUIDs",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        vectorGuids = new { type = "array", items = new { type = "string" }, description = "Array of vector GUIDs to delete" }
                    },
                    required = new[] { "tenantGuid", "vectorGuids" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    if (!args.Value.TryGetProperty("vectorGuids", out JsonElement guidsProp))
                        throw new ArgumentException("Vector GUIDs array is required");
                    
                    List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                    sdk.Vector.DeleteMany(tenantGuid, guids).GetAwaiter().GetResult();
                    return true;
                });

            server.RegisterTool(
                "vector/deleteallintenant",
                "Deletes all vectors within a tenant",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" }
                    },
                    required = new[] { "tenantGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    sdk.Vector.DeleteAllInTenant(tenantGuid).GetAwaiter().GetResult();
                    return true;
                });

            server.RegisterTool(
                "vector/deleteallingraph",
                "Deletes all vectors within a graph",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" }
                    },
                    required = new[] { "tenantGuid", "graphGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    sdk.Vector.DeleteAllInGraph(tenantGuid, graphGuid).GetAwaiter().GetResult();
                    return true;
                });

            server.RegisterTool(
                "vector/deletegraphvectors",
                "Deletes vectors associated with the graph object itself",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" }
                    },
                    required = new[] { "tenantGuid", "graphGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    sdk.Vector.DeleteGraphVectors(tenantGuid, graphGuid).GetAwaiter().GetResult();
                    return true;
                });

            server.RegisterTool(
                "vector/deletenodevectors",
                "Deletes vectors attached to a node",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        nodeGuid = new { type = "string", description = "Node GUID" }
                    },
                    required = new[] { "tenantGuid", "graphGuid", "nodeGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                    sdk.Vector.DeleteNodeVectors(tenantGuid, graphGuid, nodeGuid).GetAwaiter().GetResult();
                    return true;
                });

            server.RegisterTool(
                "vector/deleteedgevectors",
                "Deletes vectors attached to an edge",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        edgeGuid = new { type = "string", description = "Edge GUID" }
                    },
                    required = new[] { "tenantGuid", "graphGuid", "edgeGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");
                    sdk.Vector.DeleteEdgeVectors(tenantGuid, graphGuid, edgeGuid).GetAwaiter().GetResult();
                    return true;
                });

            server.RegisterTool(
                "vector/search",
                "Searches vectors using vector similarity",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID (optional)" },
                        searchRequest = new { type = "string", description = "Vector search request object serialized as JSON string using Serializer" }
                    },
                    required = new[] { "tenantGuid", "searchRequest" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    if (!args.Value.TryGetProperty("searchRequest", out JsonElement searchRequestProp))
                        throw new ArgumentException("Search request is required");

                    Guid? graphGuid = LiteGraphMcpServerHelpers.GetGuidOptional(args.Value, "graphGuid");
                    string searchRequestJson = searchRequestProp.GetString() ?? throw new ArgumentException("VectorSearchRequest JSON string cannot be null");
                    VectorSearchRequest searchRequest = Serializer.DeserializeJson<VectorSearchRequest>(searchRequestJson);
                    List<VectorSearchResult> results = sdk.Vector.SearchVectors(tenantGuid, graphGuid, searchRequest).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(results, true);
                });
        }

        #endregion

        #region TCP-Methods

        /// <summary>
        /// Registers vector methods on TCP server.
        /// </summary>
        /// <param name="server">TCP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterTcpMethods(McpTcpServer server, LiteGraphSdk sdk)
        {
            server.RegisterMethod("vector/create", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("vector", out JsonElement vectorProp))
                    throw new ArgumentException("Vector JSON string is required");
                string vectorJson = vectorProp.GetString() ?? throw new ArgumentException("Vector JSON string cannot be null");
                VectorMetadata vector = Serializer.DeserializeJson<VectorMetadata>(vectorJson);
                VectorMetadata created = sdk.Vector.Create(vector).GetAwaiter().GetResult();
                return Serializer.SerializeJson(created, true);
            });

            server.RegisterMethod("vector/get", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid vectorGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "vectorGuid");

                VectorMetadata vector = sdk.Vector.ReadByGuid(tenantGuid, vectorGuid).GetAwaiter().GetResult();
                return vector != null ? Serializer.SerializeJson(vector, true) : "null";
            });

            server.RegisterMethod("vector/all", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<VectorMetadata> vectors = sdk.Vector.ReadMany(tenantGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(vectors, true);
            });

            server.RegisterMethod("vector/readallintenant", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                List<VectorMetadata> vectors = sdk.Vector.ReadAllInTenant(tenantGuid).GetAwaiter().GetResult();
                return Serializer.SerializeJson(vectors, true);
            });

            server.RegisterMethod("vector/readallingraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                List<VectorMetadata> vectors = sdk.Vector.ReadAllInGraph(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return Serializer.SerializeJson(vectors, true);
            });

            server.RegisterMethod("vector/readmanygraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                List<VectorMetadata> vectors = sdk.Vector.ReadManyGraph(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return Serializer.SerializeJson(vectors, true);
            });

            server.RegisterMethod("vector/readmanynode", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                List<VectorMetadata> vectors = sdk.Vector.ReadManyNode(tenantGuid, graphGuid, nodeGuid).GetAwaiter().GetResult();
                return Serializer.SerializeJson(vectors, true);
            });

            server.RegisterMethod("vector/readmanyedge", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");
                List<VectorMetadata> vectors = sdk.Vector.ReadManyEdge(tenantGuid, graphGuid, edgeGuid).GetAwaiter().GetResult();
                return Serializer.SerializeJson(vectors, true);
            });

            server.RegisterMethod("vector/enumerate", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("query", out JsonElement queryProp))
                    throw new ArgumentException("Enumeration query is required");

                string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                EnumerationRequest query = Serializer.DeserializeJson<EnumerationRequest>(queryJson) ?? new EnumerationRequest();
                if (query.TenantGUID == null)
                    throw new ArgumentException("query.TenantGUID is required.");
                
                EnumerationResult<VectorMetadata> result = sdk.Vector.Enumerate(query).GetAwaiter().GetResult();
                return Serializer.SerializeJson(result, true);
            });

            server.RegisterMethod("vector/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("vector", out JsonElement vectorProp))
                    throw new ArgumentException("Vector JSON string is required");
                string vectorJson = vectorProp.GetString() ?? throw new ArgumentException("Vector JSON string cannot be null");
                VectorMetadata vector = Serializer.DeserializeJson<VectorMetadata>(vectorJson);
                VectorMetadata updated = sdk.Vector.Update(vector).GetAwaiter().GetResult();
                return Serializer.SerializeJson(updated, true);
            });

            server.RegisterMethod("vector/delete", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid vectorGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "vectorGuid");
                sdk.Vector.DeleteByGuid(tenantGuid, vectorGuid).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterMethod("vector/exists", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid vectorGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "vectorGuid");
                bool exists = sdk.Vector.ExistsByGuid(tenantGuid, vectorGuid).GetAwaiter().GetResult();
                return exists.ToString().ToLower();
            });

            server.RegisterMethod("vector/getmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("vectorGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Vector GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                List<VectorMetadata> vectors = sdk.Vector.ReadByGuids(tenantGuid, guids).GetAwaiter().GetResult();
                return Serializer.SerializeJson(vectors, true);
            });

            server.RegisterMethod("vector/createmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("vectors", out JsonElement vectorsProp))
                    throw new ArgumentException("Vectors array is required");
                
                string vectorsJson = vectorsProp.GetString() ?? throw new ArgumentException("Vectors JSON string cannot be null");
                List<VectorMetadata> vectors = Serializer.DeserializeJson<List<VectorMetadata>>(vectorsJson);
                List<VectorMetadata> created = sdk.Vector.CreateMany(tenantGuid, vectors).GetAwaiter().GetResult();
                return Serializer.SerializeJson(created, true);
            });

            server.RegisterMethod("vector/deletemany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("vectorGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Vector GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                sdk.Vector.DeleteMany(tenantGuid, guids).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterMethod("vector/deleteallintenant", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                sdk.Vector.DeleteAllInTenant(tenantGuid).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterMethod("vector/deleteallingraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                sdk.Vector.DeleteAllInGraph(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterMethod("vector/deletegraphvectors", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                sdk.Vector.DeleteGraphVectors(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterMethod("vector/deletenodevectors", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                sdk.Vector.DeleteNodeVectors(tenantGuid, graphGuid, nodeGuid).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterMethod("vector/deleteedgevectors", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");
                sdk.Vector.DeleteEdgeVectors(tenantGuid, graphGuid, edgeGuid).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterMethod("vector/search", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("searchRequest", out JsonElement searchRequestProp))
                    throw new ArgumentException("Search request is required");

                Guid? graphGuid = LiteGraphMcpServerHelpers.GetGuidOptional(args.Value, "graphGuid");
                string searchRequestJson = searchRequestProp.GetString() ?? throw new ArgumentException("VectorSearchRequest JSON string cannot be null");
                VectorSearchRequest searchRequest = Serializer.DeserializeJson<VectorSearchRequest>(searchRequestJson);
                List<VectorSearchResult> results = sdk.Vector.SearchVectors(tenantGuid, graphGuid, searchRequest).GetAwaiter().GetResult();
                return Serializer.SerializeJson(results, true);
            });
        }

        #endregion

        #region WebSocket-Methods

        /// <summary>
        /// Registers vector methods on WebSocket server.
        /// </summary>
        /// <param name="server">WebSocket server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterWebSocketMethods(McpWebsocketsServer server, LiteGraphSdk sdk)
        {
            server.RegisterMethod("vector/create", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("vector", out JsonElement vectorProp))
                    throw new ArgumentException("Vector JSON string is required");
                string vectorJson = vectorProp.GetString() ?? throw new ArgumentException("Vector JSON string cannot be null");
                VectorMetadata vector = Serializer.DeserializeJson<VectorMetadata>(vectorJson);
                VectorMetadata created = sdk.Vector.Create(vector).GetAwaiter().GetResult();
                return Serializer.SerializeJson(created, true);
            });

            server.RegisterMethod("vector/get", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid vectorGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "vectorGuid");

                VectorMetadata vector = sdk.Vector.ReadByGuid(tenantGuid, vectorGuid).GetAwaiter().GetResult();
                return vector != null ? Serializer.SerializeJson(vector, true) : "null";
            });

            server.RegisterMethod("vector/all", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<VectorMetadata> vectors = sdk.Vector.ReadMany(tenantGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(vectors, true);
            });

            server.RegisterMethod("vector/enumerate", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("query", out JsonElement queryProp))
                    throw new ArgumentException("Enumeration query is required");

                string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                EnumerationRequest query = Serializer.DeserializeJson<EnumerationRequest>(queryJson) ?? new EnumerationRequest();
                if (query.TenantGUID == null)
                    throw new ArgumentException("query.TenantGUID is required.");
                
                EnumerationResult<VectorMetadata> result = sdk.Vector.Enumerate(query).GetAwaiter().GetResult();
                return Serializer.SerializeJson(result, true);
            });

            server.RegisterMethod("vector/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("vector", out JsonElement vectorProp))
                    throw new ArgumentException("Vector JSON string is required");
                string vectorJson = vectorProp.GetString() ?? throw new ArgumentException("Vector JSON string cannot be null");
                VectorMetadata vector = Serializer.DeserializeJson<VectorMetadata>(vectorJson);
                VectorMetadata updated = sdk.Vector.Update(vector).GetAwaiter().GetResult();
                return Serializer.SerializeJson(updated, true);
            });

            server.RegisterMethod("vector/delete", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid vectorGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "vectorGuid");
                sdk.Vector.DeleteByGuid(tenantGuid, vectorGuid).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterMethod("vector/exists", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid vectorGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "vectorGuid");
                bool exists = sdk.Vector.ExistsByGuid(tenantGuid, vectorGuid).GetAwaiter().GetResult();
                return exists.ToString().ToLower();
            });

            server.RegisterMethod("vector/getmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("vectorGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Vector GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                List<VectorMetadata> vectors = sdk.Vector.ReadByGuids(tenantGuid, guids).GetAwaiter().GetResult();
                return Serializer.SerializeJson(vectors, true);
            });

            server.RegisterMethod("vector/createmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("vectors", out JsonElement vectorsProp))
                    throw new ArgumentException("Vectors array is required");
                
                string vectorsJson = vectorsProp.GetString() ?? throw new ArgumentException("Vectors JSON string cannot be null");
                List<VectorMetadata> vectors = Serializer.DeserializeJson<List<VectorMetadata>>(vectorsJson);
                List<VectorMetadata> created = sdk.Vector.CreateMany(tenantGuid, vectors).GetAwaiter().GetResult();
                return Serializer.SerializeJson(created, true);
            });

            server.RegisterMethod("vector/deletemany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("vectorGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Vector GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                sdk.Vector.DeleteMany(tenantGuid, guids).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterMethod("vector/search", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("searchRequest", out JsonElement searchRequestProp))
                    throw new ArgumentException("Search request is required");

                Guid? graphGuid = LiteGraphMcpServerHelpers.GetGuidOptional(args.Value, "graphGuid");
                string searchRequestJson = searchRequestProp.GetString() ?? throw new ArgumentException("VectorSearchRequest JSON string cannot be null");
                VectorSearchRequest searchRequest = Serializer.DeserializeJson<VectorSearchRequest>(searchRequestJson);
                List<VectorSearchResult> results = sdk.Vector.SearchVectors(tenantGuid, graphGuid, searchRequest).GetAwaiter().GetResult();
                return Serializer.SerializeJson(results, true);
            });

            server.RegisterMethod("vector/readallintenant", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                List<VectorMetadata> vectors = sdk.Vector.ReadAllInTenant(tenantGuid).GetAwaiter().GetResult();
                return Serializer.SerializeJson(vectors, true);
            });

            server.RegisterMethod("vector/readallingraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                List<VectorMetadata> vectors = sdk.Vector.ReadAllInGraph(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return Serializer.SerializeJson(vectors, true);
            });

            server.RegisterMethod("vector/readmanygraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                List<VectorMetadata> vectors = sdk.Vector.ReadManyGraph(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return Serializer.SerializeJson(vectors, true);
            });

            server.RegisterMethod("vector/readmanynode", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                List<VectorMetadata> vectors = sdk.Vector.ReadManyNode(tenantGuid, graphGuid, nodeGuid).GetAwaiter().GetResult();
                return Serializer.SerializeJson(vectors, true);
            });

            server.RegisterMethod("vector/readmanyedge", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");
                List<VectorMetadata> vectors = sdk.Vector.ReadManyEdge(tenantGuid, graphGuid, edgeGuid).GetAwaiter().GetResult();
                return Serializer.SerializeJson(vectors, true);
            });

            server.RegisterMethod("vector/deleteallintenant", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                sdk.Vector.DeleteAllInTenant(tenantGuid).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterMethod("vector/deleteallingraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                sdk.Vector.DeleteAllInGraph(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterMethod("vector/deletegraphvectors", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                sdk.Vector.DeleteGraphVectors(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterMethod("vector/deletenodevectors", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                sdk.Vector.DeleteNodeVectors(tenantGuid, graphGuid, nodeGuid).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterMethod("vector/deleteedgevectors", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");
                sdk.Vector.DeleteEdgeVectors(tenantGuid, graphGuid, edgeGuid).GetAwaiter().GetResult();
                return true;
            });

        }

        #endregion
    }
}

