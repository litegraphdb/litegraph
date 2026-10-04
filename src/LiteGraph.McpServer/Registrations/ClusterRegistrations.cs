namespace LiteGraph.McpServer.Registrations
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using LiteGraph.McpServer.Classes;
    using LiteGraph.Sdk;
    using Voltaic.Core;
    using Voltaic.Mcp;

    /// <summary>
    /// Registration methods for read-only cluster tools: cluster/status, cluster/nodes, and cluster/node.
    /// Cluster operations that change state (restarts, removing nodes) are deliberately not exposed over MCP.
    /// </summary>
    public static class ClusterRegistrations
    {
        #region HTTP-Tools

        /// <summary>
        /// Registers cluster tools on HTTP server.
        /// </summary>
        /// <param name="server">HTTP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterHttpTools(McpHttpServer server, LiteGraphSdk sdk)
        {
            server.RegisterLiteGraphTool(
                "cluster_status",
                "Summarizes the LiteGraph deployment: whether it runs as a cluster, the cluster name, whether the node registry (Redis) is reachable, node counts by state, nodes waiting for a restart or behind the latest settings, and the settings and restart versions. Read-only; requires a system administrator token.",
                new
                {
                    type = "object",
                    properties = new { },
                    required = new string[] { }
                },
                (rpcArgs) => ClusterStatus(sdk));

            server.RegisterLiteGraphTool(
                "cluster_nodes",
                "Lists the nodes in the LiteGraph node registry with their state (Healthy, Degraded, Unavailable, Draining, Restarting, Stopped, Offline), health checks, version, start time, heartbeat age, settings version, and pending restart. On a single node the answering server is the only node. Read-only; requires a system administrator token.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        state = new { type = "string", description = "Optional state filter, for example Healthy or Offline (case-insensitive)" }
                    },
                    required = new string[] { }
                },
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    return ClusterNodes(sdk, GetOptionalString(args, "state"));
                });

            server.RegisterLiteGraphTool(
                "cluster_node",
                "Reads one node from the LiteGraph node registry by node identifier. Returns null when the node is not registered. Read-only; requires a system administrator token.",
                new
                {
                    type = "object",
                    properties = new
                    {
                        nodeId = new { type = "string", description = "Node identifier, for example litegraph-1" }
                    },
                    required = new[] { "nodeId" }
                },
                (rpcArgs) =>
                {
                    JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                    return ClusterNode(sdk, RequireNodeId(args));
                });
        }

        #endregion

        #region TCP-Methods

        /// <summary>
        /// Registers cluster methods on TCP server.
        /// </summary>
        /// <param name="server">TCP server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterTcpMethods(McpTcpServer server, LiteGraphSdk sdk)
        {
            server.RegisterLiteGraphMethod("cluster_status", (rpcArgs) => ClusterStatus(sdk));
            server.RegisterLiteGraphMethod("cluster_nodes", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                return ClusterNodes(sdk, GetOptionalString(args, "state"));
            });
            server.RegisterLiteGraphMethod("cluster_node", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                return ClusterNode(sdk, RequireNodeId(args));
            });
        }

        #endregion

        #region WebSocket-Methods

        /// <summary>
        /// Registers cluster methods on WebSocket server.
        /// </summary>
        /// <param name="server">WebSocket server instance.</param>
        /// <param name="sdk">LiteGraph SDK instance.</param>
        public static void RegisterWebSocketMethods(McpWebsocketsServer server, LiteGraphSdk sdk)
        {
            server.RegisterLiteGraphMethod("cluster_status", (rpcArgs) => ClusterStatus(sdk));
            server.RegisterLiteGraphMethod("cluster_nodes", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                return ClusterNodes(sdk, GetOptionalString(args, "state"));
            });
            server.RegisterLiteGraphMethod("cluster_node", (rpcArgs) =>
            {
                JsonElement? args = LiteGraphMcpServerHelpers.ToJsonElement(rpcArgs);
                return ClusterNode(sdk, RequireNodeId(args));
            });
        }

        #endregion

        #region Private-Methods

        private static string ClusterStatus(LiteGraphSdk sdk)
        {
            JsonObject status = ReadStatus(sdk);
            JsonArray nodes = status["Nodes"] as JsonArray ?? new JsonArray();
            long settingsVersion = status["SettingsVersion"]?.GetValue<long>() ?? 0;

            Dictionary<string, int> byState = new Dictionary<string, int>(StringComparer.Ordinal);
            int restartPending = 0;
            int behindSettings = 0;
            foreach (JsonNode? node in nodes)
            {
                if (node == null) continue;
                string state = node["State"]?.GetValue<string>() ?? "Unknown";
                byState[state] = byState.TryGetValue(state, out int count) ? count + 1 : 1;
                if (node["RestartPending"]?.GetValue<bool>() == true) restartPending++;
                if ((node["SettingsVersion"]?.GetValue<long>() ?? 0) < settingsVersion) behindSettings++;
            }

            JsonObject counts = new JsonObject();
            foreach (KeyValuePair<string, int> pair in byState.OrderBy(p => p.Key, StringComparer.Ordinal)) counts[pair.Key] = pair.Value;

            JsonObject summary = new JsonObject
            {
                ["ClusterEnabled"] = status["ClusterEnabled"]?.DeepClone(),
                ["ClusterName"] = status["ClusterName"]?.DeepClone(),
                ["AnsweredBy"] = status["AnsweredBy"]?.DeepClone(),
                ["RegistryAvailable"] = status["RegistryAvailable"]?.DeepClone(),
                ["NodesTotal"] = nodes.Count,
                ["NodesByState"] = counts,
                ["NodesRestartPending"] = restartPending,
                ["NodesBehindSettings"] = behindSettings,
                ["SettingsVersion"] = settingsVersion,
                ["SettingsUpdatedUtc"] = status["SettingsUpdatedUtc"]?.DeepClone(),
                ["RestartVersion"] = status["RestartVersion"]?.DeepClone(),
                ["RestartRequestedUtc"] = status["RestartRequestedUtc"]?.DeepClone(),
                ["Utc"] = status["Utc"]?.DeepClone()
            };
            return summary.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        }

        private static string ClusterNodes(LiteGraphSdk sdk, string? state)
        {
            JsonObject status = ReadStatus(sdk);
            if (!String.IsNullOrEmpty(state) && status["Nodes"] is JsonArray nodes)
            {
                JsonArray filtered = new JsonArray();
                foreach (JsonNode? node in nodes.ToList())
                {
                    if (node != null && String.Equals(node["State"]?.GetValue<string>(), state, StringComparison.OrdinalIgnoreCase))
                        filtered.Add(node.DeepClone());
                }
                status["Nodes"] = filtered;
            }
            return status.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        }

        private static string ClusterNode(LiteGraphSdk sdk, string nodeId)
        {
            string? json = LiteGraphMcpRestProxy.SendJsonOrNullOnNotFound(sdk, HttpMethod.Get, "/v1.0/cluster/nodes/" + Uri.EscapeDataString(nodeId));
            return json ?? "null";
        }

        private static JsonObject ReadStatus(LiteGraphSdk sdk)
        {
            string json = LiteGraphMcpRestProxy.SendJson(sdk, HttpMethod.Get, "/v1.0/cluster/nodes");
            JsonObject? status = JsonNode.Parse(json) as JsonObject;
            if (status == null) throw new InvalidOperationException("The server returned an unexpected cluster status.");
            return status;
        }

        private static string? GetOptionalString(JsonElement? args, string name)
        {
            if (!args.HasValue || !args.Value.TryGetProperty(name, out JsonElement value) || value.ValueKind != JsonValueKind.String) return null;
            return value.GetString();
        }

        private static string RequireNodeId(JsonElement? args)
        {
            string? nodeId = GetOptionalString(args, "nodeId");
            if (String.IsNullOrWhiteSpace(nodeId)) throw new ArgumentException("Node identifier is required");
            return nodeId;
        }

        #endregion
    }
}
