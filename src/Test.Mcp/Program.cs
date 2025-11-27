namespace Test.Mcp
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using ExpressionTree;
    using GetSomeInput;
    using LiteGraph;
    using LiteGraph.Sdk;
    using Voltaic;

    class Program
    {
        static bool _RunForever = true;
        static bool _Debug = false;
        static McpHttpClient _McpClient = null;
        static string _McpServerUrl = "http://localhost:8200";
        static Guid _TenantGuid = Guid.Parse("00000000-0000-0000-0000-000000000000");
        static Guid _GraphGuid = Guid.Parse("00000000-0000-0000-0000-000000000000");

        static Task Main(string[] args)
        {
            return MainAsync(args, CancellationToken.None);
        }

        static async Task MainAsync(string[] args, CancellationToken token = default)
        {
            Console.WriteLine("LiteGraph MCP Server Test Console");
            Console.WriteLine("==================================");
            Console.WriteLine("");

            _McpClient = new McpHttpClient();
            _McpClient.Log += (sender, msg) =>
            {
                if (_Debug)
                {
                    Console.WriteLine("[MCP] " + msg);
                }
            };

            // Try to connect to MCP server
            Console.WriteLine($"Connecting to MCP server at {_McpServerUrl}...");
            bool connected = await _McpClient.ConnectAsync(_McpServerUrl, "/rpc", "/events", token).ConfigureAwait(false);
            
            if (!connected)
            {
                Console.WriteLine($"Failed to connect to MCP server at {_McpServerUrl}");
                Console.WriteLine("Make sure the MCP server is running.");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }

            Console.WriteLine("Connected successfully!");
            Console.WriteLine("");

            while (_RunForever)
            {
                string userInput = Inputty.GetString("Command [? for help]:", null, false);

                if (userInput.Equals("?")) Menu();
                else if (userInput.Equals("q")) _RunForever = false;
                else if (userInput.Equals("cls")) Console.Clear();
                else if (userInput.Equals("backup")) await BackupDatabase(token).ConfigureAwait(false);
                else if (userInput.Equals("debug")) ToggleDebug();
                else if (userInput.Equals("tenant")) await SetTenant().ConfigureAwait(false);
                else if (userInput.Equals("graph")) await SetGraph().ConfigureAwait(false);
                else if (userInput.Equals("load1")) await LoadGraph1(token).ConfigureAwait(false);
                else if (userInput.Equals("load2"))
                {
                    try
                    {
                        await LoadGraph2(token).ConfigureAwait(false);
                        Console.WriteLine("LoadGraph2 completed successfully");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in LoadGraph2: {ex.Message}");
                        Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    }
                }
                else if (userInput.Equals("route")) await FindRoutes(token).ConfigureAwait(false);
                else if (userInput.Equals("test1-1")) await Test1_1(token).ConfigureAwait(false);
                else if (userInput.Equals("test1-2")) await Test1_2(token).ConfigureAwait(false);
                else if (userInput.Equals("test1-3")) await Test1_3(token).ConfigureAwait(false);
                else if (userInput.Equals("test1-4")) await Test1_4(token).ConfigureAwait(false);
                else if (userInput.Equals("test2-1")) await Test2_1(token).ConfigureAwait(false);
                else if (userInput.Equals("test3-1")) await Test3_1(token).ConfigureAwait(false);
                else if (userInput.Equals("test3-2")) await Test3_2(token).ConfigureAwait(false);
                else if (userInput.Equals("test3-3")) await Test3_3(token).ConfigureAwait(false);
                else if (userInput.Equals("subgraph")) await TestSubgraph(token).ConfigureAwait(false);
                else
                {
                    string[] parts = userInput.Split(new char[] { ' ' });

                    if (parts.Length == 2)
                    {
                        if (parts[0].Equals("tenant")
                            || parts[0].Equals("graph")
                            || parts[0].Equals("user")
                            || parts[0].Equals("cred")
                            || parts[0].Equals("label")
                            || parts[0].Equals("tag")
                            || parts[0].Equals("node")
                            || parts[0].Equals("edge"))
                        {
                            if (parts[1].Equals("create")) await Create(parts[0], token).ConfigureAwait(false);
                            else if (parts[1].Equals("all")) await All(parts[0], token).ConfigureAwait(false);
                            else if (parts[1].Equals("read")) await Read(parts[0], token).ConfigureAwait(false);
                            else if (parts[1].Equals("exists")) await Exists(parts[0], token).ConfigureAwait(false);
                            else if (parts[1].Equals("update")) await Update(parts[0], token).ConfigureAwait(false);
                            else if (parts[1].Equals("delete")) await Delete(parts[0], token).ConfigureAwait(false);
                            else if (parts[1].Equals("search")) await Search(parts[0], token).ConfigureAwait(false);

                            if (parts[0].Equals("node"))
                            {
                                if (parts[1].Equals("edgesto")) await NodeEdgesTo(token).ConfigureAwait(false);
                                else if (parts[1].Equals("edgesfrom")) await NodeEdgesFrom(token).ConfigureAwait(false);
                                else if (parts[1].Equals("edgesbetween")) await NodeEdgesBetween(token).ConfigureAwait(false);
                                else if (parts[1].Equals("parents")) await NodeParents(token).ConfigureAwait(false);
                                else if (parts[1].Equals("children")) await NodeChildren(token).ConfigureAwait(false);
                                else if (parts[1].Equals("neighbors")) await NodeNeighbors(token).ConfigureAwait(false);
                                else if (parts[1].Equals("mostconnected")) await NodeMostConnected(token).ConfigureAwait(false);
                                else if (parts[1].Equals("leastconnected")) await NodeLeastConnected(token).ConfigureAwait(false);
                            }
                        }
                    }
                }
            }

            // Cleanup on exit
            CleanupTestAssets();
        }
        
        static void CleanupTestAssets()
        {
            Console.WriteLine("\n[CLEANUP] Cleaning up test assets...");
            
            try
            {
                // Dispose the MCP client
                _McpClient?.Dispose();
                _McpClient = null;
                
                Console.WriteLine("[OK] Cleanup completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] Error during cleanup: {ex.Message}");
            }
        }

        static List<string> CombineLists(List<string> list1, List<string> list2)
        {
            if (list1 == null) return list2;
            if (list2 == null) return list1;
            List<string> ret = new List<string>();
            ret.AddRange(list1);
            ret.AddRange(list2);
            return ret;
        }

        static NameValueCollection CombineNvc(NameValueCollection nvc1, NameValueCollection nvc2)
        {
            if (nvc1 == null) return nvc2;
            if (nvc2 == null) return nvc1;
            NameValueCollection nvc = new NameValueCollection();
            nvc.Add(nvc1);
            nvc.Add(nvc2);
            return nvc;
        }

        static void Menu()
        {
            Console.WriteLine("");
            Console.WriteLine("Available commands:");
            Console.WriteLine("  ?               help, this menu");
            Console.WriteLine("  q               quit");
            Console.WriteLine("  cls             clear the screen");
            Console.WriteLine("  debug           enable or disable debug (enabled: " + _Debug + ")");
            Console.WriteLine("  backup          backup database to a file");
            Console.WriteLine("");
            Console.WriteLine("  tenant          set the tenant GUID (currently " + _TenantGuid + ")");
            Console.WriteLine("  graph           set the graph GUID (currently " + _GraphGuid + ")");
            Console.WriteLine("  load1           load sample graph 1");
            Console.WriteLine("  load2           load sample graph 2");
            Console.WriteLine("  route           find routes between two nodes");
            Console.WriteLine("");
            Console.WriteLine("  test1-1         using sample graph 1, validate retrieval by labels");
            Console.WriteLine("  test1-2         using sample graph 1, validate retrieval by tags");
            Console.WriteLine("  test1-3         using sample graph 1, validate retrieval by labels and tags");
            Console.WriteLine("  test1-4         using sample graph 1, validate retrieval by vectors");
            Console.WriteLine("  test2-1         using sample graph 2, validate node retrieval by properties");
            Console.WriteLine("  test3-1         create test tenant, graph, and node using anonymous data");
            Console.WriteLine("  test3-2         create test tenant, graph, and node using var");
            Console.WriteLine("  subgraph        test subgraph retrieval from a starting node");
            Console.WriteLine("");
            Console.WriteLine("  [type] [cmd]    execute a command against a given type");
            Console.WriteLine("  where:");
            Console.WriteLine("    [type] : tenant graph node edge user cred");
            Console.WriteLine("    [cmd]  : create all read exists update delete search");
            Console.WriteLine("");
            Console.WriteLine("  For node operations, additional commands are available");
            Console.WriteLine("    edgesto    edgesfrom   edgesbetween   mostconnected");
            Console.WriteLine("    parents    children    neighbors      leastconnected");
            Console.WriteLine("");
        }

        static void Logger(SeverityEnum sev, string msg)
        {
            if (!String.IsNullOrEmpty(msg))
            {
                Console.WriteLine(sev.ToString() + " " + msg);
            }
        }

        static async Task BackupDatabase(CancellationToken token = default)
        {
            string filename = Inputty.GetString("Backup filename:", null, true);
            if (String.IsNullOrEmpty(filename)) return;
            try
            {
                await _McpClient.CallAsync<string>("admin/backup", new { outputFilename = filename }, 30000, token).ConfigureAwait(false);
                Console.WriteLine("Backup created successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }

        static void ToggleDebug()
        {
            _Debug = !_Debug;
            Console.WriteLine("Debug: " + _Debug);
        }

        static Task SetTenant()
        {
            _TenantGuid = Inputty.GetGuid("Tenant GUID:", _TenantGuid);
            return Task.CompletedTask;
        }

        static Task SetGraph()
        {
            _GraphGuid = Inputty.GetGuid("Graph GUID:", _GraphGuid);
            return Task.CompletedTask;
        }

        #region Graph-1

        static async Task LoadGraph1(CancellationToken token = default)
        {
            #region Tenant

            Console.WriteLine("| Creating tenant");
            string tenantResult = await _McpClient.CallAsync<string>("tenant/create", new { name = "Test tenant" }, 30000, token).ConfigureAwait(false);
            TenantMetadata tenant = Serializer.DeserializeJson<TenantMetadata>(tenantResult);
            Console.WriteLine("| Tenant created: " + tenant.GUID);

            #endregion

            #region Labels

            List<string> labelsGraph = new List<string> { "graph" };
            List<string> labelsOdd = new List<string> { "odd" };
            List<string> labelsEven = new List<string> { "even" };
            List<string> labelsNode = new List<string> { "node" };
            List<string> labelsEdge = new List<string> { "edge" };

            List<float> embeddings1 = new List<float> { 0.1f, 0.2f, 0.3f };
            List<float> embeddings2 = new List<float> { 0.05f, -0.25f, 0.45f };
            List<float> embeddings3 = new List<float> { -0.2f, 0.3f, -0.4f };
            List<float> embeddings4 = new List<float> { 0.25f, -0.5f, -0.75f };

            #endregion

            #region Tags

            NameValueCollection tagsGraph = new NameValueCollection();
            tagsGraph.Add("type", "graph");

            NameValueCollection tagsNode = new NameValueCollection();
            tagsNode.Add("type", "node");

            NameValueCollection tagsEdge = new NameValueCollection();
            tagsEdge.Add("type", "edge");

            NameValueCollection tagsEven = new NameValueCollection();
            tagsEven.Add("isEven", "true");

            NameValueCollection tagsOdd = new NameValueCollection();
            tagsOdd.Add("isEven", "false");

            #endregion

            #region Graph

            Console.WriteLine("| Creating graph");
            string graphResult = await _McpClient.CallAsync<string>("graph/create", new { tenantGuid = tenant.GUID.ToString(), name = "Sample Graph 1" }, 30000, token).ConfigureAwait(false);
            Graph graph = Serializer.DeserializeJson<Graph>(graphResult);
            Console.WriteLine("| Graph created: " + graph.GUID);

            List<VectorMetadata> graphVectors = new List<VectorMetadata>
            {
                new VectorMetadata
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Model = "testmodel",
                    Dimensionality = 3,
                    Content = "testcontent",
                    Vectors = embeddings1                    
                }
            };

            Graph graphObj = new Graph
            {
                TenantGUID = tenant.GUID,
                GUID = graph.GUID,
                Name = graph.Name,
                Labels = labelsGraph,
                Tags = tagsGraph,
                Vectors = graphVectors
            };
            
            string graphJson = Serializer.SerializeJson(graphObj);
            string graphUpdateResult = await _McpClient.CallAsync<string>("graph/update", new { graph = graphJson }, 30000, token).ConfigureAwait(false);
            graph = Serializer.DeserializeJson<Graph>(graphUpdateResult);

            #endregion

            #region Nodes

            Guid node1Guid = Guid.NewGuid();

            Node n1Obj = new Node
            {
                GUID = node1Guid,
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "1",
                Labels = CombineLists(labelsOdd, labelsNode),
                Tags = CombineNvc(tagsOdd, tagsNode),
                Vectors = new List<VectorMetadata> 
                { 
                    new VectorMetadata
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graph.GUID,
                        NodeGUID = node1Guid,
                        Model = "testmodel",
                        Dimensionality = 3,
                        Content = "testcontent",
                        Vectors = embeddings2
                    }
                }
            };

            string node1Result = await _McpClient.CallAsync<string>("node/create", new { tenantGuid = tenant.GUID.ToString(), graphGuid = graph.GUID.ToString(), name = n1Obj.Name }, 30000, token).ConfigureAwait(false);
            Node n1 = Serializer.DeserializeJson<Node>(node1Result);
            
            n1Obj.GUID = n1.GUID; 
            string node1Json = Serializer.SerializeJson(n1Obj);
            string node1UpdateResult = await _McpClient.CallAsync<string>("node/update", new { node = node1Json }, 30000, token).ConfigureAwait(false);
            n1 = Serializer.DeserializeJson<Node>(node1UpdateResult);
            Console.WriteLine("| Creating node 1 " + n1.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Guid node2Guid = Guid.NewGuid();

            Node n2Obj = new Node
            {
                GUID = node2Guid,
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "2",
                Labels = CombineLists(labelsEven, labelsNode),
                Tags = CombineNvc(tagsEven, tagsNode),
                Vectors = new List<VectorMetadata>
                {
                    new VectorMetadata
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graph.GUID,
                        NodeGUID = node2Guid,
                        Model = "testmodel",
                        Dimensionality = 3,
                        Content = "testcontent",
                        Vectors = embeddings3
                    }
                }
            };

            // First create the node with just tenantGuid, graphGuid, and name
            string node2Result = await _McpClient.CallAsync<string>("node/create", new { tenantGuid = tenant.GUID.ToString(), graphGuid = graph.GUID.ToString(), name = n2Obj.Name }, 30000, token).ConfigureAwait(false);
            Node n2 = Serializer.DeserializeJson<Node>(node2Result);
            
            // Update the node with labels, tags, vectors, and the pre-generated GUID
            n2Obj.GUID = n2.GUID; // Use the GUID from the created node
            string node2Json = Serializer.SerializeJson(n2Obj);
            string node2UpdateResult = await _McpClient.CallAsync<string>("node/update", new { node = node2Json }, 30000, token).ConfigureAwait(false);
            n2 = Serializer.DeserializeJson<Node>(node2UpdateResult);
            Console.WriteLine("| Creating node 2 " + n2.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Guid node3Guid = Guid.NewGuid();

            Node n3Obj = new Node
            {
                GUID = node3Guid,
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "3",
                Labels = CombineLists(labelsOdd, labelsNode),
                Tags = CombineNvc(tagsOdd, tagsNode),
                Vectors = new List<VectorMetadata>
                {
                    new VectorMetadata
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graph.GUID,
                        NodeGUID = node3Guid,
                        Model = "testmodel",
                        Dimensionality = 3,
                        Content = "testcontent",
                        Vectors = embeddings4
                    }
                }
            };

            // First create the node with just tenantGuid, graphGuid, and name
            string node3Result = await _McpClient.CallAsync<string>("node/create", new { tenantGuid = tenant.GUID.ToString(), graphGuid = graph.GUID.ToString(), name = n3Obj.Name }, 30000, token).ConfigureAwait(false);
            Node n3 = Serializer.DeserializeJson<Node>(node3Result);
            
            // Update the node with labels, tags, vectors, and the pre-generated GUID
            n3Obj.GUID = n3.GUID; // Use the GUID from the created node
            string node3Json = Serializer.SerializeJson(n3Obj);
            string node3UpdateResult = await _McpClient.CallAsync<string>("node/update", new { node = node3Json }, 30000, token).ConfigureAwait(false);
            n3 = Serializer.DeserializeJson<Node>(node3UpdateResult);
            Console.WriteLine("| Creating node 3 " + n3.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Guid node4Guid = Guid.NewGuid();

            Node n4Obj = new Node
            {
                GUID = node4Guid,
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "4",
                Labels = CombineLists(labelsEven, labelsNode),
                Tags = CombineNvc(tagsEven, tagsNode),
                Vectors = new List<VectorMetadata>
                {
                    new VectorMetadata
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graph.GUID,
                        NodeGUID = node4Guid,
                        Model = "testmodel",
                        Dimensionality = 3,
                        Content = "testcontent",
                        Vectors = embeddings1
                    }
                }
            };

            // First create the node with just tenantGuid, graphGuid, and name
            string node4Result = await _McpClient.CallAsync<string>("node/create", new { tenantGuid = tenant.GUID.ToString(), graphGuid = graph.GUID.ToString(), name = n4Obj.Name }, 30000, token).ConfigureAwait(false);
            Node n4 = Serializer.DeserializeJson<Node>(node4Result);
            
            // Update the node with labels, tags, vectors, and the pre-generated GUID
            n4Obj.GUID = n4.GUID; // Use the GUID from the created node
            string node4Json = Serializer.SerializeJson(n4Obj);
            string node4UpdateResult = await _McpClient.CallAsync<string>("node/update", new { node = node4Json }, 30000, token).ConfigureAwait(false);
            n4 = Serializer.DeserializeJson<Node>(node4UpdateResult);
            Console.WriteLine("| Creating node 4 " + n4.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Guid node5Guid = Guid.NewGuid();

            Node n5Obj = new Node
            {
                GUID = node5Guid,
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "5",
                Labels = CombineLists(labelsOdd, labelsNode),
                Tags = CombineNvc(tagsOdd, tagsNode),
                Vectors = new List<VectorMetadata>
                {
                    new VectorMetadata
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graph.GUID,
                        NodeGUID = node5Guid,
                        Model = "testmodel",
                        Dimensionality = 3,
                        Content = "testcontent",
                        Vectors = embeddings2
                    }
                }
            };

            // First create the node with just tenantGuid, graphGuid, and name
            string node5Result = await _McpClient.CallAsync<string>("node/create", new { tenantGuid = tenant.GUID.ToString(), graphGuid = graph.GUID.ToString(), name = n5Obj.Name }, 30000, token).ConfigureAwait(false);
            Node n5 = Serializer.DeserializeJson<Node>(node5Result);
            
            // Update the node with labels, tags, vectors, and the pre-generated GUID
            n5Obj.GUID = n5.GUID; // Use the GUID from the created node
            string node5Json = Serializer.SerializeJson(n5Obj);
            string node5UpdateResult = await _McpClient.CallAsync<string>("node/update", new { node = node5Json }, 30000, token).ConfigureAwait(false);
            n5 = Serializer.DeserializeJson<Node>(node5UpdateResult);
            Console.WriteLine("| Creating node 5 " + n5.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Guid node6Guid = Guid.NewGuid();

            Node n6Obj = new Node
            {
                GUID = node6Guid,
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "6",
                Labels = CombineLists(labelsEven, labelsNode),
                Tags = CombineNvc(tagsEven, tagsNode),
                Vectors = new List<VectorMetadata>
                {
                    new VectorMetadata
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graph.GUID,
                        NodeGUID = node6Guid,
                        Model = "testmodel",
                        Dimensionality = 3,
                        Content = "testcontent",
                        Vectors = embeddings3
                    }
                }
            };

            // First create the node with just tenantGuid, graphGuid, and name
            string node6Result = await _McpClient.CallAsync<string>("node/create", new { tenantGuid = tenant.GUID.ToString(), graphGuid = graph.GUID.ToString(), name = n6Obj.Name }, 30000, token).ConfigureAwait(false);
            Node n6 = Serializer.DeserializeJson<Node>(node6Result);
            
            // Update the node with labels, tags, vectors, and the pre-generated GUID
            n6Obj.GUID = n6.GUID; // Use the GUID from the created node
            string node6Json = Serializer.SerializeJson(n6Obj);
            string node6UpdateResult = await _McpClient.CallAsync<string>("node/update", new { node = node6Json }, 30000, token).ConfigureAwait(false);
            n6 = Serializer.DeserializeJson<Node>(node6UpdateResult);
            Console.WriteLine("| Creating node 6 " + n6.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Guid node7Guid = Guid.NewGuid();

            Node n7Obj = new Node
            {
                GUID = node7Guid,
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "7",
                Labels = CombineLists(labelsOdd, labelsNode),
                Tags = CombineNvc(tagsOdd, tagsNode),
                Vectors = new List<VectorMetadata>
                {
                    new VectorMetadata
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graph.GUID,
                        NodeGUID = node7Guid,
                        Model = "testmodel",
                        Dimensionality = 3,
                        Content = "testcontent",
                        Vectors = embeddings4
                    }
                }
            };

            // First create the node with just tenantGuid, graphGuid, and name
            string node7Result = await _McpClient.CallAsync<string>("node/create", new { tenantGuid = tenant.GUID.ToString(), graphGuid = graph.GUID.ToString(), name = n7Obj.Name }, 30000, token).ConfigureAwait(false);
            Node n7 = Serializer.DeserializeJson<Node>(node7Result);
            
            // Update the node with labels, tags, vectors, and the pre-generated GUID
            n7Obj.GUID = n7.GUID; // Use the GUID from the created node
            string node7Json = Serializer.SerializeJson(n7Obj);
            string node7UpdateResult = await _McpClient.CallAsync<string>("node/update", new { node = node7Json }, 30000, token).ConfigureAwait(false);
            n7 = Serializer.DeserializeJson<Node>(node7UpdateResult);
            Console.WriteLine("| Creating node 7 " + n7.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Guid node8Guid = Guid.NewGuid();

            Node n8Obj = new Node
            {
                GUID = node8Guid,
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "8",
                Labels = CombineLists(labelsEven, labelsNode),
                Tags = CombineNvc(tagsEven, tagsNode),
                Vectors = new List<VectorMetadata>
                {
                    new VectorMetadata
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graph.GUID,
                        NodeGUID = node8Guid,
                        Model = "testmodel",
                        Dimensionality = 3,
                        Content = "testcontent",
                        Vectors = embeddings1
                    }
                }
            };

            // First create the node with just tenantGuid, graphGuid, and name
            string node8Result = await _McpClient.CallAsync<string>("node/create", new { tenantGuid = tenant.GUID.ToString(), graphGuid = graph.GUID.ToString(), name = n8Obj.Name }, 30000, token).ConfigureAwait(false);
            Node n8 = Serializer.DeserializeJson<Node>(node8Result);
            
            // Update the node with labels, tags, vectors, and the pre-generated GUID
            n8Obj.GUID = n8.GUID; // Use the GUID from the created node
            string node8Json = Serializer.SerializeJson(n8Obj);
            string node8UpdateResult = await _McpClient.CallAsync<string>("node/update", new { node = node8Json }, 30000, token).ConfigureAwait(false);
            n8 = Serializer.DeserializeJson<Node>(node8UpdateResult);
            Console.WriteLine("| Creating node 8 " + n8.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            #endregion

            #region Edges

            Edge e1Obj = new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n1.GUID,
                To = n4.GUID,
                Name = "1 to 4",
                Cost = 1,
                Labels = CombineLists(labelsOdd, labelsEdge),
                Tags = CombineNvc(tagsOdd, tagsEdge)
            };
            string e1Json = Serializer.SerializeJson(e1Obj);
            string e1Result = await _McpClient.CallAsync<string>("edge/create", new { edge = e1Json }, 30000, token).ConfigureAwait(false);
            Edge e1 = Serializer.DeserializeJson<Edge>(e1Result);
            Console.WriteLine("| Creating edge " + e1.Name + " " + e1.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Edge e2Obj = new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n1.GUID,
                To = n5.GUID,
                Name = "1 to 5",
                Cost = 2,
                Labels = CombineLists(labelsEven, labelsEdge),
                Tags = CombineNvc(tagsEven, tagsEdge)
            };
            string e2Json = Serializer.SerializeJson(e2Obj);
            string e2Result = await _McpClient.CallAsync<string>("edge/create", new { edge = e2Json }, 30000, token).ConfigureAwait(false);
            Edge e2 = Serializer.DeserializeJson<Edge>(e2Result);
            Console.WriteLine("| Creating edge " + e2.Name + " " + e2.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Edge e3Obj = new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n2.GUID,
                To = n4.GUID,
                Name = "2 to 4",
                Cost = 3,
                Labels = CombineLists(labelsOdd, labelsEdge),
                Tags = CombineNvc(tagsOdd, tagsEdge)
            };
            string e3Json = Serializer.SerializeJson(e3Obj);
            string e3Result = await _McpClient.CallAsync<string>("edge/create", new { edge = e3Json }, 30000, token).ConfigureAwait(false);
            Edge e3 = Serializer.DeserializeJson<Edge>(e3Result);
            Console.WriteLine("| Creating edge " + e3.Name + " " + e3.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Edge e4Obj = new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n2.GUID,
                To = n5.GUID,
                Name = "2 to 5",
                Cost = 4,
                Labels = CombineLists(labelsEven, labelsEdge),
                Tags = CombineNvc(tagsEven, tagsEdge)
            };
            string e4Json = Serializer.SerializeJson(e4Obj);
            string e4Result = await _McpClient.CallAsync<string>("edge/create", new { edge = e4Json }, 30000, token).ConfigureAwait(false);
            Edge e4 = Serializer.DeserializeJson<Edge>(e4Result);
            Console.WriteLine("| Creating edge " + e4.Name + " " + e4.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Edge e5Obj = new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n3.GUID,
                To = n4.GUID,
                Name = "3 to 4",
                Cost = 5,
                Labels = CombineLists(labelsOdd, labelsEdge),
                Tags = CombineNvc(tagsOdd, tagsEdge)
            };
            string e5Json = Serializer.SerializeJson(e5Obj);
            string e5Result = await _McpClient.CallAsync<string>("edge/create", new { edge = e5Json }, 30000, token).ConfigureAwait(false);
            Edge e5 = Serializer.DeserializeJson<Edge>(e5Result);
            Console.WriteLine("| Creating edge " + e5.Name + " " + e5.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Edge e6Obj = new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n3.GUID,
                To = n5.GUID,
                Name = "3 to 5",
                Cost = 6,
                Labels = CombineLists(labelsEven, labelsEdge),
                Tags = CombineNvc(tagsEven, tagsEdge)
            };
            string e6Json = Serializer.SerializeJson(e6Obj);
            string e6Result = await _McpClient.CallAsync<string>("edge/create", new { edge = e6Json }, 30000, token).ConfigureAwait(false);
            Edge e6 = Serializer.DeserializeJson<Edge>(e6Result);
            Console.WriteLine("| Creating edge " + e6.Name + " " + e6.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Edge e7Obj = new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n4.GUID,
                To = n6.GUID,
                Name = "4 to 6",
                Cost = 7,
                Labels = CombineLists(labelsOdd, labelsEdge),
                Tags = CombineNvc(tagsOdd, tagsEdge)
            };
            string e7Json = Serializer.SerializeJson(e7Obj);
            string e7Result = await _McpClient.CallAsync<string>("edge/create", new { edge = e7Json }, 30000, token).ConfigureAwait(false);
            Edge e7 = Serializer.DeserializeJson<Edge>(e7Result);
            Console.WriteLine("| Creating edge " + e7.Name + " " + e7.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Edge e8Obj = new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n4.GUID,
                To = n7.GUID,
                Name = "4 to 7",
                Cost = 8,
                Labels = CombineLists(labelsEven, labelsEdge),
                Tags = CombineNvc(tagsEven, tagsEdge)
            };
            string e8Json = Serializer.SerializeJson(e8Obj);
            string e8Result = await _McpClient.CallAsync<string>("edge/create", new { edge = e8Json }, 30000, token).ConfigureAwait(false);
            Edge e8 = Serializer.DeserializeJson<Edge>(e8Result);
            Console.WriteLine("| Creating edge " + e8.Name + " " + e8.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Edge e9Obj = new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n4.GUID,
                To = n8.GUID,
                Name = "4 to 8",
                Cost = 9,
                Labels = CombineLists(labelsOdd, labelsEdge),
                Tags = CombineNvc(tagsOdd, tagsEdge)
            };
            string e9Json = Serializer.SerializeJson(e9Obj);
            string e9Result = await _McpClient.CallAsync<string>("edge/create", new { edge = e9Json }, 30000, token).ConfigureAwait(false);
            Edge e9 = Serializer.DeserializeJson<Edge>(e9Result);
            Console.WriteLine("| Creating edge " + e9.Name + " " + e9.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Edge e10Obj = new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n5.GUID,
                To = n6.GUID,
                Name = "5 to 6",
                Cost = 10,
                Labels = CombineLists(labelsEven, labelsEdge),
                Tags = CombineNvc(tagsEven, tagsEdge)
            };
            string e10Json = Serializer.SerializeJson(e10Obj);
            string e10Result = await _McpClient.CallAsync<string>("edge/create", new { edge = e10Json }, 30000, token).ConfigureAwait(false);
            Edge e10 = Serializer.DeserializeJson<Edge>(e10Result);
            Console.WriteLine("| Creating edge " + e10.Name + " " + e10.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Edge e11Obj = new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n5.GUID,
                To = n7.GUID,
                Name = "5 to 7",
                Cost = 11,
                Labels = CombineLists(labelsOdd, labelsEdge),
                Tags = CombineNvc(tagsOdd, tagsEdge)
            };
            string e11Json = Serializer.SerializeJson(e11Obj);
            string e11Result = await _McpClient.CallAsync<string>("edge/create", new { edge = e11Json }, 30000, token).ConfigureAwait(false);
            Edge e11 = Serializer.DeserializeJson<Edge>(e11Result);
            Console.WriteLine("| Creating edge " + e11.Name + " " + e11.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Edge e12Obj = new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n5.GUID,
                To = n8.GUID,
                Name = "5 to 8",
                Cost = 12,
                Labels = CombineLists(labelsEven, labelsEdge),
                Tags = CombineNvc(tagsEven, tagsEdge)
            };
            string e12Json = Serializer.SerializeJson(e12Obj);
            string e12Result = await _McpClient.CallAsync<string>("edge/create", new { edge = e12Json }, 30000, token).ConfigureAwait(false);
            Edge e12 = Serializer.DeserializeJson<Edge>(e12Result);
            Console.WriteLine("| Creating edge " + e12.Name + " " + e12.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            #endregion

            _TenantGuid = tenant.GUID;
            _GraphGuid = graph.GUID;
        }

        static async Task Test1_1(CancellationToken token = default)
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);

            Console.WriteLine("");
            Console.WriteLine("Retrieving graphs where label = 'graph'");

            List<string> labelGraph = new List<string> { "graph" };
            SearchRequest searchReq = new SearchRequest
            {
                TenantGUID = tenantGuid,
                Labels = labelGraph
            };

            string searchReqJson = Serializer.SerializeJson(searchReq);
            string result = await _McpClient.CallAsync<string>("graph/search", new { searchRequest = searchReqJson }, 30000, token).ConfigureAwait(false);
            SearchResult searchResult = Serializer.DeserializeJson<SearchResult>(result);
            List<Graph> graphs = searchResult?.Graphs ?? new List<Graph>();
            foreach (Graph graph in graphs)
            {
                Console.WriteLine("| " + graph.GUID + ": " + graph.Name);
            }

            Console.WriteLine("");
            Console.WriteLine("Retrieving nodes with labels 'node' and 'even'");

            List<string> labelEvenNodes = new List<string> { "node", "even" };
            SearchRequest nodeSearchReq = new SearchRequest
            {
                TenantGUID = tenantGuid,
                GraphGUID = graphGuid,
                Labels = labelEvenNodes
            };

            string nodeSearchReqJson = Serializer.SerializeJson(nodeSearchReq);
            string nodeResult = await _McpClient.CallAsync<string>("node/search", new { searchRequest = nodeSearchReqJson }, 30000, token).ConfigureAwait(false);
            SearchResult nodeSearchResult = Serializer.DeserializeJson<SearchResult>(nodeResult);
            List<Node> nodes = nodeSearchResult?.Nodes ?? new List<Node>();
            foreach (Node node in nodes)
            {
                Console.WriteLine("| " + node.GUID + ": " + node.Name);
            }

            Console.WriteLine("");
            Console.WriteLine("Retrieving edges with labels 'edge' and 'odd'");

            List<string> labelOddEdges = new List<string> { "edge", "odd" };
            SearchRequest edgeSearchReq = new SearchRequest
            {
                TenantGUID = tenantGuid,
                GraphGUID = graphGuid,
                Labels = labelOddEdges
            };

            string edgeSearchReqJson = Serializer.SerializeJson(edgeSearchReq);
            string edgeResult = await _McpClient.CallAsync<string>("edge/search", new { request = edgeSearchReqJson }, 30000, token).ConfigureAwait(false);
            SearchResult edgeSearchResult = Serializer.DeserializeJson<SearchResult>(edgeResult);
            List<Edge> edges = edgeSearchResult?.Edges ?? new List<Edge>();
            foreach (Edge edge in edges)
            {
                Console.WriteLine("| " + edge.GUID + ": " + edge.Name);
            }

            Console.WriteLine("");
        }

        static async Task Test1_2(CancellationToken token = default)
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);

            Console.WriteLine("");
            Console.WriteLine("Retrieving graphs where tag 'type' = 'graph'");

            NameValueCollection tagsGraph = new NameValueCollection();
            tagsGraph.Add("type", "graph");

            SearchRequest searchReq = new SearchRequest
            {
                TenantGUID = tenantGuid,
                Tags = tagsGraph
            };

            string searchReqJson = Serializer.SerializeJson(searchReq);
            string result = await _McpClient.CallAsync<string>("graph/search", new { searchRequest = searchReqJson }, 30000, token).ConfigureAwait(false);
            SearchResult searchResult = Serializer.DeserializeJson<SearchResult>(result);
            List<Graph> graphs = searchResult?.Graphs ?? new List<Graph>();
            foreach (Graph graph in graphs)
            {
                Console.WriteLine("| " + graph.GUID + ": " + graph.Name);
            }

            Console.WriteLine("");
            Console.WriteLine("Retrieving nodes where tag 'type' = 'node' and 'isEven' = 'true'");

            NameValueCollection tagsEvenNodes = new NameValueCollection();
            tagsEvenNodes.Add("type", "node");
            tagsEvenNodes.Add("isEven", "true");

            SearchRequest nodeSearchReq = new SearchRequest
            {
                TenantGUID = tenantGuid,
                GraphGUID = graphGuid,
                Tags = tagsEvenNodes
            };

            string nodeSearchReqJson = Serializer.SerializeJson(nodeSearchReq);
            string nodeResult = await _McpClient.CallAsync<string>("node/search", new { searchRequest = nodeSearchReqJson }, 30000, token).ConfigureAwait(false);
            SearchResult nodeSearchResult = Serializer.DeserializeJson<SearchResult>(nodeResult);
            List<Node> nodes = nodeSearchResult?.Nodes ?? new List<Node>();
            foreach (Node node in nodes)
            {
                Console.WriteLine("| " + node.GUID + ": " + node.Name);
            }

            Console.WriteLine("");
            Console.WriteLine("Retrieving edges where tag 'type' = 'edge' and 'isEven' = 'false'");

            NameValueCollection tagsOddEdges = new NameValueCollection();
            tagsOddEdges.Add("type", "edge");
            tagsOddEdges.Add("isEven", "false");

            SearchRequest edgeSearchReq = new SearchRequest
            {
                TenantGUID = tenantGuid,
                GraphGUID = graphGuid,
                Tags = tagsOddEdges
            };

            string edgeSearchReqJson = Serializer.SerializeJson(edgeSearchReq);
            string edgeResult = await _McpClient.CallAsync<string>("edge/search", new { request = edgeSearchReqJson }, 30000, token).ConfigureAwait(false);
            SearchResult edgeSearchResult = Serializer.DeserializeJson<SearchResult>(edgeResult);
            List<Edge> edges = edgeSearchResult?.Edges ?? new List<Edge>();
            foreach (Edge edge in edges)
            {
                Console.WriteLine("| " + edge.GUID + ": " + edge.Name);
            }

            Console.WriteLine("");
        }

        static async Task Test1_3(CancellationToken token = default)
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);

            Console.WriteLine("");
            Console.WriteLine("Retrieving graphs where label = 'graph', and tag 'type' = 'graph'");

            List<string> labelGraph = new List<string> { "graph" };
            NameValueCollection tagsGraph = new NameValueCollection();
            tagsGraph.Add("type", "graph");

            SearchRequest searchReq = new SearchRequest
            {
                TenantGUID = tenantGuid,
                Labels = labelGraph,
                Tags = tagsGraph
            };

            string searchReqJson = Serializer.SerializeJson(searchReq);
            string result = await _McpClient.CallAsync<string>("graph/search", new { searchRequest = searchReqJson }, 30000, token).ConfigureAwait(false);
            SearchResult searchResult = Serializer.DeserializeJson<SearchResult>(result);
            List<Graph> graphs = searchResult?.Graphs ?? new List<Graph>();
            foreach (Graph graph in graphs)
            {
                Console.WriteLine("| " + graph.GUID + ": " + graph.Name);
            }

            Console.WriteLine("");
            Console.WriteLine("Retrieving nodes where labels 'node' and 'even' are present, and tag 'type' = 'node' and 'isEven' = 'true'");

            List<string> labelEvenNodes = new List<string> { "node", "even" };
            NameValueCollection tagsEvenNodes = new NameValueCollection();
            tagsEvenNodes.Add("type", "node");
            tagsEvenNodes.Add("isEven", "true");

            SearchRequest nodeSearchReq = new SearchRequest
            {
                TenantGUID = tenantGuid,
                GraphGUID = graphGuid,
                Labels = labelEvenNodes,
                Tags = tagsEvenNodes
            };

            string nodeSearchReqJson = Serializer.SerializeJson(nodeSearchReq);
            string nodeResult = await _McpClient.CallAsync<string>("node/search", new { searchRequest = nodeSearchReqJson }, 30000, token).ConfigureAwait(false);
            SearchResult nodeSearchResult = Serializer.DeserializeJson<SearchResult>(nodeResult);
            List<Node> nodes = nodeSearchResult?.Nodes ?? new List<Node>();
            foreach (Node node in nodes)
            {
                Console.WriteLine("| " + node.GUID + ": " + node.Name);
            }

            Console.WriteLine("");
            Console.WriteLine("Retrieving edges where labels 'edge' and 'odd' are present, and tag 'type' = 'edge' and 'isEven' = 'false'");

            List<string> labelOddEdges = new List<string> { "edge", "odd" };
            NameValueCollection tagsOddEdges = new NameValueCollection();
            tagsOddEdges.Add("type", "edge");
            tagsOddEdges.Add("isEven", "false");

            SearchRequest edgeSearchReq = new SearchRequest
            {
                TenantGUID = tenantGuid,
                GraphGUID = graphGuid,
                Labels = labelOddEdges,
                Tags = tagsOddEdges
            };

            string edgeSearchReqJson = Serializer.SerializeJson(edgeSearchReq);
            string edgeResult = await _McpClient.CallAsync<string>("edge/search", new { request = edgeSearchReqJson }, 30000, token).ConfigureAwait(false);
            SearchResult edgeSearchResult = Serializer.DeserializeJson<SearchResult>(edgeResult);
            List<Edge> edges = edgeSearchResult?.Edges ?? new List<Edge>();
            foreach (Edge edge in edges)
            {
                Console.WriteLine("| " + edge.GUID + ": " + edge.Name);
            }

            Console.WriteLine("");
        }

        static async Task Test1_4(CancellationToken token = default)
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);

            #region Cosine-Similarity

            VectorSearchRequest searchReqCosineSim = new VectorSearchRequest
            {
                TenantGUID = tenantGuid,
                GraphGUID = graphGuid,
                Domain = VectorSearchDomainEnum.Node,
                SearchType = VectorSearchTypeEnum.CosineSimilarity,
                Embeddings = new List<float> { 0.1f, 0.2f, 0.3f }
            };

            Console.WriteLine("");
            Console.WriteLine("Retrieving nodes by cosine similarity to embeddings [ 0.1, 0.2, 0.3 ]");

            string searchReqCosineSimJson = Serializer.SerializeJson(searchReqCosineSim);
            string result = await _McpClient.CallAsync<string>("vector/search", new { tenantGuid = tenantGuid.ToString(), graphGuid = graphGuid.ToString(), searchRequest = searchReqCosineSimJson }, 30000, token).ConfigureAwait(false);
            List<VectorSearchResult> cosineSimResults = Serializer.DeserializeJson<List<VectorSearchResult>>(result);
            foreach (VectorSearchResult res in cosineSimResults.OrderByDescending(p => p.Score))
            {
                Console.WriteLine("| Node " + res.Node.GUID + " " + res.Node.Name + ": score " + res.Score);
            }

            #endregion

            #region Cosine-Distance

            VectorSearchRequest searchReqCosineDis = new VectorSearchRequest
            {
                TenantGUID = tenantGuid,
                GraphGUID = graphGuid,
                Domain = VectorSearchDomainEnum.Node,
                SearchType = VectorSearchTypeEnum.CosineDistance,
                Embeddings = new List<float> { 0.1f, 0.2f, 0.3f }
            };

            Console.WriteLine("");
            Console.WriteLine("Retrieving nodes by cosine distance from embeddings [ 0.1, 0.2, 0.3 ]");

            string searchReqCosineDisJson = Serializer.SerializeJson(searchReqCosineDis);
            string disResult = await _McpClient.CallAsync<string>("vector/search", new { tenantGuid = tenantGuid.ToString(), graphGuid = graphGuid.ToString(), searchRequest = searchReqCosineDisJson }, 30000, token).ConfigureAwait(false);
            List<VectorSearchResult> cosineDisResults = Serializer.DeserializeJson<List<VectorSearchResult>>(disResult);
            foreach (VectorSearchResult res in cosineDisResults.OrderBy(p => p.Distance))
            {
                Console.WriteLine("| Node " + res.Node.GUID + " " + res.Node.Name + ": distance " + res.Distance);
            }

            #endregion

            #region Euclidian-Similarity

            VectorSearchRequest searchReqEucSim = new VectorSearchRequest
            {
                TenantGUID = tenantGuid,
                GraphGUID = graphGuid,
                Domain = VectorSearchDomainEnum.Node,
                SearchType = VectorSearchTypeEnum.EuclidianSimilarity,
                Embeddings = new List<float> { 0.1f, 0.2f, 0.3f }
            };

            Console.WriteLine("");
            Console.WriteLine("Retrieving nodes by Euclidian similarity to embeddings [ 0.1, 0.2, 0.3 ]");

            string searchReqEucSimJson = Serializer.SerializeJson(searchReqEucSim);
            string eucSimResult = await _McpClient.CallAsync<string>("vector/search", new { tenantGuid = tenantGuid.ToString(), graphGuid = graphGuid.ToString(), searchRequest = searchReqEucSimJson }, 30000, token).ConfigureAwait(false);
            List<VectorSearchResult> eucSimResults = Serializer.DeserializeJson<List<VectorSearchResult>>(eucSimResult);
            foreach (VectorSearchResult res in eucSimResults.OrderByDescending(p => p.Score))
            {
                Console.WriteLine("| Node " + res.Node.GUID + " " + res.Node.Name + ": score " + res.Score);
            }

            #endregion

            #region Euclidian-Distance

            VectorSearchRequest searchReqEucDis = new VectorSearchRequest
            {
                TenantGUID = tenantGuid,
                GraphGUID = graphGuid,
                Domain = VectorSearchDomainEnum.Node,
                SearchType = VectorSearchTypeEnum.EuclidianDistance,
                Embeddings = new List<float> { 0.1f, 0.2f, 0.3f }
            };

            Console.WriteLine("");
            Console.WriteLine("Retrieving nodes by Euclidian distance from embeddings [ 0.1, 0.2, 0.3 ]");

            string searchReqEucDisJson = Serializer.SerializeJson(searchReqEucDis);
            string eucDisResult = await _McpClient.CallAsync<string>("vector/search", new { tenantGuid = tenantGuid.ToString(), graphGuid = graphGuid.ToString(), searchRequest = searchReqEucDisJson }, 30000, token).ConfigureAwait(false);
            List<VectorSearchResult> eucDisResults = Serializer.DeserializeJson<List<VectorSearchResult>>(eucDisResult);
            foreach (VectorSearchResult res in eucDisResults.OrderBy(p => p.Distance))
            {
                Console.WriteLine("| Node " + res.Node.GUID + " " + res.Node.Name + ": distance " + res.Distance);
            }

            #endregion

            #region Inner-Product

            VectorSearchRequest searchReqDp = new VectorSearchRequest
            {
                TenantGUID = tenantGuid,
                GraphGUID = graphGuid,
                Domain = VectorSearchDomainEnum.Node,
                SearchType = VectorSearchTypeEnum.DotProduct,
                Embeddings = new List<float> { 0.1f, 0.2f, 0.3f }
            };

            Console.WriteLine("");
            Console.WriteLine("Retrieving nodes by dot product with embeddings [ 0.1, 0.2, 0.3 ]");

            string searchReqDpJson = Serializer.SerializeJson(searchReqDp);
            string dpResult = await _McpClient.CallAsync<string>("vector/search", new { tenantGuid = tenantGuid.ToString(), graphGuid = graphGuid.ToString(), searchRequest = searchReqDpJson }, 30000, token).ConfigureAwait(false);
            List<VectorSearchResult> dpResults = Serializer.DeserializeJson<List<VectorSearchResult>>(dpResult);
            foreach (VectorSearchResult res in dpResults.OrderByDescending(p => p.InnerProduct))
            {
                Console.WriteLine("| Node " + res.Node.GUID + " " + res.Node.Name + ": inner product " + res.InnerProduct);
            }

            #endregion

            Console.WriteLine("");
        }

        #endregion

        #region Graph-2

        static async Task LoadGraph2(CancellationToken token = default)
        {
            #region Tenant

            Console.WriteLine("| Creating tenant");
            string tenantResult = await _McpClient.CallAsync<string>("tenant/create", new { name = "Test tenant" }, 30000, token).ConfigureAwait(false);
            TenantMetadata tenant = Serializer.DeserializeJson<TenantMetadata>(tenantResult);
            Console.WriteLine("| Tenant created: " + tenant.GUID);

            #endregion

            #region Graph

            Console.WriteLine("| Creating graph");
            string graphResult = await _McpClient.CallAsync<string>("graph/create", new { tenantGuid = tenant.GUID.ToString(), name = "Sample Graph 2" }, 30000, token).ConfigureAwait(false);
            Graph graph = Serializer.DeserializeJson<Graph>(graphResult);
            Console.WriteLine("| Graph created: " + graph.GUID);

            #endregion

            #region Objects

            Person joel = new Person { Name = "Joel", Age = 47, Hobby = new Hobby { Name = "BJJ", HoursPerWeek = 8 } };
            Person yip = new Person { Name = "Yip", Age = 39, Hobby = new Hobby { Name = "Law", HoursPerWeek = 40 } };
            Person keith = new Person { Name = "Keith", Age = 48, Hobby = new Hobby { Name = "Planes", HoursPerWeek = 10 } };
            Person alex = new Person { Name = "Alex", Age = 34, Hobby = new Hobby { Name = "Art", HoursPerWeek = 10 } };
            Person blake = new Person { Name = "Blake", Age = 34, Hobby = new Hobby { Name = "Music", HoursPerWeek = 20 } };

            ISP xfi = new ISP { Name = "Xfinity", Mbps = 1000 };
            ISP starlink = new ISP { Name = "Starlink", Mbps = 100 };
            ISP att = new ISP { Name = "AT&T", Mbps = 500 };

            Internet internet = new Internet();

            HostingProvider equinix = new HostingProvider { Name = "Equinix" };
            HostingProvider aws = new HostingProvider { Name = "Amazon Web Services" };
            HostingProvider azure = new HostingProvider { Name = "Microsoft Azure" };
            HostingProvider digitalOcean = new HostingProvider { Name = "DigitalOcean" };
            HostingProvider rackspace = new HostingProvider { Name = "Rackspace" };

            Application ccp = new Application { Name = "Cloud Control Plane" };
            Application website = new Application { Name = "Website" };
            Application ad = new Application { Name = "Active Directory" };

            #endregion

            #region Nodes

            Console.WriteLine("| Creating nodes");
            Node joelNode = new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Joel", Data = joel };
            string joelResult = await _McpClient.CallAsync<string>("node/create", new { tenantGuid = tenant.GUID.ToString(), graphGuid = graph.GUID.ToString(), name = joelNode.Name }, 30000, token).ConfigureAwait(false);
            joelNode = Serializer.DeserializeJson<Node>(joelResult);
            joelNode.Data = joel; // Restore data
            string joelNodeJson = Serializer.SerializeJson(joelNode);
            string joelUpdateResult = await _McpClient.CallAsync<string>("node/update", new { node = joelNodeJson }, 30000, token).ConfigureAwait(false);
            joelNode = Serializer.DeserializeJson<Node>(joelUpdateResult);
            Console.WriteLine("| Created node: " + joelNode.Name + " (" + joelNode.GUID + ")");

            Node yipNode = new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Yip", Data = yip };
            string yipResult = await _McpClient.CallAsync<string>("node/create", new { tenantGuid = tenant.GUID.ToString(), graphGuid = graph.GUID.ToString(), name = yipNode.Name }, 30000, token).ConfigureAwait(false);
            yipNode = Serializer.DeserializeJson<Node>(yipResult);
            yipNode.Data = yip; // Restore data
            string yipNodeJson = Serializer.SerializeJson(yipNode);
            string yipUpdateResult = await _McpClient.CallAsync<string>("node/update", new { node = yipNodeJson }, 30000, token).ConfigureAwait(false);
            yipNode = Serializer.DeserializeJson<Node>(yipUpdateResult);
            Console.WriteLine("| Created node: " + yipNode.Name + " (" + yipNode.GUID + ")");

            Node keithNode = new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Keith", Data = keith };
            string keithResult = await _McpClient.CallAsync<string>("node/create", new { tenantGuid = tenant.GUID.ToString(), graphGuid = graph.GUID.ToString(), name = keithNode.Name }, 30000, token).ConfigureAwait(false);
            keithNode = Serializer.DeserializeJson<Node>(keithResult);
            keithNode.Data = keith; // Restore data
            string keithNodeJson = Serializer.SerializeJson(keithNode);
            string keithUpdateResult = await _McpClient.CallAsync<string>("node/update", new { node = keithNodeJson }, 30000, token).ConfigureAwait(false);
            keithNode = Serializer.DeserializeJson<Node>(keithUpdateResult);
            Console.WriteLine("| Created node: " + keithNode.Name + " (" + keithNode.GUID + ")");

            Node alexNode = new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Alex", Data = alex };
            string alexResult = await _McpClient.CallAsync<string>("node/create", new { tenantGuid = tenant.GUID.ToString(), graphGuid = graph.GUID.ToString(), name = alexNode.Name }, 30000, token).ConfigureAwait(false);
            alexNode = Serializer.DeserializeJson<Node>(alexResult);
            alexNode.Data = alex; // Restore data
            string alexNodeJson = Serializer.SerializeJson(alexNode);
            string alexUpdateResult = await _McpClient.CallAsync<string>("node/update", new { node = alexNodeJson }, 30000, token).ConfigureAwait(false);
            alexNode = Serializer.DeserializeJson<Node>(alexUpdateResult);
            Console.WriteLine("| Created node: " + alexNode.Name + " (" + alexNode.GUID + ")");

            Node blakeNode = new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Blake", Data = blake };
            string blakeResult = await _McpClient.CallAsync<string>("node/create", new { tenantGuid = tenant.GUID.ToString(), graphGuid = graph.GUID.ToString(), name = blakeNode.Name }, 30000, token).ConfigureAwait(false);
            blakeNode = Serializer.DeserializeJson<Node>(blakeResult);
            blakeNode.Data = blake; // Restore data
            string blakeNodeJson = Serializer.SerializeJson(blakeNode);
            string blakeUpdateResult = await _McpClient.CallAsync<string>("node/update", new { node = blakeNodeJson }, 30000, token).ConfigureAwait(false);
            blakeNode = Serializer.DeserializeJson<Node>(blakeUpdateResult);
            Console.WriteLine("| Created node: " + blakeNode.Name + " (" + blakeNode.GUID + ")");

            Node xfiNode = new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Xfinity", Data = xfi };
            string xfiResult = await _McpClient.CallAsync<string>("node/create", new { tenantGuid = tenant.GUID.ToString(), graphGuid = graph.GUID.ToString(), name = xfiNode.Name }, 30000, token).ConfigureAwait(false);
            xfiNode = Serializer.DeserializeJson<Node>(xfiResult);
            xfiNode.Data = xfi; // Restore data
            string xfiNodeJson = Serializer.SerializeJson(xfiNode);
            string xfiUpdateResult = await _McpClient.CallAsync<string>("node/update", new { node = xfiNodeJson }, 30000, token).ConfigureAwait(false);
            xfiNode = Serializer.DeserializeJson<Node>(xfiUpdateResult);
            Console.WriteLine("| Created node: " + xfiNode.Name + " (" + xfiNode.GUID + ")");

            Node starlinkNode = new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Starlink", Data = starlink };
            string starlinkResult = await _McpClient.CallAsync<string>("node/create", new { tenantGuid = tenant.GUID.ToString(), graphGuid = graph.GUID.ToString(), name = starlinkNode.Name }, 30000, token).ConfigureAwait(false);
            starlinkNode = Serializer.DeserializeJson<Node>(starlinkResult);
            starlinkNode.Data = starlink; // Restore data
            string starlinkNodeJson = Serializer.SerializeJson(starlinkNode);
            string starlinkUpdateResult = await _McpClient.CallAsync<string>("node/update", new { node = starlinkNodeJson }, 30000, token).ConfigureAwait(false);
            starlinkNode = Serializer.DeserializeJson<Node>(starlinkUpdateResult);
            Console.WriteLine("| Created node: " + starlinkNode.Name + " (" + starlinkNode.GUID + ")");

            Node attNode = new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "AT&T", Data = att };
            string attResult = await _McpClient.CallAsync<string>("node/create", new { tenantGuid = tenant.GUID.ToString(), graphGuid = graph.GUID.ToString(), name = attNode.Name }, 30000, token).ConfigureAwait(false);
            attNode = Serializer.DeserializeJson<Node>(attResult);
            attNode.Data = att; // Restore data
            string attNodeJson = Serializer.SerializeJson(attNode);
            string attUpdateResult = await _McpClient.CallAsync<string>("node/update", new { node = attNodeJson }, 30000, token).ConfigureAwait(false);
            attNode = Serializer.DeserializeJson<Node>(attUpdateResult);
            Console.WriteLine("| Created node: " + attNode.Name + " (" + attNode.GUID + ")");

            Node internetNode = new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Internet", Data = internet };
            string internetResult = await _McpClient.CallAsync<string>("node/create", new { tenantGuid = tenant.GUID.ToString(), graphGuid = graph.GUID.ToString(), name = internetNode.Name }, 30000, token).ConfigureAwait(false);
            internetNode = Serializer.DeserializeJson<Node>(internetResult);
            internetNode.Data = internet; // Restore data
            string internetNodeJson = Serializer.SerializeJson(internetNode);
            string internetUpdateResult = await _McpClient.CallAsync<string>("node/update", new { node = internetNodeJson }, 30000, token).ConfigureAwait(false);
            internetNode = Serializer.DeserializeJson<Node>(internetUpdateResult);
            Console.WriteLine("| Created node: " + internetNode.Name + " (" + internetNode.GUID + ")");

            Node equinixNode = new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Equinix", Data = equinix };
            string equinixResult = await _McpClient.CallAsync<string>("node/create", new { tenantGuid = tenant.GUID.ToString(), graphGuid = graph.GUID.ToString(), name = equinixNode.Name }, 30000, token).ConfigureAwait(false);
            equinixNode = Serializer.DeserializeJson<Node>(equinixResult);
            equinixNode.Data = equinix; // Restore data
            string equinixNodeJson = Serializer.SerializeJson(equinixNode);
            string equinixUpdateResult = await _McpClient.CallAsync<string>("node/update", new { node = equinixNodeJson }, 30000, token).ConfigureAwait(false);
            equinixNode = Serializer.DeserializeJson<Node>(equinixUpdateResult);
            Console.WriteLine("| Created node: " + equinixNode.Name + " (" + equinixNode.GUID + ")");

            Node awsNode = new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "AWS", Data = aws };
            string awsResult = await _McpClient.CallAsync<string>("node/create", new { tenantGuid = tenant.GUID.ToString(), graphGuid = graph.GUID.ToString(), name = awsNode.Name }, 30000, token).ConfigureAwait(false);
            awsNode = Serializer.DeserializeJson<Node>(awsResult);
            awsNode.Data = aws; // Restore data
            string awsNodeJson = Serializer.SerializeJson(awsNode);
            string awsUpdateResult = await _McpClient.CallAsync<string>("node/update", new { node = awsNodeJson }, 30000, token).ConfigureAwait(false);
            awsNode = Serializer.DeserializeJson<Node>(awsUpdateResult);
            Console.WriteLine("| Created node: " + awsNode.Name + " (" + awsNode.GUID + ")");

            Node azureNode = new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Azure", Data = azure };
            string azureResult = await _McpClient.CallAsync<string>("node/create", new { tenantGuid = tenant.GUID.ToString(), graphGuid = graph.GUID.ToString(), name = azureNode.Name }, 30000, token).ConfigureAwait(false);
            azureNode = Serializer.DeserializeJson<Node>(azureResult);
            azureNode.Data = azure; // Restore data
            string azureNodeJson = Serializer.SerializeJson(azureNode);
            string azureUpdateResult = await _McpClient.CallAsync<string>("node/update", new { node = azureNodeJson }, 30000, token).ConfigureAwait(false);
            azureNode = Serializer.DeserializeJson<Node>(azureUpdateResult);
            Console.WriteLine("| Created node: " + azureNode.Name + " (" + azureNode.GUID + ")");

            Node digitalOceanNode = new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "DigitalOcean", Data = digitalOcean };
            string doResult = await _McpClient.CallAsync<string>("node/create", new { tenantGuid = tenant.GUID.ToString(), graphGuid = graph.GUID.ToString(), name = digitalOceanNode.Name }, 30000, token).ConfigureAwait(false);
            digitalOceanNode = Serializer.DeserializeJson<Node>(doResult);
            digitalOceanNode.Data = digitalOcean; // Restore data
            string doNodeJson = Serializer.SerializeJson(digitalOceanNode);
            string doUpdateResult = await _McpClient.CallAsync<string>("node/update", new { node = doNodeJson }, 30000, token).ConfigureAwait(false);
            digitalOceanNode = Serializer.DeserializeJson<Node>(doUpdateResult);
            Console.WriteLine("| Created node: " + digitalOceanNode.Name + " (" + digitalOceanNode.GUID + ")");

            Node rackspaceNode = new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Rackspace", Data = rackspace };
            string rackspaceResult = await _McpClient.CallAsync<string>("node/create", new { tenantGuid = tenant.GUID.ToString(), graphGuid = graph.GUID.ToString(), name = rackspaceNode.Name }, 30000, token).ConfigureAwait(false);
            rackspaceNode = Serializer.DeserializeJson<Node>(rackspaceResult);
            rackspaceNode.Data = rackspace; // Restore data
            string rackspaceNodeJson = Serializer.SerializeJson(rackspaceNode);
            string rackspaceUpdateResult = await _McpClient.CallAsync<string>("node/update", new { node = rackspaceNodeJson }, 30000, token).ConfigureAwait(false);
            rackspaceNode = Serializer.DeserializeJson<Node>(rackspaceUpdateResult);
            Console.WriteLine("| Created node: " + rackspaceNode.Name + " (" + rackspaceNode.GUID + ")");

            Node ccpNode = new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Control Plane", Data = ccp };
            string ccpResult = await _McpClient.CallAsync<string>("node/create", new { tenantGuid = tenant.GUID.ToString(), graphGuid = graph.GUID.ToString(), name = ccpNode.Name }, 30000, token).ConfigureAwait(false);
            ccpNode = Serializer.DeserializeJson<Node>(ccpResult);
            ccpNode.Data = ccp; // Restore data
            string ccpNodeJson = Serializer.SerializeJson(ccpNode);
            string ccpUpdateResult = await _McpClient.CallAsync<string>("node/update", new { node = ccpNodeJson }, 30000, token).ConfigureAwait(false);
            ccpNode = Serializer.DeserializeJson<Node>(ccpUpdateResult);
            Console.WriteLine("| Created node: " + ccpNode.Name + " (" + ccpNode.GUID + ")");

            Node websiteNode = new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Website", Data = website };
            string websiteResult = await _McpClient.CallAsync<string>("node/create", new { tenantGuid = tenant.GUID.ToString(), graphGuid = graph.GUID.ToString(), name = websiteNode.Name }, 30000, token).ConfigureAwait(false);
            websiteNode = Serializer.DeserializeJson<Node>(websiteResult);
            websiteNode.Data = website; // Restore data
            string websiteNodeJson = Serializer.SerializeJson(websiteNode);
            string websiteUpdateResult = await _McpClient.CallAsync<string>("node/update", new { node = websiteNodeJson }, 30000, token).ConfigureAwait(false);
            websiteNode = Serializer.DeserializeJson<Node>(websiteUpdateResult);
            Console.WriteLine("| Created node: " + websiteNode.Name + " (" + websiteNode.GUID + ")");

            Node adNode = new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Active Directory", Data = ad };
            string adResult = await _McpClient.CallAsync<string>("node/create", new { tenantGuid = tenant.GUID.ToString(), graphGuid = graph.GUID.ToString(), name = adNode.Name }, 30000, token).ConfigureAwait(false);
            adNode = Serializer.DeserializeJson<Node>(adResult);
            adNode.Data = ad; // Restore data
            string adNodeJson = Serializer.SerializeJson(adNode);
            string adUpdateResult = await _McpClient.CallAsync<string>("node/update", new { node = adNodeJson }, 30000, token).ConfigureAwait(false);
            adNode = Serializer.DeserializeJson<Node>(adUpdateResult);
            Console.WriteLine("| Created node: " + adNode.Name + " (" + adNode.GUID + ")");

            Console.WriteLine("| All nodes created");

            #endregion

            #region Edges

            Console.WriteLine("| Creating edges");
            Edge joelXfiEdge = new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = joelNode.GUID, To = xfiNode.GUID, Name = "Joel to Xfinity" };
            string je1Json = Serializer.SerializeJson(joelXfiEdge);
            string je1 = await _McpClient.CallAsync<string>("edge/create", new { edge = je1Json }, 30000, token).ConfigureAwait(false);
            joelXfiEdge = Serializer.DeserializeJson<Edge>(je1);
            Console.WriteLine("| Creating edge " + joelXfiEdge.Name + " " + joelXfiEdge.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Edge joelStarlinkEdge = new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = joelNode.GUID, To = starlinkNode.GUID, Name = "Joel to Starlink" };
            string je2Json = Serializer.SerializeJson(joelStarlinkEdge);
            string je2 = await _McpClient.CallAsync<string>("edge/create", new { edge = je2Json }, 30000, token).ConfigureAwait(false);
            joelStarlinkEdge = Serializer.DeserializeJson<Edge>(je2);
            Console.WriteLine("| Creating edge " + joelStarlinkEdge.Name + " " + joelStarlinkEdge.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Edge yipXfiEdge = new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = yipNode.GUID, To = xfiNode.GUID, Name = "Yip to Xfinity" };
            string je3Json = Serializer.SerializeJson(yipXfiEdge);
            string je3 = await _McpClient.CallAsync<string>("edge/create", new { edge = je3Json }, 30000, token).ConfigureAwait(false);
            yipXfiEdge = Serializer.DeserializeJson<Edge>(je3);
            Console.WriteLine("| Creating edge " + yipXfiEdge.Name + " " + yipXfiEdge.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Edge keithStarlinkEdge = new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = keithNode.GUID, To = starlinkNode.GUID, Name = "Keith to Starlink" };
            string je4Json = Serializer.SerializeJson(keithStarlinkEdge);
            string je4 = await _McpClient.CallAsync<string>("edge/create", new { edge = je4Json }, 30000, token).ConfigureAwait(false);
            keithStarlinkEdge = Serializer.DeserializeJson<Edge>(je4);
            Console.WriteLine("| Creating edge " + keithStarlinkEdge.Name + " " + keithStarlinkEdge.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Edge keithXfiEdge = new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = keithNode.GUID, To = xfiNode.GUID, Name = "Keith to Xfinity" };
            string je5Json = Serializer.SerializeJson(keithXfiEdge);
            string je5 = await _McpClient.CallAsync<string>("edge/create", new { edge = je5Json }, 30000, token).ConfigureAwait(false);
            keithXfiEdge = Serializer.DeserializeJson<Edge>(je5);
            Console.WriteLine("| Creating edge " + keithXfiEdge.Name + " " + keithXfiEdge.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Edge keithAttEdge = new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = keithNode.GUID, To = attNode.GUID, Name = "Keith to AT&T" };
            string je6Json = Serializer.SerializeJson(keithAttEdge);
            string je6 = await _McpClient.CallAsync<string>("edge/create", new { edge = je6Json }, 30000, token).ConfigureAwait(false);
            keithAttEdge = Serializer.DeserializeJson<Edge>(je6);
            Console.WriteLine("| Creating edge " + keithAttEdge.Name + " " + keithAttEdge.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Edge alexAttEdge = new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = alexNode.GUID, To = attNode.GUID, Name = "Alex to AT&T" };
            string je7Json = Serializer.SerializeJson(alexAttEdge);
            string je7 = await _McpClient.CallAsync<string>("edge/create", new { edge = je7Json }, 30000, token).ConfigureAwait(false);
            alexAttEdge = Serializer.DeserializeJson<Edge>(je7);
            Console.WriteLine("| Creating edge " + alexAttEdge.Name + " " + alexAttEdge.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Edge blakeAttEdge = new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = blakeNode.GUID, To = attNode.GUID, Name = "Blake to AT&T" };
            string je8Json = Serializer.SerializeJson(blakeAttEdge);
            string je8 = await _McpClient.CallAsync<string>("edge/create", new { edge = je8Json }, 30000, token).ConfigureAwait(false);
            blakeAttEdge = Serializer.DeserializeJson<Edge>(je8);
            Console.WriteLine("| Creating edge " + blakeAttEdge.Name + " " + blakeAttEdge.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Edge xfiInternetEdge1 = new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = xfiNode.GUID, To = internetNode.GUID, Name = "Xfinity to Internet 1" };
            string je9Json = Serializer.SerializeJson(xfiInternetEdge1);
            string je9 = await _McpClient.CallAsync<string>("edge/create", new { edge = je9Json }, 30000, token).ConfigureAwait(false);
            xfiInternetEdge1 = Serializer.DeserializeJson<Edge>(je9);
            Console.WriteLine("| Creating edge " + xfiInternetEdge1.Name + " " + xfiInternetEdge1.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Edge xfiInternetEdge2 = new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = xfiNode.GUID, To = internetNode.GUID, Name = "Xfinity to Internet 2" };
            string je10Json = Serializer.SerializeJson(xfiInternetEdge2);
            string je10 = await _McpClient.CallAsync<string>("edge/create", new { edge = je10Json }, 30000, token).ConfigureAwait(false);
            xfiInternetEdge2 = Serializer.DeserializeJson<Edge>(je10);
            Console.WriteLine("| Creating edge " + xfiInternetEdge2.Name + " " + xfiInternetEdge2.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Edge starlinkInternetEdge = new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = starlinkNode.GUID, To = internetNode.GUID, Name = "Starlink to Internet" };
            string je11Json = Serializer.SerializeJson(starlinkInternetEdge);
            string je11 = await _McpClient.CallAsync<string>("edge/create", new { edge = je11Json }, 30000, token).ConfigureAwait(false);
            starlinkInternetEdge = Serializer.DeserializeJson<Edge>(je11);
            Console.WriteLine("| Creating edge " + starlinkInternetEdge.Name + " " + starlinkInternetEdge.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Edge attInternetEdge1 = new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = attNode.GUID, To = internetNode.GUID, Name = "AT&T to Internet 1" };
            string je12Json = Serializer.SerializeJson(attInternetEdge1);
            string je12 = await _McpClient.CallAsync<string>("edge/create", new { edge = je12Json }, 30000, token).ConfigureAwait(false);
            attInternetEdge1 = Serializer.DeserializeJson<Edge>(je12);
            Console.WriteLine("| Creating edge " + attInternetEdge1.Name + " " + attInternetEdge1.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Edge attInternetEdge2 = new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = attNode.GUID, To = internetNode.GUID, Name = "AT&T to Internet 2" };
            string je13Json = Serializer.SerializeJson(attInternetEdge2);
            string je13 = await _McpClient.CallAsync<string>("edge/create", new { edge = je13Json }, 30000, token).ConfigureAwait(false);
            attInternetEdge2 = Serializer.DeserializeJson<Edge>(je13);
            Console.WriteLine("| Creating edge " + attInternetEdge2.Name + " " + attInternetEdge2.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Edge internetEquinixEdge = new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = internetNode.GUID, To = equinixNode.GUID, Name = "Internet to Equinix" };
            string je14Json = Serializer.SerializeJson(internetEquinixEdge);
            string je14 = await _McpClient.CallAsync<string>("edge/create", new { edge = je14Json }, 30000, token).ConfigureAwait(false);
            internetEquinixEdge = Serializer.DeserializeJson<Edge>(je14);
            Console.WriteLine("| Creating edge " + internetEquinixEdge.Name + " " + internetEquinixEdge.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Edge internetAwsEdge = new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = internetNode.GUID, To = awsNode.GUID, Name = "Internet to AWS" };
            string je15Json = Serializer.SerializeJson(internetAwsEdge);
            string je15 = await _McpClient.CallAsync<string>("edge/create", new { edge = je15Json }, 30000, token).ConfigureAwait(false);
            internetAwsEdge = Serializer.DeserializeJson<Edge>(je15);
            Console.WriteLine("| Creating edge " + internetAwsEdge.Name + " " + internetAwsEdge.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Edge internetAzureEdge = new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = internetNode.GUID, To = azureNode.GUID, Name = "Internet to Azure" };
            string je16Json = Serializer.SerializeJson(internetAzureEdge);
            string je16 = await _McpClient.CallAsync<string>("edge/create", new { edge = je16Json }, 30000, token).ConfigureAwait(false);
            internetAzureEdge = Serializer.DeserializeJson<Edge>(je16);
            Console.WriteLine("| Creating edge " + internetAzureEdge.Name + " " + internetAzureEdge.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Edge equinixDoEdge = new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = equinixNode.GUID, To = digitalOceanNode.GUID, Name = "Equinix to DigitalOcean" };
            string je17Json = Serializer.SerializeJson(equinixDoEdge);
            string je17 = await _McpClient.CallAsync<string>("edge/create", new { edge = je17Json }, 30000, token).ConfigureAwait(false);
            equinixDoEdge = Serializer.DeserializeJson<Edge>(je17);
            Console.WriteLine("| Creating edge " + equinixDoEdge.Name + " " + equinixDoEdge.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Edge equinixAwsEdge = new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = equinixNode.GUID, To = awsNode.GUID, Name = "Equinix to AWS" };
            string je18Json = Serializer.SerializeJson(equinixAwsEdge);
            string je18 = await _McpClient.CallAsync<string>("edge/create", new { edge = je18Json }, 30000, token).ConfigureAwait(false);
            equinixAwsEdge = Serializer.DeserializeJson<Edge>(je18);
            Console.WriteLine("| Creating edge " + equinixAwsEdge.Name + " " + equinixAwsEdge.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Edge equinixRackspaceEdge = new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = equinixNode.GUID, To = rackspaceNode.GUID, Name = "Equinix to Rackspace" };
            string je19Json = Serializer.SerializeJson(equinixRackspaceEdge);
            string je19 = await _McpClient.CallAsync<string>("edge/create", new { edge = je19Json }, 30000, token).ConfigureAwait(false);
            equinixRackspaceEdge = Serializer.DeserializeJson<Edge>(je19);
            Console.WriteLine("| Creating edge " + equinixRackspaceEdge.Name + " " + equinixRackspaceEdge.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Edge awsWebsiteEdge = new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = awsNode.GUID, To = websiteNode.GUID, Name = "AWS to Website" };
            string je20Json = Serializer.SerializeJson(awsWebsiteEdge);
            string je20 = await _McpClient.CallAsync<string>("edge/create", new { edge = je20Json }, 30000, token).ConfigureAwait(false);
            awsWebsiteEdge = Serializer.DeserializeJson<Edge>(je20);
            Console.WriteLine("| Creating edge " + awsWebsiteEdge.Name + " " + awsWebsiteEdge.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Edge azureAdEdge = new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = azureNode.GUID, To = adNode.GUID, Name = "Azure to Active Directory" };
            string je21Json = Serializer.SerializeJson(azureAdEdge);
            string je21 = await _McpClient.CallAsync<string>("edge/create", new { edge = je21Json }, 30000, token).ConfigureAwait(false);
            azureAdEdge = Serializer.DeserializeJson<Edge>(je21);
            Console.WriteLine("| Creating edge " + azureAdEdge.Name + " " + azureAdEdge.GUID + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Edge doCcpEdge = new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = digitalOceanNode.GUID, To = ccpNode.GUID, Name = "DigitalOcean to Control Plane" };
            string je22Json = Serializer.SerializeJson(doCcpEdge);
            string je22 = await _McpClient.CallAsync<string>("edge/create", new { edge = je22Json }, 30000, token).ConfigureAwait(false);

            Console.WriteLine("| All edges created");

            #endregion

            _TenantGuid = tenant.GUID;
            _GraphGuid = graph.GUID;
            Console.WriteLine("");
            Console.WriteLine("LoadGraph2 completed successfully");
            Console.WriteLine("Tenant GUID: " + tenant.GUID);
            Console.WriteLine("Graph GUID: " + graph.GUID);
            Console.WriteLine("");
        }

        static async Task Test2_1(CancellationToken token = default)
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
            Expr e1 = new Expr("$.Name", OperatorEnum.Equals, "Joel");
            Expr e2 = new Expr("$.Age", OperatorEnum.GreaterThan, 38);

            Console.WriteLine("");
            Console.WriteLine("Retrieving nodes where Name = 'Joel'");
            SearchRequest searchReq1 = new SearchRequest
            {
                TenantGUID = tenantGuid,
                GraphGUID = graphGuid,
                Expr = e1
            };
            string searchReq1Json = Serializer.SerializeJson(searchReq1);
            string result1 = await _McpClient.CallAsync<string>("node/search", new { searchRequest = searchReq1Json }, 30000, token).ConfigureAwait(false);
            SearchResult searchResult1 = Serializer.DeserializeJson<SearchResult>(result1);
            List<Node> nodes1 = searchResult1?.Nodes ?? new List<Node>();
            foreach (Node node in nodes1)
            {
                Person person = Serializer.DeserializeJson<Person>(Serializer.SerializeJson(node.Data));
                Console.WriteLine(person.ToString());
            }

            Console.WriteLine("");
            Console.WriteLine("Retrieve nodes where Age >= 38");
            SearchRequest searchReq2 = new SearchRequest
            {
                TenantGUID = tenantGuid,
                GraphGUID = graphGuid,
                Expr = e2
            };
            string searchReq2Json = Serializer.SerializeJson(searchReq2);
            string result2 = await _McpClient.CallAsync<string>("node/search", new { searchRequest = searchReq2Json }, 30000, token).ConfigureAwait(false);
            SearchResult searchResult2 = Serializer.DeserializeJson<SearchResult>(result2);
            List<Node> nodes2 = searchResult2?.Nodes ?? new List<Node>();
            foreach (Node node in nodes2)
            {
                Person person = Serializer.DeserializeJson<Person>(Serializer.SerializeJson(node.Data));
                Console.WriteLine(person.ToString());
            }

            Console.WriteLine("");
        }

        #endregion

        #region Misc

        static async Task Test3_1(CancellationToken token = default)
        {
            string tenantResult = await _McpClient.CallAsync<string>("tenant/create", new { name = "Test" }, 30000, token).ConfigureAwait(false);
            TenantMetadata tenant = Serializer.DeserializeJson<TenantMetadata>(tenantResult);

            string graphResult = await _McpClient.CallAsync<string>("graph/create", new { tenantGuid = tenant.GUID.ToString(), name = "Test" }, 30000, token).ConfigureAwait(false);
            Graph graph = Serializer.DeserializeJson<Graph>(graphResult);

            Node node1 = new Node
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "Node1",
                Data = new { Text = "hello" }
            };

            Console.WriteLine(Serializer.SerializeJson(node1));

            string nodeResult = await _McpClient.CallAsync<string>("node/create", new { tenantGuid = tenant.GUID.ToString(), graphGuid = graph.GUID.ToString(), name = node1.Name }, 30000, token).ConfigureAwait(false);
            node1 = Serializer.DeserializeJson<Node>(nodeResult);
            
            node1.Data = new { Text = "hello" };
            string node1Json = Serializer.SerializeJson(node1);
            string nodeUpdateResult = await _McpClient.CallAsync<string>("node/update", new { node = node1Json }, 30000, token).ConfigureAwait(false);
            node1 = Serializer.DeserializeJson<Node>(nodeUpdateResult);
        }

        static async Task Test3_2(CancellationToken token = default)
        {
            string tenantResult = await _McpClient.CallAsync<string>("tenant/create", new { name = "Test" }, 30000, token).ConfigureAwait(false);
            TenantMetadata tenant = Serializer.DeserializeJson<TenantMetadata>(tenantResult);

            string graphResult = await _McpClient.CallAsync<string>("graph/create", new { tenantGuid = tenant.GUID.ToString(), name = "Test" }, 30000, token).ConfigureAwait(false);
            Graph graph = Serializer.DeserializeJson<Graph>(graphResult);

            object data = new { Text = "hello" };

            Node node1 = new Node
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "Node1",
                Data = data
            };

            Console.WriteLine(Serializer.SerializeJson(node1));

            string nodeResult = await _McpClient.CallAsync<string>("node/create", new { tenantGuid = tenant.GUID.ToString(), graphGuid = graph.GUID.ToString(), name = node1.Name }, 30000, token).ConfigureAwait(false);
            node1 = Serializer.DeserializeJson<Node>(nodeResult);
            
            node1.Data = data;
            string node1Json = Serializer.SerializeJson(node1);
            string nodeUpdateResult = await _McpClient.CallAsync<string>("node/update", new { node = node1Json }, 30000, token).ConfigureAwait(false);
            node1 = Serializer.DeserializeJson<Node>(nodeUpdateResult);
        }

        static async Task Test3_3(CancellationToken token = default)
        {
            EnumerationRequest query = new EnumerationRequest
            {
                TenantGUID = _TenantGuid,
                GraphGUID = _GraphGuid,
                Labels = new List<string> { },
                IncludeSubordinates = false,
                IncludeData = false,
                Ordering = EnumerationOrderEnum.CreatedDescending,
                MaxResults = 10,
                Tags = null,
                Expr = null
            };

            string graphsResult = await _McpClient.CallAsync<string>("graph/enumerate", new { tenantGuid = _TenantGuid.ToString(), query = query }, 30000, token).ConfigureAwait(false);
            EnumerationResult<Graph> graphs = Serializer.DeserializeJson<EnumerationResult<Graph>>(graphsResult);
            Console.WriteLine(Serializer.SerializeJson(graphs, true));

            string nodesResult = await _McpClient.CallAsync<string>("node/enumerate", new { query = query }, 30000, token).ConfigureAwait(false);
            EnumerationResult<Node> nodes = Serializer.DeserializeJson<EnumerationResult<Node>>(nodesResult);
            Console.WriteLine(Serializer.SerializeJson(nodes, true));

            string edgesResult = await _McpClient.CallAsync<string>("edge/enumerate", new { tenantGuid = _TenantGuid.ToString(), query = query }, 30000, token).ConfigureAwait(false);
            EnumerationResult<Edge> edges = Serializer.DeserializeJson<EnumerationResult<Edge>>(edgesResult);
            Console.WriteLine(Serializer.SerializeJson(edges, true));
        }

        static async Task TestSubgraph(CancellationToken token = default)
        {
            Console.WriteLine("");
            Console.WriteLine("=== CREATING TEST DATA FOR SUBGRAPH TEST ===");
            Console.WriteLine("");

            #region Create Tenant and Graph

            string tenantResult = await _McpClient.CallAsync<string>("tenant/create", new { name = "Subgraph Test Tenant" }, 30000, token).ConfigureAwait(false);
            TenantMetadata tenant = Serializer.DeserializeJson<TenantMetadata>(tenantResult);
            Console.WriteLine("| Created tenant: " + tenant.GUID);

            string graphResult = await _McpClient.CallAsync<string>("graph/create", new { tenantGuid = tenant.GUID.ToString(), name = "Subgraph Test Graph" }, 30000, token).ConfigureAwait(false);
            Graph graph = Serializer.DeserializeJson<Graph>(graphResult);
            Console.WriteLine("| Created graph: " + graph.GUID);
            Console.WriteLine("");

            #endregion

            #region Create Nodes

            Console.WriteLine("| Creating test nodes...");
            Node nodeA = new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Node A (Root)", Data = new { Type = "Root", Level = 0 } };
            string nodeAResult = await _McpClient.CallAsync<string>("node/create", new { tenantGuid = tenant.GUID.ToString(), graphGuid = graph.GUID.ToString(), name = nodeA.Name }, 30000, token).ConfigureAwait(false);
            nodeA = Serializer.DeserializeJson<Node>(nodeAResult);
            nodeA.Data = new { Type = "Root", Level = 0 };
            string nodeAJson = Serializer.SerializeJson(nodeA);
            string nodeAUpdateResult = await _McpClient.CallAsync<string>("node/update", new { node = nodeAJson }, 30000, token).ConfigureAwait(false);
            nodeA = Serializer.DeserializeJson<Node>(nodeAUpdateResult);
            Console.WriteLine("  | Created Node A: " + nodeA.GUID);

            Node nodeB = new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Node B (Layer 1)", Data = new { Type = "Layer1", Level = 1 } };
            string nodeBResult = await _McpClient.CallAsync<string>("node/create", new { tenantGuid = tenant.GUID.ToString(), graphGuid = graph.GUID.ToString(), name = nodeB.Name }, 30000, token).ConfigureAwait(false);
            nodeB = Serializer.DeserializeJson<Node>(nodeBResult);
            nodeB.Data = new { Type = "Layer1", Level = 1 };
            string nodeBJson = Serializer.SerializeJson(nodeB);
            string nodeBUpdateResult = await _McpClient.CallAsync<string>("node/update", new { node = nodeBJson }, 30000, token).ConfigureAwait(false);
            nodeB = Serializer.DeserializeJson<Node>(nodeBUpdateResult);
            Console.WriteLine("  | Created Node B: " + nodeB.GUID);

            Node nodeC = new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Node C (Layer 1)", Data = new { Type = "Layer1", Level = 1 } };
            string nodeCResult = await _McpClient.CallAsync<string>("node/create", new { tenantGuid = tenant.GUID.ToString(), graphGuid = graph.GUID.ToString(), name = nodeC.Name }, 30000, token).ConfigureAwait(false);
            nodeC = Serializer.DeserializeJson<Node>(nodeCResult);
            nodeC.Data = new { Type = "Layer1", Level = 1 };
            string nodeCJson = Serializer.SerializeJson(nodeC);
            string nodeCUpdateResult = await _McpClient.CallAsync<string>("node/update", new { node = nodeCJson }, 30000, token).ConfigureAwait(false);
            nodeC = Serializer.DeserializeJson<Node>(nodeCUpdateResult);
            Console.WriteLine("  | Created Node C: " + nodeC.GUID);

            Node nodeD = new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Node D (Layer 2)", Data = new { Type = "Layer2", Level = 2 } };
            string nodeDResult = await _McpClient.CallAsync<string>("node/create", new { tenantGuid = tenant.GUID.ToString(), graphGuid = graph.GUID.ToString(), name = nodeD.Name }, 30000, token).ConfigureAwait(false);
            nodeD = Serializer.DeserializeJson<Node>(nodeDResult);
            nodeD.Data = new { Type = "Layer2", Level = 2 };
            string nodeDJson = Serializer.SerializeJson(nodeD);
            string nodeDUpdateResult = await _McpClient.CallAsync<string>("node/update", new { node = nodeDJson }, 30000, token).ConfigureAwait(false);
            nodeD = Serializer.DeserializeJson<Node>(nodeDUpdateResult);
            Console.WriteLine("  | Created Node D: " + nodeD.GUID);

            Node nodeE = new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Node E (Layer 2)", Data = new { Type = "Layer2", Level = 2 } };
            string nodeEResult = await _McpClient.CallAsync<string>("node/create", new { tenantGuid = tenant.GUID.ToString(), graphGuid = graph.GUID.ToString(), name = nodeE.Name }, 30000, token).ConfigureAwait(false);
            nodeE = Serializer.DeserializeJson<Node>(nodeEResult);
            nodeE.Data = new { Type = "Layer2", Level = 2 };
            string nodeEJson = Serializer.SerializeJson(nodeE);
            string nodeEUpdateResult = await _McpClient.CallAsync<string>("node/update", new { node = nodeEJson }, 30000, token).ConfigureAwait(false);
            nodeE = Serializer.DeserializeJson<Node>(nodeEUpdateResult);
            Console.WriteLine("  | Created Node E: " + nodeE.GUID);

            Node nodeF = new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Node F (Layer 2)", Data = new { Type = "Layer2", Level = 2 } };
            string nodeFResult = await _McpClient.CallAsync<string>("node/create", new { tenantGuid = tenant.GUID.ToString(), graphGuid = graph.GUID.ToString(), name = nodeF.Name }, 30000, token).ConfigureAwait(false);
            nodeF = Serializer.DeserializeJson<Node>(nodeFResult);
            nodeF.Data = new { Type = "Layer2", Level = 2 };
            string nodeFJson = Serializer.SerializeJson(nodeF);
            string nodeFUpdateResult = await _McpClient.CallAsync<string>("node/update", new { node = nodeFJson }, 30000, token).ConfigureAwait(false);
            nodeF = Serializer.DeserializeJson<Node>(nodeFUpdateResult);
            Console.WriteLine("  | Created Node F: " + nodeF.GUID);

            Node nodeG = new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Node G (Layer 3)", Data = new { Type = "Layer3", Level = 3 } };
            string nodeGResult = await _McpClient.CallAsync<string>("node/create", new { tenantGuid = tenant.GUID.ToString(), graphGuid = graph.GUID.ToString(), name = nodeG.Name }, 30000, token).ConfigureAwait(false);
            nodeG = Serializer.DeserializeJson<Node>(nodeGResult);
            nodeG.Data = new { Type = "Layer3", Level = 3 };
            string nodeGJson = Serializer.SerializeJson(nodeG);
            string nodeGUpdateResult = await _McpClient.CallAsync<string>("node/update", new { node = nodeGJson }, 30000, token).ConfigureAwait(false);
            nodeG = Serializer.DeserializeJson<Node>(nodeGUpdateResult);
            Console.WriteLine("  | Created Node G: " + nodeG.GUID);
            Console.WriteLine("");

            #endregion

            #region Create Edges

            Console.WriteLine("| Creating test edges...");
            Edge edgeAB = new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = nodeA.GUID, To = nodeB.GUID, Name = "A -> B", Cost = 1 };
            string e1Json = Serializer.SerializeJson(edgeAB);
            string e1 = await _McpClient.CallAsync<string>("edge/create", new { edge = e1Json }, 30000, token).ConfigureAwait(false);
            Console.WriteLine("  | Created Edge A->B");

            Edge edgeAC = new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = nodeA.GUID, To = nodeC.GUID, Name = "A -> C", Cost = 1 };
            string e2Json = Serializer.SerializeJson(edgeAC);
            string e2 = await _McpClient.CallAsync<string>("edge/create", new { edge = e2Json }, 30000, token).ConfigureAwait(false);
            Console.WriteLine("  | Created Edge A->C");

            Edge edgeBD = new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = nodeB.GUID, To = nodeD.GUID, Name = "B -> D", Cost = 1 };
            string e3Json = Serializer.SerializeJson(edgeBD);
            string e3 = await _McpClient.CallAsync<string>("edge/create", new { edge = e3Json }, 30000, token).ConfigureAwait(false);
            Console.WriteLine("  | Created Edge B->D");

            Edge edgeBE = new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = nodeB.GUID, To = nodeE.GUID, Name = "B -> E", Cost = 1 };
            string e4Json = Serializer.SerializeJson(edgeBE);
            string e4 = await _McpClient.CallAsync<string>("edge/create", new { edge = e4Json }, 30000, token).ConfigureAwait(false);
            Console.WriteLine("  | Created Edge B->E");

            Edge edgeCF = new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = nodeC.GUID, To = nodeF.GUID, Name = "C -> F", Cost = 1 };
            string e5Json = Serializer.SerializeJson(edgeCF);
            string e5 = await _McpClient.CallAsync<string>("edge/create", new { edge = e5Json }, 30000, token).ConfigureAwait(false);
            Console.WriteLine("  | Created Edge C->F");

            Edge edgeDG = new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = nodeD.GUID, To = nodeG.GUID, Name = "D -> G", Cost = 1 };
            string e6Json = Serializer.SerializeJson(edgeDG);
            string e6 = await _McpClient.CallAsync<string>("edge/create", new { edge = e6Json }, 30000, token).ConfigureAwait(false);
            Console.WriteLine("  | Created Edge D->G");

            Edge edgeCA = new Edge { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, From = nodeC.GUID, To = nodeA.GUID, Name = "C -> A (back edge)", Cost = 1 };
            string e7Json = Serializer.SerializeJson(edgeCA);
            string e7 = await _McpClient.CallAsync<string>("edge/create", new { edge = e7Json }, 30000, token).ConfigureAwait(false);
            Console.WriteLine("  | Created Edge C->A (back)");
            Console.WriteLine("");

            #endregion

            #region Test Subgraph Retrieval

            Console.WriteLine("=== TESTING SUBGRAPH RETRIEVAL ===");
            Console.WriteLine("");
            Console.WriteLine("Graph Structure:");
            Console.WriteLine("  A (root)");
            Console.WriteLine("  ├─> B");
            Console.WriteLine("  │   ├─> D");
            Console.WriteLine("  │   │   └─> G");
            Console.WriteLine("  │   └─> E");
            Console.WriteLine("  └─> C");
            Console.WriteLine("      ├─> F");
            Console.WriteLine("      └─> A (back edge)");
            Console.WriteLine("");

            int maxDepth = Inputty.GetInteger("Max Depth (0 = starting node only, 1 = neighbors, 2 = two layers):", 2, false, false);
            int maxNodes = Inputty.GetInteger("Max Nodes (0 = unlimited):", 0, false, false);
            int maxEdges = Inputty.GetInteger("Max Edges (0 = unlimited):", 0, false, false);
            bool includeData = Inputty.GetBoolean("Include Data :", false);
            bool includeSubordinates = Inputty.GetBoolean("Include Subordinates (labels, tags, vectors):", false);

            Console.WriteLine("");
            Console.WriteLine("Retrieving subgraph starting from Node A (" + nodeA.GUID + ")");
            Console.WriteLine("  Max Depth: " + maxDepth);
            Console.WriteLine("  Max Nodes: " + (maxNodes == 0 ? "unlimited" : maxNodes.ToString()));
            Console.WriteLine("  Max Edges: " + (maxEdges == 0 ? "unlimited" : maxEdges.ToString()));
            Console.WriteLine("");

            try
            {
                string result = await _McpClient.CallAsync<string>("graph/getsubgraph", new 
                { 
                    tenantGuid = tenant.GUID.ToString(), 
                    graphGuid = graph.GUID.ToString(), 
                    nodeGuid = nodeA.GUID.ToString(),
                    maxDepth = maxDepth,
                    maxNodes = maxNodes,
                    maxEdges = maxEdges,
                    includeData = includeData,
                    includeSubordinates = includeSubordinates
                }, 30000, token).ConfigureAwait(false);

                SearchResult subgraphResult = Serializer.DeserializeJson<SearchResult>(result);

                if (subgraphResult == null)
                {
                    Console.WriteLine("Result is null");
                    return;
                }

                Console.WriteLine("=== SUBGRAPH RESULTS ===");
                Console.WriteLine("");
                Console.WriteLine("Graphs: " + (subgraphResult.Graphs?.Count ?? 0));
                Console.WriteLine("Nodes: " + (subgraphResult.Nodes?.Count ?? 0));
                Console.WriteLine("Edges: " + (subgraphResult.Edges?.Count ?? 0));
                Console.WriteLine("");
                Console.WriteLine("=== JSON OUTPUT ===");
                Console.WriteLine("");
                Console.WriteLine(Serializer.SerializeJson(subgraphResult, true));
            }
            catch (Exception ex)
            {
                Console.WriteLine("");
                Console.WriteLine("[ERROR] Exception occurred:");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            Console.WriteLine("");
            Console.WriteLine("=== TEST SUMMARY ===");
            Console.WriteLine("Tenant GUID: " + tenant.GUID);
            Console.WriteLine("Graph GUID: " + graph.GUID);
            Console.WriteLine("Starting Node: Node A (" + nodeA.GUID + ")");
            Console.WriteLine("");

            #endregion
        }

        #endregion

        #region Primitives

        static async Task FindRoutes(CancellationToken token = default)
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
            Guid fromGuid = Inputty.GetGuid("From GUID  :", default(Guid));
            Guid toGuid = Inputty.GetGuid("To GUID    :", default(Guid));

            string result = await _McpClient.CallAsync<string>("node/traverse", new 
            { 
                tenantGuid = tenantGuid.ToString(), 
                graphGuid = graphGuid.ToString(), 
                fromNodeGuid = fromGuid.ToString(),
                toNodeGuid = toGuid.ToString(),
                searchType = "DepthFirstSearch"
            }, 30000, token).ConfigureAwait(false);
            
            List<RouteDetail> routes = Serializer.DeserializeJson<List<RouteDetail>>(result);

            if (routes != null && routes.Count > 0)
                Console.WriteLine(Serializer.SerializeJson(routes, true));
        }

        static async Task Create(string str, CancellationToken token = default)
        {
            object obj = null;
            string json = null;

            if (str.Equals("tenant"))
            {
                string name = Inputty.GetString("Name:", null, false);
                string result = await _McpClient.CallAsync<string>("tenant/create", new { name = name }, 30000, token).ConfigureAwait(false);
                obj = Serializer.DeserializeJson<TenantMetadata>(result);
            }
            else if (str.Equals("user"))
            {
                UserMaster user = new UserMaster
                {
                    TenantGUID = Inputty.GetGuid("Tenant GUID:", _TenantGuid),
                    FirstName = Inputty.GetString("First name:", null, false),
                    LastName = Inputty.GetString("Last name:", null, false),
                    Email = Inputty.GetString("Email:", null, false),
                    Password = Inputty.GetString("Password:", null, false)
                };
                string userJson = Serializer.SerializeJson(user);
                string result = await _McpClient.CallAsync<string>("user/create", new { user = userJson }, 30000, token).ConfigureAwait(false);
                obj = Serializer.DeserializeJson<UserMaster>(result);
            }
            else if (str.Equals("cred"))
            {
                Credential cred = new Credential
                {
                    TenantGUID = Inputty.GetGuid("Tenant GUID:", _TenantGuid),
                    UserGUID = Inputty.GetGuid("User GUID:", default(Guid)),
                    Name = Inputty.GetString("Name:", null, false)
                };
                string credJson = Serializer.SerializeJson(cred);
                string result = await _McpClient.CallAsync<string>("credential/create", new { credential = credJson }, 30000, token).ConfigureAwait(false);
                obj = Serializer.DeserializeJson<Credential>(result);
            }
            else if (str.Equals("graph"))
            {
                Graph graph = new Graph
                {
                    TenantGUID = Inputty.GetGuid("Tenant GUID:", _TenantGuid),
                    Name = Inputty.GetString("Name:", null, false)
                };
                string result = await _McpClient.CallAsync<string>("graph/create", new { tenantGuid = graph.TenantGUID.ToString(), name = graph.Name }, 30000, token).ConfigureAwait(false);
                obj = Serializer.DeserializeJson<Graph>(result);
            }
            else if (str.Equals("node"))
            {
                json = Inputty.GetString("JSON:", null, false);
                Node node = Serializer.DeserializeJson<Node>(json);
                // First create the node with just tenantGuid, graphGuid, and name
                string result = await _McpClient.CallAsync<string>("node/create", new { tenantGuid = node.TenantGUID.ToString(), graphGuid = node.GraphGUID.ToString(), name = node.Name }, 30000, token).ConfigureAwait(false);
                Node created = Serializer.DeserializeJson<Node>(result);
                // Update the node with all properties if they exist
                if (node.Labels != null || node.Tags != null || node.Vectors != null || node.Data != null)
                {
                    created.Labels = node.Labels;
                    created.Tags = node.Tags;
                    created.Vectors = node.Vectors;
                    created.Data = node.Data;
                    string createdJson = Serializer.SerializeJson(created);
                    string updateResult = await _McpClient.CallAsync<string>("node/update", new { node = createdJson }, 30000, token).ConfigureAwait(false);
                    obj = Serializer.DeserializeJson<Node>(updateResult);
                }
                else
                {
                    obj = created;
                }
            }
            else if (str.Equals("edge"))
            {
                json = Inputty.GetString("JSON:", null, false);
                Edge edge = Serializer.DeserializeJson<Edge>(json);
                string result = await _McpClient.CallAsync<string>("edge/create", new { edge = edge }, 30000, token).ConfigureAwait(false);
                obj = Serializer.DeserializeJson<Edge>(result);
            }

            if (obj != null)
                Console.WriteLine(Serializer.SerializeJson(obj, true));
        }

        static async Task All(string str, CancellationToken token = default)
        {
            object obj = null;
            if (str.Equals("tenant"))
            {
                string result = await _McpClient.CallAsync<string>("tenant/all", null, 30000, token).ConfigureAwait(false);
                obj = Serializer.DeserializeJson<List<TenantMetadata>>(result);
            }
            else if (str.Equals("user"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                string result = await _McpClient.CallAsync<string>("user/all", new { tenantGuid = tenantGuid.ToString() }, 30000, token).ConfigureAwait(false);
                obj = Serializer.DeserializeJson<List<UserMaster>>(result);
            }
            else if (str.Equals("cred"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                string result = await _McpClient.CallAsync<string>("credential/all", new { tenantGuid = tenantGuid.ToString() }, 30000, token).ConfigureAwait(false);
                obj = Serializer.DeserializeJson<List<Credential>>(result);
            }
            else if (str.Equals("graph"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                string result = await _McpClient.CallAsync<string>("graph/all", new { tenantGuid = tenantGuid.ToString() }, 30000, token).ConfigureAwait(false);
                obj = Serializer.DeserializeJson<List<Graph>>(result);
            }
            else if (str.Equals("node"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                string result = await _McpClient.CallAsync<string>("node/all", new { tenantGuid = tenantGuid.ToString(), graphGuid = graphGuid.ToString() }, 30000, token).ConfigureAwait(false);
                obj = Serializer.DeserializeJson<List<Node>>(result);
            }
            else if (str.Equals("edge"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                string result = await _McpClient.CallAsync<string>("edge/all", new { tenantGuid = tenantGuid.ToString(), graphGuid = graphGuid.ToString() }, 30000, token).ConfigureAwait(false);
                obj = Serializer.DeserializeJson<List<Edge>>(result);
            }

            if (obj != null)
                Console.WriteLine(Serializer.SerializeJson(obj, true));
        }

        static async Task Read(string str, CancellationToken token = default)
        {
            object obj = null;

            if (str.Equals("tenant"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                string result = await _McpClient.CallAsync<string>("tenant/get", new { tenantGuid = tenantGuid.ToString() }, 30000, token).ConfigureAwait(false);
                obj = Serializer.DeserializeJson<TenantMetadata>(result);
            }
            else if (str.Equals("user"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid userGuid = Inputty.GetGuid("GUID        :", default(Guid));
                string result = await _McpClient.CallAsync<string>("user/get", new { tenantGuid = tenantGuid.ToString(), userGuid = userGuid.ToString() }, 30000, token).ConfigureAwait(false);
                obj = Serializer.DeserializeJson<UserMaster>(result);
            }
            else if (str.Equals("cred"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid credGuid = Inputty.GetGuid("GUID        :", default(Guid));
                string result = await _McpClient.CallAsync<string>("credential/get", new { tenantGuid = tenantGuid.ToString(), credentialGuid = credGuid.ToString() }, 30000, token).ConfigureAwait(false);
                obj = Serializer.DeserializeJson<Credential>(result);
            }
            else if (str.Equals("graph"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                string result = await _McpClient.CallAsync<string>("graph/get", new { tenantGuid = tenantGuid.ToString(), graphGuid = graphGuid.ToString() }, 30000, token).ConfigureAwait(false);
                obj = Serializer.DeserializeJson<Graph>(result);
            }
            else if (str.Equals("node"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                Guid guid = Inputty.GetGuid("Node GUID   :", default(Guid));
                string result = await _McpClient.CallAsync<string>("node/get", new { tenantGuid = tenantGuid.ToString(), graphGuid = graphGuid.ToString(), nodeGuid = guid.ToString() }, 30000, token).ConfigureAwait(false);
                obj = Serializer.DeserializeJson<Node>(result);
            }
            else if (str.Equals("edge"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                Guid guid = Inputty.GetGuid("Edge GUID   :", default(Guid));
                string result = await _McpClient.CallAsync<string>("edge/get", new { tenantGuid = tenantGuid.ToString(), graphGuid = graphGuid.ToString(), edgeGuid = guid.ToString() }, 30000, token).ConfigureAwait(false);
                obj = Serializer.DeserializeJson<Edge>(result);
            }

            if (obj != null)
                Console.WriteLine(Serializer.SerializeJson(obj, true));
        }

        static async Task Exists(string str, CancellationToken token = default)
        {
            bool exists = false;

            if (str.Equals("tenant"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                string result = await _McpClient.CallAsync<string>("tenant/exists", new { tenantGuid = tenantGuid.ToString() }, 30000, token).ConfigureAwait(false);
                exists = bool.Parse(result);
            }
            else if (str.Equals("user"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid userGuid = Inputty.GetGuid("GUID        :", default(Guid));
                string result = await _McpClient.CallAsync<string>("user/exists", new { tenantGuid = tenantGuid.ToString(), userGuid = userGuid.ToString() }, 30000, token).ConfigureAwait(false);
                exists = bool.Parse(result);
            }
            else if (str.Equals("cred"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid credGuid = Inputty.GetGuid("GUID        :", default(Guid));
                string result = await _McpClient.CallAsync<string>("credential/exists", new { tenantGuid = tenantGuid.ToString(), credentialGuid = credGuid.ToString() }, 30000, token).ConfigureAwait(false);
                exists = bool.Parse(result);
            }
            else if (str.Equals("graph"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                string result = await _McpClient.CallAsync<string>("graph/exists", new { tenantGuid = tenantGuid.ToString(), graphGuid = graphGuid.ToString() }, 30000, token).ConfigureAwait(false);
                exists = bool.Parse(result);
            }
            else if (str.Equals("node"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                Guid guid = Inputty.GetGuid("Node GUID   :", default(Guid));
                string result = await _McpClient.CallAsync<string>("node/exists", new { tenantGuid = tenantGuid.ToString(), graphGuid = graphGuid.ToString(), nodeGuid = guid.ToString() }, 30000, token).ConfigureAwait(false);
                exists = bool.Parse(result);
            }
            else if (str.Equals("edge"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                Guid guid = Inputty.GetGuid("Edge GUID   :", default(Guid));
                string result = await _McpClient.CallAsync<string>("edge/exists", new { tenantGuid = tenantGuid.ToString(), graphGuid = graphGuid.ToString(), edgeGuid = guid.ToString() }, 30000, token).ConfigureAwait(false);
                exists = bool.Parse(result);
            }

            Console.WriteLine("Exists: " + exists);
        }

        static async Task Update(string str, CancellationToken token = default)
        {
            object obj = null;
            string json = Inputty.GetString("JSON:", null, false);

            if (str.Equals("tenant"))
            {
                TenantMetadata tenant = Serializer.DeserializeJson<TenantMetadata>(json);
                string tenantJson = Serializer.SerializeJson(tenant);
                string result = await _McpClient.CallAsync<string>("tenant/update", new { tenant = tenantJson }, 30000, token).ConfigureAwait(false);
                obj = Serializer.DeserializeJson<TenantMetadata>(result);
            }
            else if (str.Equals("graph"))
            {
                Graph graph = Serializer.DeserializeJson<Graph>(json);
                string graphJson = Serializer.SerializeJson(graph);
                string result = await _McpClient.CallAsync<string>("graph/update", new { graph = graphJson }, 30000, token).ConfigureAwait(false);
                obj = Serializer.DeserializeJson<Graph>(result);
            }
            else if (str.Equals("node"))
            {
                Node node = Serializer.DeserializeJson<Node>(json);
                string nodeJson = Serializer.SerializeJson(node);
                string result = await _McpClient.CallAsync<string>("node/update", new { node = nodeJson }, 30000, token).ConfigureAwait(false);
                obj = Serializer.DeserializeJson<Node>(result);
            }
            else if (str.Equals("edge"))
            {
                Edge edge = Serializer.DeserializeJson<Edge>(json);
                string edgeJson = Serializer.SerializeJson(edge);
                string result = await _McpClient.CallAsync<string>("edge/update", new { edge = edgeJson }, 30000, token).ConfigureAwait(false);
                obj = Serializer.DeserializeJson<Edge>(result);
            }

            if (obj != null)
                Console.WriteLine(Serializer.SerializeJson(obj, true));
        }

        static async Task Delete(string str, CancellationToken token = default)
        {
            if (str.Equals("tenant"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                bool force = Inputty.GetBoolean("Force       :", true);
                await _McpClient.CallAsync<string>("tenant/delete", new { tenantGuid = tenantGuid.ToString(), force = force }, 30000, token).ConfigureAwait(false);
                Console.WriteLine("Tenant deleted successfully");
            }
            else if (str.Equals("user"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid userGuid = Inputty.GetGuid("GUID        :", default(Guid));
                await _McpClient.CallAsync<string>("user/delete", new { tenantGuid = tenantGuid.ToString(), userGuid = userGuid.ToString() }, 30000, token).ConfigureAwait(false);
                Console.WriteLine("User deleted successfully");
            }
            else if (str.Equals("cred"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid credGuid = Inputty.GetGuid("GUID        :", default(Guid));
                await _McpClient.CallAsync<string>("credential/delete", new { tenantGuid = tenantGuid.ToString(), credentialGuid = credGuid.ToString() }, 30000, token).ConfigureAwait(false);
                Console.WriteLine("Credential deleted successfully");
            }
            else if (str.Equals("graph"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                bool force = Inputty.GetBoolean("Force       :", true);
                await _McpClient.CallAsync<string>("graph/delete", new { tenantGuid = tenantGuid.ToString(), graphGuid = graphGuid.ToString(), force = force }, 30000, token).ConfigureAwait(false);
                Console.WriteLine("Graph deleted successfully");
            }
            else if (str.Equals("node"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                Guid guid = Inputty.GetGuid("Node GUID   :", default(Guid));
                await _McpClient.CallAsync<string>("node/delete", new { tenantGuid = tenantGuid.ToString(), graphGuid = graphGuid.ToString(), nodeGuid = guid.ToString() }, 30000, token).ConfigureAwait(false);
                Console.WriteLine("Node deleted successfully");
            }
            else if (str.Equals("edge"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                Guid guid = Inputty.GetGuid("Edge GUID   :", default(Guid));
                await _McpClient.CallAsync<string>("edge/delete", new { tenantGuid = tenantGuid.ToString(), graphGuid = graphGuid.ToString(), edgeGuid = guid.ToString() }, 30000, token).ConfigureAwait(false);
                Console.WriteLine("Edge deleted successfully");
            }
        }

        static async Task Search(string str, CancellationToken token = default)
        {
            if (!str.Equals("graph") && !str.Equals("node") && !str.Equals("edge")) return;

            Expr expr = GetExpression();
            string resultJson = null;

            if (str.Equals("graph"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                SearchRequest searchReq = new SearchRequest
                {
                    TenantGUID = tenantGuid,
                    Expr = expr,
                    Ordering = EnumerationOrderEnum.CreatedDescending
                };
            string searchReqJson = Serializer.SerializeJson(searchReq);
            string result = await _McpClient.CallAsync<string>("graph/search", new { searchRequest = searchReqJson }, 30000, token).ConfigureAwait(false);
                SearchResult searchResult = Serializer.DeserializeJson<SearchResult>(result);
                List<Graph> graphResult = searchResult?.Graphs ?? new List<Graph>();
                if (graphResult != null && graphResult.Count > 0) resultJson = Serializer.SerializeJson(graphResult);
            }
            else if (str.Equals("node"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                SearchRequest searchReq = new SearchRequest
                {
                    TenantGUID = tenantGuid,
                    GraphGUID = graphGuid,
                    Expr = expr,
                    Ordering = EnumerationOrderEnum.CreatedDescending
                };
                string searchReqJson = Serializer.SerializeJson(searchReq);
                string result = await _McpClient.CallAsync<string>("node/search", new { searchRequest = searchReqJson }, 30000, token).ConfigureAwait(false);
                SearchResult nodeSearchResult = Serializer.DeserializeJson<SearchResult>(result);
                List<Node> nodeResult = nodeSearchResult?.Nodes ?? new List<Node>();
                if (nodeResult != null) resultJson = Serializer.SerializeJson(nodeResult);
            }
            else if (str.Equals("edge"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                SearchRequest searchReq = new SearchRequest
                {
                    TenantGUID = tenantGuid,
                    GraphGUID = graphGuid,
                    Expr = expr,
                    Ordering = EnumerationOrderEnum.CreatedDescending
                };
                string searchReqJson = Serializer.SerializeJson(searchReq);
                string result = await _McpClient.CallAsync<string>("edge/search", new { searchRequest = searchReqJson }, 30000, token).ConfigureAwait(false);
                SearchResult edgeSearchResult = Serializer.DeserializeJson<SearchResult>(result);
                List<Edge> edgeResult = edgeSearchResult?.Edges ?? new List<Edge>();
                if (edgeResult != null) resultJson = Serializer.SerializeJson(edgeResult);
            }

            Console.WriteLine("");
            if (!String.IsNullOrEmpty(resultJson)) Console.WriteLine(resultJson);
            else Console.WriteLine("(null)");
            Console.WriteLine("");
        }

        static Expr GetExpression()
        {
            Console.WriteLine("");
            Console.WriteLine("Example expressions:");

            Expr e1 = new Expr("Age", OperatorEnum.GreaterThan, 38);
            e1.PrependAnd("Hobby.Name", OperatorEnum.Equals, "BJJ");
            Console.WriteLine(Serializer.SerializeJson(e1, false));

            Expr e2 = new Expr("Mbps", OperatorEnum.GreaterThan, 250);
            Console.WriteLine(Serializer.SerializeJson(e2, false));
            Console.WriteLine("");

            string json = Inputty.GetString("JSON:", null, true);
            if (String.IsNullOrEmpty(json)) return null;

            Expr expr = Serializer.DeserializeJson<Expr>(json);
            Console.WriteLine("");
            Console.WriteLine("Using expression: " + expr.ToString());
            Console.WriteLine("");
            return expr;
        }

        static async Task NodeEdgesTo(CancellationToken token = default)
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
            Guid guid = Inputty.GetGuid("Node GUID   :", default(Guid));
            string result = await _McpClient.CallAsync<string>("edge/tonode", new { tenantGuid = tenantGuid.ToString(), graphGuid = graphGuid.ToString(), nodeGuid = guid.ToString() }, 30000, token).ConfigureAwait(false);
            List<Edge> edges = Serializer.DeserializeJson<List<Edge>>(result);
            object obj = edges;

            if (obj != null)
                Console.WriteLine(Serializer.SerializeJson(obj, true));
        }

        static async Task NodeEdgesFrom(CancellationToken token = default)
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
            Guid guid = Inputty.GetGuid("Node GUID   :", default(Guid));
            string result = await _McpClient.CallAsync<string>("edge/fromnode", new { tenantGuid = tenantGuid.ToString(), graphGuid = graphGuid.ToString(), nodeGuid = guid.ToString() }, 30000, token).ConfigureAwait(false);
            List<Edge> edges = Serializer.DeserializeJson<List<Edge>>(result);
            object obj = edges;

            if (obj != null)
                Console.WriteLine(Serializer.SerializeJson(obj, true));
        }

        static async Task NodeEdgesBetween(CancellationToken token = default)
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
            Guid fromGuid = Inputty.GetGuid("From GUID   :", default(Guid));
            Guid toGuid = Inputty.GetGuid("To GUID     :", default(Guid));
            string result = await _McpClient.CallAsync<string>("edge/betweennodes", new { tenantGuid = tenantGuid.ToString(), graphGuid = graphGuid.ToString(), fromNodeGuid = fromGuid.ToString(), toNodeGuid = toGuid.ToString() }, 30000, token).ConfigureAwait(false);
            List<Edge> edges = Serializer.DeserializeJson<List<Edge>>(result);
            object obj = edges;

            if (obj != null)
                Console.WriteLine(Serializer.SerializeJson(obj, true));
        }

        static async Task NodeParents(CancellationToken token = default)
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
            Guid guid = Inputty.GetGuid("Node GUID   :", default(Guid));
            string result = await _McpClient.CallAsync<string>("node/parents", new { tenantGuid = tenantGuid.ToString(), graphGuid = graphGuid.ToString(), nodeGuid = guid.ToString() }, 30000, token).ConfigureAwait(false);
            object obj = Serializer.DeserializeJson<object>(result);

            if (obj != null)
                Console.WriteLine(Serializer.SerializeJson(obj, true));
        }

        static async Task NodeChildren(CancellationToken token = default)
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
            Guid guid = Inputty.GetGuid("Node GUID   :", default(Guid));
            string result = await _McpClient.CallAsync<string>("node/children", new { tenantGuid = tenantGuid.ToString(), graphGuid = graphGuid.ToString(), nodeGuid = guid.ToString() }, 30000, token).ConfigureAwait(false);
            object obj = Serializer.DeserializeJson<object>(result);

            if (obj != null)
                Console.WriteLine(Serializer.SerializeJson(obj, true));
        }

        static async Task NodeNeighbors(CancellationToken token = default)
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
            Guid guid = Inputty.GetGuid("Node GUID   :", default(Guid));
            string result = await _McpClient.CallAsync<string>("node/neighbors", new { tenantGuid = tenantGuid.ToString(), graphGuid = graphGuid.ToString(), nodeGuid = guid.ToString() }, 30000, token).ConfigureAwait(false);
            object obj = Serializer.DeserializeJson<object>(result);

            if (obj != null)
                Console.WriteLine(Serializer.SerializeJson(obj, true));
        }

        static async Task NodeMostConnected(CancellationToken token = default)
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
            string result = await _McpClient.CallAsync<string>(
                "node/readmostconnected",
                new { tenantGuid = tenantGuid.ToString(), graphGuid = graphGuid.ToString() },
                30000,
                token).ConfigureAwait(false);
            List<Node> nodes = Serializer.DeserializeJson<List<Node>>(result);
            object obj = nodes;

            if (obj != null)
                Console.WriteLine(Serializer.SerializeJson(obj, true));
        }

        static async Task NodeLeastConnected(CancellationToken token = default)
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
            string result = await _McpClient.CallAsync<string>(
                "node/readleastconnected",
                new { tenantGuid = tenantGuid.ToString(), graphGuid = graphGuid.ToString() },
                30000,
                token).ConfigureAwait(false);
            List<Node> nodes = Serializer.DeserializeJson<List<Node>>(result);
            object obj = nodes;

            if (obj != null)
                Console.WriteLine(Serializer.SerializeJson(obj, true));
        }

        #endregion
    }
}