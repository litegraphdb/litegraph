namespace LiteGraph.Server.Services.Chat
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using LiteGraph;
    using LiteGraph.Serialization;
    using LiteGraph.Server.API.Agnostic;
    using LiteGraph.Server.Classes;

    /// <summary>
    /// The curated set of graph tools advertised to the model.
    /// Tool names mirror the MCP server's catalog; a parity test asserts alignment.
    /// The one deliberate divergence is vector/search, which accepts text here (the dispatcher embeds it)
    /// where the MCP tool accepts raw embeddings.
    /// </summary>
    internal static class ChatToolCatalog
    {
        #region Public-Methods

        /// <summary>
        /// Build the tool catalog against an agnostic service handler.
        /// </summary>
        /// <param name="handler">Agnostic service handler.</param>
        /// <returns>Tool definitions.</returns>
        internal static List<ChatToolDefinition> Build(ServiceHandler handler)
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            List<ChatToolDefinition> tools = new List<ChatToolDefinition>();

            #region Graph-Read

            tools.Add(new ChatToolDefinition
            {
                Name = "graph/all",
                Description = "Lists all graphs in the tenant",
                RequestType = RequestTypeEnum.GraphReadAllInTenant,
                Schema = SchemaOf(new { }),
                Bind = (args, req) => { },
                Handler = handler.GraphReadAllInTenant
            });

            tools.Add(new ChatToolDefinition
            {
                Name = "graph/get",
                Description = "Reads a graph by GUID",
                RequestType = RequestTypeEnum.GraphRead,
                Schema = SchemaOf(new
                {
                    graphGuid = new { type = "string", description = "Graph GUID" },
                    includeData = new { type = "boolean", description = "Include graph data" },
                    includeSubordinates = new { type = "boolean", description = "Include labels, tags, vectors" }
                }, "graphGuid"),
                Bind = (args, req) =>
                {
                    req.GraphGUID = GetGuid(args, "graphGuid");
                    req.IncludeData = GetBool(args, "includeData");
                    req.IncludeSubordinates = GetBool(args, "includeSubordinates");
                },
                Handler = handler.GraphRead
            });

            tools.Add(new ChatToolDefinition
            {
                Name = "graph/search",
                Description = "Searches graphs by name and labels",
                RequestType = RequestTypeEnum.GraphSearch,
                Schema = SchemaOf(new
                {
                    name = new { type = "string", description = "Graph name filter" },
                    labels = new { type = "array", items = new { type = "string" }, description = "Label filters" },
                    maxResults = new { type = "integer", description = "Maximum results to return" }
                }),
                Bind = (args, req) => { req.SearchRequest = BindSearch(args); },
                Handler = handler.GraphSearch
            });

            tools.Add(new ChatToolDefinition
            {
                Name = "graph/statistics",
                Description = "Gets node, edge, label, tag, and vector counts for a graph",
                RequestType = RequestTypeEnum.GraphStatistics,
                Schema = SchemaOf(new
                {
                    graphGuid = new { type = "string", description = "Graph GUID" }
                }, "graphGuid"),
                Bind = (args, req) => { req.GraphGUID = GetGuid(args, "graphGuid"); },
                Handler = handler.GraphStatistics
            });

            #endregion

            #region Node-Read

            tools.Add(new ChatToolDefinition
            {
                Name = "node/readallingraph",
                Description = "Lists all nodes in a graph",
                RequestType = RequestTypeEnum.NodeReadAllInGraph,
                Schema = SchemaOf(new
                {
                    graphGuid = new { type = "string", description = "Graph GUID" }
                }, "graphGuid"),
                Bind = (args, req) => { req.GraphGUID = GetGuid(args, "graphGuid"); },
                Handler = handler.NodeReadAllInGraph
            });

            tools.Add(new ChatToolDefinition
            {
                Name = "node/get",
                Description = "Reads a node by GUID",
                RequestType = RequestTypeEnum.NodeRead,
                Schema = SchemaOf(new
                {
                    graphGuid = new { type = "string", description = "Graph GUID" },
                    nodeGuid = new { type = "string", description = "Node GUID" },
                    includeData = new { type = "boolean", description = "Include node data" },
                    includeSubordinates = new { type = "boolean", description = "Include labels, tags, vectors" }
                }, "graphGuid", "nodeGuid"),
                Bind = (args, req) =>
                {
                    req.GraphGUID = GetGuid(args, "graphGuid");
                    req.NodeGUID = GetGuid(args, "nodeGuid");
                    req.IncludeData = GetBool(args, "includeData");
                    req.IncludeSubordinates = GetBool(args, "includeSubordinates");
                },
                Handler = handler.NodeRead
            });

            tools.Add(new ChatToolDefinition
            {
                Name = "node/search",
                Description = "Searches nodes in a graph by name and labels",
                RequestType = RequestTypeEnum.NodeSearch,
                Schema = SchemaOf(new
                {
                    graphGuid = new { type = "string", description = "Graph GUID" },
                    name = new { type = "string", description = "Node name filter" },
                    labels = new { type = "array", items = new { type = "string" }, description = "Label filters" },
                    maxResults = new { type = "integer", description = "Maximum results to return" }
                }, "graphGuid"),
                Bind = (args, req) =>
                {
                    req.GraphGUID = GetGuid(args, "graphGuid");
                    req.SearchRequest = BindSearch(args);
                },
                Handler = handler.NodeSearch
            });

            tools.Add(new ChatToolDefinition
            {
                Name = "node/neighbors",
                Description = "Lists the neighbors of a node",
                RequestType = RequestTypeEnum.NodeNeighbors,
                Schema = NodeTargetSchema(),
                Bind = BindNodeTarget,
                Handler = handler.NodeNeighbors
            });

            tools.Add(new ChatToolDefinition
            {
                Name = "node/children",
                Description = "Lists the child nodes of a node",
                RequestType = RequestTypeEnum.NodeChildren,
                Schema = NodeTargetSchema(),
                Bind = BindNodeTarget,
                Handler = handler.NodeChildren
            });

            tools.Add(new ChatToolDefinition
            {
                Name = "node/parents",
                Description = "Lists the parent nodes of a node",
                RequestType = RequestTypeEnum.NodeParents,
                Schema = NodeTargetSchema(),
                Bind = BindNodeTarget,
                Handler = handler.NodeParents
            });

            #endregion

            #region Edge-Read

            tools.Add(new ChatToolDefinition
            {
                Name = "edge/readallingraph",
                Description = "Lists all edges in a graph",
                RequestType = RequestTypeEnum.EdgeReadAllInGraph,
                Schema = SchemaOf(new
                {
                    graphGuid = new { type = "string", description = "Graph GUID" }
                }, "graphGuid"),
                Bind = (args, req) => { req.GraphGUID = GetGuid(args, "graphGuid"); },
                Handler = handler.EdgeReadAllInGraph
            });

            tools.Add(new ChatToolDefinition
            {
                Name = "edge/get",
                Description = "Reads an edge by GUID",
                RequestType = RequestTypeEnum.EdgeRead,
                Schema = SchemaOf(new
                {
                    graphGuid = new { type = "string", description = "Graph GUID" },
                    edgeGuid = new { type = "string", description = "Edge GUID" }
                }, "graphGuid", "edgeGuid"),
                Bind = (args, req) =>
                {
                    req.GraphGUID = GetGuid(args, "graphGuid");
                    req.EdgeGUID = GetGuid(args, "edgeGuid");
                },
                Handler = handler.EdgeRead
            });

            tools.Add(new ChatToolDefinition
            {
                Name = "edge/search",
                Description = "Searches edges in a graph by name and labels",
                RequestType = RequestTypeEnum.EdgeSearch,
                Schema = SchemaOf(new
                {
                    graphGuid = new { type = "string", description = "Graph GUID" },
                    name = new { type = "string", description = "Edge name filter" },
                    labels = new { type = "array", items = new { type = "string" }, description = "Label filters" },
                    maxResults = new { type = "integer", description = "Maximum results to return" }
                }, "graphGuid"),
                Bind = (args, req) =>
                {
                    req.GraphGUID = GetGuid(args, "graphGuid");
                    req.SearchRequest = BindSearch(args);
                },
                Handler = handler.EdgeSearch
            });

            tools.Add(new ChatToolDefinition
            {
                Name = "edge/betweennodes",
                Description = "Lists edges between two nodes",
                RequestType = RequestTypeEnum.EdgeBetween,
                Schema = SchemaOf(new
                {
                    graphGuid = new { type = "string", description = "Graph GUID" },
                    fromNodeGuid = new { type = "string", description = "Source node GUID" },
                    toNodeGuid = new { type = "string", description = "Destination node GUID" }
                }, "graphGuid", "fromNodeGuid", "toNodeGuid"),
                Bind = (args, req) =>
                {
                    req.GraphGUID = GetGuid(args, "graphGuid");
                    req.FromGUID = GetGuid(args, "fromNodeGuid");
                    req.ToGUID = GetGuid(args, "toNodeGuid");
                },
                Handler = handler.EdgesBetween
            });

            tools.Add(new ChatToolDefinition
            {
                Name = "edge/fromnode",
                Description = "Lists edges originating from a node",
                RequestType = RequestTypeEnum.EdgesFromNode,
                Schema = NodeTargetSchema(),
                Bind = BindNodeTarget,
                Handler = handler.EdgesFromNode
            });

            tools.Add(new ChatToolDefinition
            {
                Name = "edge/tonode",
                Description = "Lists edges terminating at a node",
                RequestType = RequestTypeEnum.EdgesToNode,
                Schema = NodeTargetSchema(),
                Bind = BindNodeTarget,
                Handler = handler.EdgesToNode
            });

            #endregion

            #region Vector-Search

            tools.Add(new ChatToolDefinition
            {
                Name = "vector/search",
                Description = "Semantic similarity search over graph vectors.  Provide natural-language text; the server embeds it and returns the most similar nodes with scores.",
                RequestType = RequestTypeEnum.VectorSearch,
                RequiresEmbedding = true,
                Schema = SchemaOf(new
                {
                    graphGuid = new { type = "string", description = "Graph GUID to search within" },
                    text = new { type = "string", description = "Natural-language text to search for" },
                    topK = new { type = "integer", description = "Number of results to return (default 8)" },
                    minScore = new { type = "number", description = "Minimum similarity score between -1 and 1" }
                }, "graphGuid", "text"),
                Bind = (args, req) =>
                {
                    VectorSearchRequest vsr = new VectorSearchRequest();
                    vsr.GraphGUID = GetGuid(args, "graphGuid");
                    req.GraphGUID = vsr.GraphGUID;
                    int? topK = GetInt(args, "topK");
                    if (topK != null) vsr.TopK = topK.Value;
                    double? minScore = GetDouble(args, "minScore");
                    if (minScore != null) vsr.MinimumScore = (float)minScore.Value;
                    req.VectorSearchRequest = vsr;
                },
                Handler = handler.VectorSearch
            });

            #endregion

            #region Label-Tag-Read

            tools.Add(new ChatToolDefinition
            {
                Name = "label/readallingraph",
                Description = "Lists all labels in a graph",
                RequestType = RequestTypeEnum.LabelReadAllInGraph,
                Schema = SchemaOf(new
                {
                    graphGuid = new { type = "string", description = "Graph GUID" }
                }, "graphGuid"),
                Bind = (args, req) => { req.GraphGUID = GetGuid(args, "graphGuid"); },
                Handler = handler.LabelReadAllInGraph
            });

            tools.Add(new ChatToolDefinition
            {
                Name = "label/readmanynode",
                Description = "Lists the labels attached to a node",
                RequestType = RequestTypeEnum.LabelReadManyNode,
                Schema = NodeTargetSchema(),
                Bind = BindNodeTarget,
                Handler = handler.LabelReadManyNode
            });

            tools.Add(new ChatToolDefinition
            {
                Name = "label/readmanyedge",
                Description = "Lists the labels attached to an edge",
                RequestType = RequestTypeEnum.LabelReadManyEdge,
                Schema = EdgeTargetSchema(),
                Bind = BindEdgeTarget,
                Handler = handler.LabelReadManyEdge
            });

            tools.Add(new ChatToolDefinition
            {
                Name = "tag/readallingraph",
                Description = "Lists all tags in a graph",
                RequestType = RequestTypeEnum.TagReadAllInGraph,
                Schema = SchemaOf(new
                {
                    graphGuid = new { type = "string", description = "Graph GUID" }
                }, "graphGuid"),
                Bind = (args, req) => { req.GraphGUID = GetGuid(args, "graphGuid"); },
                Handler = handler.TagReadAllInGraph
            });

            tools.Add(new ChatToolDefinition
            {
                Name = "tag/readmanynode",
                Description = "Lists the tags attached to a node",
                RequestType = RequestTypeEnum.TagReadManyNode,
                Schema = NodeTargetSchema(),
                Bind = BindNodeTarget,
                Handler = handler.TagReadManyNode
            });

            tools.Add(new ChatToolDefinition
            {
                Name = "tag/readmanyedge",
                Description = "Lists the tags attached to an edge",
                RequestType = RequestTypeEnum.TagReadManyEdge,
                Schema = EdgeTargetSchema(),
                Bind = BindEdgeTarget,
                Handler = handler.TagReadManyEdge
            });

            #endregion

            #region Mutations

            tools.Add(new ChatToolDefinition
            {
                Name = "graph/create",
                Description = "Creates a new graph in LiteGraph",
                RequestType = RequestTypeEnum.GraphCreate,
                Mutation = true,
                Schema = SchemaOf(new
                {
                    name = new { type = "string", description = "Graph name" }
                }, "name"),
                Bind = (args, req) => { req.Graph = new Graph { Name = GetString(args, "name") }; },
                Handler = handler.GraphCreate
            });

            tools.Add(new ChatToolDefinition
            {
                Name = "graph/update",
                Description = "Updates a graph.  Supply the full graph object; omitted fields are cleared.",
                RequestType = RequestTypeEnum.GraphUpdate,
                Mutation = true,
                Schema = SchemaOf(new
                {
                    graphGuid = new { type = "string", description = "Graph GUID" },
                    graph = new { type = "object", description = "Full graph object" }
                }, "graphGuid", "graph"),
                Bind = (args, req) =>
                {
                    req.GraphGUID = GetGuid(args, "graphGuid");
                    req.Graph = GetObject<Graph>(args, "graph");
                    if (req.Graph != null && req.GraphGUID != null) req.Graph.GUID = req.GraphGUID.Value;
                },
                Handler = handler.GraphUpdate
            });

            tools.Add(new ChatToolDefinition
            {
                Name = "graph/delete",
                Description = "Deletes a graph",
                RequestType = RequestTypeEnum.GraphDelete,
                Mutation = true,
                Schema = SchemaOf(new
                {
                    graphGuid = new { type = "string", description = "Graph GUID" },
                    force = new { type = "boolean", description = "Delete contained nodes and edges as well" }
                }, "graphGuid"),
                Bind = (args, req) =>
                {
                    req.GraphGUID = GetGuid(args, "graphGuid");
                    req.Force = GetBool(args, "force");
                },
                Handler = handler.GraphDelete
            });

            tools.Add(new ChatToolDefinition
            {
                Name = "node/create",
                Description = "Creates a node in a graph",
                RequestType = RequestTypeEnum.NodeCreate,
                Mutation = true,
                Schema = SchemaOf(new
                {
                    graphGuid = new { type = "string", description = "Graph GUID" },
                    node = new { type = "object", description = "Node object with name, data, labels, tags" }
                }, "graphGuid", "node"),
                Bind = (args, req) =>
                {
                    req.GraphGUID = GetGuid(args, "graphGuid");
                    req.Node = GetObject<Node>(args, "node");
                },
                Handler = handler.NodeCreate
            });

            tools.Add(new ChatToolDefinition
            {
                Name = "node/update",
                Description = "Updates a node.  Supply the full node object; omitted fields are cleared.",
                RequestType = RequestTypeEnum.NodeUpdate,
                Mutation = true,
                Schema = SchemaOf(new
                {
                    graphGuid = new { type = "string", description = "Graph GUID" },
                    nodeGuid = new { type = "string", description = "Node GUID" },
                    node = new { type = "object", description = "Full node object" }
                }, "graphGuid", "nodeGuid", "node"),
                Bind = (args, req) =>
                {
                    req.GraphGUID = GetGuid(args, "graphGuid");
                    req.NodeGUID = GetGuid(args, "nodeGuid");
                    req.Node = GetObject<Node>(args, "node");
                    if (req.Node != null && req.NodeGUID != null) req.Node.GUID = req.NodeGUID.Value;
                },
                Handler = handler.NodeUpdate
            });

            tools.Add(new ChatToolDefinition
            {
                Name = "node/delete",
                Description = "Deletes a node",
                RequestType = RequestTypeEnum.NodeDelete,
                Mutation = true,
                Schema = NodeTargetSchema(),
                Bind = BindNodeTarget,
                Handler = handler.NodeDelete
            });

            tools.Add(new ChatToolDefinition
            {
                Name = "edge/create",
                Description = "Creates an edge between two nodes",
                RequestType = RequestTypeEnum.EdgeCreate,
                Mutation = true,
                Schema = SchemaOf(new
                {
                    graphGuid = new { type = "string", description = "Graph GUID" },
                    edge = new { type = "object", description = "Edge object with from, to, name, cost, data, labels, tags" }
                }, "graphGuid", "edge"),
                Bind = (args, req) =>
                {
                    req.GraphGUID = GetGuid(args, "graphGuid");
                    req.Edge = GetObject<Edge>(args, "edge");
                },
                Handler = handler.EdgeCreate
            });

            tools.Add(new ChatToolDefinition
            {
                Name = "edge/update",
                Description = "Updates an edge.  Supply the full edge object; omitted fields are cleared.",
                RequestType = RequestTypeEnum.EdgeUpdate,
                Mutation = true,
                Schema = SchemaOf(new
                {
                    graphGuid = new { type = "string", description = "Graph GUID" },
                    edgeGuid = new { type = "string", description = "Edge GUID" },
                    edge = new { type = "object", description = "Full edge object" }
                }, "graphGuid", "edgeGuid", "edge"),
                Bind = (args, req) =>
                {
                    req.GraphGUID = GetGuid(args, "graphGuid");
                    req.EdgeGUID = GetGuid(args, "edgeGuid");
                    req.Edge = GetObject<Edge>(args, "edge");
                    if (req.Edge != null && req.EdgeGUID != null) req.Edge.GUID = req.EdgeGUID.Value;
                },
                Handler = handler.EdgeUpdate
            });

            tools.Add(new ChatToolDefinition
            {
                Name = "edge/delete",
                Description = "Deletes an edge",
                RequestType = RequestTypeEnum.EdgeDelete,
                Mutation = true,
                Schema = EdgeTargetSchema(),
                Bind = BindEdgeTarget,
                Handler = handler.EdgeDelete
            });

            #endregion

            return tools;
        }

        #endregion

        #region Private-Methods

        private static Serializer _Serializer = new Serializer();

        private static object SchemaOf(object properties, params string[] required)
        {
            if (required != null && required.Length > 0)
            {
                return new { type = "object", properties = properties, required = required };
            }

            return new { type = "object", properties = properties };
        }

        private static object NodeTargetSchema()
        {
            return SchemaOf(new
            {
                graphGuid = new { type = "string", description = "Graph GUID" },
                nodeGuid = new { type = "string", description = "Node GUID" }
            }, "graphGuid", "nodeGuid");
        }

        private static object EdgeTargetSchema()
        {
            return SchemaOf(new
            {
                graphGuid = new { type = "string", description = "Graph GUID" },
                edgeGuid = new { type = "string", description = "Edge GUID" }
            }, "graphGuid", "edgeGuid");
        }

        private static void BindNodeTarget(JsonElement? args, RequestContext req)
        {
            req.GraphGUID = GetGuid(args, "graphGuid");
            req.NodeGUID = GetGuid(args, "nodeGuid");
        }

        private static void BindEdgeTarget(JsonElement? args, RequestContext req)
        {
            req.GraphGUID = GetGuid(args, "graphGuid");
            req.EdgeGUID = GetGuid(args, "edgeGuid");
        }

        private static SearchRequest BindSearch(JsonElement? args)
        {
            SearchRequest search = new SearchRequest();
            string name = GetString(args, "name");
            if (!String.IsNullOrEmpty(name)) search.Name = name;
            List<string> labels = GetStringList(args, "labels");
            if (labels != null && labels.Count > 0) search.Labels = labels;
            int? maxResults = GetInt(args, "maxResults");
            if (maxResults != null) search.MaxResults = maxResults.Value;
            return search;
        }

        private static Guid? GetGuid(JsonElement? args, string name)
        {
            string val = GetString(args, name);
            if (String.IsNullOrEmpty(val)) return null;
            return Guid.Parse(val);
        }

        private static string GetString(JsonElement? args, string name)
        {
            if (args == null) return null;
            if (!args.Value.TryGetProperty(name, out JsonElement prop)) return null;
            if (prop.ValueKind != JsonValueKind.String) return null;
            return prop.GetString();
        }

        private static bool GetBool(JsonElement? args, string name)
        {
            if (args == null) return false;
            if (!args.Value.TryGetProperty(name, out JsonElement prop)) return false;
            return (prop.ValueKind == JsonValueKind.True);
        }

        private static int? GetInt(JsonElement? args, string name)
        {
            if (args == null) return null;
            if (!args.Value.TryGetProperty(name, out JsonElement prop)) return null;
            if (prop.ValueKind != JsonValueKind.Number) return null;
            return prop.GetInt32();
        }

        private static double? GetDouble(JsonElement? args, string name)
        {
            if (args == null) return null;
            if (!args.Value.TryGetProperty(name, out JsonElement prop)) return null;
            if (prop.ValueKind != JsonValueKind.Number) return null;
            return prop.GetDouble();
        }

        private static List<string> GetStringList(JsonElement? args, string name)
        {
            if (args == null) return null;
            if (!args.Value.TryGetProperty(name, out JsonElement prop)) return null;
            if (prop.ValueKind != JsonValueKind.Array) return null;
            return prop.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => e.GetString())
                .Where(s => !String.IsNullOrEmpty(s))
                .ToList();
        }

        private static T GetObject<T>(JsonElement? args, string name) where T : class
        {
            if (args == null) return null;
            if (!args.Value.TryGetProperty(name, out JsonElement prop)) return null;
            if (prop.ValueKind != JsonValueKind.Object) return null;
            return _Serializer.DeserializeJson<T>(prop.GetRawText());
        }

        #endregion
    }
}
