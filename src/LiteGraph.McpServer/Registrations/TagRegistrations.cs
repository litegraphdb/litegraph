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
    /// Registration methods for Tag operations.
    /// </summary>
    public static class TagRegistrations
    {
        #region HTTP-Tools

        /// <summary>
        /// Registers tag tools on HTTP server.
        /// </summary>
        /// <param name="server">HTTP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterHttpTools(McpHttpServer server, LiteGraphSdk sdk)
        {
            server.RegisterTool(
                "tag/create",
                "Creates a new tag in LiteGraph",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tag = new { type = "string", description = "Tag object serialized as JSON string using Serializer" }
                    },
                    required = new[] { "tag" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("tag", out JsonElement tagProp))
                        throw new ArgumentException("Tag JSON string is required");
                    string tagJson = tagProp.GetString() ?? throw new ArgumentException("Tag JSON string cannot be null");
                    TagMetadata tag = Serializer.DeserializeJson<TagMetadata>(tagJson);
                    return CreateTag(sdk, tag);
                });

            server.RegisterTool(
                "tag/get",
                "Reads a tag by GUID",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        tagGuid = new { type = "string", description = "Tag GUID" }
                    },
                    required = new[] { "tenantGuid", "tagGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid tagGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tagGuid");

                    return ReadTag(sdk, tenantGuid, tagGuid);
                });

            server.RegisterTool(
                "tag/readmany",
                "Reads tags with optional filters. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID (optional)" },
                        nodeGuid = new { type = "string", description = "Node GUID (optional)" },
                        edgeGuid = new { type = "string", description = "Edge GUID (optional)" },
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
                    Guid? graphGuid = LiteGraphMcpServerHelpers.GetGuidOptional(args.Value, "graphGuid");
                    Guid? nodeGuid = LiteGraphMcpServerHelpers.GetGuidOptional(args.Value, "nodeGuid");
                    Guid? edgeGuid = LiteGraphMcpServerHelpers.GetGuidOptional(args.Value, "edgeGuid");
                    (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                    
                    return ReadTags(sdk, tenantGuid, graphGuid, nodeGuid, edgeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
                });

            server.RegisterTool(
                "tag/enumerate",
                "Enumerates tags with pagination and filtering",
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

                    return EnumerateTags(sdk, query);
                });

            server.RegisterTool(
                "tag/update",
                "Updates a tag",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tag = new { type = "string", description = "Tag object serialized as JSON string using Serializer" }
                    },
                    required = new[] { "tag" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("tag", out JsonElement tagProp))
                        throw new ArgumentException("Tag JSON string is required");
                    string tagJson = tagProp.GetString() ?? throw new ArgumentException("Tag JSON string cannot be null");
                    TagMetadata tag = Serializer.DeserializeJson<TagMetadata>(tagJson);
                    return UpdateTag(sdk, tag);
                });

            server.RegisterTool(
                "tag/delete",
                "Deletes a tag by GUID",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        tagGuid = new { type = "string", description = "Tag GUID" }
                    },
                    required = new[] { "tenantGuid", "tagGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid tagGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tagGuid");
                    DeleteTag(sdk, tenantGuid, tagGuid);
                    return true;
                });

            server.RegisterTool(
                "tag/exists",
                "Checks if a tag exists by GUID",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        tagGuid = new { type = "string", description = "Tag GUID" }
                    },
                    required = new[] { "tenantGuid", "tagGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid tagGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tagGuid");
                    return TagExists(sdk, tenantGuid, tagGuid).ToString().ToLowerInvariant();
                });

            server.RegisterTool(
                "tag/getmany",
                "Reads multiple tags by their GUIDs. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        tagGuids = new { type = "array", items = new { type = "string" }, description = "Array of tag GUIDs" },
                        maxResults = new { type = "integer", description = "Maximum results to return, 1-1000, default 1000" }
                    },
                    required = new[] { "tenantGuid", "tagGuids" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    if (!args.Value.TryGetProperty("tagGuids", out JsonElement guidsProp))
                        throw new ArgumentException("Tag GUIDs array is required");
                    
                    List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                    return ReadTagsByGuids(sdk, tenantGuid, guids, LiteGraphMcpServerHelpers.GetMaxResults(args));
                });

            server.RegisterTool(
                "tag/createmany",
                "Creates multiple tags",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        tags = new { type = "string", description = "Array of tag objects serialized as JSON string using Serializer" }
                    },
                    required = new[] { "tenantGuid", "tags" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    if (!args.Value.TryGetProperty("tags", out JsonElement tagsProp))
                        throw new ArgumentException("Tags array is required");
                    
                    string tagsJson = tagsProp.GetString() ?? throw new ArgumentException("Tags JSON string cannot be null");
                    List<TagMetadata> tags = Serializer.DeserializeJson<List<TagMetadata>>(tagsJson);
                    return CreateTags(sdk, tenantGuid, tags);
                });

            server.RegisterTool(
                "tag/deletemany",
                "Deletes multiple tags by their GUIDs",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        tagGuids = new { type = "array", items = new { type = "string" }, description = "Array of tag GUIDs to delete" }
                    },
                    required = new[] { "tenantGuid", "tagGuids" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    if (!args.Value.TryGetProperty("tagGuids", out JsonElement guidsProp))
                        throw new ArgumentException("Tag GUIDs array is required");
                    
                    List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                    DeleteTags(sdk, tenantGuid, guids);
                    return true;
                });

            server.RegisterTool(
                "tag/readallintenant",
                "Reads all tags in a tenant. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
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
                    return ReadAllTagsInTenant(sdk, tenantGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
                });

            server.RegisterTool(
                "tag/readallingraph",
                "Reads all tags in a graph. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
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
                    return ReadAllTagsInGraph(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
                });

            server.RegisterTool(
                "tag/readmanygraph",
                "Reads tags attached to a graph. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
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
                    return ReadGraphTags(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
                });

            server.RegisterTool(
                "tag/readmanynode",
                "Reads tags attached to a node. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
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
                    return ReadNodeTags(sdk, tenantGuid, graphGuid, nodeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
                });

            server.RegisterTool(
                "tag/readmanyedge",
                "Reads tags attached to an edge. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
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
                    return ReadEdgeTags(sdk, tenantGuid, graphGuid, edgeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
                });

            server.RegisterTool(
                "tag/deleteallintenant",
                "Deletes all tags in a tenant",
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
                    DeleteAllTagsInTenant(sdk, tenantGuid);
                    return true;
                });

            server.RegisterTool(
                "tag/deleteallingraph",
                "Deletes all tags in a graph",
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
                    DeleteAllTagsInGraph(sdk, tenantGuid, graphGuid);
                    return true;
                });

            server.RegisterTool(
                "tag/deletegraphlabels",
                "Deletes graph-level tags",
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
                    DeleteGraphTags(sdk, tenantGuid, graphGuid);
                    return true;
                });

            server.RegisterTool(
                "tag/deletenodelabels",
                "Deletes tags attached to a node",
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
                    DeleteNodeTags(sdk, tenantGuid, graphGuid, nodeGuid);
                    return true;
                });

            server.RegisterTool(
                "tag/deleteedgetags",
                "Deletes tags attached to an edge",
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
                    DeleteEdgeTags(sdk, tenantGuid, graphGuid, edgeGuid);
                    return true;
                });
        }

        #endregion

        #region TCP-Methods

        /// <summary>
        /// Registers tag methods on TCP server.
        /// </summary>
        /// <param name="server">TCP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterTcpMethods(McpTcpServer server, LiteGraphSdk sdk)
        {
            server.RegisterMethod("tag/create", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tag", out JsonElement tagProp))
                    throw new ArgumentException("Tag JSON string is required");
                string tagJson = tagProp.GetString() ?? throw new ArgumentException("Tag JSON string cannot be null");
                TagMetadata tag = Serializer.DeserializeJson<TagMetadata>(tagJson);
                return CreateTag(sdk, tag);
            });

            server.RegisterMethod("tag/get", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid tagGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tagGuid");

                return ReadTag(sdk, tenantGuid, tagGuid);
            });

            server.RegisterMethod("tag/readmany", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                Guid? graphGuid = LiteGraphMcpServerHelpers.GetGuidOptional(args.Value, "graphGuid");
                Guid? nodeGuid = LiteGraphMcpServerHelpers.GetGuidOptional(args.Value, "nodeGuid");
                Guid? edgeGuid = LiteGraphMcpServerHelpers.GetGuidOptional(args.Value, "edgeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadTags(sdk, tenantGuid, graphGuid, nodeGuid, edgeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
            });

            server.RegisterMethod("tag/enumerate", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("query", out JsonElement queryProp))
                    throw new ArgumentException("Enumeration query is required");

                string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                EnumerationRequest query = Serializer.DeserializeJson<EnumerationRequest>(queryJson) ?? new EnumerationRequest();
                if (query.TenantGUID == null)
                    throw new ArgumentException("query.TenantGUID is required.");
                
                return EnumerateTags(sdk, query);
            });

            server.RegisterMethod("tag/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tag", out JsonElement tagProp))
                    throw new ArgumentException("Tag JSON string is required");
                string tagJson = tagProp.GetString() ?? throw new ArgumentException("Tag JSON string cannot be null");
                TagMetadata tag = Serializer.DeserializeJson<TagMetadata>(tagJson);
                return UpdateTag(sdk, tag);
            });

            server.RegisterMethod("tag/delete", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid tagGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tagGuid");
                DeleteTag(sdk, tenantGuid, tagGuid);
                return true;
            });

            server.RegisterMethod("tag/exists", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid tagGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tagGuid");
                return TagExists(sdk, tenantGuid, tagGuid).ToString().ToLowerInvariant();
            });

            server.RegisterMethod("tag/getmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("tagGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Tag GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                return ReadTagsByGuids(sdk, tenantGuid, guids, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterMethod("tag/createmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("tags", out JsonElement tagsProp))
                    throw new ArgumentException("Tags array is required");
                
                string tagsJson = tagsProp.GetString() ?? throw new ArgumentException("Tags JSON string cannot be null");
                List<TagMetadata> tags = Serializer.DeserializeJson<List<TagMetadata>>(tagsJson);
                return CreateTags(sdk, tenantGuid, tags);
            });

            server.RegisterMethod("tag/deletemany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("tagGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Tag GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                DeleteTags(sdk, tenantGuid, guids);
                return true;
            });

            server.RegisterMethod("tag/readallintenant", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadAllTagsInTenant(sdk, tenantGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
            });

            server.RegisterMethod("tag/readallingraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadAllTagsInGraph(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterMethod("tag/readmanygraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadGraphTags(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterMethod("tag/readmanynode", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadNodeTags(sdk, tenantGuid, graphGuid, nodeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterMethod("tag/readmanyedge", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadEdgeTags(sdk, tenantGuid, graphGuid, edgeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterMethod("tag/deleteallintenant", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                DeleteAllTagsInTenant(sdk, tenantGuid);
                return true;
            });

            server.RegisterMethod("tag/deleteallingraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                DeleteAllTagsInGraph(sdk, tenantGuid, graphGuid);
                return true;
            });

            server.RegisterMethod("tag/deletegraphlabels", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                DeleteGraphTags(sdk, tenantGuid, graphGuid);
                return true;
            });

            server.RegisterMethod("tag/deletenodelabels", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                DeleteNodeTags(sdk, tenantGuid, graphGuid, nodeGuid);
                return true;
            });

            server.RegisterMethod("tag/deleteedgetags", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");
                DeleteEdgeTags(sdk, tenantGuid, graphGuid, edgeGuid);
                return true;
            });
        }

        #endregion

        #region WebSocket-Methods

        /// <summary>
        /// Registers tag methods on WebSocket server.
        /// </summary>
        /// <param name="server">WebSocket server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterWebSocketMethods(McpWebsocketsServer server, LiteGraphSdk sdk)
        {
            server.RegisterMethod("tag/create", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tag", out JsonElement tagProp))
                    throw new ArgumentException("Tag JSON string is required");
                string tagJson = tagProp.GetString() ?? throw new ArgumentException("Tag JSON string cannot be null");
                TagMetadata tag = Serializer.DeserializeJson<TagMetadata>(tagJson);
                return CreateTag(sdk, tag);
            });

            server.RegisterMethod("tag/get", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid tagGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tagGuid");

                return ReadTag(sdk, tenantGuid, tagGuid);
            });

            server.RegisterMethod("tag/readmany", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                Guid? graphGuid = LiteGraphMcpServerHelpers.GetGuidOptional(args.Value, "graphGuid");
                Guid? nodeGuid = LiteGraphMcpServerHelpers.GetGuidOptional(args.Value, "nodeGuid");
                Guid? edgeGuid = LiteGraphMcpServerHelpers.GetGuidOptional(args.Value, "edgeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadTags(sdk, tenantGuid, graphGuid, nodeGuid, edgeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
            });

            server.RegisterMethod("tag/enumerate", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("query", out JsonElement queryProp))
                    throw new ArgumentException("Enumeration query is required");

                string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                EnumerationRequest query = Serializer.DeserializeJson<EnumerationRequest>(queryJson) ?? new EnumerationRequest();
                if (query.TenantGUID == null)
                    throw new ArgumentException("query.TenantGUID is required.");
                
                return EnumerateTags(sdk, query);
            });

            server.RegisterMethod("tag/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tag", out JsonElement tagProp))
                    throw new ArgumentException("Tag JSON string is required");
                string tagJson = tagProp.GetString() ?? throw new ArgumentException("Tag JSON string cannot be null");
                TagMetadata tag = Serializer.DeserializeJson<TagMetadata>(tagJson);
                return UpdateTag(sdk, tag);
            });

            server.RegisterMethod("tag/delete", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid tagGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tagGuid");
                DeleteTag(sdk, tenantGuid, tagGuid);
                return true;
            });

            server.RegisterMethod("tag/exists", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid tagGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tagGuid");
                return TagExists(sdk, tenantGuid, tagGuid).ToString().ToLowerInvariant();
            });

            server.RegisterMethod("tag/getmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("tagGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Tag GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                return ReadTagsByGuids(sdk, tenantGuid, guids, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterMethod("tag/createmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("tags", out JsonElement tagsProp))
                    throw new ArgumentException("Tags array is required");
                
                string tagsJson = tagsProp.GetString() ?? throw new ArgumentException("Tags JSON string cannot be null");
                List<TagMetadata> tags = Serializer.DeserializeJson<List<TagMetadata>>(tagsJson);
                return CreateTags(sdk, tenantGuid, tags);
            });

            server.RegisterMethod("tag/deletemany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("tagGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Tag GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                DeleteTags(sdk, tenantGuid, guids);
                return true;
            });

            server.RegisterMethod("tag/readallintenant", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadAllTagsInTenant(sdk, tenantGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
            });

            server.RegisterMethod("tag/readallingraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadAllTagsInGraph(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterMethod("tag/readmanygraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadGraphTags(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterMethod("tag/readmanynode", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadNodeTags(sdk, tenantGuid, graphGuid, nodeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterMethod("tag/readmanyedge", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadEdgeTags(sdk, tenantGuid, graphGuid, edgeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterMethod("tag/deleteallintenant", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                DeleteAllTagsInTenant(sdk, tenantGuid);
                return true;
            });

            server.RegisterMethod("tag/deleteallingraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                DeleteAllTagsInGraph(sdk, tenantGuid, graphGuid);
                return true;
            });

            server.RegisterMethod("tag/deletegraphlabels", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                DeleteGraphTags(sdk, tenantGuid, graphGuid);
                return true;
            });

            server.RegisterMethod("tag/deletenodelabels", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                DeleteNodeTags(sdk, tenantGuid, graphGuid, nodeGuid);
                return true;
            });

            server.RegisterMethod("tag/deleteedgetags", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");
                DeleteEdgeTags(sdk, tenantGuid, graphGuid, edgeGuid);
                return true;
            });
        }

        #endregion

        #region Private-Methods

        private static string CreateTag(LiteGraphSdk sdk, TagMetadata tag)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));

            string body = Serializer.SerializeJson(tag, false);
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Put,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tag.TenantGUID)
                + "/tags",
                body);
        }

        private static string ReadTag(LiteGraphSdk sdk, Guid tenantGuid, Guid tagGuid)
        {
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                TagPath(tenantGuid, tagGuid));
        }

        private static string ReadTags(
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
                return ReadNodeTags(sdk, tenantGuid, graphGuid.Value, nodeGuid.Value, order, skip, maxResults);
            }

            if (edgeGuid.HasValue)
            {
                if (!graphGuid.HasValue) throw new ArgumentException("Graph GUID is required when edge GUID is provided.");
                return ReadEdgeTags(sdk, tenantGuid, graphGuid.Value, edgeGuid.Value, order, skip, maxResults);
            }

            if (graphGuid.HasValue) return ReadAllTagsInGraph(sdk, tenantGuid, graphGuid.Value, order, skip, maxResults);

            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                TagCollectionPath(tenantGuid) + BuildReadQuery(order, skip, maxResults, continuationToken));
        }

        private static string EnumerateTags(LiteGraphSdk sdk, EnumerationRequest query)
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
                        + "/tags"
                    : "/v2.0/tenants/"
                        + LiteGraphMcpRestProxy.Escape(query.TenantGUID.Value)
                        + "/tags",
                body);
        }

        private static bool TagExists(LiteGraphSdk sdk, Guid tenantGuid, Guid tagGuid)
        {
            return LiteGraphMcpRestProxy.HeadExists(sdk, TagPath(tenantGuid, tagGuid));
        }

        private static string ReadTagsByGuids(LiteGraphSdk sdk, Guid tenantGuid, List<Guid> tagGuids, int maxResults)
        {
            if (tagGuids == null) throw new ArgumentNullException(nameof(tagGuids));
            if (tagGuids.Count == 0) throw new ArgumentException("At least one tag GUID is required.");

            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                TagCollectionPath(tenantGuid)
                + "?guids="
                + String.Join(",", tagGuids)
                + "&max-keys="
                + maxResults);
        }

        private static string CreateTags(LiteGraphSdk sdk, Guid tenantGuid, List<TagMetadata> tags)
        {
            string body = Serializer.SerializeJson(tags ?? new List<TagMetadata>(), false);
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Put,
                TagCollectionPath(tenantGuid) + "/bulk",
                body);
        }

        private static void DeleteTags(LiteGraphSdk sdk, Guid tenantGuid, List<Guid> tagGuids)
        {
            string body = Serializer.SerializeJson(tagGuids ?? new List<Guid>(), false);
            LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Delete,
                TagCollectionPath(tenantGuid) + "/bulk",
                body);
        }

        private static string ReadAllTagsInTenant(
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
                TagCollectionPath(tenantGuid) + "/all" + BuildReadQuery(order, skip, maxResults, continuationToken));
        }

        private static string ReadAllTagsInGraph(
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
                GraphTagCollectionPath(tenantGuid, graphGuid) + "/all" + BuildReadQuery(order, skip, maxResults, null));
        }

        private static string ReadGraphTags(
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
                GraphTagCollectionPath(tenantGuid, graphGuid) + BuildReadQuery(order, skip, maxResults, null));
        }

        private static string ReadNodeTags(
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
                + "/tags"
                + BuildReadQuery(order, skip, maxResults, null));
        }

        private static string ReadEdgeTags(
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
                + "/tags"
                + BuildReadQuery(order, skip, maxResults, null));
        }

        private static string UpdateTag(LiteGraphSdk sdk, TagMetadata tag)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));

            string body = Serializer.SerializeJson(tag, false);
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Put,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tag.TenantGUID)
                + "/tags/"
                + LiteGraphMcpRestProxy.Escape(tag.GUID),
                body);
        }

        private static void DeleteTag(LiteGraphSdk sdk, Guid tenantGuid, Guid tagGuid)
        {
            LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Delete,
                TagPath(tenantGuid, tagGuid));
        }

        private static void DeleteAllTagsInTenant(LiteGraphSdk sdk, Guid tenantGuid)
        {
            LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Delete,
                TagCollectionPath(tenantGuid) + "/all");
        }

        private static void DeleteAllTagsInGraph(LiteGraphSdk sdk, Guid tenantGuid, Guid graphGuid)
        {
            LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Delete,
                GraphTagCollectionPath(tenantGuid, graphGuid) + "/all");
        }

        private static void DeleteGraphTags(LiteGraphSdk sdk, Guid tenantGuid, Guid graphGuid)
        {
            LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Delete,
                GraphTagCollectionPath(tenantGuid, graphGuid));
        }

        private static void DeleteNodeTags(LiteGraphSdk sdk, Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
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
                + "/tags");
        }

        private static void DeleteEdgeTags(LiteGraphSdk sdk, Guid tenantGuid, Guid graphGuid, Guid edgeGuid)
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
                + "/tags");
        }

        private static string TagCollectionPath(Guid tenantGuid)
        {
            return "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/tags";
        }

        private static string TagPath(Guid tenantGuid, Guid tagGuid)
        {
            return TagCollectionPath(tenantGuid)
                + "/"
                + LiteGraphMcpRestProxy.Escape(tagGuid);
        }

        private static string GraphTagCollectionPath(Guid tenantGuid, Guid graphGuid)
        {
            return "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/graphs/"
                + LiteGraphMcpRestProxy.Escape(graphGuid)
                + "/tags";
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
