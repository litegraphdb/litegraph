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
                    return true;
                });

            server.RegisterTool(
                "label/readallintenant",
                "Reads all labels in a tenant",
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
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                    List<LabelMetadata> labels = sdk.Label.ReadAllInTenant(tenantGuid, order, skip).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(labels, true);
                });

            server.RegisterTool(
                "label/readallingraph",
                "Reads all labels in a graph",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        order = new { type = "string", description = "Enumeration order (default: CreatedDescending)" },
                        skip = new { type = "integer", description = "Number of records to skip (default: 0)" }
                    },
                    required = new[] { "tenantGuid", "graphGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                    List<LabelMetadata> labels = sdk.Label.ReadAllInGraph(tenantGuid, graphGuid, order, skip).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(labels, true);
                });

            server.RegisterTool(
                "label/readmanygraph",
                "Reads labels scoped to a graph",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        order = new { type = "string", description = "Enumeration order (default: CreatedDescending)" },
                        skip = new { type = "integer", description = "Number of records to skip (default: 0)" }
                    },
                    required = new[] { "tenantGuid", "graphGuid" }
                },
                (args) =>
                {
                    if (!args.HasValue) throw new ArgumentException("Parameters required");
                    Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                    Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                    (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                    List<LabelMetadata> labels = sdk.Label.ReadManyGraph(tenantGuid, graphGuid, order, skip).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(labels, true);
                });

            server.RegisterTool(
                "label/readmanynode",
                "Reads labels attached to a node",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        nodeGuid = new { type = "string", description = "Node GUID" },
                        order = new { type = "string", description = "Enumeration order (default: CreatedDescending)" },
                        skip = new { type = "integer", description = "Number of records to skip (default: 0)" }
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
                    List<LabelMetadata> labels = sdk.Label.ReadManyNode(tenantGuid, graphGuid, nodeGuid, order, skip).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(labels, true);
                });

            server.RegisterTool(
                "label/readmanyedge",
                "Reads labels attached to an edge",
                new
                {
                    type = "object",
                    properties = new
                    {
                        tenantGuid = new { type = "string", description = "Tenant GUID" },
                        graphGuid = new { type = "string", description = "Graph GUID" },
                        edgeGuid = new { type = "string", description = "Edge GUID" },
                        order = new { type = "string", description = "Enumeration order (default: CreatedDescending)" },
                        skip = new { type = "integer", description = "Number of records to skip (default: 0)" }
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
                    List<LabelMetadata> labels = sdk.Label.ReadManyEdge(tenantGuid, graphGuid, edgeGuid, order, skip).GetAwaiter().GetResult();
                    return Serializer.SerializeJson(labels, true);
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
                    sdk.Label.DeleteAllInTenant(tenantGuid).GetAwaiter().GetResult();
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
                    sdk.Label.DeleteAllInGraph(tenantGuid, graphGuid).GetAwaiter().GetResult();
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
                    sdk.Label.DeleteGraphLabels(tenantGuid, graphGuid).GetAwaiter().GetResult();
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
                    sdk.Label.DeleteNodeLabels(tenantGuid, graphGuid, nodeGuid).GetAwaiter().GetResult();
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
                    sdk.Label.DeleteEdgeLabels(tenantGuid, graphGuid, edgeGuid).GetAwaiter().GetResult();
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
                if (!args.HasValue || !args.Value.TryGetProperty("query", out JsonElement queryProp))
                    throw new ArgumentException("Enumeration query is required");

                string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                EnumerationRequest query = Serializer.DeserializeJson<EnumerationRequest>(queryJson) ?? new EnumerationRequest();
                if (query.TenantGUID == null)
                    throw new ArgumentException("query.TenantGUID is required.");

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
                return true;
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
                return true;
            });

            server.RegisterMethod("label/readallintenant", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<LabelMetadata> labels = sdk.Label.ReadAllInTenant(tenantGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(labels, true);
            });

            server.RegisterMethod("label/readallingraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<LabelMetadata> labels = sdk.Label.ReadAllInGraph(tenantGuid, graphGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(labels, true);
            });

            server.RegisterMethod("label/readmanygraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<LabelMetadata> labels = sdk.Label.ReadManyGraph(tenantGuid, graphGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(labels, true);
            });

            server.RegisterMethod("label/readmanynode", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<LabelMetadata> labels = sdk.Label.ReadManyNode(tenantGuid, graphGuid, nodeGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(labels, true);
            });

            server.RegisterMethod("label/readmanyedge", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<LabelMetadata> labels = sdk.Label.ReadManyEdge(tenantGuid, graphGuid, edgeGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(labels, true);
            });

            server.RegisterMethod("label/deleteallintenant", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                sdk.Label.DeleteAllInTenant(tenantGuid).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterMethod("label/deleteallingraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                sdk.Label.DeleteAllInGraph(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterMethod("label/deletegraphlabels", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                sdk.Label.DeleteGraphLabels(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterMethod("label/deletenodelabels", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                sdk.Label.DeleteNodeLabels(tenantGuid, graphGuid, nodeGuid).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterMethod("label/deleteedgelabels", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");
                sdk.Label.DeleteEdgeLabels(tenantGuid, graphGuid, edgeGuid).GetAwaiter().GetResult();
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
                if (!args.HasValue || !args.Value.TryGetProperty("query", out JsonElement queryProp))
                    throw new ArgumentException("Enumeration query is required");

                string queryJson = queryProp.GetString() ?? throw new ArgumentException("Query JSON string cannot be null");
                EnumerationRequest query = Serializer.DeserializeJson<EnumerationRequest>(queryJson) ?? new EnumerationRequest();
                if (query.TenantGUID == null)
                    throw new ArgumentException("query.TenantGUID is required.");

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
                return true;
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
                return true;
            });

            server.RegisterMethod("label/readallintenant", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<LabelMetadata> labels = sdk.Label.ReadAllInTenant(tenantGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(labels, true);
            });

            server.RegisterMethod("label/readallingraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<LabelMetadata> labels = sdk.Label.ReadAllInGraph(tenantGuid, graphGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(labels, true);
            });

            server.RegisterMethod("label/readmanygraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<LabelMetadata> labels = sdk.Label.ReadManyGraph(tenantGuid, graphGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(labels, true);
            });

            server.RegisterMethod("label/readmanynode", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<LabelMetadata> labels = sdk.Label.ReadManyNode(tenantGuid, graphGuid, nodeGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(labels, true);
            });

            server.RegisterMethod("label/readmanyedge", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");
                (EnumerationOrderEnum order, int skip) = LiteGraphMcpServerHelpers.GetEnumerationParams(args.Value);
                List<LabelMetadata> labels = sdk.Label.ReadManyEdge(tenantGuid, graphGuid, edgeGuid, order, skip).GetAwaiter().GetResult();
                return Serializer.SerializeJson(labels, true);
            });

            server.RegisterMethod("label/deleteallintenant", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                sdk.Label.DeleteAllInTenant(tenantGuid).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterMethod("label/deleteallingraph", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                sdk.Label.DeleteAllInGraph(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterMethod("label/deletegraphlabels", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                sdk.Label.DeleteGraphLabels(tenantGuid, graphGuid).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterMethod("label/deletenodelabels", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid nodeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "nodeGuid");
                sdk.Label.DeleteNodeLabels(tenantGuid, graphGuid, nodeGuid).GetAwaiter().GetResult();
                return true;
            });

            server.RegisterMethod("label/deleteedgelabels", (args) =>
            {
                if (!args.HasValue) throw new ArgumentException("Parameters required");
                Guid tenantGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "tenantGuid");
                Guid graphGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "graphGuid");
                Guid edgeGuid = LiteGraphMcpServerHelpers.GetGuidRequired(args.Value, "edgeGuid");
                sdk.Label.DeleteEdgeLabels(tenantGuid, graphGuid, edgeGuid).GetAwaiter().GetResult();
                return true;
            });
        }

        #endregion
    }
}

