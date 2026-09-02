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
                    return CreateLabel(sdk, label);
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

                    return ReadLabel(sdk, tenantGuid, labelGuid);
                });

            server.RegisterTool(
                "label/all",
                "Lists all labels in a tenant. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
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
                    return ReadLabels(sdk, tenantGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
                });

            server.RegisterTool(
                "label/enumerate",
                "Enumerates labels with pagination and filtering",
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

                    return EnumerateLabels(sdk, query);
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
                    return UpdateLabel(sdk, label);
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
                    DeleteLabel(sdk, tenantGuid, labelGuid);
                    return true;
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
                    return LabelExists(sdk, tenantGuid, labelGuid).ToString().ToLowerInvariant();
                });

            server.RegisterTool(
                "label/getmany",
                "Reads multiple labels by their GUIDs. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        labelGuids = new { type = "array", items = new { type = "string" }, description = "Array of label GUIDs" },
                        maxResults = new { type = "integer", description = "Maximum results to return, 1-1000, default 1000" }
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
                    return ReadLabelsByGuids(sdk, tenantGuid, guids, LiteGraphMcpServerHelpers.GetMaxResults(args));
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
                    return CreateLabels(sdk, tenantGuid, labels);
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
                    DeleteLabels(sdk, tenantGuid, guids);
                    return true;
                });

            server.RegisterTool(
                "label/readallintenant",
                "Reads all labels in a tenant. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
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
                    return ReadAllLabelsInTenant(sdk, tenantGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
                });

            server.RegisterTool(
                "label/readallingraph",
                "Reads all labels in a graph. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
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
                    return ReadAllLabelsInGraph(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
                });

            server.RegisterTool(
                "label/readmanygraph",
                "Reads labels scoped to a graph. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
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
                    return ReadGraphLabels(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
                });

            server.RegisterTool(
                "label/readmanynode",
                "Reads labels attached to a node. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
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
                    return ReadNodeLabels(sdk, tenantGuid, graphGuid, nodeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
                });

            server.RegisterTool(
                "label/readmanyedge",
                "Reads labels attached to an edge. Returns a paginated EnumerationResult envelope (Objects, TotalRecords, RecordsRemaining, ContinuationToken/EndOfResults)",
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
                    return ReadEdgeLabels(sdk, tenantGuid, graphGuid, edgeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
                });

            server.RegisterTool(
                "label/deleteallintenant",
                "Deletes all labels in a tenant",
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
                    DeleteAllLabelsInTenant(sdk, tenantGuid);
                    return true;
                });

            server.RegisterTool(
                "label/deleteallingraph",
                "Deletes all labels in a graph",
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
                    DeleteAllLabelsInGraph(sdk, tenantGuid, graphGuid);
                    return true;
                });

            server.RegisterTool(
                "label/deletegraphlabels",
                "Deletes labels assigned to a graph",
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
                    DeleteGraphLabels(sdk, tenantGuid, graphGuid);
                    return true;
                });

            server.RegisterTool(
                "label/deletenodelabels",
                "Deletes labels assigned to a node",
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
                    DeleteNodeLabels(sdk, tenantGuid, graphGuid, nodeGuid);
                    return true;
                });

            server.RegisterTool(
                "label/deleteedgelabels",
                "Deletes labels assigned to an edge",
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
                    DeleteEdgeLabels(sdk, tenantGuid, graphGuid, edgeGuid);
                    return true;
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
                return CreateLabel(sdk, label);
            });

            server.RegisterMethod("label/get", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid labelGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "labelGuid");

                return ReadLabel(sdk, tenantGuid, labelGuid);
            });

            server.RegisterMethod("label/all", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadLabels(sdk, tenantGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
            });

            server.RegisterMethod("label/enumerate", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("query", out JsonElement queryProp))
                    throw new ArgumentException("Enumeration query is required");

                string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                EnumerationRequest query = Serializer.DeserializeJson<EnumerationRequest>(queryJson) ?? new EnumerationRequest();
                if (query.TenantGUID == null)
                    throw new ArgumentException("query.TenantGUID is required.");

                return EnumerateLabels(sdk, query);
            });

            server.RegisterMethod("label/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("label", out JsonElement labelProp))
                    throw new ArgumentException("Label JSON string is required");
                string labelJson = labelProp.GetString() ?? throw new ArgumentException("Label JSON string cannot be null");
                LabelMetadata label = Serializer.DeserializeJson<LabelMetadata>(labelJson);
                return UpdateLabel(sdk, label);
            });

            server.RegisterMethod("label/delete", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid labelGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "labelGuid");
                DeleteLabel(sdk, tenantGuid, labelGuid);
                return true;
            });

            server.RegisterMethod("label/exists", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid labelGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "labelGuid");
                return LabelExists(sdk, tenantGuid, labelGuid).ToString().ToLowerInvariant();
            });

            server.RegisterMethod("label/getmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("labelGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Label GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                return ReadLabelsByGuids(sdk, tenantGuid, guids, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterMethod("label/createmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("labels", out JsonElement labelsProp))
                    throw new ArgumentException("Labels array is required");
                
                string labelsJson = labelsProp.GetString() ?? throw new ArgumentException("Labels JSON string cannot be null");
                List<LabelMetadata> labels = Serializer.DeserializeJson<List<LabelMetadata>>(labelsJson);
                return CreateLabels(sdk, tenantGuid, labels);
            });

            server.RegisterMethod("label/deletemany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("labelGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Label GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                DeleteLabels(sdk, tenantGuid, guids);
                return true;
            });

            server.RegisterMethod("label/readallintenant", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadAllLabelsInTenant(sdk, tenantGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
            });

            server.RegisterMethod("label/readallingraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadAllLabelsInGraph(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterMethod("label/readmanygraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadGraphLabels(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterMethod("label/readmanynode", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadNodeLabels(sdk, tenantGuid, graphGuid, nodeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterMethod("label/readmanyedge", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadEdgeLabels(sdk, tenantGuid, graphGuid, edgeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterMethod("label/deleteallintenant", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                DeleteAllLabelsInTenant(sdk, tenantGuid);
                return true;
            });

            server.RegisterMethod("label/deleteallingraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                DeleteAllLabelsInGraph(sdk, tenantGuid, graphGuid);
                return true;
            });

            server.RegisterMethod("label/deletegraphlabels", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                DeleteGraphLabels(sdk, tenantGuid, graphGuid);
                return true;
            });

            server.RegisterMethod("label/deletenodelabels", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                DeleteNodeLabels(sdk, tenantGuid, graphGuid, nodeGuid);
                return true;
            });

            server.RegisterMethod("label/deleteedgelabels", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");
                DeleteEdgeLabels(sdk, tenantGuid, graphGuid, edgeGuid);
                return true;
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
                return CreateLabel(sdk, label);
            });

            server.RegisterMethod("label/get", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid labelGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "labelGuid");

                return ReadLabel(sdk, tenantGuid, labelGuid);
            });

            server.RegisterMethod("label/all", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("tenantGuid", out JsonElement tenantGuidProp))
                    throw new ArgumentException("Tenant GUID is required");
                Guid tenantGuid = Guid.Parse(tenantGuidProp.GetString()!);
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadLabels(sdk, tenantGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
            });

            server.RegisterMethod("label/enumerate", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("query", out JsonElement queryProp))
                    throw new ArgumentException("Enumeration query is required");

                string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                EnumerationRequest query = Serializer.DeserializeJson<EnumerationRequest>(queryJson) ?? new EnumerationRequest();
                if (query.TenantGUID == null)
                    throw new ArgumentException("query.TenantGUID is required.");

                return EnumerateLabels(sdk, query);
            });

            server.RegisterMethod("label/update", (args) =>
            {
                if (!args.HasValue || !args.Value.TryGetProperty("label", out JsonElement labelProp))
                    throw new ArgumentException("Label JSON string is required");
                string labelJson = labelProp.GetString() ?? throw new ArgumentException("Label JSON string cannot be null");
                LabelMetadata label = Serializer.DeserializeJson<LabelMetadata>(labelJson);
                return UpdateLabel(sdk, label);
            });

            server.RegisterMethod("label/delete", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid labelGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "labelGuid");
                DeleteLabel(sdk, tenantGuid, labelGuid);
                return true;
            });

            server.RegisterMethod("label/exists", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid labelGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "labelGuid");
                return LabelExists(sdk, tenantGuid, labelGuid).ToString().ToLowerInvariant();
            });

            server.RegisterMethod("label/getmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("labelGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Label GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                return ReadLabelsByGuids(sdk, tenantGuid, guids, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterMethod("label/createmany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("labels", out JsonElement labelsProp))
                    throw new ArgumentException("Labels array is required");
                
                string labelsJson = labelsProp.GetString() ?? throw new ArgumentException("Labels JSON string cannot be null");
                List<LabelMetadata> labels = Serializer.DeserializeJson<List<LabelMetadata>>(labelsJson);
                return CreateLabels(sdk, tenantGuid, labels);
            });

            server.RegisterMethod("label/deletemany", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                if (!args.Value.TryGetProperty("labelGuids", out JsonElement guidsProp))
                    throw new ArgumentException("Label GUIDs array is required");
                
                List<Guid> guids = Serializer.DeserializeJson<List<Guid>>(guidsProp.GetRawText());
                DeleteLabels(sdk, tenantGuid, guids);
                return true;
            });

            server.RegisterMethod("label/readallintenant", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadAllLabelsInTenant(sdk, tenantGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args), LiteGraphMcpServerHelpers.GetContinuationToken(args));
            });

            server.RegisterMethod("label/readallingraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadAllLabelsInGraph(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterMethod("label/readmanygraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadGraphLabels(sdk, tenantGuid, graphGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterMethod("label/readmanynode", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadNodeLabels(sdk, tenantGuid, graphGuid, nodeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterMethod("label/readmanyedge", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                return ReadEdgeLabels(sdk, tenantGuid, graphGuid, edgeGuid, order, skip, LiteGraphMcpServerHelpers.GetMaxResults(args));
            });

            server.RegisterMethod("label/deleteallintenant", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                DeleteAllLabelsInTenant(sdk, tenantGuid);
                return true;
            });

            server.RegisterMethod("label/deleteallingraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                DeleteAllLabelsInGraph(sdk, tenantGuid, graphGuid);
                return true;
            });

            server.RegisterMethod("label/deletegraphlabels", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                DeleteGraphLabels(sdk, tenantGuid, graphGuid);
                return true;
            });

            server.RegisterMethod("label/deletenodelabels", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                DeleteNodeLabels(sdk, tenantGuid, graphGuid, nodeGuid);
                return true;
            });

            server.RegisterMethod("label/deleteedgelabels", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");
                DeleteEdgeLabels(sdk, tenantGuid, graphGuid, edgeGuid);
                return true;
            });
        }

        #endregion

        #region Private-Methods

        private static string CreateLabel(LiteGraphSdk sdk, LabelMetadata label)
        {
            if (label == null) throw new ArgumentNullException(nameof(label));

            string body = Serializer.SerializeJson(label, false);
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Put,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(label.TenantGUID)
                + "/labels",
                body);
        }

        private static string ReadLabel(LiteGraphSdk sdk, Guid tenantGuid, Guid labelGuid)
        {
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                LabelPath(tenantGuid, labelGuid));
        }

        private static string ReadLabels(
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
                LabelCollectionPath(tenantGuid) + BuildReadQuery(order, skip, maxResults, continuationToken));
        }

        private static string EnumerateLabels(LiteGraphSdk sdk, EnumerationRequest query)
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
                        + "/labels"
                    : "/v2.0/tenants/"
                        + LiteGraphMcpRestProxy.Escape(query.TenantGUID.Value)
                        + "/labels",
                body);
        }

        private static bool LabelExists(LiteGraphSdk sdk, Guid tenantGuid, Guid labelGuid)
        {
            return LiteGraphMcpRestProxy.HeadExists(sdk, LabelPath(tenantGuid, labelGuid));
        }

        private static string ReadLabelsByGuids(LiteGraphSdk sdk, Guid tenantGuid, List<Guid> labelGuids, int maxResults)
        {
            if (labelGuids == null) throw new ArgumentNullException(nameof(labelGuids));
            if (labelGuids.Count == 0) throw new ArgumentException("At least one label GUID is required.");

            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Get,
                LabelCollectionPath(tenantGuid)
                + "?guids="
                + String.Join(",", labelGuids)
                + "&max-keys="
                + maxResults);
        }

        private static string CreateLabels(LiteGraphSdk sdk, Guid tenantGuid, List<LabelMetadata> labels)
        {
            string body = Serializer.SerializeJson(labels ?? new List<LabelMetadata>(), false);
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Put,
                LabelCollectionPath(tenantGuid) + "/bulk",
                body);
        }

        private static void DeleteLabels(LiteGraphSdk sdk, Guid tenantGuid, List<Guid> labelGuids)
        {
            string body = Serializer.SerializeJson(labelGuids ?? new List<Guid>(), false);
            LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Delete,
                LabelCollectionPath(tenantGuid) + "/bulk",
                body);
        }

        private static string ReadAllLabelsInTenant(
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
                LabelCollectionPath(tenantGuid) + "/all" + BuildReadQuery(order, skip, maxResults, continuationToken));
        }

        private static string ReadAllLabelsInGraph(
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
                GraphLabelCollectionPath(tenantGuid, graphGuid) + "/all" + BuildReadQuery(order, skip, maxResults, null));
        }

        private static string ReadGraphLabels(
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
                GraphLabelCollectionPath(tenantGuid, graphGuid) + BuildReadQuery(order, skip, maxResults, null));
        }

        private static string ReadNodeLabels(
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
                + "/labels"
                + BuildReadQuery(order, skip, maxResults, null));
        }

        private static string ReadEdgeLabels(
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
                + "/labels"
                + BuildReadQuery(order, skip, maxResults, null));
        }

        private static string UpdateLabel(LiteGraphSdk sdk, LabelMetadata label)
        {
            if (label == null) throw new ArgumentNullException(nameof(label));

            string body = Serializer.SerializeJson(label, false);
            return LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Put,
                "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(label.TenantGUID)
                + "/labels/"
                + LiteGraphMcpRestProxy.Escape(label.GUID),
                body);
        }

        private static void DeleteLabel(LiteGraphSdk sdk, Guid tenantGuid, Guid labelGuid)
        {
            LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Delete,
                LabelPath(tenantGuid, labelGuid));
        }

        private static void DeleteAllLabelsInTenant(LiteGraphSdk sdk, Guid tenantGuid)
        {
            LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Delete,
                LabelCollectionPath(tenantGuid) + "/all");
        }

        private static void DeleteAllLabelsInGraph(LiteGraphSdk sdk, Guid tenantGuid, Guid graphGuid)
        {
            LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Delete,
                GraphLabelCollectionPath(tenantGuid, graphGuid) + "/all");
        }

        private static void DeleteGraphLabels(LiteGraphSdk sdk, Guid tenantGuid, Guid graphGuid)
        {
            LiteGraphMcpRestProxy.SendJson(
                sdk,
                HttpMethod.Delete,
                GraphLabelCollectionPath(tenantGuid, graphGuid));
        }

        private static void DeleteNodeLabels(LiteGraphSdk sdk, Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
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
                + "/labels");
        }

        private static void DeleteEdgeLabels(LiteGraphSdk sdk, Guid tenantGuid, Guid graphGuid, Guid edgeGuid)
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
                + "/labels");
        }

        private static string LabelCollectionPath(Guid tenantGuid)
        {
            return "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/labels";
        }

        private static string LabelPath(Guid tenantGuid, Guid labelGuid)
        {
            return LabelCollectionPath(tenantGuid)
                + "/"
                + LiteGraphMcpRestProxy.Escape(labelGuid);
        }

        private static string GraphLabelCollectionPath(Guid tenantGuid, Guid graphGuid)
        {
            return "/v1.0/tenants/"
                + LiteGraphMcpRestProxy.Escape(tenantGuid)
                + "/graphs/"
                + LiteGraphMcpRestProxy.Escape(graphGuid)
                + "/labels";
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
