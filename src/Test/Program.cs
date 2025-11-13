namespace Test
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using ExpressionTree;
    using GetSomeInput;
    using LiteGraph;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.Helpers;
    using LiteGraph.Serialization;

    class Program
    {
        static bool _RunForever = true;
        static bool _Debug = false;
        static Serializer _Serializer = new Serializer();
        static LiteGraphClient _Client = null;
        static Guid _TenantGuid = Guid.Parse("00000000-0000-0000-0000-000000000000");
        static Guid _GraphGuid = Guid.Parse("00000000-0000-0000-0000-000000000000");

        static void Main(string[] args)
        {
            _Client = new LiteGraphClient(new SqliteGraphRepository());
            _Client.Logging.MinimumSeverity = 0;
            _Client.Logging.Logger = Logger;
            _Client.Logging.LogQueries = _Debug;
            _Client.Logging.LogResults = _Debug;

            _Client.InitializeRepository();

            while (_RunForever)
            {
                string userInput = Inputty.GetString("Command [? for help]:", null, false);

                if (userInput.Equals("?")) Menu();
                else if (userInput.Equals("q")) _RunForever = false;
                else if (userInput.Equals("cls")) Console.Clear();
                else if (userInput.Equals("backup")) BackupDatabase();
                else if (userInput.Equals("debug")) ToggleDebug();
                else if (userInput.Equals("tenant")) SetTenant();
                else if (userInput.Equals("graph")) SetGraph();
                else if (userInput.Equals("load1")) LoadGraph1();
                else if (userInput.Equals("load2")) LoadGraph2();
                else if (userInput.Equals("route")) FindRoutes();
                else if (userInput.Equals("test1-1")) Test1_1();
                else if (userInput.Equals("test1-2")) Test1_2();
                else if (userInput.Equals("test1-3")) Test1_3();
                else if (userInput.Equals("test1-4")) Test1_4();
                else if (userInput.Equals("test2-1")) Test2_1();
                else if (userInput.Equals("test3-1")) Test3_1();
                else if (userInput.Equals("test3-2")) Test3_2();
                else if (userInput.Equals("test3-3")) Test3_3();
                else if (userInput.Equals("subgraph")) TestSubgraph();
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
                            if (parts[1].Equals("create")) Create(parts[0]);
                            else if (parts[1].Equals("all")) All(parts[0]);
                            else if (parts[1].Equals("read")) Read(parts[0]);
                            else if (parts[1].Equals("exists")) Exists(parts[0]);
                            else if (parts[1].Equals("update")) Update(parts[0]);
                            else if (parts[1].Equals("delete")) Delete(parts[0]);
                            else if (parts[1].Equals("search")) Search(parts[0]);

                            if (parts[0].Equals("node"))
                            {
                                if (parts[1].Equals("edgesto")) NodeEdgesTo();
                                else if (parts[1].Equals("edgesfrom")) NodeEdgesFrom();
                                else if (parts[1].Equals("edgesbetween")) NodeEdgesBetween();
                                else if (parts[1].Equals("parents")) NodeParents();
                                else if (parts[1].Equals("children")) NodeChildren();
                                else if (parts[1].Equals("neighbors")) NodeNeighbors();
                                else if (parts[1].Equals("mostconnected")) NodeMostConnected();
                                else if (parts[1].Equals("leastconnected")) NodeLeastConnected();
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
                // Dispose the client
                _Client?.Dispose();
                
                // Clean up database files
                string[] dbFiles = {
                    "litegraph.db",
                    "litegraph.db-shm",
                    "litegraph.db-wal"
                };
                
                foreach (string file in dbFiles)
                {
                    if (File.Exists(file))
                    {
                        try
                        {
                            File.Delete(file);
                            Console.WriteLine($"[OK] Deleted {file}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[WARNING] Could not delete {file}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] Error during cleanup: {ex.Message}");
            }
            
            Console.WriteLine("[OK] Cleanup completed");
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

        static void BackupDatabase()
        {
            string filename = Inputty.GetString("Backup filename:", null, true);
            if (String.IsNullOrEmpty(filename)) return;
            _Client.Admin.Backup(filename).GetAwaiter().GetResult();
        }

        static void ToggleDebug()
        {
            _Debug = !_Debug;
            _Client.Logging.LogQueries = _Debug;
            _Client.Logging.LogResults = _Debug;
        }

        static void SetTenant()
        {
            _TenantGuid = Inputty.GetGuid("Tenant GUID:", _TenantGuid);
        }

        static void SetGraph()
        {
            _GraphGuid = Inputty.GetGuid("Graph GUID:", _GraphGuid);
        }

        #region Graph-1

        static void LoadGraph1()
        {
            #region Tenant

            TenantMetadata tenant = _Client.Tenant.Create(new TenantMetadata { Name = "Test tenant" });

            #endregion

            #region Labels

            List<string> labelsGraph = new List<string>
            {
                "graph"
            };

            List<string> labelsOdd = new List<string>
            {
                "odd"
            };

            List<string> labelsEven = new List<string>
            {
                "even"
            };

            List<string> labelsNode = new List<string>
            {
                "node"
            };

            List<string> labelsEdge = new List<string>
            {
                "edge"
            };

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

            Guid graphGuid = Guid.NewGuid();

            List<VectorMetadata> graphVectors = new List<VectorMetadata>
            {
                new VectorMetadata
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graphGuid,
                    Model = "testmodel",
                    Dimensionality = 3,
                    Content = "testcontent",
                    Vectors = embeddings1                    
                }
            };

            Console.WriteLine("| Creating graph with GUID " + graphGuid);

            Graph graph = _Client.Graph.Create(new Graph
            {
                TenantGUID = tenant.GUID,
                GUID = graphGuid,
                Name = "Sample Graph 1",
                Labels = labelsGraph,
                Tags = tagsGraph,
                Vectors = graphVectors
            });

            #endregion

            #region Nodes

            Guid node1Guid = Guid.NewGuid();
            Console.WriteLine("| Creating node 1 " + node1Guid + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Node n1 = _Client.Node.Create(new Node
            {
                GUID = node1Guid,
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "1",
                Labels = StringHelpers.Combine(labelsOdd, labelsNode),
                Tags = NvcHelpers.Combine(tagsOdd, tagsNode),
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
            });

            Guid node2Guid = Guid.NewGuid();
            Console.WriteLine("| Creating node 2 " + node2Guid + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Node n2 = _Client.Node.Create(new Node
            {
                GUID = node2Guid,
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "2",
                Labels = StringHelpers.Combine(labelsEven, labelsNode),
                Tags = NvcHelpers.Combine(tagsEven, tagsNode),
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
            });

            Guid node3Guid = Guid.NewGuid();
            Console.WriteLine("| Creating node 3 " + node3Guid + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Node n3 = _Client.Node.Create(new Node
            {
                GUID = node3Guid,
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "3",
                Labels = StringHelpers.Combine(labelsOdd, labelsNode),
                Tags = NvcHelpers.Combine(tagsOdd, tagsNode),
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
            });

            Guid node4Guid = Guid.NewGuid();
            Console.WriteLine("| Creating node 4 " + node4Guid + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Node n4 = _Client.Node.Create(new Node
            {
                GUID = node4Guid,
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "4",
                Labels = StringHelpers.Combine(labelsEven, labelsNode),
                Tags = NvcHelpers.Combine(tagsEven, tagsNode),
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
            });

            Guid node5Guid = Guid.NewGuid();
            Console.WriteLine("| Creating node 5 " + node5Guid + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Node n5 = _Client.Node.Create(new Node
            {
                GUID = node5Guid,
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "5",
                Labels = StringHelpers.Combine(labelsOdd, labelsNode),
                Tags = NvcHelpers.Combine(tagsOdd, tagsNode),
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
            });

            Guid node6Guid = Guid.NewGuid();
            Console.WriteLine("| Creating node 6 " + node6Guid + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Node n6 = _Client.Node.Create(new Node
            {
                GUID = node6Guid,
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "6",
                Labels = StringHelpers.Combine(labelsEven, labelsNode),
                Tags = NvcHelpers.Combine(tagsEven, tagsNode),
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
            });

            Guid node7Guid = Guid.NewGuid();
            Console.WriteLine("| Creating node 7 " + node7Guid + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Node n7 = _Client.Node.Create(new Node
            {
                GUID = node7Guid,
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "7",
                Labels = StringHelpers.Combine(labelsOdd, labelsNode),
                Tags = NvcHelpers.Combine(tagsOdd, tagsNode),
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
            });

            Guid node8Guid = Guid.NewGuid();
            Console.WriteLine("| Creating node 8 " + node8Guid + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Node n8 = _Client.Node.Create(new Node
            {
                GUID = node8Guid,
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "8",
                Labels = StringHelpers.Combine(labelsEven, labelsNode),
                Tags = NvcHelpers.Combine(tagsEven, tagsNode),
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
            });

            #endregion

            #region Edges

            Edge e1 = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n1.GUID,
                To = n4.GUID,
                Name = "1 to 4",
                Cost = 1,
                Labels = StringHelpers.Combine(labelsOdd, labelsEdge),
                Tags = NvcHelpers.Combine(tagsOdd, tagsEdge)
            }).GetAwaiter().GetResult();

            Edge e2 = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n1.GUID,
                To = n5.GUID,
                Name = "1 to 5",
                Cost = 2,
                Labels = StringHelpers.Combine(labelsEven, labelsEdge),
                Tags = NvcHelpers.Combine(tagsEven, tagsEdge)
            }).GetAwaiter().GetResult();

            Edge e3 = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n2.GUID,
                To = n4.GUID,
                Name = "2 to 4",
                Cost = 3,
                Labels = StringHelpers.Combine(labelsOdd, labelsEdge),
                Tags = NvcHelpers.Combine(tagsOdd, tagsEdge)
            }).GetAwaiter().GetResult();

            Edge e4 = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n2.GUID,
                To = n5.GUID,
                Name = "2 to 5",
                Cost = 4,
                Labels = StringHelpers.Combine(labelsEven, labelsEdge),
                Tags = NvcHelpers.Combine(tagsEven, tagsEdge)
            }).GetAwaiter().GetResult();

            Edge e5 = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n3.GUID,
                To = n4.GUID,
                Name = "3 to 4",
                Cost = 5,
                Labels = StringHelpers.Combine(labelsOdd, labelsEdge),
                Tags = NvcHelpers.Combine(tagsOdd, tagsEdge)
            }).GetAwaiter().GetResult();

            Edge e6 = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n3.GUID,
                To = n5.GUID,
                Name = "3 to 5",
                Cost = 6,
                Labels = StringHelpers.Combine(labelsEven, labelsEdge),
                Tags = NvcHelpers.Combine(tagsEven, tagsEdge)
            }).GetAwaiter().GetResult();

            Edge e7 = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n4.GUID,
                To = n6.GUID,
                Name = "4 to 6",
                Cost = 7,
                Labels = StringHelpers.Combine(labelsOdd, labelsEdge),
                Tags = NvcHelpers.Combine(tagsOdd, tagsEdge)
            }).GetAwaiter().GetResult();

            Edge e8 = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n4.GUID,
                To = n7.GUID,
                Name = "4 to 7",
                Cost = 8,
                Labels = StringHelpers.Combine(labelsEven, labelsEdge),
                Tags = NvcHelpers.Combine(tagsEven, tagsEdge)
            }).GetAwaiter().GetResult();

            Edge e9 = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n4.GUID,
                To = n8.GUID,
                Name = "4 to 8",
                Cost = 9,
                Labels = StringHelpers.Combine(labelsOdd, labelsEdge),
                Tags = NvcHelpers.Combine(tagsOdd, tagsEdge)
            }).GetAwaiter().GetResult();

            Edge e10 = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n5.GUID,
                To = n6.GUID,
                Name = "5 to 6",
                Cost = 10,
                Labels = StringHelpers.Combine(labelsEven, labelsEdge),
                Tags = NvcHelpers.Combine(tagsEven, tagsEdge)
            }).GetAwaiter().GetResult();

            Edge e11 = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n5.GUID,
                To = n7.GUID,
                Name = "5 to 7",
                Cost = 11,
                Labels = StringHelpers.Combine(labelsOdd, labelsEdge),
                Tags = NvcHelpers.Combine(tagsOdd, tagsEdge)
            }).GetAwaiter().GetResult();

            Edge e12 = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n5.GUID,
                To = n8.GUID,
                Name = "5 to 8",
                Cost = 12,
                Labels = StringHelpers.Combine(labelsEven, labelsEdge),
                Tags = NvcHelpers.Combine(tagsEven, tagsEdge)
            }).GetAwaiter().GetResult();

            #endregion
        }

        static void Test1_1()
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);

            Console.WriteLine("");
            Console.WriteLine("Retrieving graphs where label = 'graph'");

            List<string> labelGraph = new List<string>
            {
                "graph"
            };

            foreach (Graph graph in _Client.Graph.ReadMany(tenantGuid, null, labelGraph))
                Console.WriteLine("| " + graph.GUID + ": " + graph.Name);

            Console.WriteLine("");
            Console.WriteLine("Retrieving nodes with labels 'node' and 'even'");

            List<string> labelEvenNodes = new List<string>
            {
                "node",
                "even"
            };

            foreach (Node node in _Client.Node.ReadMany(tenantGuid, graphGuid, null, labelEvenNodes))
                Console.WriteLine("| " + node.GUID + ": " + node.Name);

            Console.WriteLine("");
            Console.WriteLine("Retrieving edges with labels 'edge' and 'odd'");

            List<string> labelOddEdges = new List<string>
            {
                "edge",
                "odd"
            };

            var enumerator = _Client.Edge.ReadMany(tenantGuid, graphGuid, null, labelOddEdges).GetAsyncEnumerator();
            try
            {
                while (enumerator.MoveNextAsync().GetAwaiter().GetResult())
                {
                    Edge edge = enumerator.Current;
                    Console.WriteLine("| " + edge.GUID + ": " + edge.Name);
                }
            }
            finally
            {
                enumerator.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }

            Console.WriteLine("");
        }

        static void Test1_2()
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);

            Console.WriteLine("");
            Console.WriteLine("Retrieving graphs where tag 'type' = 'graph'");

            NameValueCollection tagsGraph = new NameValueCollection();
            tagsGraph.Add("type", "graph");

            foreach (Graph graph in _Client.Graph.ReadMany(tenantGuid, null, null, tagsGraph))
                Console.WriteLine("| " + graph.GUID + ": " + graph.Name);

            Console.WriteLine("");
            Console.WriteLine("Retrieving nodes where tag 'type' = 'node' and 'isEven' = 'true'");

            NameValueCollection tagsEvenNodes = new NameValueCollection();
            tagsEvenNodes.Add("type", "node");
            tagsEvenNodes.Add("isEven", "true");

            foreach (Node node in _Client.Node.ReadMany(tenantGuid, graphGuid, null, null, tagsEvenNodes))
                Console.WriteLine("| " + node.GUID + ": " + node.Name);

            Console.WriteLine("");
            Console.WriteLine("Retrieving edges where tag 'type' = 'edge' and 'isEven' = 'false'");

            NameValueCollection tagsOddEdges = new NameValueCollection();
            tagsOddEdges.Add("type", "edge");
            tagsOddEdges.Add("isEven", "false");

            var enumerator = _Client.Edge.ReadMany(tenantGuid, graphGuid, null, null, tagsOddEdges).GetAsyncEnumerator();
            try
            {
                while (enumerator.MoveNextAsync().GetAwaiter().GetResult())
                {
                    Edge edge = enumerator.Current;
                    Console.WriteLine("| " + edge.GUID + ": " + edge.Name);
                }
            }
            finally
            {
                enumerator.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }

            Console.WriteLine("");
        }

        static void Test1_3()
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);

            Console.WriteLine("");
            Console.WriteLine("Retrieving graphs where label = 'graph', and tag 'type' = 'graph'");

            List<string> labelGraph = new List<string>
            {
                "graph"
            };

            NameValueCollection tagsGraph = new NameValueCollection();
            tagsGraph.Add("type", "graph");

            foreach (Graph graph in _Client.Graph.ReadMany(tenantGuid, null, labelGraph, tagsGraph))
                Console.WriteLine("| " + graph.GUID + ": " + graph.Name);

            Console.WriteLine("");
            Console.WriteLine("Retrieving nodes where labels 'node' and 'even' are present, and tag 'type' = 'node' and 'isEven' = 'true'");

            List<string> labelEvenNodes = new List<string>
            {
                "node",
                "even"
            };

            NameValueCollection tagsEvenNodes = new NameValueCollection();
            tagsEvenNodes.Add("type", "node");
            tagsEvenNodes.Add("isEven", "true");

            foreach (Node node in _Client.Node.ReadMany(tenantGuid, graphGuid, null, labelEvenNodes, tagsEvenNodes))
                Console.WriteLine("| " + node.GUID + ": " + node.Name);

            Console.WriteLine("");
            Console.WriteLine("Retrieving edges where labels 'edge' and 'odd' are present, and tag 'type' = 'edge' and 'isEven' = 'false'");

            List<string> labelOddEdges = new List<string>
            {
                "edge",
                "odd"
            };

            NameValueCollection tagsOddEdges = new NameValueCollection();
            tagsOddEdges.Add("type", "edge");
            tagsOddEdges.Add("isEven", "false");

            var enumerator = _Client.Edge.ReadMany(tenantGuid, graphGuid, null, labelOddEdges, tagsOddEdges).GetAsyncEnumerator();
            try
            {
                while (enumerator.MoveNextAsync().GetAwaiter().GetResult())
                {
                    Edge edge = enumerator.Current;
                    Console.WriteLine("| " + edge.GUID + ": " + edge.Name);
                }
            }
            finally
            {
                enumerator.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }

            Console.WriteLine("");
        }

        static void Test1_4()
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);

            #region Cosine-Similarity

            VectorSearchRequest searchReqCosineSim = new VectorSearchRequest
            {
                TenantGUID = _TenantGuid,
                GraphGUID = _GraphGuid,
                Domain = VectorSearchDomainEnum.Node,
                SearchType = VectorSearchTypeEnum.CosineSimilarity,
                Embeddings = new List<float> { 0.1f, 0.2f, 0.3f }
            };

            Console.WriteLine("");
            Console.WriteLine("Retrieving nodes by cosine similarity to embeddings [ 0.1, 0.2, 0.3 ]");

            foreach (VectorSearchResult result in _Client.Vector.Search(searchReqCosineSim).OrderByDescending(p => p.Score))
            {
                Console.WriteLine("| Node " + result.Node.GUID + " " + result.Node.Name + ": score " + result.Score);
            }

            #endregion

            #region Cosine-Distance

            VectorSearchRequest searchReqCosineDis = new VectorSearchRequest
            {
                TenantGUID = _TenantGuid,
                GraphGUID = _GraphGuid,
                Domain = VectorSearchDomainEnum.Node,
                SearchType = VectorSearchTypeEnum.CosineDistance,
                Embeddings = new List<float> { 0.1f, 0.2f, 0.3f }
            };

            Console.WriteLine("");
            Console.WriteLine("Retrieving nodes by cosine distance from embeddings [ 0.1, 0.2, 0.3 ]");

            foreach (VectorSearchResult result in _Client.Vector.Search(searchReqCosineDis).OrderBy(p => p.Distance))
            {
                Console.WriteLine("| Node " + result.Node.GUID + " " + result.Node.Name + ": distance " + result.Distance);
            }

            #endregion

            #region Euclidian-Similarity

            VectorSearchRequest searchReqEucSim = new VectorSearchRequest
            {
                TenantGUID = _TenantGuid,
                GraphGUID = _GraphGuid,
                Domain = VectorSearchDomainEnum.Node,
                SearchType = VectorSearchTypeEnum.EuclidianSimilarity,
                Embeddings = new List<float> { 0.1f, 0.2f, 0.3f }
            };

            Console.WriteLine("");
            Console.WriteLine("Retrieving nodes by Euclidian similarity to embeddings [ 0.1, 0.2, 0.3 ]");

            foreach (VectorSearchResult result in _Client.Vector.Search(searchReqEucSim).OrderByDescending(p => p.Score))
            {
                Console.WriteLine("| Node " + result.Node.GUID + " " + result.Node.Name + ": score " + result.Score);
            }

            #endregion

            #region Euclidian-Distance

            VectorSearchRequest searchReqEucDis = new VectorSearchRequest
            {
                TenantGUID = _TenantGuid,
                GraphGUID = _GraphGuid,
                Domain = VectorSearchDomainEnum.Node,
                SearchType = VectorSearchTypeEnum.EuclidianDistance,
                Embeddings = new List<float> { 0.1f, 0.2f, 0.3f }
            };

            Console.WriteLine("");
            Console.WriteLine("Retrieving nodes by Euclidian distance from embeddings [ 0.1, 0.2, 0.3 ]");

            foreach (VectorSearchResult result in _Client.Vector.Search(searchReqEucDis).OrderBy(p => p.Distance))
            {
                Console.WriteLine("| Node " + result.Node.GUID + " " + result.Node.Name + ": distance " + result.Distance);
            }

            #endregion

            #region Inner-Product

            VectorSearchRequest searchReqDp = new VectorSearchRequest
            {
                TenantGUID = _TenantGuid,
                GraphGUID = _GraphGuid,
                Domain = VectorSearchDomainEnum.Node,
                SearchType = VectorSearchTypeEnum.DotProduct,
                Embeddings = new List<float> { 0.1f, 0.2f, 0.3f }
            };

            Console.WriteLine("");
            Console.WriteLine("Retrieving nodes by dot product with embeddings [ 0.1, 0.2, 0.3 ]");

            foreach (VectorSearchResult result in _Client.Vector.Search(searchReqDp).OrderByDescending(p => p.InnerProduct))
            {
                Console.WriteLine("| Node " + result.Node.GUID + " " + result.Node.Name + ": inner product " + result.InnerProduct);
            }

            #endregion

            Console.WriteLine("");
        }

        #endregion

        #region Graph-2

        static void LoadGraph2()
        {
            #region Tenant

            TenantMetadata tenant = _Client.Tenant.Create(new TenantMetadata { Name = "Test tenant" });

            #endregion

            #region Graph

            Graph graph = _Client.Graph.Create(new Graph
            {
                TenantGUID = tenant.GUID,
                Name = "Sample Graph 2"
            });

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

            Node joelNode = _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Joel", Data = joel });
            Node yipNode = _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Yip", Data = yip });
            Node keithNode = _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Keith", Data = keith });
            Node alexNode = _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Alex", Data = alex });
            Node blakeNode = _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Blake", Data = blake });

            Node xfiNode = _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Xfinity", Data = xfi });
            Node starlinkNode = _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Starlink", Data = starlink });
            Node attNode = _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "AT&T", Data = att });

            Node internetNode = _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Internet", Data = internet });

            Node equinixNode = _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Equinix", Data = equinix });
            Node awsNode = _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "AWS", Data = aws });
            Node azureNode = _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Azure", Data = azure });
            Node digitalOceanNode = _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "DigitalOcean", Data = digitalOcean });
            Node rackspaceNode = _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Rackspace", Data = rackspace });

            Node ccpNode = _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Control Plane", Data = ccp });
            Node websiteNode = _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Website", Data = website });
            Node adNode = _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Active Directory", Data = ad });

            #endregion

            #region Edges

            Edge joelXfiEdge = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = joelNode.GUID,
                To = xfiNode.GUID,
                Name = "Joel to Xfinity"
            }).GetAwaiter().GetResult();

            Edge joelStarlinkEdge = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = joelNode.GUID,
                To = starlinkNode.GUID,
                Name = "Joel to Starlink"
            }).GetAwaiter().GetResult();

            Edge yipXfiEdge = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = yipNode.GUID,
                To = xfiNode.GUID,
                Name = "Yip to Xfinity"
            }).GetAwaiter().GetResult();

            Edge keithStarlinkEdge = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = keithNode.GUID,
                To = starlinkNode.GUID,
                Name = "Keith to Starlink"
            }).GetAwaiter().GetResult();

            Edge keithXfiEdge = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = keithNode.GUID,
                To = xfiNode.GUID,
                Name = "Keith to Xfinity"
            }).GetAwaiter().GetResult();

            Edge keithAttEdge = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = keithNode.GUID,
                To = attNode.GUID,
                Name = "Keith to AT&T"
            }).GetAwaiter().GetResult();

            Edge alexAttEdge = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = alexNode.GUID,
                To = attNode.GUID,
                Name = "Alex to AT&T"
            }).GetAwaiter().GetResult();

            Edge blakeAttEdge = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = blakeNode.GUID,
                To = attNode.GUID,
                Name = "Blake to AT&T"
            }).GetAwaiter().GetResult();

            Edge xfiInternetEdge1 = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = xfiNode.GUID,
                To = internetNode.GUID,
                Name = "Xfinity to Internet 1"
            }).GetAwaiter().GetResult();

            Edge xfiInternetEdge2 = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = xfiNode.GUID,
                To = internetNode.GUID,
                Name = "Xfinity to Internet 2"
            }).GetAwaiter().GetResult();

            Edge starlinkInternetEdge = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = starlinkNode.GUID,
                To = internetNode.GUID,
                Name = "Starlink to Internet"
            }).GetAwaiter().GetResult();

            Edge attInternetEdge1 = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = attNode.GUID,
                To = internetNode.GUID,
                Name = "AT&T to Internet 1"
            }).GetAwaiter().GetResult();

            Edge attInternetEdge2 = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = attNode.GUID,
                To = internetNode.GUID,
                Name = "AT&T to Internet 2"
            }).GetAwaiter().GetResult();

            Edge internetEquinixEdge = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = internetNode.GUID,
                To = equinixNode.GUID,
                Name = "Internet to Equinix"
            }).GetAwaiter().GetResult();

            Edge internetAwsEdge = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = internetNode.GUID,
                To = awsNode.GUID,
                Name = "Internet to AWS"
            }).GetAwaiter().GetResult();

            Edge internetAzureEdge = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = internetNode.GUID,
                To = azureNode.GUID,
                Name = "Internet to Azure"
            }).GetAwaiter().GetResult();

            Edge equinixDoEdge = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = equinixNode.GUID,
                To = digitalOceanNode.GUID,
                Name = "Equinix to DigitalOcean"
            }).GetAwaiter().GetResult();

            Edge equinixAwsEdge = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = equinixNode.GUID,
                To = awsNode.GUID,
                Name = "Equinix to AWS"
            }).GetAwaiter().GetResult();

            Edge equinixRackspaceEdge = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = equinixNode.GUID,
                To = rackspaceNode.GUID,
                Name = "Equinix to Rackspace"
            }).GetAwaiter().GetResult();

            Edge awsWebsiteEdge = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = awsNode.GUID,
                To = websiteNode.GUID,
                Name = "AWS to Website"
            }).GetAwaiter().GetResult();

            Edge azureAdEdge = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = azureNode.GUID,
                To = adNode.GUID,
                Name = "Azure to Active Directory"
            }).GetAwaiter().GetResult();

            Edge doCcpEdge = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = digitalOceanNode.GUID,
                To = ccpNode.GUID,
                Name = "DigitalOcean to Control Plane"
            }).GetAwaiter().GetResult();

            #endregion
        }

        static void Test2_1()
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
            Expr e1 = new Expr("$.Name", OperatorEnum.Equals, "Joel");
            Expr e2 = new Expr("$.Age", OperatorEnum.GreaterThan, 38);

            Console.WriteLine("");
            Console.WriteLine("Retrieving nodes where Name = 'Joel'");
            foreach (Node node in _Client.Node.ReadMany(tenantGuid, graphGuid, null, null, null, e1))
            {
                // Console.WriteLine(node.Data.ToString());
                Console.WriteLine(_Client.ConvertData<Person>(node.Data).ToString());
            }

            Console.WriteLine("");
            Console.WriteLine("Retrieve nodes where Age >= 38");
            foreach (Node node in _Client.Node.ReadMany(tenantGuid, graphGuid, null, null, null, e2))
            {
                // Console.WriteLine(node.Data.ToString());
                Console.WriteLine(_Client.ConvertData<Person>(node.Data).ToString());
            }

            Console.WriteLine("");
        }

        #endregion

        #region Misc

        static void Test3_1()
        {
            TenantMetadata tenant = _Client.Tenant.Create(new TenantMetadata
            {
                Name = "Test"
            });

            Graph graph = _Client.Graph.Create(new Graph
            {
                TenantGUID = tenant.GUID,
                Name = "Test"
            });

            Node node1 = new Node
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "Node1",
                Data = new { Text = "hello" }
            };

            Console.WriteLine(_Serializer.SerializeJson(node1));

            node1 = _Client.Node.Create(node1);
        }

        static void Test3_2()
        {
            TenantMetadata tenant = _Client.Tenant.Create(new TenantMetadata
            {
                Name = "Test"
            });

            Graph graph = _Client.Graph.Create(new Graph
            {
                TenantGUID = tenant.GUID,
                Name = "Test"
            });

            object data = new { Text = "hello" };

            Node node1 = new Node
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "Node1",
                Data = data
            };

            Console.WriteLine(_Serializer.SerializeJson(node1));

            node1 = _Client.Node.Create(node1);
        }

        static void Test3_3()
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

            EnumerationResult<Graph> graphs = _Client.Graph.Enumerate(query);
            Console.WriteLine(_Serializer.SerializeJson(graphs, true));

            EnumerationResult<Node> nodes = _Client.Node.Enumerate(query);
            Console.WriteLine(_Serializer.SerializeJson(nodes, true));

            EnumerationResult<Edge> edges = _Client.Edge.Enumerate(query).GetAwaiter().GetResult();
            Console.WriteLine(_Serializer.SerializeJson(edges, true));
        }

        #endregion

        #region Primitives

        static void FindRoutes()
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
            Guid fromGuid = Inputty.GetGuid("From GUID  :", default(Guid));
            Guid toGuid = Inputty.GetGuid("To GUID    :", default(Guid));
            object obj = _Client.Node.ReadRoutes(SearchTypeEnum.DepthFirstSearch, tenantGuid, graphGuid, fromGuid, toGuid);

            if (obj != null)
                Console.WriteLine(_Serializer.SerializeJson(obj, true));
        }

        static void Create(string str)
        {
            object obj = null;
            string json = null;

            if (str.Equals("tenant"))
            {
                obj = _Client.Tenant.Create(new TenantMetadata { Name = Inputty.GetString("Name:", null, false) });
            }
            else if (str.Equals("user"))
            {
                obj = _Client.User.Create(new UserMaster
                {
                    TenantGUID = Inputty.GetGuid("Tenant GUID:", _TenantGuid),
                    FirstName = Inputty.GetString("First name:", null, false),
                    LastName = Inputty.GetString("Last name:", null, false),
                    Email = Inputty.GetString("Email:", null, false),
                    Password = Inputty.GetString("Password:", null, false)
                });
            }
            else if (str.Equals("cred"))
            {
                obj = _Client.Credential.Create(new Credential
                {
                    TenantGUID = Inputty.GetGuid("Tenant GUID:", _TenantGuid),
                    UserGUID = Inputty.GetGuid("User GUID:", default(Guid)),
                    Name = Inputty.GetString("Name:", null, false)
                }).GetAwaiter().GetResult();
            }
            else if (str.Equals("graph"))
            {
                obj = _Client.Graph.Create(new Graph
                {
                    TenantGUID = Inputty.GetGuid("Tenant GUID:", _TenantGuid),
                    Name = Inputty.GetString("Name:", null, false)
                });
            }
            else if (str.Equals("node"))
            {
                json = Inputty.GetString("JSON:", null, false);
                obj = _Client.Node.Create(_Serializer.DeserializeJson<Node>(json));
            }
            else if (str.Equals("edge"))
            {
                json = Inputty.GetString("JSON:", null, false);
                obj = _Client.Edge.Create(_Serializer.DeserializeJson<Edge>(json)).GetAwaiter().GetResult();
            }

            if (obj != null)
                Console.WriteLine(_Serializer.SerializeJson(obj, true));
        }

        static void All(string str)
        {
            object obj = null;
            if (str.Equals("tenant"))
            {
                obj = _Client.Tenant.ReadMany();
            }
            else if (str.Equals("user"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                obj = _Client.User.ReadMany(tenantGuid, null);
            }
            else if (str.Equals("cred"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                obj = _Client.Credential.ReadMany(tenantGuid, null, null).GetAwaiter().GetResult();
            }
            else if (str.Equals("graph"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                obj = _Client.Graph.ReadMany(tenantGuid);
            }
            else if (str.Equals("node"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                obj = _Client.Node.ReadMany(tenantGuid, graphGuid);
            }
            else if (str.Equals("edge"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                List<Edge> edges = new List<Edge>();
                var enumerator = _Client.Edge.ReadMany(tenantGuid, graphGuid).GetAsyncEnumerator();
                try
                {
                    while (enumerator.MoveNextAsync().GetAwaiter().GetResult())
                    {
                        edges.Add(enumerator.Current);
                    }
                }
                finally
                {
                    enumerator.DisposeAsync().AsTask().GetAwaiter().GetResult();
                }
                obj = edges;
            }

            if (obj != null)
                Console.WriteLine(_Serializer.SerializeJson(obj, true));
        }

        static void Read(string str)
        {
            object obj = null;

            if (str.Equals("tenant"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                obj = _Client.Tenant.ReadByGuid(tenantGuid);
            }
            else if (str.Equals("user"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid userGuid =   Inputty.GetGuid("GUID        :", default(Guid));
                obj = _Client.User.ReadByGuid(tenantGuid, userGuid);
            }
            else if (str.Equals("cred"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid credGuid = Inputty.GetGuid("GUID        :", default(Guid));
                obj = _Client.Credential.ReadByGuid(tenantGuid, credGuid).GetAwaiter().GetResult();
            }
            else if (str.Equals("graph"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                obj = _Client.Graph.ReadByGuid(tenantGuid, graphGuid);
            }
            else if (str.Equals("node"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                Guid guid = Inputty.GetGuid("Node GUID   :", default(Guid));
                obj = _Client.Node.ReadByGuid(tenantGuid, graphGuid, guid);
            }
            else if (str.Equals("edge"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                Guid guid = Inputty.GetGuid("Edge GUID   :", default(Guid));
                obj = _Client.Edge.ReadByGuid(tenantGuid, graphGuid, guid).GetAwaiter().GetResult();
            }

            if (obj != null)
                Console.WriteLine(_Serializer.SerializeJson(obj, true));
        }

        static void Exists(string str)
        {
            bool exists = false;

            if (str.Equals("tenant"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                exists = _Client.Tenant.ExistsByGuid(tenantGuid);
            }
            else if (str.Equals("user"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid userGuid = Inputty.GetGuid("GUID        :", default(Guid));
                exists = _Client.User.ExistsByGuid(tenantGuid, userGuid);
            }
            else if (str.Equals("cred"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid credGuid = Inputty.GetGuid("GUID        :", default(Guid));
                exists = _Client.Credential.ExistsByGuid(tenantGuid, credGuid).GetAwaiter().GetResult();
            }
            else if (str.Equals("graph"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                exists = _Client.Graph.ExistsByGuid(tenantGuid, graphGuid);
            }
            else if (str.Equals("node"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                Guid guid = Inputty.GetGuid("Node GUID   :", default(Guid));
                exists = _Client.Node.ExistsByGuid(tenantGuid, guid);
            }
            else if (str.Equals("edge"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                Guid guid = Inputty.GetGuid("Edge GUID   :", default(Guid));
                exists = _Client.Edge.ExistsByGuid(tenantGuid, graphGuid, guid).GetAwaiter().GetResult();
            }

            Console.WriteLine("Exists: " + exists);
        }

        static void Update(string str)
        {
            object obj = null;
            string json = Inputty.GetString("JSON:", null, false);

            if (str.Equals("graph"))
            {
                obj = _Client.Tenant.Update(_Serializer.DeserializeJson<TenantMetadata>(json));
            }
            else if (str.Equals("graph"))
            {
                obj = _Client.Graph.Update(_Serializer.DeserializeJson<Graph>(json));
            }
            else if (str.Equals("node"))
            {
                obj = _Client.Node.Update(_Serializer.DeserializeJson<Node>(json));
            }
            else if (str.Equals("edge"))
            {
                obj = _Client.Edge.Update(_Serializer.DeserializeJson<Edge>(json)).GetAwaiter().GetResult();
            }

            if (obj != null)
                Console.WriteLine(_Serializer.SerializeJson(obj, true));
        }

        static void Delete(string str)
        {
            if (str.Equals("tenant"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                bool force = Inputty.GetBoolean("Force       :", true);
                _Client.Tenant.DeleteByGuid(tenantGuid, force);
            }
            else if (str.Equals("user"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid userGuid = Inputty.GetGuid("GUID        :", default(Guid));
                _Client.User.DeleteByGuid(tenantGuid, userGuid);
            }
            else if (str.Equals("cred"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid credGuid = Inputty.GetGuid("GUID        :", default(Guid));
                _Client.Credential.DeleteByGuid(tenantGuid, credGuid).GetAwaiter().GetResult();
            }
            else if (str.Equals("graph"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                bool force = Inputty.GetBoolean("Force       :", true);
                _Client.Graph.DeleteByGuid(tenantGuid, graphGuid, force);
            }
            else if (str.Equals("node"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                Guid guid = Inputty.GetGuid("Node GUID   :", default(Guid));
                _Client.Node.DeleteByGuid(tenantGuid, graphGuid, guid);
            }
            else if (str.Equals("edge"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                Guid guid = Inputty.GetGuid("Edge GUID   :", default(Guid));
                _Client.Edge.DeleteByGuid(tenantGuid, graphGuid, guid).GetAwaiter().GetResult();
            }
        }

        static void Search(string str)
        {
            if (!str.Equals("graph") && !str.Equals("node") && !str.Equals("edge")) return;

            Expr expr = GetExpression();
            string resultJson = null;

            if (str.Equals("graph"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                IEnumerable<Graph> graphResult = _Client.Graph.ReadMany(tenantGuid, null, null, null, expr, EnumerationOrderEnum.CreatedDescending);
                if (graphResult != null) resultJson = _Serializer.SerializeJson(graphResult.ToList());
            }
            else if (str.Equals("node"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                IEnumerable<Node> nodeResult = _Client.Node.ReadMany(tenantGuid, graphGuid, null, null, null, expr, EnumerationOrderEnum.CreatedDescending);
                if (nodeResult != null) resultJson = _Serializer.SerializeJson(nodeResult.ToList());
            }
            else if (str.Equals("edge"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                List<Edge> edgeResult = new List<Edge>();
                var enumerator = _Client.Edge.ReadMany(tenantGuid, graphGuid, null, null, null, expr, EnumerationOrderEnum.CreatedDescending).GetAsyncEnumerator();
                try
                {
                    while (enumerator.MoveNextAsync().GetAwaiter().GetResult())
                    {
                        edgeResult.Add(enumerator.Current);
                    }
                }
                finally
                {
                    enumerator.DisposeAsync().AsTask().GetAwaiter().GetResult();
                }
                if (edgeResult != null) resultJson = _Serializer.SerializeJson(edgeResult);
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
            Console.WriteLine(_Serializer.SerializeJson(e1, false));

            Expr e2 = new Expr("Mbps", OperatorEnum.GreaterThan, 250);
            Console.WriteLine(_Serializer.SerializeJson(e2, false));
            Console.WriteLine("");

            string json = Inputty.GetString("JSON:", null, true);
            if (String.IsNullOrEmpty(json)) return null;

            Expr expr = _Serializer.DeserializeJson<Expr>(json);
            Console.WriteLine("");
            Console.WriteLine("Using expression: " + expr.ToString());
            Console.WriteLine("");
            return expr;
        }

        static void NodeEdgesTo()
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
            Guid guid = Inputty.GetGuid("Node GUID   :", default(Guid));
            List<Edge> edges = new List<Edge>();
            var enumerator = _Client.Edge.ReadEdgesToNode(tenantGuid, graphGuid, guid).GetAsyncEnumerator();
            try
            {
                while (enumerator.MoveNextAsync().GetAwaiter().GetResult())
                {
                    edges.Add(enumerator.Current);
                }
            }
            finally
            {
                enumerator.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
            object obj = edges;

            if (obj != null)
                Console.WriteLine(_Serializer.SerializeJson(obj, true));
        }

        static void NodeEdgesFrom()
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
            Guid guid = Inputty.GetGuid("Node GUID   :", default(Guid));
            List<Edge> edges = new List<Edge>();
            var enumerator = _Client.Edge.ReadEdgesFromNode(tenantGuid, graphGuid, guid).GetAsyncEnumerator();
            try
            {
                while (enumerator.MoveNextAsync().GetAwaiter().GetResult())
                {
                    edges.Add(enumerator.Current);
                }
            }
            finally
            {
                enumerator.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
            object obj = edges;

            if (obj != null)
                Console.WriteLine(_Serializer.SerializeJson(obj, true));
        }

        static void NodeEdgesBetween()
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
            Guid fromGuid = Inputty.GetGuid("From GUID   :", default(Guid));
            Guid toGuid = Inputty.GetGuid("To GUID     :", default(Guid));
            List<Edge> edges = new List<Edge>();
            var enumerator = _Client.Edge.ReadEdgesBetweenNodes(tenantGuid, graphGuid, fromGuid, toGuid).GetAsyncEnumerator();
            try
            {
                while (enumerator.MoveNextAsync().GetAwaiter().GetResult())
                {
                    edges.Add(enumerator.Current);
                }
            }
            finally
            {
                enumerator.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
            object obj = edges;

            if (obj != null)
                Console.WriteLine(_Serializer.SerializeJson(obj, true));
        }

        static void NodeParents()
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
            Guid guid = Inputty.GetGuid("Node GUID   :", default(Guid));
            object obj = _Client.Node.ReadParents(tenantGuid, graphGuid, guid);

            if (obj != null)
                Console.WriteLine(_Serializer.SerializeJson(obj, true));
        }

        static void NodeChildren()
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
            Guid guid = Inputty.GetGuid("Node GUID   :", default(Guid));
            object obj = _Client.Node.ReadChildren(tenantGuid, graphGuid, guid);

            if (obj != null)
                Console.WriteLine(_Serializer.SerializeJson(obj, true));
        }

        static void NodeNeighbors()
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
            Guid guid = Inputty.GetGuid("Node GUID   :", default(Guid));
            object obj = _Client.Node.ReadNeighbors(tenantGuid, graphGuid, guid);

            if (obj != null)
                Console.WriteLine(_Serializer.SerializeJson(obj, true));
        }

        static void NodeMostConnected()
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
            object obj = _Client.Node.ReadMostConnected(tenantGuid, graphGuid);

            if (obj != null)
                Console.WriteLine(_Serializer.SerializeJson(obj, true));
        }

        static void NodeLeastConnected()
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
            object obj = _Client.Node.ReadLeastConnected(tenantGuid, graphGuid);

            if (obj != null)
                Console.WriteLine(_Serializer.SerializeJson(obj, true));
        }

        static void TestSubgraph()
        {
            Console.WriteLine("");
            Console.WriteLine("=== CREATING TEST DATA FOR SUBGRAPH TEST ===");
            Console.WriteLine("");

            #region Create Tenant and Graph

            TenantMetadata tenant = _Client.Tenant.Create(new TenantMetadata { Name = "Subgraph Test Tenant" });
            Console.WriteLine("| Created tenant: " + tenant.GUID);

            Graph graph = _Client.Graph.Create(new Graph
            {
                TenantGUID = tenant.GUID,
                Name = "Subgraph Test Graph"
            });
            Console.WriteLine("| Created graph: " + graph.GUID);
            Console.WriteLine("");

            #endregion

            #region Create Nodes

            // Create a hierarchical structure for testing:
            // Node A (root/starting node)
            //   -> Node B, C (layer 1)
            //       -> Node D, E (from B, layer 2)
            //       -> Node F (from C, layer 2)
            //           -> Node G (from D, layer 3)

            Console.WriteLine("| Creating test nodes...");
            Node nodeA = _Client.Node.Create(new Node
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "Node A (Root)",
                Data = new { Type = "Root", Level = 0 }
            });
            Console.WriteLine("  | Created Node A: " + nodeA.GUID);

            Node nodeB = _Client.Node.Create(new Node
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "Node B (Layer 1)",
                Data = new { Type = "Layer1", Level = 1 }
            });
            Console.WriteLine("  | Created Node B: " + nodeB.GUID);

            Node nodeC = _Client.Node.Create(new Node
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "Node C (Layer 1)",
                Data = new { Type = "Layer1", Level = 1 }
            });
            Console.WriteLine("  | Created Node C: " + nodeC.GUID);

            Node nodeD = _Client.Node.Create(new Node
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "Node D (Layer 2)",
                Data = new { Type = "Layer2", Level = 2 }
            });
            Console.WriteLine("  | Created Node D: " + nodeD.GUID);

            Node nodeE = _Client.Node.Create(new Node
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "Node E (Layer 2)",
                Data = new { Type = "Layer2", Level = 2 }
            });
            Console.WriteLine("  | Created Node E: " + nodeE.GUID);

            Node nodeF = _Client.Node.Create(new Node
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "Node F (Layer 2)",
                Data = new { Type = "Layer2", Level = 2 }
            });
            Console.WriteLine("  | Created Node F: " + nodeF.GUID);

            Node nodeG = _Client.Node.Create(new Node
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "Node G (Layer 3)",
                Data = new { Type = "Layer3", Level = 3 }
            });
            Console.WriteLine("  | Created Node G: " + nodeG.GUID);
            Console.WriteLine("");

            #endregion

            #region Create Edges

            Console.WriteLine("| Creating test edges...");
            Edge edgeAB = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = nodeA.GUID,
                To = nodeB.GUID,
                Name = "A -> B",
                Cost = 1
            }).GetAwaiter().GetResult();
            Console.WriteLine("  | Created Edge A->B: " + edgeAB.GUID);

            Edge edgeAC = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = nodeA.GUID,
                To = nodeC.GUID,
                Name = "A -> C",
                Cost = 1
            }).GetAwaiter().GetResult();
            Console.WriteLine("  | Created Edge A->C: " + edgeAC.GUID);

            Edge edgeBD = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = nodeB.GUID,
                To = nodeD.GUID,
                Name = "B -> D",
                Cost = 1
            }).GetAwaiter().GetResult();
            Console.WriteLine("  | Created Edge B->D: " + edgeBD.GUID);

            Edge edgeBE = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = nodeB.GUID,
                To = nodeE.GUID,
                Name = "B -> E",
                Cost = 1
            }).GetAwaiter().GetResult();
            Console.WriteLine("  | Created Edge B->E: " + edgeBE.GUID);

            Edge edgeCF = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = nodeC.GUID,
                To = nodeF.GUID,
                Name = "C -> F",
                Cost = 1
            }).GetAwaiter().GetResult();
            Console.WriteLine("  | Created Edge C->F: " + edgeCF.GUID);

            Edge edgeDG = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = nodeD.GUID,
                To = nodeG.GUID,
                Name = "D -> G",
                Cost = 1
            }).GetAwaiter().GetResult();
            Console.WriteLine("  | Created Edge D->G: " + edgeDG.GUID);

            // Add a back edge to test bidirectional traversal
            Edge edgeCA = _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = nodeC.GUID,
                To = nodeA.GUID,
                Name = "C -> A (back edge)",
                Cost = 1
            }).GetAwaiter().GetResult();
            Console.WriteLine("  | Created Edge C->A (back): " + edgeCA.GUID);
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
            Console.WriteLine("  Max Depth: " + maxDepth + (maxDepth == 0 ? " (starting node only)" : maxDepth == 1 ? " (immediate neighbors)" : " (two layers)"));
            Console.WriteLine("  Max Nodes: " + (maxNodes == 0 ? "unlimited" : maxNodes.ToString()));
            Console.WriteLine("  Max Edges: " + (maxEdges == 0 ? "unlimited" : maxEdges.ToString()));
            Console.WriteLine("");

            try
            {
                SearchResult result = _Client.Graph.GetSubgraph(
                    tenant.GUID,
                    graph.GUID,
                    nodeA.GUID,
                    maxDepth,
                    maxNodes,
                    maxEdges,
                    includeData,
                    includeSubordinates);

                if (result == null)
                {
                    Console.WriteLine("Result is null");
                    return;
                }

                Console.WriteLine("=== SUBGRAPH RESULTS ===");
                Console.WriteLine("");

                // Graph information
                if (result.Graphs != null && result.Graphs.Count > 0)
                {
                    Console.WriteLine("Graphs: " + result.Graphs.Count);
                    foreach (Graph resultGraph in result.Graphs)
                    {
                        Console.WriteLine("  - Graph GUID: " + resultGraph.GUID + ", Name: " + resultGraph.Name);
                    }
                    Console.WriteLine("");
                }

                // Node information
                if (result.Nodes != null)
                {
                    Console.WriteLine("Nodes: " + result.Nodes.Count);
                    foreach (Node node in result.Nodes)
                    {
                        Console.WriteLine("  - Node GUID: " + node.GUID + ", Name: " + node.Name);
                        if (includeSubordinates)
                        {
                            if (node.Labels != null && node.Labels.Count > 0)
                                Console.WriteLine("    Labels: " + string.Join(", ", node.Labels));
                            if (node.Tags != null && node.Tags.Count > 0)
                            {
                                Console.Write("    Tags: ");
                                for (int i = 0; i < node.Tags.Count; i++)
                                {
                                    if (i > 0) Console.Write(", ");
                                    Console.Write(node.Tags.Keys[i] + "=" + node.Tags[i]);
                                }
                                Console.WriteLine("");
                            }
                            if (node.Vectors != null && node.Vectors.Count > 0)
                                Console.WriteLine("    Vectors: " + node.Vectors.Count);
                        }
                    }
                    Console.WriteLine("");
                }

                // Edge information
                if (result.Edges != null)
                {
                    Console.WriteLine("Edges: " + result.Edges.Count);
                    foreach (Edge edge in result.Edges)
                    {
                        Console.WriteLine("  - Edge GUID: " + edge.GUID + ", Name: " + edge.Name);
                        Console.WriteLine("    From: " + edge.From + " -> To: " + edge.To + ", Cost: " + edge.Cost);
                        if (includeSubordinates)
                        {
                            if (edge.Labels != null && edge.Labels.Count > 0)
                                Console.WriteLine("    Labels: " + string.Join(", ", edge.Labels));
                            if (edge.Tags != null && edge.Tags.Count > 0)
                            {
                                Console.Write("    Tags: ");
                                for (int i = 0; i < edge.Tags.Count; i++)
                                {
                                    if (i > 0) Console.Write(", ");
                                    Console.Write(edge.Tags.Keys[i] + "=" + edge.Tags[i]);
                                }
                                Console.WriteLine("");
                            }
                            if (edge.Vectors != null && edge.Vectors.Count > 0)
                                Console.WriteLine("    Vectors: " + edge.Vectors.Count);
                        }
                    }
                    Console.WriteLine("");
                }

                // Validation checks
                Console.WriteLine("=== VALIDATION ===");
                Console.WriteLine("");

                // Verify all edges connect nodes in the result set
                if (result.Edges != null && result.Nodes != null)
                {
                    HashSet<Guid> nodeGuids = new HashSet<Guid>(result.Nodes.Select(n => n.GUID));
                    int invalidEdges = 0;

                    foreach (Edge edge in result.Edges)
                    {
                        if (!nodeGuids.Contains(edge.From) || !nodeGuids.Contains(edge.To))
                        {
                            Console.WriteLine("  [WARNING] Edge " + edge.GUID + " connects to nodes outside the subgraph!");
                            invalidEdges++;
                        }
                    }

                    if (invalidEdges == 0)
                    {
                        Console.WriteLine("  [OK] All edges connect nodes within the subgraph");
                    }
                    else
                    {
                        Console.WriteLine("  [ERROR] " + invalidEdges + " edge(s) connect to nodes outside the subgraph");
                    }
                }

                // Verify constraints
                Console.WriteLine("");
                Console.WriteLine("  Max Depth Requested: " + maxDepth);
                Console.WriteLine("  Max Nodes Requested: " + (maxNodes == 0 ? "unlimited" : maxNodes.ToString()));
                Console.WriteLine("  Max Edges Requested: " + (maxEdges == 0 ? "unlimited" : maxEdges.ToString()));
                Console.WriteLine("  Nodes Retrieved: " + (result.Nodes?.Count ?? 0));
                Console.WriteLine("  Edges Retrieved: " + (result.Edges?.Count ?? 0));
                
                // Check if thresholds were hit
                if (maxNodes > 0 && (result.Nodes?.Count ?? 0) >= maxNodes)
                {
                    Console.WriteLine("  [INFO] Max nodes threshold reached (" + maxNodes + ")");
                }
                if (maxEdges > 0 && (result.Edges?.Count ?? 0) >= maxEdges)
                {
                    Console.WriteLine("  [INFO] Max edges threshold reached (" + maxEdges + ")");
                }

                // Serialized JSON output
                Console.WriteLine("");
                Console.WriteLine("=== JSON OUTPUT ===");
                Console.WriteLine("");
                Console.WriteLine(_Serializer.SerializeJson(result, true));
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
            Console.WriteLine("Test data created successfully. You can use these GUIDs for manual testing.");
            Console.WriteLine("");

            #endregion
        }

        #endregion
    }
}