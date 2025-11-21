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
                        vector = new { type = "object", description = "Vector object with TenantGUID, GraphGUID, and vector data" }
                    },
                    required = new[] { "vector" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("vector", out JsonElement vectorProp))
                        throw new ArgumentException("Vector object is required");

                    VectorMetadata vector = Serializer.DeserializeJson<VectorMetadata>(vectorProp.GetRawText());
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
                "vector/enumerate",
                "Enumerates vectors with pagination and filtering",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        query = new { type = "object", description = "Enumeration query with pagination options" }
                    },
                    required = new[] { "tenantGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                        throw new ArgumentException("Tenant GUID is required");
                    
                    Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                    EnumerationRequest query = new EnumerationRequest { TenantGUID = tenantGuid };
                    
                    if (args.Value.TryGetProperty("query", out JsonElement queryProp))
                    {
                        EnumerationRequest? deserializedQuery = Serializer.DeserializeJson<EnumerationRequest>(queryProp.GetRawText());
                        if (deserializedQuery != null)
                        {
                            query.MaxResults = deserializedQuery.MaxResults;
                            query.Ordering = deserializedQuery.Ordering;
                            query.ContinuationToken = deserializedQuery.ContinuationToken;
                            query.IncludeData = deserializedQuery.IncludeData;
                            query.IncludeSubordinates = deserializedQuery.IncludeSubordinates;
                        }
                    }
                    
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
                        vector = new { type = "object", description = "Vector object with GUID and updated properties" }
                    },
                    required = new[] { "vector" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("vector", out JsonElement vectorProp))
                        throw new ArgumentException("Vector object is required");
                    VectorMetadata vector = Serializer.DeserializeJson<VectorMetadata>(vectorProp.GetRawText());
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
                    return string.Empty;
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
                        vectors = new { type = "array", items = new { type = "object" }, description = "Array of vector objects" }
                    },
                    required = new[] { "tenantGuid", "vectors" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    if (!args.Value.TryGetProperty("vectors", out JsonElement vectorsProp))
                        throw new ArgumentException("Vectors array is required");
                    
                    List<VectorMetadata> vectors = Serializer.DeserializeJson<List<VectorMetadata>>(vectorsProp.GetRawText());
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
                    return string.Empty;
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
                        searchRequest = new { type = "object", description = "Vector search request with embeddings, domain, search type, etc." }
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
                    VectorSearchRequest searchRequest = Serializer.DeserializeJson<VectorSearchRequest>(searchRequestProp.GetRawText());
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
                    throw new ArgumentException("Vector object is required");

                VectorMetadata vector = Serializer.DeserializeJson<VectorMetadata>(vectorProp.GetRawText());
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
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");
                
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                EnumerationRequest query = new EnumerationRequest { TenantGUID = tenantGuid };
                
                if (args.Value.TryGetProperty("query", out JsonElement queryProp))
                {
                    EnumerationRequest? deserializedQuery = Serializer.DeserializeJson<EnumerationRequest>(queryProp.GetRawText());
                    if (deserializedQuery != null)
                    {
                        query.MaxResults = deserializedQuery.MaxResults;
                        query.Ordering = deserializedQuery.Ordering;
                        query.ContinuationToken = deserializedQuery.ContinuationToken;
                        query.IncludeData = deserializedQuery.IncludeData;
                        query.IncludeSubordinates = deserializedQuery.IncludeSubordinates;
                    }
                }
                
                EnumerationResult<VectorMetadata> result = sdk.Vector.Enumerate(query).GetAwaiter().GetResult();
                return Serializer.SerializeJson(result, true);
            });

            server.RegisterMethod("vector/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("vector", out JsonElement vectorProp))
                    throw new ArgumentException("Vector object is required");
                VectorMetadata vector = Serializer.DeserializeJson<VectorMetadata>(vectorProp.GetRawText());
                VectorMetadata updated = sdk.Vector.Update(vector).GetAwaiter().GetResult();
                return Serializer.SerializeJson(updated, true);
            });

            server.RegisterMethod("vector/delete", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid vectorGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "vectorGuid");
                sdk.Vector.DeleteByGuid(tenantGuid, vectorGuid).GetAwaiter().GetResult();
                return "{\"success\": true}";
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
                
                List<VectorMetadata> vectors = Serializer.DeserializeJson<List<VectorMetadata>>(vectorsProp.GetRawText());
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
                return "{\"success\": true}";
            });

            server.RegisterMethod("vector/search", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("searchRequest", out JsonElement searchRequestProp))
                    throw new ArgumentException("Search request is required");

                Guid? graphGuid = LiteGraphMcpServerHelpers.GetGuidOptional(args.Value, "graphGuid");
                VectorSearchRequest searchRequest = Serializer.DeserializeJson<VectorSearchRequest>(searchRequestProp.GetRawText());
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
                    throw new ArgumentException("Vector object is required");

                VectorMetadata vector = Serializer.DeserializeJson<VectorMetadata>(vectorProp.GetRawText());
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
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");
                
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                EnumerationRequest query = new EnumerationRequest { TenantGUID = tenantGuid };
                
                if (args.Value.TryGetProperty("query", out JsonElement queryProp))
                {
                    EnumerationRequest? deserializedQuery = Serializer.DeserializeJson<EnumerationRequest>(queryProp.GetRawText());
                    if (deserializedQuery != null)
                    {
                        query.MaxResults = deserializedQuery.MaxResults;
                        query.Ordering = deserializedQuery.Ordering;
                        query.ContinuationToken = deserializedQuery.ContinuationToken;
                        query.IncludeData = deserializedQuery.IncludeData;
                        query.IncludeSubordinates = deserializedQuery.IncludeSubordinates;
                    }
                }
                
                EnumerationResult<VectorMetadata> result = sdk.Vector.Enumerate(query).GetAwaiter().GetResult();
                return Serializer.SerializeJson(result, true);
            });

            server.RegisterMethod("vector/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("vector", out JsonElement vectorProp))
                    throw new ArgumentException("Vector object is required");
                VectorMetadata vector = Serializer.DeserializeJson<VectorMetadata>(vectorProp.GetRawText());
                VectorMetadata updated = sdk.Vector.Update(vector).GetAwaiter().GetResult();
                return Serializer.SerializeJson(updated, true);
            });

            server.RegisterMethod("vector/delete", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid vectorGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "vectorGuid");
                sdk.Vector.DeleteByGuid(tenantGuid, vectorGuid).GetAwaiter().GetResult();
                return "{\"success\": true}";
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
                
                List<VectorMetadata> vectors = Serializer.DeserializeJson<List<VectorMetadata>>(vectorsProp.GetRawText());
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
                return "{\"success\": true}";
            });

            server.RegisterMethod("vector/search", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("searchRequest", out JsonElement searchRequestProp))
                    throw new ArgumentException("Search request is required");

                Guid? graphGuid = LiteGraphMcpServerHelpers.GetGuidOptional(args.Value, "graphGuid");
                VectorSearchRequest searchRequest = Serializer.DeserializeJson<VectorSearchRequest>(searchRequestProp.GetRawText());
                List<VectorSearchResult> results = sdk.Vector.SearchVectors(tenantGuid, graphGuid, searchRequest).GetAwaiter().GetResult();
                return Serializer.SerializeJson(results, true);
            });
        }

        #endregion
    }
}

