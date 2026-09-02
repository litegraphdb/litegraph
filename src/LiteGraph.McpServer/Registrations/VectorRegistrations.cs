namespace LiteGraph.McpServer.Registrations
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
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
                    return CreateVector(sdk, vector);
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

                    return ReadVector(sdk, tenantGuid, vectorGuid);
                });

            server.RegisterTool(
                "vector/all",
                "Lists all vectors in a tenant. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        order = new { type = "string", description = "Enumeration order (default: CreatedDescending)" },
                        skip = new { type = "integer", description = "Number of records to skip (default: 0)" },
                        maxResults = new { type = "integer", description = "Maximum results to return, 1-1000, default 1000" },
                        continuationToken = new { type = "string", description = "Continuation token (GUID) from a previous response for marker-based pagination" }
                    },
                    required = new[] { "tenantGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                        throw new ArgumentException("Tenant GUID is required");

                    Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                    (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                    return ReadVectors(sdk, tenantGuid, null, null, null, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
                });

            server.RegisterTool(
                "vector/readallintenant",
                "Reads all vectors within a tenant. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        order = new { type = "string", description = "Enumeration order (default: CreatedDescending)" },
                        skip = new { type = "integer", description = "Number of records to skip (default: 0)" },
                        maxResults = new { type = "integer", description = "Maximum results to return, 1-1000, default 1000" },
                        continuationToken = new { type = "string", description = "Continuation token (GUID) from a previous response for marker-based pagination" }
                    },
                    required = new[] { "tenantGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                    return ReadAllVectorsInTenant(sdk, tenantGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
                });

            server.RegisterTool(
                "vector/readallingraph",
                "Reads all vectors within a graph. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        order = new { type = "string", description = "Enumeration order (default: CreatedDescending)" },
                        skip = new { type = "integer", description = "Number of records to skip (default: 0)" },
                        maxResults = new { type = "integer", description = "Maximum results to return, 1-1000, default 1000" }
                    },
                    required = new[] { "tenantGuid", "graphGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                    return ReadAllVectorsInGraph(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
                });

            server.RegisterTool(
                "vector/readmanygraph",
                "Reads vectors attached to a graph object. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        order = new { type = "string", description = "Enumeration order (default: CreatedDescending)" },
                        skip = new { type = "integer", description = "Number of records to skip (default: 0)" },
                        maxResults = new { type = "integer", description = "Maximum results to return, 1-1000, default 1000" }
                    },
                    required = new[] { "tenantGuid", "graphGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                    return ReadGraphVectors(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
                });

            server.RegisterTool(
                "vector/readmanynode",
                "Reads vectors attached to a node. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        nodeGuid = new { type = "string", description = "Node GUID" },
                        order = new { type = "string", description = "Enumeration order (default: CreatedDescending)" },
                        skip = new { type = "integer", description = "Number of records to skip (default: 0)" },
                        maxResults = new { type = "integer", description = "Maximum results to return, 1-1000, default 1000" }
                    },
                    required = new[] { "tenantGuid", "graphGuid", "nodeGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                    (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                    return ReadNodeVectors(sdk, tenantGuid, graphGuid, nodeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
                });

            server.RegisterTool(
                "vector/readmanyedge",
                "Reads vectors attached to an edge. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        edgeGuid = new { type = "string", description = "Edge GUID" },
                        order = new { type = "string", description = "Enumeration order (default: CreatedDescending)" },
                        skip = new { type = "integer", description = "Number of records to skip (default: 0)" },
                        maxResults = new { type = "integer", description = "Maximum results to return, 1-1000, default 1000" }
                    },
                    required = new[] { "tenantGuid", "graphGuid", "edgeGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");
                    (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                    return ReadEdgeVectors(sdk, tenantGuid, graphGuid, edgeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
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
                    
                    return EnumerateVectors(sdk, query);
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
                    return UpdateVector(sdk, vector);
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
                    DeleteVector(sdk, tenantGuid, vectorGuid);
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
                    return VectorExists(sdk, tenantGuid, vectorGuid).ToString().ToLowerInvariant();
                });

            server.RegisterTool(
                "vector/getmany",
                "Reads multiple vectors by their GUIDs. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        vectorGuids = new { type = "array", items = new { type = "string" }, description = "Array of vector GUIDs" },
                        maxResults = new { type = "integer", description = "Maximum results to return, 1-1000, default 1000" }
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
                    return ReadVectorsByGuids(sdk, tenantGuid, guids, LiteGraphMcpServerHelpers.GetMaxResults(args));
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
                    return CreateVectors(sdk, tenantGuid, vectors);
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
                    DeleteVectors(sdk, tenantGuid, guids);
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
                    DeleteAllVectorsInTenant(sdk, tenantGuid);
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
                    DeleteAllVectorsInGraph(sdk, tenantGuid, graphGuid);
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
                    DeleteGraphVectors(sdk, tenantGuid, graphGuid);
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
                    DeleteNodeVectors(sdk, tenantGuid, graphGuid, nodeGuid);
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
                    DeleteEdgeVectors(sdk, tenantGuid, graphGuid, edgeGuid);
                    return true;
                });

            server.RegisterTool(
                "vector/search",
                "Searches vectors using vector similarity. Returns a paginated EnumerationResult envelope of VectorSearchResult objects (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID (optional)" },
                        searchRequest = new { type = "string", description = "Vector search request object serialized as JSON string using Serializer" },
                        skip = new { type = "integer", description = "Number of records to skip (default: 0)" },
                        maxResults = new { type = "integer", description = "Maximum results to return, 1-1000, default 1000" }
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
                    return SearchVectors(sdk, tenantGuid, graphGuid, searchRequest, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetIntOrDefault(args.Value, "skip", 0));
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
                return CreateVector(sdk, vector);
            });

            server.RegisterMethod("vector/get", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid vectorGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "vectorGuid");

                return ReadVector(sdk, tenantGuid, vectorGuid);
            });

            server.RegisterMethod("vector/all", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadVectors(sdk, tenantGuid, null, null, null, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
            });

            server.RegisterMethod("vector/readallintenant", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadAllVectorsInTenant(sdk, tenantGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
            });

            server.RegisterMethod("vector/readallingraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadAllVectorsInGraph(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterMethod("vector/readmanygraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadGraphVectors(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterMethod("vector/readmanynode", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadNodeVectors(sdk, tenantGuid, graphGuid, nodeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterMethod("vector/readmanyedge", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadEdgeVectors(sdk, tenantGuid, graphGuid, edgeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterMethod("vector/enumerate", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("query", out JsonElement queryProp))
                    throw new ArgumentException("Enumeration query is required");

                string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                EnumerationRequest query = Serializer.DeserializeJson<EnumerationRequest>(queryJson) ?? new EnumerationRequest();
                if (query.TenantGUID == null)
                    throw new ArgumentException("query.TenantGUID is required.");
                
                return EnumerateVectors(sdk, query);
            });

            server.RegisterMethod("vector/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("vector", out JsonElement vectorProp))
                    throw new ArgumentException("Vector JSON string is required");
                string vectorJson = vectorProp.GetString() ?? throw new ArgumentException("Vector JSON string cannot be null");
                VectorMetadata vector = Serializer.DeserializeJson<VectorMetadata>(vectorJson);
                return UpdateVector(sdk, vector);
            });

            server.RegisterMethod("vector/delete", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid vectorGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "vectorGuid");
                DeleteVector(sdk, tenantGuid, vectorGuid);
                return true;
            });

            server.RegisterMethod("vector/exists", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid vectorGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "vectorGuid");
                return VectorExists(sdk, tenantGuid, vectorGuid).ToString().ToLowerInvariant();
            });

            server.RegisterMethod("vector/getmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("vectorGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Vector GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                return ReadVectorsByGuids(sdk, tenantGuid, guids, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterMethod("vector/createmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("vectors", out JsonElement vectorsProp))
                    throw new ArgumentException("Vectors array is required");
                
                string vectorsJson = vectorsProp.GetString() ?? throw new ArgumentException("Vectors JSON string cannot be null");
                List<VectorMetadata> vectors = Serializer.DeserializeJson<List<VectorMetadata>>(vectorsJson);
                return CreateVectors(sdk, tenantGuid, vectors);
            });

            server.RegisterMethod("vector/deletemany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("vectorGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Vector GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                DeleteVectors(sdk, tenantGuid, guids);
                return true;
            });

            server.RegisterMethod("vector/deleteallintenant", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                DeleteAllVectorsInTenant(sdk, tenantGuid);
                return true;
            });

            server.RegisterMethod("vector/deleteallingraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                DeleteAllVectorsInGraph(sdk, tenantGuid, graphGuid);
                return true;
            });

            server.RegisterMethod("vector/deletegraphvectors", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                DeleteGraphVectors(sdk, tenantGuid, graphGuid);
                return true;
            });

            server.RegisterMethod("vector/deletenodevectors", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                DeleteNodeVectors(sdk, tenantGuid, graphGuid, nodeGuid);
                return true;
            });

            server.RegisterMethod("vector/deleteedgevectors", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");
                DeleteEdgeVectors(sdk, tenantGuid, graphGuid, edgeGuid);
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
                return SearchVectors(sdk, tenantGuid, graphGuid, searchRequest, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetIntOrDefault(args.Value, "skip", 0));
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
                return CreateVector(sdk, vector);
            });

            server.RegisterMethod("vector/get", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid vectorGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "vectorGuid");

                return ReadVector(sdk, tenantGuid, vectorGuid);
            });

            server.RegisterMethod("vector/all", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadVectors(sdk, tenantGuid, null, null, null, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
            });

            server.RegisterMethod("vector/enumerate", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("query", out JsonElement queryProp))
                    throw new ArgumentException("Enumeration query is required");

                string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                EnumerationRequest query = Serializer.DeserializeJson<EnumerationRequest>(queryJson) ?? new EnumerationRequest();
                if (query.TenantGUID == null)
                    throw new ArgumentException("query.TenantGUID is required.");
                
                return EnumerateVectors(sdk, query);
            });

            server.RegisterMethod("vector/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("vector", out JsonElement vectorProp))
                    throw new ArgumentException("Vector JSON string is required");
                string vectorJson = vectorProp.GetString() ?? throw new ArgumentException("Vector JSON string cannot be null");
                VectorMetadata vector = Serializer.DeserializeJson<VectorMetadata>(vectorJson);
                return UpdateVector(sdk, vector);
            });

            server.RegisterMethod("vector/delete", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid vectorGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "vectorGuid");
                DeleteVector(sdk, tenantGuid, vectorGuid);
                return true;
            });

            server.RegisterMethod("vector/exists", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid vectorGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "vectorGuid");
                return VectorExists(sdk, tenantGuid, vectorGuid).ToString().ToLowerInvariant();
            });

            server.RegisterMethod("vector/getmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("vectorGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Vector GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                return ReadVectorsByGuids(sdk, tenantGuid, guids, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterMethod("vector/createmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("vectors", out JsonElement vectorsProp))
                    throw new ArgumentException("Vectors array is required");
                
                string vectorsJson = vectorsProp.GetString() ?? throw new ArgumentException("Vectors JSON string cannot be null");
                List<VectorMetadata> vectors = Serializer.DeserializeJson<List<VectorMetadata>>(vectorsJson);
                return CreateVectors(sdk, tenantGuid, vectors);
            });

            server.RegisterMethod("vector/deletemany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("vectorGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Vector GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                DeleteVectors(sdk, tenantGuid, guids);
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
                return SearchVectors(sdk, tenantGuid, graphGuid, searchRequest, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetIntOrDefault(args.Value, "skip", 0));
            });

            server.RegisterMethod("vector/readallintenant", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadAllVectorsInTenant(sdk, tenantGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
            });

            server.RegisterMethod("vector/readallingraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadAllVectorsInGraph(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterMethod("vector/readmanygraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadGraphVectors(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterMethod("vector/readmanynode", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadNodeVectors(sdk, tenantGuid, graphGuid, nodeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterMethod("vector/readmanyedge", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadEdgeVectors(sdk, tenantGuid, graphGuid, edgeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterMethod("vector/deleteallintenant", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                DeleteAllVectorsInTenant(sdk, tenantGuid);
                return true;
            });

            server.RegisterMethod("vector/deleteallingraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                DeleteAllVectorsInGraph(sdk, tenantGuid, graphGuid);
                return true;
            });

            server.RegisterMethod("vector/deletegraphvectors", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                DeleteGraphVectors(sdk, tenantGuid, graphGuid);
                return true;
            });

            server.RegisterMethod("vector/deletenodevectors", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                DeleteNodeVectors(sdk, tenantGuid, graphGuid, nodeGuid);
                return true;
            });

            server.RegisterMethod("vector/deleteedgevectors", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");
                DeleteEdgeVectors(sdk, tenantGuid, graphGuid, edgeGuid);
                return true;
            });

        }

        #endregion

        #region Private-Methods

        private static string CreateVector(LiteGraphSdk sdk, VectorMetadata vector)
        {
            if (vector == null) throw new ArgumentNullException(nameof(vector));

            string body = Serializer.SerializeJson(vector, false);
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Put,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(vector.TenantGUID)
                + "/vectors",
                body);
        }

        private static string ReadVector(LiteGraphSdk sdk, Guid tenantGuid, Guid vectorGuid)
        {
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                VectorPath(tenantGuid, vectorGuid));
        }

        private static string ReadVectors(
            LiteGraphSdk sdk,
            Guid tenantGuid,
            Guid? graphGuid,
            Guid? nodeGuid,
            Guid? edgeGuid,
            EnumerationOrderEnum order,
            int skip,
            int maxResults,
            Guid? continuationToken)
        {
            if (nodeGuid.HasValue)
            {
                if (!graphGuid.HasValue) throw new ArgumentException("Graph GUID is required when node GUID is provided.");
                return ReadNodeVectors(sdk, tenantGuid, graphGuid.Value, nodeGuid.Value, order, skip, maxResults);
            }

            if (edgeGuid.HasValue)
            {
                if (!graphGuid.HasValue) throw new ArgumentException("Graph GUID is required when edge GUID is provided.");
                return ReadEdgeVectors(sdk, tenantGuid, graphGuid.Value, edgeGuid.Value, order, skip, maxResults);
            }

            if (graphGuid.HasValue) return ReadGraphVectors(sdk, tenantGuid, graphGuid.Value, order, skip, maxResults);

            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                VectorCollectionPath(tenantGuid) + BuildReadQuery(order, skip, maxResults, continuationToken));
        }

        private static string EnumerateVectors(LiteGraphSdk sdk, EnumerationRequest query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (query.TenantGUID == null) throw new ArgumentException("query.TenantGUID is required.");

            string body = Serializer.SerializeJson(query, false);
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Post,
                query.GraphGUID.HasValue
                    ? "/v2.0/tenants/"
                        + LiteGraphMcpRestProxy.Escape(query.TenantGUID.Value)
                        + "/graphs/"
                        + LiteGraphMcpRestProxy.Escape(query.GraphGUID.Value)
                        + "/vectors"
                    : "/v2.0/tenants/"
                        + LiteGraphMcpRestProxy.Escape(query.TenantGUID.Value)
                        + "/vectors",
                body);
        }

        private static bool VectorExists(LiteGraphSdk sdk, Guid tenantGuid, Guid vectorGuid)
        {
            return LiteGraphMcpRestProxy.HeadExists(sdk, VectorPath(tenantGuid, vectorGuid));
        }

        private static string ReadVectorsByGuids(LiteGraphSdk sdk, Guid tenantGuid, List<Guid> vectorGuids, int maxResults)
        {
            if (vectorGuids == null) throw new ArgumentNullException(nameof(vectorGuids));
            if (vectorGuids.Count == 0) throw new ArgumentException("At least one vector GUID is required.");

            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                VectorCollectionPath(tenantGuid)
                + "?guids="
                + String.Join(",", vectorGuids)
                + "&max-keys="
                + maxResults);
        }

        private static string CreateVectors(LiteGraphSdk sdk, Guid tenantGuid, List<VectorMetadata> vectors)
        {
            string body = Serializer.SerializeJson(vectors ?? new List<VectorMetadata>(), false);
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Put,
                VectorCollectionPath(tenantGuid) + "/bulk",
                body);
        }

        private static void DeleteVectors(LiteGraphSdk sdk, Guid tenantGuid, List<Guid> vectorGuids)
        {
            string body = Serializer.SerializeJson(vectorGuids ?? new List<Guid>(), false);
            LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Delete,
                VectorCollectionPath(tenantGuid) + "/bulk",
                body);
        }

        private static string ReadAllVectorsInTenant(
            LiteGraphSdk sdk,
            Guid tenantGuid,
            EnumerationOrderEnum order,
            int skip,
            int maxResults,
            Guid? continuationToken)
        {
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                VectorCollectionPath(tenantGuid) + "/all" + BuildReadQuery(order, skip, maxResults, continuationToken));
        }

        private static string ReadAllVectorsInGraph(
            LiteGraphSdk sdk,
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order,
            int skip,
            int maxResults)
        {
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                GraphVectorCollectionPath(tenantGuid, graphGuid) + "/all" + BuildReadQuery(order, skip, maxResults, null));
        }

        private static string ReadGraphVectors(
            LiteGraphSdk sdk,
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order,
            int skip,
            int maxResults)
        {
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                GraphVectorCollectionPath(tenantGuid, graphGuid) + BuildReadQuery(order, skip, maxResults, null));
        }

        private static string ReadNodeVectors(
            LiteGraphSdk sdk,
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            EnumerationOrderEnum order,
            int skip,
            int maxResults)
        {
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/graphs/"
                + LiteGraphMcpRestProxy.Escape(graphGuid)
                + "/nodes/"
                + LiteGraphMcpRestProxy.Escape(nodeGuid)
                + "/vectors"
                + BuildReadQuery(order, skip, maxResults, null));
        }

        private static string ReadEdgeVectors(
            LiteGraphSdk sdk,
            Guid tenantGuid,
            Guid graphGuid,
            Guid edgeGuid,
            EnumerationOrderEnum order,
            int skip,
            int maxResults)
        {
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/graphs/"
                + LiteGraphMcpRestProxy.Escape(graphGuid)
                + "/edges/"
                + LiteGraphMcpRestProxy.Escape(edgeGuid)
                + "/vectors"
                + BuildReadQuery(order, skip, maxResults, null));
        }

        private static string SearchVectors(
            LiteGraphSdk sdk,
            Guid tenantGuid,
            Guid? graphGuid,
            VectorSearchRequest searchRequest,
            int maxResults,
            int skip)
        {
            if (searchRequest == null) throw new ArgumentNullException(nameof(searchRequest));
            searchRequest.TenantGUID = tenantGuid;
            if (graphGuid.HasValue) searchRequest.GraphGUID = graphGuid.Value;

            string body = Serializer.SerializeJson(searchRequest, false);
            string path = graphGuid.HasValue
                ? "/v1.0/tenants/"
                    + LiteGraphMcpRestProxy.Escape(tenantGuid)
                    + "/graphs/"
                    + LiteGraphMcpRestProxy.Escape(graphGuid.Value)
                    + "/vectors/search"
                : VectorCollectionPath(tenantGuid);

            path += "?max-keys=" + maxResults + "&skip=" + skip;

            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Post,
                path,
                body);
        }

        private static string UpdateVector(LiteGraphSdk sdk, VectorMetadata vector)
        {
            if (vector == null) throw new ArgumentNullException(nameof(vector));

            string body = Serializer.SerializeJson(vector, false);
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Put,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(vector.TenantGUID)
                + "/vectors/"
                + LiteGraphMcpRestProxy.Escape(vector.GUID),
                body);
        }

        private static void DeleteVector(LiteGraphSdk sdk, Guid tenantGuid, Guid vectorGuid)
        {
            LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Delete,
                VectorPath(tenantGuid, vectorGuid));
        }

        private static void DeleteAllVectorsInTenant(LiteGraphSdk sdk, Guid tenantGuid)
        {
            LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Delete,
                VectorCollectionPath(tenantGuid) + "/all");
        }

        private static void DeleteAllVectorsInGraph(LiteGraphSdk sdk, Guid tenantGuid, Guid graphGuid)
        {
            LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Delete,
                GraphVectorCollectionPath(tenantGuid, graphGuid) + "/all");
        }

        private static void DeleteGraphVectors(LiteGraphSdk sdk, Guid tenantGuid, Guid graphGuid)
        {
            LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Delete,
                GraphVectorCollectionPath(tenantGuid, graphGuid));
        }

        private static void DeleteNodeVectors(LiteGraphSdk sdk, Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
        {
            LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Delete,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/graphs/"
                + LiteGraphMcpRestProxy.Escape(graphGuid)
                + "/nodes/"
                + LiteGraphMcpRestProxy.Escape(nodeGuid)
                + "/vectors");
        }

        private static void DeleteEdgeVectors(LiteGraphSdk sdk, Guid tenantGuid, Guid graphGuid, Guid edgeGuid)
        {
            LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Delete,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/graphs/"
                + LiteGraphMcpRestProxy.Escape(graphGuid)
                + "/edges/"
                + LiteGraphMcpRestProxy.Escape(edgeGuid)
                + "/vectors");
        }

        private static string VectorCollectionPath(Guid tenantGuid)
        {
            return "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/vectors";
        }

        private static string VectorPath(Guid tenantGuid, Guid vectorGuid)
        {
            return VectorCollectionPath(tenantGuid)
                + "/"
                + LiteGraphMcpRestProxy.Escape(vectorGuid);
        }

        private static string GraphVectorCollectionPath(Guid tenantGuid, Guid graphGuid)
        {
            return "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/graphs/"
                + LiteGraphMcpRestProxy.Escape(graphGuid)
                + "/vectors";
        }

        private static string BuildReadQuery(EnumerationOrderEnum order, int skip, int maxResults, Guid? continuationToken)
        {
            List<string> query = new List<string>
            {
                "order=" + LiteGraphMcpRestProxy.Escape(order.ToString()),
                "skip=" + skip,
                "max-keys=" + maxResults
            };

            if (continuationToken != null) query.Add("token=" + LiteGraphMcpRestProxy.Escape(continuationToken.Value));

            return "?" + String.Join("&", query);
        }

        #endregion
    }
}
