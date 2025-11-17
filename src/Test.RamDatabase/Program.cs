namespace Test.RamDatabase
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
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.Helpers;
    using LiteGraph.Serialization;

    class Program
    {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

        static bool _RunForever = true;
        static bool _Debug = false;
        static Serializer _Serializer = new Serializer();
        static LiteGraphClient _Client = null;
        static Guid _TenantGuid = Guid.Parse("00000000-0000-0000-0000-000000000000");
        static Guid _GraphGuid = Guid.Parse("00000000-0000-0000-0000-000000000000");

        static Task Main(string[] args)
        {
            return MainAsync(args, CancellationToken.None);
        }

        static async Task MainAsync(string[] args, CancellationToken token = default)
        {
            _Client = new LiteGraphClient(new SqliteGraphRepository("litegraph.db", true));
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
                else if (userInput.Equals("backup")) await BackupDatabase(token).ConfigureAwait(false);
                else if (userInput.Equals("flush")) await FlushDatabase(token).ConfigureAwait(false);
                else if (userInput.Equals("debug")) ToggleDebug();
                else if (userInput.Equals("tenant")) await SetTenant(token).ConfigureAwait(false);
                else if (userInput.Equals("graph")) await SetGraph(token).ConfigureAwait(false);
                else if (userInput.Equals("load1")) await LoadGraph1(token).ConfigureAwait(false);
                else if (userInput.Equals("load2")) await LoadGraph2(token).ConfigureAwait(false);
                else if (userInput.Equals("route")) await FindRoutes(token).ConfigureAwait(false);
                else if (userInput.Equals("test1-1")) await Test1_1(token).ConfigureAwait(false);
                else if (userInput.Equals("test1-2")) await Test1_2(token).ConfigureAwait(false);
                else if (userInput.Equals("test1-3")) await Test1_3(token).ConfigureAwait(false);
                else if (userInput.Equals("test1-4")) await Test1_4(token).ConfigureAwait(false);
                else if (userInput.Equals("test2-1")) await Test2_1(token).ConfigureAwait(false);
                else if (userInput.Equals("test3-1")) await Test3_1(token).ConfigureAwait(false);
                else if (userInput.Equals("test3-2")) await Test3_2(token).ConfigureAwait(false);
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
            await CleanupTestAssets(token).ConfigureAwait(false);
        }

        static async Task CleanupTestAssets(CancellationToken token = default)
        {
            Console.WriteLine("\n[CLEANUP] Cleaning up test assets...");

            await Task.Run(() =>
            {
                try
                {
                    // Dispose the client first
                    _Client?.Dispose();

                    // Clean up database files (including in-memory flushed data)
                    string[] dbFiles = {
                        "litegraph.db",
                        "litegraph.db-shm",
                        "litegraph.db-wal"
                    };

                    foreach (string file in dbFiles)
                    {
                        if (System.IO.File.Exists(file))
                        {
                            try
                            {
                                System.IO.File.Delete(file);
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
            }, token).ConfigureAwait(false);

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
            Console.WriteLine("  flush           flush the database to disk");
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
            await _Client.Admin.Backup(filename, token).ConfigureAwait(false);
        }

        static Task FlushDatabase(CancellationToken token = default)
        {
            _Client.Flush();
            return Task.CompletedTask;
        }

        static void ToggleDebug()
        {
            _Debug = !_Debug;
            _Client.Logging.LogQueries = _Debug;
            _Client.Logging.LogResults = _Debug;
        }

        static Task SetTenant(CancellationToken token = default)
        {
            _TenantGuid = Inputty.GetGuid("Tenant GUID:", _TenantGuid);
            return Task.CompletedTask;
        }

        static Task SetGraph(CancellationToken token = default)
        {
            _GraphGuid = Inputty.GetGuid("Graph GUID:", _GraphGuid);
            return Task.CompletedTask;
        }

        #region Graph-1

        static async Task LoadGraph1(CancellationToken token = default)
        {
            #region Tenant

            TenantMetadata tenant = await _Client.Tenant.Create(new TenantMetadata { Name = "Test tenant" }, token).ConfigureAwait(false);

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

            Graph graph = await _Client.Graph.Create(new Graph
            {
                TenantGUID = tenant.GUID,
                GUID = graphGuid,
                Name = "Sample Graph 1",
                Labels = labelsGraph,
                Tags = tagsGraph,
                Vectors = graphVectors
            }, token).ConfigureAwait(false);

            #endregion

            #region Nodes

            Guid node1Guid = Guid.NewGuid();
            Console.WriteLine("| Creating node 1 " + node1Guid + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Node n1 = await _Client.Node.Create(new Node
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
            }, token).ConfigureAwait(false);

            Guid node2Guid = Guid.NewGuid();
            Console.WriteLine("| Creating node 2 " + node2Guid + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Node n2 = await _Client.Node.Create(new Node
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
            }, token).ConfigureAwait(false);

            Guid node3Guid = Guid.NewGuid();
            Console.WriteLine("| Creating node 3 " + node3Guid + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Node n3 = await _Client.Node.Create(new Node
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
            }, token).ConfigureAwait(false);

            Guid node4Guid = Guid.NewGuid();
            Console.WriteLine("| Creating node 4 " + node4Guid + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Node n4 = await _Client.Node.Create(new Node
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
            }, token).ConfigureAwait(false);

            Guid node5Guid = Guid.NewGuid();
            Console.WriteLine("| Creating node 5 " + node5Guid + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Node n5 = await _Client.Node.Create(new Node
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
            }, token).ConfigureAwait(false);

            Guid node6Guid = Guid.NewGuid();
            Console.WriteLine("| Creating node 6 " + node6Guid + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Node n6 = await _Client.Node.Create(new Node
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
            }, token).ConfigureAwait(false);

            Guid node7Guid = Guid.NewGuid();
            Console.WriteLine("| Creating node 7 " + node7Guid + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Node n7 = await _Client.Node.Create(new Node
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
            }, token).ConfigureAwait(false);

            Guid node8Guid = Guid.NewGuid();
            Console.WriteLine("| Creating node 8 " + node8Guid + " in tenant " + tenant.GUID + " graph " + graph.GUID);

            Node n8 = await _Client.Node.Create(new Node
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
            }, token).ConfigureAwait(false);

            #endregion

            #region Edges

            Edge e1 = await _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n1.GUID,
                To = n4.GUID,
                Name = "1 to 4",
                Cost = 1,
                Labels = StringHelpers.Combine(labelsOdd, labelsEdge),
                Tags = NvcHelpers.Combine(tagsOdd, tagsEdge)
            }, token).ConfigureAwait(false);

            Edge e2 = await _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n1.GUID,
                To = n5.GUID,
                Name = "1 to 5",
                Cost = 2,
                Labels = StringHelpers.Combine(labelsEven, labelsEdge),
                Tags = NvcHelpers.Combine(tagsEven, tagsEdge)
            }, token).ConfigureAwait(false);

            Edge e3 = await _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n2.GUID,
                To = n4.GUID,
                Name = "2 to 4",
                Cost = 3,
                Labels = StringHelpers.Combine(labelsOdd, labelsEdge),
                Tags = NvcHelpers.Combine(tagsOdd, tagsEdge)
            }, token).ConfigureAwait(false);

            Edge e4 = await _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n2.GUID,
                To = n5.GUID,
                Name = "2 to 5",
                Cost = 4,
                Labels = StringHelpers.Combine(labelsEven, labelsEdge),
                Tags = NvcHelpers.Combine(tagsEven, tagsEdge)
            }, token).ConfigureAwait(false);

            Edge e5 = await _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n3.GUID,
                To = n4.GUID,
                Name = "3 to 4",
                Cost = 5,
                Labels = StringHelpers.Combine(labelsOdd, labelsEdge),
                Tags = NvcHelpers.Combine(tagsOdd, tagsEdge)
            }, token).ConfigureAwait(false);

            Edge e6 = await _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n3.GUID,
                To = n5.GUID,
                Name = "3 to 5",
                Cost = 6,
                Labels = StringHelpers.Combine(labelsEven, labelsEdge),
                Tags = NvcHelpers.Combine(tagsEven, tagsEdge)
            }, token).ConfigureAwait(false);

            Edge e7 = await _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n4.GUID,
                To = n6.GUID,
                Name = "4 to 6",
                Cost = 7,
                Labels = StringHelpers.Combine(labelsOdd, labelsEdge),
                Tags = NvcHelpers.Combine(tagsOdd, tagsEdge)
            }, token).ConfigureAwait(false);

            Edge e8 = await _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n4.GUID,
                To = n7.GUID,
                Name = "4 to 7",
                Cost = 8,
                Labels = StringHelpers.Combine(labelsEven, labelsEdge),
                Tags = NvcHelpers.Combine(tagsEven, tagsEdge)
            }, token).ConfigureAwait(false);

            Edge e9 = await _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n4.GUID,
                To = n8.GUID,
                Name = "4 to 8",
                Cost = 9,
                Labels = StringHelpers.Combine(labelsOdd, labelsEdge),
                Tags = NvcHelpers.Combine(tagsOdd, tagsEdge)
            }, token).ConfigureAwait(false);

            Edge e10 = await _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n5.GUID,
                To = n6.GUID,
                Name = "5 to 6",
                Cost = 10,
                Labels = StringHelpers.Combine(labelsEven, labelsEdge),
                Tags = NvcHelpers.Combine(tagsEven, tagsEdge)
            }, token).ConfigureAwait(false);

            Edge e11 = await _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n5.GUID,
                To = n7.GUID,
                Name = "5 to 7",
                Cost = 11,
                Labels = StringHelpers.Combine(labelsOdd, labelsEdge),
                Tags = NvcHelpers.Combine(tagsOdd, tagsEdge)
            }, token).ConfigureAwait(false);

            Edge e12 = await _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = n5.GUID,
                To = n8.GUID,
                Name = "5 to 8",
                Cost = 12,
                Labels = StringHelpers.Combine(labelsEven, labelsEdge),
                Tags = NvcHelpers.Combine(tagsEven, tagsEdge)
            }, token).ConfigureAwait(false);

            #endregion
        }

        static async Task Test1_1(CancellationToken token = default)
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);

            Console.WriteLine("");
            Console.WriteLine("Retrieving graphs where label = 'graph'");

            List<string> labelGraph = new List<string>
            {
                "graph"
            };

            await foreach (Graph graph in _Client.Graph.ReadMany(tenantGuid, null, labelGraph, token: token)
                .WithCancellation(token).ConfigureAwait(false))
            {
                Console.WriteLine("| " + graph.GUID + ": " + graph.Name);
            }

            Console.WriteLine("");
            Console.WriteLine("Retrieving nodes with labels 'node' and 'even'");

            List<string> labelEvenNodes = new List<string>
            {
                "node",
                "even"
            };

            await foreach (Node node in _Client.Node.ReadMany(tenantGuid, graphGuid, null, labelEvenNodes, token: token)
                .WithCancellation(token).ConfigureAwait(false))
            {
                Console.WriteLine("| " + node.GUID + ": " + node.Name);
            }

            Console.WriteLine("");
            Console.WriteLine("Retrieving edges with labels 'edge' and 'odd'");

            List<string> labelOddEdges = new List<string>
            {
                "edge",
                "odd"
            };

            await foreach (Edge edge in _Client.Edge.ReadMany(tenantGuid, graphGuid, null, labelOddEdges, token: token)
                .WithCancellation(token).ConfigureAwait(false))
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

            await foreach (Graph graph in _Client.Graph.ReadMany(tenantGuid, null, null, tagsGraph, token: token)
                .WithCancellation(token).ConfigureAwait(false))
            {
                Console.WriteLine("| " + graph.GUID + ": " + graph.Name);
            }

            Console.WriteLine("");
            Console.WriteLine("Retrieving nodes where tag 'type' = 'node' and 'isEven' = 'true'");

            NameValueCollection tagsEvenNodes = new NameValueCollection();
            tagsEvenNodes.Add("type", "node");
            tagsEvenNodes.Add("isEven", "true");

            await foreach (Node node in _Client.Node.ReadMany(tenantGuid, graphGuid, null, null, tagsEvenNodes, token: token)
                .WithCancellation(token).ConfigureAwait(false))
            {
                Console.WriteLine("| " + node.GUID + ": " + node.Name);
            }

            Console.WriteLine("");
            Console.WriteLine("Retrieving edges where tag 'type' = 'edge' and 'isEven' = 'false'");

            NameValueCollection tagsOddEdges = new NameValueCollection();
            tagsOddEdges.Add("type", "edge");
            tagsOddEdges.Add("isEven", "false");

            await foreach (Edge edge in _Client.Edge.ReadMany(tenantGuid, graphGuid, null, null, tagsOddEdges, token: token)
                .WithCancellation(token).ConfigureAwait(false))
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

            List<string> labelGraph = new List<string>
            {
                "graph"
            };

            NameValueCollection tagsGraph = new NameValueCollection();
            tagsGraph.Add("type", "graph");

            await foreach (Graph graph in _Client.Graph.ReadMany(tenantGuid, null, labelGraph, tagsGraph, token: token)
                .WithCancellation(token).ConfigureAwait(false))
            {
                Console.WriteLine("| " + graph.GUID + ": " + graph.Name);
            }

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

            await foreach (Node node in _Client.Node.ReadMany(tenantGuid, graphGuid, null, labelEvenNodes, tagsEvenNodes, token: token)
                .WithCancellation(token).ConfigureAwait(false))
            {
                Console.WriteLine("| " + node.GUID + ": " + node.Name);
            }

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

            await foreach (Edge edge in _Client.Edge.ReadMany(tenantGuid, graphGuid, null, labelOddEdges, tagsOddEdges, token: token)
                .WithCancellation(token).ConfigureAwait(false))
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
                TenantGUID = _TenantGuid,
                GraphGUID = _GraphGuid,
                Domain = VectorSearchDomainEnum.Node,
                SearchType = VectorSearchTypeEnum.CosineSimilarity,
                Embeddings = new List<float> { 0.1f, 0.2f, 0.3f }
            };

            Console.WriteLine("");
            Console.WriteLine("Retrieving nodes by cosine similarity to embeddings [ 0.1, 0.2, 0.3 ]");

            List<VectorSearchResult> cosineSimResults = new List<VectorSearchResult>();
            await foreach (VectorSearchResult result in _Client.Vector.Search(searchReqCosineSim, token)
                .WithCancellation(token).ConfigureAwait(false))
            {
                cosineSimResults.Add(result);
            }
            foreach (VectorSearchResult result in cosineSimResults.OrderByDescending(p => p.Score))
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

            List<VectorSearchResult> cosineDisResults = new List<VectorSearchResult>();
            await foreach (VectorSearchResult result in _Client.Vector.Search(searchReqCosineDis, token)
                .WithCancellation(token).ConfigureAwait(false))
            {
                cosineDisResults.Add(result);
            }
            foreach (VectorSearchResult result in cosineDisResults.OrderBy(p => p.Distance))
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

            List<VectorSearchResult> eucSimResults = new List<VectorSearchResult>();
            await foreach (VectorSearchResult result in _Client.Vector.Search(searchReqEucSim, token)
                .WithCancellation(token).ConfigureAwait(false))
            {
                eucSimResults.Add(result);
            }
            foreach (VectorSearchResult result in eucSimResults.OrderByDescending(p => p.Score))
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

            List<VectorSearchResult> eucDisResults = new List<VectorSearchResult>();
            await foreach (VectorSearchResult result in _Client.Vector.Search(searchReqEucDis, token)
                .WithCancellation(token).ConfigureAwait(false))
            {
                eucDisResults.Add(result);
            }
            foreach (VectorSearchResult result in eucDisResults.OrderBy(p => p.Distance))
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

            List<VectorSearchResult> dpResults = new List<VectorSearchResult>();
            await foreach (VectorSearchResult result in _Client.Vector.Search(searchReqDp, token)
                .WithCancellation(token).ConfigureAwait(false))
            {
                dpResults.Add(result);
            }
            foreach (VectorSearchResult result in dpResults.OrderByDescending(p => p.InnerProduct))
            {
                Console.WriteLine("| Node " + result.Node.GUID + " " + result.Node.Name + ": inner product " + result.InnerProduct);
            }

            #endregion

            Console.WriteLine("");
        }

        #endregion

        #region Graph-2

        static async Task LoadGraph2(CancellationToken token = default)
        {
            #region Tenant

            TenantMetadata tenant = await _Client.Tenant.Create(new TenantMetadata { Name = "Test tenant" }, token).ConfigureAwait(false);

            #endregion

            #region Graph

            Graph graph = await _Client.Graph.Create(new Graph
            {
                TenantGUID = tenant.GUID,
                Name = "Sample Graph 2"
            }, token).ConfigureAwait(false);

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

            Node joelNode = await _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Joel", Data = joel }, token).ConfigureAwait(false);
            Node yipNode = await _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Yip", Data = yip }, token).ConfigureAwait(false);
            Node keithNode = await _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Keith", Data = keith }, token).ConfigureAwait(false);
            Node alexNode = await _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Alex", Data = alex }, token).ConfigureAwait(false);
            Node blakeNode = await _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Blake", Data = blake }, token).ConfigureAwait(false);

            Node xfiNode = await _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Xfinity", Data = xfi }, token).ConfigureAwait(false);
            Node starlinkNode = await _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Starlink", Data = starlink }, token).ConfigureAwait(false);
            Node attNode = await _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "AT&T", Data = att }, token).ConfigureAwait(false);

            Node internetNode = await _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Internet", Data = internet }, token).ConfigureAwait(false);

            Node equinixNode = await _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Equinix", Data = equinix }, token).ConfigureAwait(false);
            Node awsNode = await _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "AWS", Data = aws }, token).ConfigureAwait(false);
            Node azureNode = await _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Azure", Data = azure }, token).ConfigureAwait(false);
            Node digitalOceanNode = await _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "DigitalOcean", Data = digitalOcean }, token).ConfigureAwait(false);
            Node rackspaceNode = await _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Rackspace", Data = rackspace }, token).ConfigureAwait(false);

            Node ccpNode = await _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Control Plane", Data = ccp }, token).ConfigureAwait(false);
            Node websiteNode = await _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Website", Data = website }, token).ConfigureAwait(false);
            Node adNode = await _Client.Node.Create(new Node { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Name = "Active Directory", Data = ad }, token).ConfigureAwait(false);

            #endregion

            #region Edges

            Edge joelXfiEdge = await _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = joelNode.GUID,
                To = xfiNode.GUID,
                Name = "Joel to Xfinity"
            }, token).ConfigureAwait(false);

            Edge joelStarlinkEdge = await _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = joelNode.GUID,
                To = starlinkNode.GUID,
                Name = "Joel to Starlink"
            }, token).ConfigureAwait(false);

            Edge yipXfiEdge = await _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = yipNode.GUID,
                To = xfiNode.GUID,
                Name = "Yip to Xfinity"
            }, token).ConfigureAwait(false);

            Edge keithStarlinkEdge = await _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = keithNode.GUID,
                To = starlinkNode.GUID,
                Name = "Keith to Starlink"
            }, token).ConfigureAwait(false);

            Edge keithXfiEdge = await _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = keithNode.GUID,
                To = xfiNode.GUID,
                Name = "Keith to Xfinity"
            }, token).ConfigureAwait(false);

            Edge keithAttEdge = await _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = keithNode.GUID,
                To = attNode.GUID,
                Name = "Keith to AT&T"
            }, token).ConfigureAwait(false);

            Edge alexAttEdge = await _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = alexNode.GUID,
                To = attNode.GUID,
                Name = "Alex to AT&T"
            }, token).ConfigureAwait(false);

            Edge blakeAttEdge = await _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = blakeNode.GUID,
                To = attNode.GUID,
                Name = "Blake to AT&T"
            }, token).ConfigureAwait(false);

            Edge xfiInternetEdge1 = await _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = xfiNode.GUID,
                To = internetNode.GUID,
                Name = "Xfinity to Internet 1"
            }, token).ConfigureAwait(false);

            Edge xfiInternetEdge2 = await _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = xfiNode.GUID,
                To = internetNode.GUID,
                Name = "Xfinity to Internet 2"
            }, token).ConfigureAwait(false);

            Edge starlinkInternetEdge = await _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = starlinkNode.GUID,
                To = internetNode.GUID,
                Name = "Starlink to Internet"
            }, token).ConfigureAwait(false);

            Edge attInternetEdge1 = await _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = attNode.GUID,
                To = internetNode.GUID,
                Name = "AT&T to Internet 1"
            }, token).ConfigureAwait(false);

            Edge attInternetEdge2 = await _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = attNode.GUID,
                To = internetNode.GUID,
                Name = "AT&T to Internet 2"
            }, token).ConfigureAwait(false);

            Edge internetEquinixEdge = await _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = internetNode.GUID,
                To = equinixNode.GUID,
                Name = "Internet to Equinix"
            }, token).ConfigureAwait(false);

            Edge internetAwsEdge = await _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = internetNode.GUID,
                To = awsNode.GUID,
                Name = "Internet to AWS"
            }, token).ConfigureAwait(false);

            Edge internetAzureEdge = await _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = internetNode.GUID,
                To = azureNode.GUID,
                Name = "Internet to Azure"
            }, token).ConfigureAwait(false);

            Edge equinixDoEdge = await _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = equinixNode.GUID,
                To = digitalOceanNode.GUID,
                Name = "Equinix to DigitalOcean"
            }, token).ConfigureAwait(false);

            Edge equinixAwsEdge = await _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = equinixNode.GUID,
                To = awsNode.GUID,
                Name = "Equinix to AWS"
            }, token).ConfigureAwait(false);

            Edge equinixRackspaceEdge = await _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = equinixNode.GUID,
                To = rackspaceNode.GUID,
                Name = "Equinix to Rackspace"
            }, token).ConfigureAwait(false);

            Edge awsWebsiteEdge = await _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = awsNode.GUID,
                To = websiteNode.GUID,
                Name = "AWS to Website"
            }, token).ConfigureAwait(false);

            Edge azureAdEdge = await _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = azureNode.GUID,
                To = adNode.GUID,
                Name = "Azure to Active Directory"
            }, token).ConfigureAwait(false);

            Edge doCcpEdge = await _Client.Edge.Create(new Edge
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                From = digitalOceanNode.GUID,
                To = ccpNode.GUID,
                Name = "DigitalOcean to Control Plane"
            }, token).ConfigureAwait(false);

            #endregion
        }

        static async Task Test2_1(CancellationToken token = default)
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
            Expr e1 = new Expr("$.Name", OperatorEnum.Equals, "Joel");
            Expr e2 = new Expr("$.Age", OperatorEnum.GreaterThan, 38);

            Console.WriteLine("");
            Console.WriteLine("Retrieving nodes where Name = 'Joel'");
            await foreach (Node node in _Client.Node.ReadMany(tenantGuid, graphGuid, null, null, null, e1, token: token)
                .WithCancellation(token).ConfigureAwait(false))
            {
                Console.WriteLine(_Client.ConvertData<Person>(node.Data).ToString());
            }

            Console.WriteLine("");
            Console.WriteLine("Retrieve nodes where Age >= 38");
            await foreach (Node node in _Client.Node.ReadMany(tenantGuid, graphGuid, null, null, null, e2, token: token)
                .WithCancellation(token).ConfigureAwait(false))
            {
                Console.WriteLine(_Client.ConvertData<Person>(node.Data).ToString());
            }

            Console.WriteLine("");
        }

        #endregion

        #region Misc

        static async Task Test3_1(CancellationToken token = default)
        {
            TenantMetadata tenant = await _Client.Tenant.Create(new TenantMetadata
            {
                Name = "Test"
            }, token).ConfigureAwait(false);

            Graph graph = await _Client.Graph.Create(new Graph
            {
                TenantGUID = tenant.GUID,
                Name = "Test"
            }, token).ConfigureAwait(false);

            Node node1 = new Node
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "Node1",
                Data = new { Text = "hello" }
            };

            Console.WriteLine(_Serializer.SerializeJson(node1));

            node1 = await _Client.Node.Create(node1, token).ConfigureAwait(false);
        }

        static async Task Test3_2(CancellationToken token = default)
        {
            TenantMetadata tenant = await _Client.Tenant.Create(new TenantMetadata
            {
                Name = "Test"
            }, token).ConfigureAwait(false);

            Graph graph = await _Client.Graph.Create(new Graph
            {
                TenantGUID = tenant.GUID,
                Name = "Test"
            }, token).ConfigureAwait(false);

            object data = new { Text = "hello" };

            Node node1 = new Node
            {
                TenantGUID = tenant.GUID,
                GraphGUID = graph.GUID,
                Name = "Node1",
                Data = data
            };

            Console.WriteLine(_Serializer.SerializeJson(node1));

            node1 = await _Client.Node.Create(node1, token).ConfigureAwait(false);
        }

        #endregion

        #region Primitives

        static async Task FindRoutes(CancellationToken token = default)
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
            Guid fromGuid = Inputty.GetGuid("From GUID  :", default(Guid));
            Guid toGuid = Inputty.GetGuid("To GUID    :", default(Guid));
            List<RouteDetail> routes = new List<RouteDetail>();
            await foreach (RouteDetail detail in _Client.Node.ReadRoutes(SearchTypeEnum.DepthFirstSearch, tenantGuid, graphGuid, fromGuid, toGuid, token: token)
                .WithCancellation(token).ConfigureAwait(false))
            {
                routes.Add(detail);
            }

            if (routes.Count > 0)
                Console.WriteLine(_Serializer.SerializeJson(routes, true));
        }

        static async Task Create(string str, CancellationToken token = default)
        {
            object obj = null;
            string json = null;

            if (str.Equals("tenant"))
            {
                obj = await _Client.Tenant.Create(new TenantMetadata { Name = Inputty.GetString("Name:", null, false) }, token).ConfigureAwait(false);
            }
            else if (str.Equals("user"))
            {
                obj = await _Client.User.Create(new UserMaster
                {
                    TenantGUID = Inputty.GetGuid("Tenant GUID:", _TenantGuid),
                    FirstName = Inputty.GetString("First name:", null, false),
                    LastName = Inputty.GetString("Last name:", null, false),
                    Email = Inputty.GetString("Email:", null, false),
                    Password = Inputty.GetString("Password:", null, false)
                }, token).ConfigureAwait(false);
            }
            else if (str.Equals("cred"))
            {
                obj = await _Client.Credential.Create(new Credential
                {
                    TenantGUID = Inputty.GetGuid("Tenant GUID:", _TenantGuid),
                    UserGUID = Inputty.GetGuid("User GUID:", default(Guid)),
                    Name = Inputty.GetString("Name:", null, false)
                }, token).ConfigureAwait(false);
            }
            else if (str.Equals("graph"))
            {
                obj = await _Client.Graph.Create(new Graph
                {
                    TenantGUID = Inputty.GetGuid("Tenant GUID:", _TenantGuid),
                    Name = Inputty.GetString("Name:", null, false)
                }, token).ConfigureAwait(false);
            }
            else if (str.Equals("node"))
            {
                json = Inputty.GetString("JSON:", null, false);
                obj = await _Client.Node.Create(_Serializer.DeserializeJson<Node>(json), token).ConfigureAwait(false);
            }
            else if (str.Equals("edge"))
            {
                json = Inputty.GetString("JSON:", null, false);
                obj = await _Client.Edge.Create(_Serializer.DeserializeJson<Edge>(json), token).ConfigureAwait(false);
            }

            if (obj != null)
                Console.WriteLine(_Serializer.SerializeJson(obj, true));
        }

        static async Task All(string str, CancellationToken token = default)
        {
            object obj = null;
            if (str.Equals("tenant"))
            {
                List<TenantMetadata> tenants = new List<TenantMetadata>();
                await foreach (TenantMetadata tenant in _Client.Tenant.ReadMany(token: token)
                    .WithCancellation(token).ConfigureAwait(false))
                {
                    tenants.Add(tenant);
                }
                obj = tenants;
            }
            else if (str.Equals("user"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                obj = await Task.Run(() => _Client.User.ReadMany(tenantGuid, null), token).ConfigureAwait(false);
            }
            else if (str.Equals("cred"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                obj = await _Client.Credential.ReadMany(tenantGuid, null, null, token: token).ConfigureAwait(false);
            }
            else if (str.Equals("graph"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                List<Graph> graphs = new List<Graph>();
                await foreach (Graph graph in _Client.Graph.ReadMany(tenantGuid, token: token)
                    .WithCancellation(token).ConfigureAwait(false))
                {
                    graphs.Add(graph);
                }
                obj = graphs;
            }
            else if (str.Equals("node"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                List<Node> nodes = new List<Node>();
                await foreach (Node node in _Client.Node.ReadMany(tenantGuid, graphGuid, token: token)
                    .WithCancellation(token).ConfigureAwait(false))
                {
                    nodes.Add(node);
                }
                obj = nodes;
            }
            else if (str.Equals("edge"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                List<Edge> edges = new List<Edge>();
                await foreach (Edge edge in _Client.Edge.ReadMany(tenantGuid, graphGuid, token: token)
                    .WithCancellation(token).ConfigureAwait(false))
                {
                    edges.Add(edge);
                }
                obj = edges;
            }

            if (obj != null)
                Console.WriteLine(_Serializer.SerializeJson(obj, true));
        }

        static async Task Read(string str, CancellationToken token = default)
        {
            object obj = null;

            if (str.Equals("tenant"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                obj = await _Client.Tenant.ReadByGuid(tenantGuid, token).ConfigureAwait(false);
            }
            else if (str.Equals("user"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid userGuid = Inputty.GetGuid("GUID        :", default(Guid));
                obj = await _Client.User.ReadByGuid(tenantGuid, userGuid, token).ConfigureAwait(false);
            }
            else if (str.Equals("cred"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid credGuid = Inputty.GetGuid("GUID        :", default(Guid));
                obj = await _Client.Credential.ReadByGuid(tenantGuid, credGuid, token).ConfigureAwait(false);
            }
            else if (str.Equals("graph"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                obj = await _Client.Graph.ReadByGuid(tenantGuid, graphGuid, false, false, token).ConfigureAwait(false);
            }
            else if (str.Equals("node"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                Guid guid = Inputty.GetGuid("Node GUID   :", default(Guid));
                obj = await _Client.Node.ReadByGuid(tenantGuid, graphGuid, guid, token: token).ConfigureAwait(false);
            }
            else if (str.Equals("edge"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                Guid guid = Inputty.GetGuid("Edge GUID   :", default(Guid));
                obj = await _Client.Edge.ReadByGuid(tenantGuid, graphGuid, guid, token: token).ConfigureAwait(false);
            }

            if (obj != null)
                Console.WriteLine(_Serializer.SerializeJson(obj, true));
        }

        static async Task Exists(string str, CancellationToken token = default)
        {
            bool exists = false;

            if (str.Equals("tenant"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                exists = await _Client.Tenant.ExistsByGuid(tenantGuid, token).ConfigureAwait(false);
            }
            else if (str.Equals("user"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid userGuid = Inputty.GetGuid("GUID        :", default(Guid));
                exists = await _Client.User.ExistsByGuid(tenantGuid, userGuid, token).ConfigureAwait(false);
            }
            else if (str.Equals("cred"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid credGuid = Inputty.GetGuid("GUID        :", default(Guid));
                exists = await _Client.Credential.ExistsByGuid(tenantGuid, credGuid, token).ConfigureAwait(false);
            }
            else if (str.Equals("graph"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                exists = await _Client.Graph.ExistsByGuid(tenantGuid, graphGuid, token).ConfigureAwait(false);
            }
            else if (str.Equals("node"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                Guid guid = Inputty.GetGuid("Node GUID   :", default(Guid));
                exists = await _Client.Node.ExistsByGuid(tenantGuid, guid, token).ConfigureAwait(false);
            }
            else if (str.Equals("edge"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                Guid guid = Inputty.GetGuid("Edge GUID   :", default(Guid));
                exists = await _Client.Edge.ExistsByGuid(tenantGuid, graphGuid, guid, token).ConfigureAwait(false);
            }

            Console.WriteLine("Exists: " + exists);
        }

        static async Task Update(string str, CancellationToken token = default)
        {
            object obj = null;
            string json = Inputty.GetString("JSON:", null, false);

            if (str.Equals("tenant"))
            {
                obj = await _Client.Tenant.Update(_Serializer.DeserializeJson<TenantMetadata>(json), token).ConfigureAwait(false);
            }
            else if (str.Equals("graph"))
            {
                obj = await _Client.Graph.Update(_Serializer.DeserializeJson<Graph>(json), token).ConfigureAwait(false);
            }
            else if (str.Equals("node"))
            {
                obj = await _Client.Node.Update(_Serializer.DeserializeJson<Node>(json), token).ConfigureAwait(false);
            }
            else if (str.Equals("edge"))
            {
                obj = await _Client.Edge.Update(_Serializer.DeserializeJson<Edge>(json), token).ConfigureAwait(false);
            }

            if (obj != null)
                Console.WriteLine(_Serializer.SerializeJson(obj, true));
        }

        static async Task Delete(string str, CancellationToken token = default)
        {
            if (str.Equals("tenant"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                bool force = Inputty.GetBoolean("Force       :", true);
                await _Client.Tenant.DeleteByGuid(tenantGuid, force, token).ConfigureAwait(false);
            }
            else if (str.Equals("user"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid userGuid = Inputty.GetGuid("GUID        :", default(Guid));
                await _Client.User.DeleteByGuid(tenantGuid, userGuid, token).ConfigureAwait(false);
            }
            else if (str.Equals("cred"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid credGuid = Inputty.GetGuid("GUID        :", default(Guid));
                await _Client.Credential.DeleteByGuid(tenantGuid, credGuid, token).ConfigureAwait(false);
            }
            else if (str.Equals("graph"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                bool force = Inputty.GetBoolean("Force       :", true);
                await _Client.Graph.DeleteByGuid(tenantGuid, graphGuid, force, token).ConfigureAwait(false);
            }
            else if (str.Equals("node"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                Guid guid = Inputty.GetGuid("Node GUID   :", default(Guid));
                await _Client.Node.DeleteByGuid(tenantGuid, graphGuid, guid, token).ConfigureAwait(false);
            }
            else if (str.Equals("edge"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                Guid guid = Inputty.GetGuid("Edge GUID   :", default(Guid));
                await _Client.Edge.DeleteByGuid(tenantGuid, graphGuid, guid, token).ConfigureAwait(false);
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
                List<Graph> graphResult = new List<Graph>();
                await foreach (Graph graph in _Client.Graph.ReadMany(tenantGuid, null, null, null, expr, EnumerationOrderEnum.CreatedDescending, token: token)
                    .WithCancellation(token).ConfigureAwait(false))
                {
                    graphResult.Add(graph);
                }
                if (graphResult != null && graphResult.Count > 0) resultJson = _Serializer.SerializeJson(graphResult);
            }
            else if (str.Equals("node"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                List<Node> nodeResult = new List<Node>();
                await foreach (Node node in _Client.Node.ReadMany(tenantGuid, graphGuid, null, null, null, expr, EnumerationOrderEnum.CreatedDescending, token: token)
                    .WithCancellation(token).ConfigureAwait(false))
                {
                    nodeResult.Add(node);
                }
                if (nodeResult != null) resultJson = _Serializer.SerializeJson(nodeResult.ToList());
            }
            else if (str.Equals("edge"))
            {
                Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
                Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
                List<Edge> edgeResult = new List<Edge>();
                await foreach (Edge edge in _Client.Edge.ReadMany(tenantGuid, graphGuid, null, null, null, expr, EnumerationOrderEnum.CreatedDescending, token: token)
                    .WithCancellation(token).ConfigureAwait(false))
                {
                    edgeResult.Add(edge);
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

        static async Task NodeEdgesTo(CancellationToken token = default)
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
            Guid guid = Inputty.GetGuid("Node GUID   :", default(Guid));
            List<Edge> edges = new List<Edge>();
            await foreach (Edge edge in _Client.Edge.ReadEdgesToNode(tenantGuid, graphGuid, guid, token: token)
                .WithCancellation(token).ConfigureAwait(false))
            {
                edges.Add(edge);
            }
            object obj = edges;

            if (obj != null)
                Console.WriteLine(_Serializer.SerializeJson(obj, true));
        }

        static async Task NodeEdgesFrom(CancellationToken token = default)
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
            Guid guid = Inputty.GetGuid("Node GUID   :", default(Guid));
            List<Edge> edges = new List<Edge>();
            await foreach (Edge edge in _Client.Edge.ReadEdgesFromNode(tenantGuid, graphGuid, guid, token: token)
                .WithCancellation(token).ConfigureAwait(false))
            {
                edges.Add(edge);
            }
            object obj = edges;

            if (obj != null)
                Console.WriteLine(_Serializer.SerializeJson(obj, true));
        }

        static async Task NodeEdgesBetween(CancellationToken token = default)
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
            Guid fromGuid = Inputty.GetGuid("From GUID   :", default(Guid));
            Guid toGuid = Inputty.GetGuid("To GUID     :", default(Guid));
            List<Edge> edges = new List<Edge>();
            await foreach (Edge edge in _Client.Edge.ReadEdgesBetweenNodes(tenantGuid, graphGuid, fromGuid, toGuid, token: token)
                .WithCancellation(token).ConfigureAwait(false))
            {
                edges.Add(edge);
            }
            object obj = edges;

            if (obj != null)
                Console.WriteLine(_Serializer.SerializeJson(obj, true));
        }

        static async Task NodeParents(CancellationToken token = default)
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
            Guid guid = Inputty.GetGuid("Node GUID   :", default(Guid));
            object obj = await Task.Run(() => _Client.Node.ReadParents(tenantGuid, graphGuid, guid), token).ConfigureAwait(false);

            if (obj != null)
                Console.WriteLine(_Serializer.SerializeJson(obj, true));
        }

        static async Task NodeChildren(CancellationToken token = default)
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
            Guid guid = Inputty.GetGuid("Node GUID   :", default(Guid));
            object obj = await Task.Run(() => _Client.Node.ReadChildren(tenantGuid, graphGuid, guid), token).ConfigureAwait(false);

            if (obj != null)
                Console.WriteLine(_Serializer.SerializeJson(obj, true));
        }

        static async Task NodeNeighbors(CancellationToken token = default)
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
            Guid guid = Inputty.GetGuid("Node GUID   :", default(Guid));
            object obj = await Task.Run(() => _Client.Node.ReadNeighbors(tenantGuid, graphGuid, guid), token).ConfigureAwait(false);

            if (obj != null)
                Console.WriteLine(_Serializer.SerializeJson(obj, true));
        }

        static async Task NodeMostConnected(CancellationToken token = default)
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
            object obj = await Task.Run(() => _Client.Node.ReadMostConnected(tenantGuid, graphGuid), token).ConfigureAwait(false);

            if (obj != null)
                Console.WriteLine(_Serializer.SerializeJson(obj, true));
        }

        static async Task NodeLeastConnected(CancellationToken token = default)
        {
            Guid tenantGuid = Inputty.GetGuid("Tenant GUID :", _TenantGuid);
            Guid graphGuid = Inputty.GetGuid("Graph GUID  :", _GraphGuid);
            object obj = await Task.Run(() => _Client.Node.ReadLeastConnected(tenantGuid, graphGuid), token).ConfigureAwait(false);

            if (obj != null)
                Console.WriteLine(_Serializer.SerializeJson(obj, true));
        }

        #endregion

#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
    }
}