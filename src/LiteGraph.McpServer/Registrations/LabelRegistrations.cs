namespace LiteGraph.McpServer.Registrations
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using LiteGraph.McpServer.Classes;
    using LiteGraph.Sdk;
    using Voltaic;

    /// <summary>
    /// Registration methods for Label operations.
    /// </summary>
    public static class LabelRegistrations
    {
        #region HTTP-Tools

        /// <summary>
        /// Registers label tools on HTTP server.
        /// </summary>
        /// <param name="server">HTTP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterHttpTools(McpHttpServer server, LiteGraphSdk sdk)
        {
            server.RegisterTool(
                "label/create",
                "Creates a new label in LiteGraph",
                new
                {
                    type = "object",
                    properties = new
                    {
                        label = new { type = "string", description = "Label object serialized as JSON string using Serializer" }
                    },
                    required = new[] { "label" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("label", out JsonElement labelProp))
                        throw new ArgumentException("Label JSON string is required");
                    string labelJson = labelProp.GetString() ?? throw new ArgumentException("Label JSON string cannot be null");
                    LabelMetadata label = Serializer.DeserializeJson<LabelMetadata>(labelJson);
                    LabelMetadata created = sdk.Label.Create(label).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(created, true);
                });

            server.RegisterTool(
                "label/get",
                "Reads a label by GUID",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        labelGuid = new { type = "string", description = "Label GUID" }
                    },
                    required = new[] { "tenantGuid", "labelGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid labelGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "labelGuid");

                    LabelMetadata label = sdk.Label.ReadByGuid(tenantGuid, labelGuid).GetAwaiter().GetResult();
                    return label != null ? Serializer.SerializeJson(label, true) : "null";
                });

            server.RegisterTool(
                "label/all",
                "Lists all labels in a tenant",
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
                    List<LabelMetadata> labels = sdk.Label.ReadMany(tenantGuid, order, skip).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(labels, true);
                });

            server.RegisterTool(
                "label/enumerate",
                "Enumerates labels with pagination and filtering",
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
                    
                    EnumerationResult<LabelMetadata> result = sdk.Label.Enumerate(query).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(result, true);
                });

            server.RegisterTool(
                "label/update",
                "Updates a label",
                new
                {
                    type = "object",
                    properties = new
                    {
                        label = new { type = "string", description = "Label object serialized as JSON string using Serializer" }
                    },
                    required = new[] { "label" }
                },
                (args) =>
                {
                    if (!args.HasValue || !args.Value.TryGetProperty("label", out JsonElement labelProp))
                        throw new ArgumentException("Label JSON string is required");
                    string labelJson = labelProp.GetString() ?? throw new ArgumentException("Label JSON string cannot be null");
                    LabelMetadata label = Serializer.DeserializeJson<LabelMetadata>(labelJson);
                    LabelMetadata updated = sdk.Label.Update(label).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(updated, true);
                });

            server.RegisterTool(
                "label/delete",
                "Deletes a label by GUID",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        labelGuid = new { type = "string", description = "Label GUID" }
                    },
                    required = new[] { "tenantGuid", "labelGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid labelGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "labelGuid");
                    sdk.Label.DeleteByGuid(tenantGuid, labelGuid).GetAwaiter().GetResult();
                    return string.Empty;
                });

            server.RegisterTool(
                "label/exists",
                "Checks if a label exists by GUID",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        labelGuid = new { type = "string", description = "Label GUID" }
                    },
                    required = new[] { "tenantGuid", "labelGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid labelGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "labelGuid");
                    bool exists = sdk.Label.ExistsByGuid(tenantGuid, labelGuid).GetAwaiter().GetResult();
                    return exists.ToString().ToLower();
                });

            server.RegisterTool(
                "label/getmany",
                "Reads multiple labels by their GUIDs",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        labelGuids = new { type = "array", items = new { type = "string" }, description = "Array of label GUIDs" }
                    },
                    required = new[] { "tenantGuid", "labelGuids" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    if (!args.Value.TryGetProperty("labelGuids", out JsonElement guidsProp))
                        throw new ArgumentException("Label GUIDs array is required");
                    
                    List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                    List<LabelMetadata> labels = sdk.Label.ReadByGuids(tenantGuid, guids).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(labels, true);
                });

            server.RegisterTool(
                "label/createmany",
                "Creates multiple labels",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        labels = new { type = "string", description = "Array of label objects serialized as JSON string using Serializer" }
                    },
                    required = new[] { "tenantGuid", "labels" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    if (!args.Value.TryGetProperty("labels", out JsonElement labelsProp))
                        throw new ArgumentException("Labels array is required");
                    
                    string labelsJson = labelsProp.GetString() ?? throw new ArgumentException("Labels JSON string cannot be null");
                    List<LabelMetadata> labels = Serializer.DeserializeJson<List<LabelMetadata>>(labelsJson);
                    List<LabelMetadata> created = sdk.Label.CreateMany(tenantGuid, labels).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(created, true);
                });

            server.RegisterTool(
                "label/deletemany",
                "Deletes multiple labels by their GUIDs",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        labelGuids = new { type = "array", items = new { type = "string" }, description = "Array of label GUIDs to delete" }
                    },
                    required = new[] { "tenantGuid", "labelGuids" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    if (!args.Value.TryGetProperty("labelGuids", out JsonElement guidsProp))
                        throw new ArgumentException("Label GUIDs array is required");
                    
                    List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                    sdk.Label.DeleteMany(tenantGuid, guids).GetAwaiter().GetResult();
                    return string.Empty;
                });
        }

        #endregion

        #region TCP-Methods

        /// <summary>
        /// Registers label methods on TCP server.
        /// </summary>
        /// <param name="server">TCP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterTcpMethods(McpTcpServer server, LiteGraphSdk sdk)
        {
            server.RegisterMethod("label/create", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("label", out JsonElement labelProp))
                    throw new ArgumentException("Label JSON string is required");
                string labelJson = labelProp.GetString() ?? throw new ArgumentException("Label JSON string cannot be null");
                LabelMetadata label = Serializer.DeserializeJson<LabelMetadata>(labelJson);
                LabelMetadata created = sdk.Label.Create(label).GetAwaiter().GetResult();
                return Serializer.SerializeJson(created, true);
            });

            server.RegisterMethod("label/get", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid labelGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "labelGuid");

                LabelMetadata label = sdk.Label.ReadByGuid(tenantGuid, labelGuid).GetAwaiter().GetResult();
                return label != null ? Serializer.SerializeJson(label, true) : "null";
            });

            server.RegisterMethod("label/all", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<LabelMetadata> labels = sdk.Label.ReadMany(tenantGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(labels, true);
            });

            server.RegisterMethod("label/enumerate", (args) =>
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
                
                EnumerationResult<LabelMetadata> result = sdk.Label.Enumerate(query).GetAwaiter().GetResult();
                return Serializer.SerializeJson(result, true);
            });

            server.RegisterMethod("label/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("label", out JsonElement labelProp))
                    throw new ArgumentException("Label JSON string is required");
                string labelJson = labelProp.GetString() ?? throw new ArgumentException("Label JSON string cannot be null");
                LabelMetadata label = Serializer.DeserializeJson<LabelMetadata>(labelJson);
                LabelMetadata updated = sdk.Label.Update(label).GetAwaiter().GetResult();
                return Serializer.SerializeJson(updated, true);
            });

            server.RegisterMethod("label/delete", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid labelGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "labelGuid");
                sdk.Label.DeleteByGuid(tenantGuid, labelGuid).GetAwaiter().GetResult();
                return "{\"success\": true}";
            });

            server.RegisterMethod("label/exists", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid labelGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "labelGuid");
                bool exists = sdk.Label.ExistsByGuid(tenantGuid, labelGuid).GetAwaiter().GetResult();
                return exists.ToString().ToLower();
            });

            server.RegisterMethod("label/getmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("labelGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Label GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                List<LabelMetadata> labels = sdk.Label.ReadByGuids(tenantGuid, guids).GetAwaiter().GetResult();
                return Serializer.SerializeJson(labels, true);
            });

            server.RegisterMethod("label/createmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("labels", out JsonElement labelsProp))
                    throw new ArgumentException("Labels array is required");
                
                string labelsJson = labelsProp.GetString() ?? throw new ArgumentException("Labels JSON string cannot be null");
                List<LabelMetadata> labels = Serializer.DeserializeJson<List<LabelMetadata>>(labelsJson);
                List<LabelMetadata> created = sdk.Label.CreateMany(tenantGuid, labels).GetAwaiter().GetResult();
                return Serializer.SerializeJson(created, true);
            });

            server.RegisterMethod("label/deletemany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("labelGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Label GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                sdk.Label.DeleteMany(tenantGuid, guids).GetAwaiter().GetResult();
                return "{\"success\": true}";
            });
        }

        #endregion

        #region WebSocket-Methods

        /// <summary>
        /// Registers label methods on WebSocket server.
        /// </summary>
        /// <param name="server">WebSocket server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterWebSocketMethods(McpWebsocketsServer server, LiteGraphSdk sdk)
        {
            server.RegisterMethod("label/create", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("label", out JsonElement labelProp))
                    throw new ArgumentException("Label JSON string is required");
                string labelJson = labelProp.GetString() ?? throw new ArgumentException("Label JSON string cannot be null");
                LabelMetadata label = Serializer.DeserializeJson<LabelMetadata>(labelJson);
                LabelMetadata created = sdk.Label.Create(label).GetAwaiter().GetResult();
                return Serializer.SerializeJson(created, true);
            });

            server.RegisterMethod("label/get", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid labelGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "labelGuid");

                LabelMetadata label = sdk.Label.ReadByGuid(tenantGuid, labelGuid).GetAwaiter().GetResult();
                return label != null ? Serializer.SerializeJson(label, true) : "null";
            });

            server.RegisterMethod("label/all", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<LabelMetadata> labels = sdk.Label.ReadMany(tenantGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(labels, true);
            });

            server.RegisterMethod("label/enumerate", (args) =>
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
                
                EnumerationResult<LabelMetadata> result = sdk.Label.Enumerate(query).GetAwaiter().GetResult();
                return Serializer.SerializeJson(result, true);
            });

            server.RegisterMethod("label/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("label", out JsonElement labelProp))
                    throw new ArgumentException("Label JSON string is required");
                string labelJson = labelProp.GetString() ?? throw new ArgumentException("Label JSON string cannot be null");
                LabelMetadata label = Serializer.DeserializeJson<LabelMetadata>(labelJson);
                LabelMetadata updated = sdk.Label.Update(label).GetAwaiter().GetResult();
                return Serializer.SerializeJson(updated, true);
            });

            server.RegisterMethod("label/delete", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid labelGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "labelGuid");
                sdk.Label.DeleteByGuid(tenantGuid, labelGuid).GetAwaiter().GetResult();
                return "{\"success\": true}";
            });

            server.RegisterMethod("label/exists", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid labelGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "labelGuid");
                bool exists = sdk.Label.ExistsByGuid(tenantGuid, labelGuid).GetAwaiter().GetResult();
                return exists.ToString().ToLower();
            });

            server.RegisterMethod("label/getmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("labelGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Label GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                List<LabelMetadata> labels = sdk.Label.ReadByGuids(tenantGuid, guids).GetAwaiter().GetResult();
                return Serializer.SerializeJson(labels, true);
            });

            server.RegisterMethod("label/createmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("labels", out JsonElement labelsProp))
                    throw new ArgumentException("Labels array is required");
                
                string labelsJson = labelsProp.GetString() ?? throw new ArgumentException("Labels JSON string cannot be null");
                List<LabelMetadata> labels = Serializer.DeserializeJson<List<LabelMetadata>>(labelsJson);
                List<LabelMetadata> created = sdk.Label.CreateMany(tenantGuid, labels).GetAwaiter().GetResult();
                return Serializer.SerializeJson(created, true);
            });

            server.RegisterMethod("label/deletemany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("labelGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Label GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                sdk.Label.DeleteMany(tenantGuid, guids).GetAwaiter().GetResult();
                return "{\"success\": true}";
            });
        }

        #endregion
    }
}

