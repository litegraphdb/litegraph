namespace LiteGraph.McpServer.Registrations
{
    using System;
    using System.Collections.Generic;
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
                    TagMetadata created = sdk.Tag.Create(tag).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(created, true);
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

                    TagMetadata tag = sdk.Tag.ReadByGuid(tenantGuid, tagGuid).GetAwaiter().GetResult();
                    return tag != null ? Serializer.SerializeJson(tag, true) : "null";
                });

            server.RegisterTool(
                "tag/readmany",
                "Reads tags with optional filters",
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
                        skip = new { type = "integer", description = "Number of records to skip (default: 0)" }
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
                    
                    List<TagMetadata> tags = sdk.Tag.ReadMany(tenantGuid, graphGuid, nodeGuid, edgeGuid, order, skip).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(tags, true);
                });

            server.RegisterTool(
                "tag/enumerate",
                "Enumerates tags with pagination and filtering",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        query = new { type = "string", description = "Enumeration query object serialized as JSON string using Serializer" }
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
                        string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                        EnumerationRequest? deserializedQuery = Serializer.DeserializeJson<EnumerationRequest>(queryJson);
                        if (deserializedQuery != null)
                        {
                            query.MaxResults = deserializedQuery.MaxResults;
                            query.Ordering = deserializedQuery.Ordering;
                            query.ContinuationToken = deserializedQuery.ContinuationToken;
                            query.IncludeData = deserializedQuery.IncludeData;
                            query.IncludeSubordinates = deserializedQuery.IncludeSubordinates;
                        }
                    }
                    
                    EnumerationResult<TagMetadata> result = sdk.Tag.Enumerate(query).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(result, true);
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
                    TagMetadata updated = sdk.Tag.Update(tag).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(updated, true);
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
                    sdk.Tag.DeleteByGuid(tenantGuid, tagGuid).GetAwaiter().GetResult();
                    return string.Empty;
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
                    bool exists = sdk.Tag.ExistsByGuid(tenantGuid, tagGuid).GetAwaiter().GetResult();
                    return exists.ToString().ToLower();
                });

            server.RegisterTool(
                "tag/getmany",
                "Reads multiple tags by their GUIDs",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        tagGuids = new { type = "array", items = new { type = "string" }, description = "Array of tag GUIDs" }
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
                    List<TagMetadata> tags = sdk.Tag.ReadByGuids(tenantGuid, guids).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(tags, true);
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
                    List<TagMetadata> created = sdk.Tag.CreateMany(tenantGuid, tags).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(created, true);
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
                    sdk.Tag.DeleteMany(tenantGuid, guids).GetAwaiter().GetResult();
                    return string.Empty;
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
                TagMetadata created = sdk.Tag.Create(tag).GetAwaiter().GetResult();
                return Serializer.SerializeJson(created, true);
            });

            server.RegisterMethod("tag/get", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid tagGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tagGuid");

                TagMetadata tag = sdk.Tag.ReadByGuid(tenantGuid, tagGuid).GetAwaiter().GetResult();
                return tag != null ? Serializer.SerializeJson(tag, true) : "null";
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
                List<TagMetadata> tags = sdk.Tag.ReadMany(tenantGuid, graphGuid, nodeGuid, edgeGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(tags, true);
            });

            server.RegisterMethod("tag/enumerate", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");
                
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                EnumerationRequest query = new EnumerationRequest { TenantGUID = tenantGuid };
                
                if (args.Value.TryGetProperty("query", out JsonElement queryProp))
                {
                    string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                    EnumerationRequest? deserializedQuery = Serializer.DeserializeJson<EnumerationRequest>(queryJson);
                    if (deserializedQuery != null)
                    {
                        query.MaxResults = deserializedQuery.MaxResults;
                        query.Ordering = deserializedQuery.Ordering;
                        query.ContinuationToken = deserializedQuery.ContinuationToken;
                        query.IncludeData = deserializedQuery.IncludeData;
                        query.IncludeSubordinates = deserializedQuery.IncludeSubordinates;
                    }
                }
                
                EnumerationResult<TagMetadata> result = sdk.Tag.Enumerate(query).GetAwaiter().GetResult();
                return Serializer.SerializeJson(result, true);
            });

            server.RegisterMethod("tag/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tag", out JsonElement tagProp))
                    throw new ArgumentException("Tag JSON string is required");
                string tagJson = tagProp.GetString() ?? throw new ArgumentException("Tag JSON string cannot be null");
                TagMetadata tag = Serializer.DeserializeJson<TagMetadata>(tagJson);
                TagMetadata updated = sdk.Tag.Update(tag).GetAwaiter().GetResult();
                return Serializer.SerializeJson(updated, true);
            });

            server.RegisterMethod("tag/delete", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid tagGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tagGuid");
                sdk.Tag.DeleteByGuid(tenantGuid, tagGuid).GetAwaiter().GetResult();
                return "{\"success\": true}";
            });

            server.RegisterMethod("tag/exists", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid tagGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tagGuid");
                bool exists = sdk.Tag.ExistsByGuid(tenantGuid, tagGuid).GetAwaiter().GetResult();
                return exists.ToString().ToLower();
            });

            server.RegisterMethod("tag/getmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("tagGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Tag GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                List<TagMetadata> tags = sdk.Tag.ReadByGuids(tenantGuid, guids).GetAwaiter().GetResult();
                return Serializer.SerializeJson(tags, true);
            });

            server.RegisterMethod("tag/createmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("tags", out JsonElement tagsProp))
                    throw new ArgumentException("Tags array is required");
                
                string tagsJson = tagsProp.GetString() ?? throw new ArgumentException("Tags JSON string cannot be null");
                List<TagMetadata> tags = Serializer.DeserializeJson<List<TagMetadata>>(tagsJson);
                List<TagMetadata> created = sdk.Tag.CreateMany(tenantGuid, tags).GetAwaiter().GetResult();
                return Serializer.SerializeJson(created, true);
            });

            server.RegisterMethod("tag/deletemany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("tagGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Tag GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                sdk.Tag.DeleteMany(tenantGuid, guids).GetAwaiter().GetResult();
                return "{\"success\": true}";
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
                TagMetadata created = sdk.Tag.Create(tag).GetAwaiter().GetResult();
                return Serializer.SerializeJson(created, true);
            });

            server.RegisterMethod("tag/get", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid tagGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tagGuid");

                TagMetadata tag = sdk.Tag.ReadByGuid(tenantGuid, tagGuid).GetAwaiter().GetResult();
                return tag != null ? Serializer.SerializeJson(tag, true) : "null";
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
                List<TagMetadata> tags = sdk.Tag.ReadMany(tenantGuid, graphGuid, nodeGuid, edgeGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(tags, true);
            });

            server.RegisterMethod("tag/enumerate", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");
                
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                EnumerationRequest query = new EnumerationRequest { TenantGUID = tenantGuid };
                
                if (args.Value.TryGetProperty("query", out JsonElement queryProp))
                {
                    string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                    EnumerationRequest? deserializedQuery = Serializer.DeserializeJson<EnumerationRequest>(queryJson);
                    if (deserializedQuery != null)
                    {
                        query.MaxResults = deserializedQuery.MaxResults;
                        query.Ordering = deserializedQuery.Ordering;
                        query.ContinuationToken = deserializedQuery.ContinuationToken;
                        query.IncludeData = deserializedQuery.IncludeData;
                        query.IncludeSubordinates = deserializedQuery.IncludeSubordinates;
                    }
                }
                
                EnumerationResult<TagMetadata> result = sdk.Tag.Enumerate(query).GetAwaiter().GetResult();
                return Serializer.SerializeJson(result, true);
            });

            server.RegisterMethod("tag/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tag", out JsonElement tagProp))
                    throw new ArgumentException("Tag JSON string is required");
                string tagJson = tagProp.GetString() ?? throw new ArgumentException("Tag JSON string cannot be null");
                TagMetadata tag = Serializer.DeserializeJson<TagMetadata>(tagJson);
                TagMetadata updated = sdk.Tag.Update(tag).GetAwaiter().GetResult();
                return Serializer.SerializeJson(updated, true);
            });

            server.RegisterMethod("tag/delete", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid tagGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tagGuid");
                sdk.Tag.DeleteByGuid(tenantGuid, tagGuid).GetAwaiter().GetResult();
                return "{\"success\": true}";
            });

            server.RegisterMethod("tag/exists", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid tagGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tagGuid");
                bool exists = sdk.Tag.ExistsByGuid(tenantGuid, tagGuid).GetAwaiter().GetResult();
                return exists.ToString().ToLower();
            });

            server.RegisterMethod("tag/getmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("tagGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Tag GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                List<TagMetadata> tags = sdk.Tag.ReadByGuids(tenantGuid, guids).GetAwaiter().GetResult();
                return Serializer.SerializeJson(tags, true);
            });

            server.RegisterMethod("tag/createmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("tags", out JsonElement tagsProp))
                    throw new ArgumentException("Tags array is required");
                
                string tagsJson = tagsProp.GetString() ?? throw new ArgumentException("Tags JSON string cannot be null");
                List<TagMetadata> tags = Serializer.DeserializeJson<List<TagMetadata>>(tagsJson);
                List<TagMetadata> created = sdk.Tag.CreateMany(tenantGuid, tags).GetAwaiter().GetResult();
                return Serializer.SerializeJson(created, true);
            });

            server.RegisterMethod("tag/deletemany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("tagGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Tag GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                sdk.Tag.DeleteMany(tenantGuid, guids).GetAwaiter().GetResult();
                return "{\"success\": true}";
            });
        }

        #endregion
    }
}

