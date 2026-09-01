namespace LoadGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;
    using LiteGraph.GraphRepositories;

    /// <summary>
    /// Seeds a LiteGraph database with synthetic, backdated activity, and removes previously seeded data on request.
    /// This class is not thread-safe; use one instance per run.
    /// </summary>
    public class Seeder
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LiteGraphClient _Client;
        private GraphRepositoryBase _Repo;
        private LoadGeneratorSettings _Settings;
        private Random _Random;
        private ContentFactory _Content;
        private ChatFactory _Chat;
        private ActivityClock _Clock;
        private DateTime _WindowStartUtc;
        private DateTime _WindowEndUtc;
        private List<UserMaster> _SyntheticUsers = new List<UserMaster>();
        private List<Graph> _SyntheticGraphs = new List<Graph>();
        private const int _SyntheticUserCount = 4;
        private const int _VectorDimensions = 384;
        private const string _VectorModel = "all-minilm";
        private const int _NodeBatchSize = 100;
        private const int _EdgeBatchSize = 250;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="client">LiteGraph client.</param>
        /// <param name="repo">Graph repository, used for request-history primitives not exposed by the client.</param>
        /// <param name="settings">Load generator settings.</param>
        /// <param name="random">Random number generator.</param>
        /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
        public Seeder(LiteGraphClient client, GraphRepositoryBase repo, LoadGeneratorSettings settings, Random random)
        {
            _Client = client ?? throw new ArgumentNullException(nameof(client));
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Random = random ?? throw new ArgumentNullException(nameof(random));

            _Content = new ContentFactory(_Random);
            _Chat = new ChatFactory(_Random);

            _WindowEndUtc = DateTime.UtcNow;
            _WindowStartUtc = _WindowEndUtc.AddDays(-1 * _Settings.Days);
            _Clock = new ActivityClock(_Random, _WindowStartUtc, _WindowEndUtc);
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Seed the database with synthetic activity.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Summary of created entities.</returns>
        public async Task<SeedSummary> SeedAsync(CancellationToken token = default)
        {
            SeedSummary summary = new SeedSummary
            {
                WindowStartUtc = _WindowStartUtc,
                WindowEndUtc = _WindowEndUtc
            };

            await EnsureTenantAsync(summary, token).ConfigureAwait(false);
            await EnsureUsersAsync(summary, token).ConfigureAwait(false);
            await SeedGraphsAsync(summary, token).ConfigureAwait(false);
            await SeedRequestHistoryAsync(summary, token).ConfigureAwait(false);
            await SeedChatAsync(summary, token).ConfigureAwait(false);

            await _Client.FlushAsync(token).ConfigureAwait(false);

            return summary;
        }

        /// <summary>
        /// Remove previously seeded synthetic data: graphs labeled 'synthetic', chat threads owned by
        /// synthetic users, the synthetic users themselves, and request-history entries carrying the
        /// synthetic correlation ID.  Data not created by this tool is left untouched.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task WipeAsync(CancellationToken token = default)
        {
            bool tenantExists = await _Client.Tenant.ExistsByGuid(_Settings.TenantGuid, token).ConfigureAwait(false);
            if (!tenantExists)
            {
                Console.WriteLine("Wipe: tenant " + _Settings.TenantGuid + " does not exist; nothing to remove");
                return;
            }

            int threadsRemoved = 0;
            int usersRemoved = 0;
            int graphsRemoved = 0;

            List<UserMaster> users = new List<UserMaster>();
            await foreach (UserMaster user in _Client.User.ReadAllInTenant(_Settings.TenantGuid, token: token).ConfigureAwait(false))
            {
                if (!String.IsNullOrEmpty(user.Email) && user.Email.EndsWith("@" + ContentFactory.SyntheticEmailDomain, StringComparison.OrdinalIgnoreCase))
                    users.Add(user);
            }

            foreach (UserMaster user in users)
            {
                token.ThrowIfCancellationRequested();

                List<ChatThread> threads = new List<ChatThread>();
                await foreach (ChatThread thread in _Client.ChatThread.ReadAllInTenant(_Settings.TenantGuid, user.GUID, token: token).ConfigureAwait(false))
                {
                    threads.Add(thread);
                }

                foreach (ChatThread thread in threads)
                {
                    await _Client.ChatThread.DeleteByGuid(_Settings.TenantGuid, thread.GUID, token).ConfigureAwait(false);
                    threadsRemoved++;
                }

                await _Client.User.DeleteByGuid(_Settings.TenantGuid, user.GUID, token).ConfigureAwait(false);
                usersRemoved++;
            }

            List<Graph> graphs = new List<Graph>();
            await foreach (Graph graph in _Client.Graph.ReadAllInTenant(_Settings.TenantGuid, EnumerationOrderEnum.CreatedDescending, 0, false, true, token).ConfigureAwait(false))
            {
                if (graph.Labels != null && graph.Labels.Contains(ContentFactory.SyntheticLabel)) graphs.Add(graph);
            }

            foreach (Graph graph in graphs)
            {
                token.ThrowIfCancellationRequested();
                await _Client.Graph.DeleteByGuid(_Settings.TenantGuid, graph.GUID, true, token).ConfigureAwait(false);
                graphsRemoved++;
            }

            RequestHistorySearchRequest search = new RequestHistorySearchRequest
            {
                TenantGUID = _Settings.TenantGuid,
                CorrelationId = ContentFactory.RequestCorrelationId
            };

            int requestsRemoved = await _Repo.RequestHistory.DeleteMany(search, token).ConfigureAwait(false);

            await _Client.FlushAsync(token).ConfigureAwait(false);

            Console.WriteLine(
                "Wipe: removed " + graphsRemoved + " graph(s), " + threadsRemoved + " chat thread(s), "
                + usersRemoved + " user(s), " + requestsRemoved + " request-history entrie(s)");
        }

        #endregion

        #region Private-Methods

        private async Task EnsureTenantAsync(SeedSummary summary, CancellationToken token)
        {
            bool exists = await _Client.Tenant.ExistsByGuid(_Settings.TenantGuid, token).ConfigureAwait(false);
            if (exists)
            {
                Console.WriteLine("Tenant " + _Settings.TenantGuid + " already exists");
                return;
            }

            TenantMetadata tenant = new TenantMetadata
            {
                GUID = _Settings.TenantGuid,
                Name = "Default Tenant",
                Active = true,
                CreatedUtc = _WindowStartUtc,
                LastUpdateUtc = _WindowStartUtc
            };

            await _Client.Tenant.Create(tenant, token).ConfigureAwait(false);
            summary.TenantsCreated++;
            Console.WriteLine("Created tenant " + _Settings.TenantGuid);
        }

        private async Task EnsureUsersAsync(SeedSummary summary, CancellationToken token)
        {
            await foreach (UserMaster existing in _Client.User.ReadAllInTenant(_Settings.TenantGuid, token: token).ConfigureAwait(false))
            {
                if (!String.IsNullOrEmpty(existing.Email) && existing.Email.EndsWith("@" + ContentFactory.SyntheticEmailDomain, StringComparison.OrdinalIgnoreCase))
                    _SyntheticUsers.Add(existing);
            }

            int ordinal = 0;
            while (_SyntheticUsers.Count < _SyntheticUserCount && ordinal < _SyntheticUserCount * 2)
            {
                string[] name = _Content.PickUserName(ordinal);
                string email = (name[0] + "." + name[1] + "@" + ContentFactory.SyntheticEmailDomain).ToLowerInvariant();
                ordinal++;

                bool taken = _SyntheticUsers.Any(u => u.Email != null && u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
                if (taken) continue;

                UserMaster user = new UserMaster
                {
                    TenantGUID = _Settings.TenantGuid,
                    FirstName = name[0],
                    LastName = name[1],
                    Email = email,
                    Password = _Content.RandomHex(24),
                    Active = true,
                    CreatedUtc = _WindowStartUtc.AddMinutes(_Random.Next(0, 240)),
                };

                user.LastUpdateUtc = user.CreatedUtc;

                UserMaster created = await _Client.User.Create(user, token).ConfigureAwait(false);
                _SyntheticUsers.Add(created);
                summary.UsersCreated++;
            }

            Console.WriteLine("Users: " + _SyntheticUsers.Count + " synthetic user(s) available (" + summary.UsersCreated + " created)");
        }

        private async Task SeedGraphsAsync(SeedSummary summary, CancellationToken token)
        {
            for (int g = 0; g < _Settings.GraphCount; g++)
            {
                token.ThrowIfCancellationRequested();

                GraphTheme theme = _Content.GetTheme(g);
                DateTime graphCreated = _WindowStartUtc.AddMinutes(_Random.NextDouble() * 0.1 * (_WindowEndUtc - _WindowStartUtc).TotalMinutes);

                Graph graph = new Graph
                {
                    TenantGUID = _Settings.TenantGuid,
                    Name = _Content.BuildGraphName(g),
                    Labels = new List<string> { ContentFactory.SyntheticLabel, theme.ThemeLabel },
                    Tags = BuildTags(theme.ThemeLabel),
                    Data = new Dictionary<string, object>
                    {
                        { "description", "Synthetic " + theme.ThemeLabel + " graph generated by LoadGenerator" },
                        { "theme", theme.ThemeLabel }
                    },
                    CreatedUtc = graphCreated,
                    LastUpdateUtc = graphCreated
                };

                Graph createdGraph = await _Client.Graph.Create(graph, token).ConfigureAwait(false);
                _SyntheticGraphs.Add(createdGraph);
                summary.GraphsCreated++;

                List<Node> nodes = BuildNodes(createdGraph, theme, graphCreated, summary);
                for (int offset = 0; offset < nodes.Count; offset += _NodeBatchSize)
                {
                    List<Node> batch = nodes.Skip(offset).Take(_NodeBatchSize).ToList();
                    await _Client.Node.CreateMany(_Settings.TenantGuid, createdGraph.GUID, batch, BulkCreateReturnModeEnum.Minimal, token).ConfigureAwait(false);
                }

                summary.NodesCreated += nodes.Count;

                List<Edge> edges = BuildEdges(createdGraph, theme, nodes);
                for (int offset = 0; offset < edges.Count; offset += _EdgeBatchSize)
                {
                    List<Edge> batch = edges.Skip(offset).Take(_EdgeBatchSize).ToList();
                    await _Client.Edge.CreateMany(_Settings.TenantGuid, createdGraph.GUID, batch, BulkCreateReturnModeEnum.Minimal, token).ConfigureAwait(false);
                }

                summary.EdgesCreated += edges.Count;

                Console.WriteLine(
                    "Graph " + (g + 1) + "/" + _Settings.GraphCount + ": '" + createdGraph.Name + "' with "
                    + nodes.Count + " node(s), " + edges.Count + " edge(s)");
            }
        }

        private List<Node> BuildNodes(Graph graph, GraphTheme theme, DateTime graphCreated, SeedSummary summary)
        {
            List<DateTime> timestamps = _Clock.GenerateTimestamps(_Settings.NodesPerGraph);
            List<Node> nodes = new List<Node>();

            for (int i = 0; i < _Settings.NodesPerGraph; i++)
            {
                DateTime created = timestamps[i];
                if (created <= graphCreated) created = graphCreated.AddSeconds(30 + _Random.Next(0, 3600));
                if (created >= _WindowEndUtc) created = _WindowEndUtc.AddSeconds(-1);

                string nodeType = _Content.PickNodeType(theme);
                string nodeName = _Content.BuildNodeName(theme, i);

                Node node = new Node
                {
                    TenantGUID = _Settings.TenantGuid,
                    GraphGUID = graph.GUID,
                    Name = nodeName,
                    Labels = new List<string> { ContentFactory.SyntheticLabel, nodeType },
                    Tags = BuildTags(nodeType),
                    Data = _Content.BuildNodeData(theme, nodeType),
                    CreatedUtc = created,
                    LastUpdateUtc = created
                };

                if (_Random.NextDouble() < _Settings.VectorFraction)
                {
                    VectorMetadata vector = new VectorMetadata
                    {
                        TenantGUID = _Settings.TenantGuid,
                        GraphGUID = graph.GUID,
                        NodeGUID = node.GUID,
                        Model = _VectorModel,
                        Dimensionality = _VectorDimensions,
                        Content = _Content.BuildVectorContent(theme, nodeName, nodeType),
                        Vectors = _Content.BuildUnitVector(_VectorDimensions),
                        CreatedUtc = created,
                        LastUpdateUtc = created
                    };

                    node.Vectors = new List<VectorMetadata> { vector };
                    summary.VectorsCreated++;
                }

                nodes.Add(node);
            }

            return nodes;
        }

        private List<Edge> BuildEdges(Graph graph, GraphTheme theme, List<Node> nodes)
        {
            List<Edge> edges = new List<Edge>();
            HashSet<string> connected = new HashSet<string>();

            List<Node> shuffled = nodes.OrderBy(n => _Random.Next()).ToList();

            for (int i = 0; i + 1 < shuffled.Count; i++)
            {
                edges.Add(BuildEdge(graph, theme, shuffled[i], shuffled[i + 1]));
                connected.Add(shuffled[i].GUID.ToString() + "|" + shuffled[i + 1].GUID.ToString());
            }

            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = 0; j < nodes.Count; j++)
                {
                    if (i == j) continue;
                    if (_Random.NextDouble() >= _Settings.Density) continue;

                    string key = nodes[i].GUID.ToString() + "|" + nodes[j].GUID.ToString();
                    if (!connected.Add(key)) continue;

                    edges.Add(BuildEdge(graph, theme, nodes[i], nodes[j]));
                }
            }

            return edges;
        }

        private Edge BuildEdge(Graph graph, GraphTheme theme, Node from, Node to)
        {
            DateTime created = from.CreatedUtc > to.CreatedUtc ? from.CreatedUtc : to.CreatedUtc;
            created = created.AddMinutes(_Random.Next(1, 360));
            if (created >= _WindowEndUtc) created = _WindowEndUtc.AddSeconds(-1);

            string edgeType = _Content.PickEdgeType(theme);

            return new Edge
            {
                TenantGUID = _Settings.TenantGuid,
                GraphGUID = graph.GUID,
                Name = edgeType,
                From = from.GUID,
                To = to.GUID,
                Cost = 1 + _Random.Next(0, 100),
                Labels = new List<string> { ContentFactory.SyntheticLabel, edgeType },
                Tags = BuildTags(edgeType),
                CreatedUtc = created,
                LastUpdateUtc = created
            };
        }

        private async Task SeedRequestHistoryAsync(SeedSummary summary, CancellationToken token)
        {
            if (_Settings.RequestCount < 1)
            {
                Console.WriteLine("Request history: skipped (count is 0)");
                return;
            }

            List<DateTime> timestamps = _Clock.GenerateTimestamps(_Settings.RequestCount);

            for (int i = 0; i < timestamps.Count; i++)
            {
                token.ThrowIfCancellationRequested();

                RequestHistoryDetail detail = BuildRequestDetail(timestamps[i]);
                await _Repo.RequestHistory.Insert(detail, token).ConfigureAwait(false);
                summary.RequestHistoryCreated++;

                if ((i + 1) % 500 == 0)
                    Console.WriteLine("Request history: " + (i + 1) + "/" + timestamps.Count);
            }

            Console.WriteLine("Request history: " + summary.RequestHistoryCreated + " entrie(s) created");
        }

        private RequestHistoryDetail BuildRequestDetail(DateTime createdUtc)
        {
            string method = PickMethod();
            string path = PickPath(method);
            bool success = _Random.NextDouble() < 0.95;
            int statusCode = PickStatusCode(method, success);
            double processingMs = Math.Round(Math.Exp(2.0 + (_Random.NextDouble() * 3.5)), 2);

            long requestLength = method == "GET" || method == "HEAD" || method == "DELETE" ? 0 : _Random.Next(64, 4096);
            long responseLength = statusCode == 204 || method == "HEAD" ? 0 : _Random.Next(128, 65536);

            RequestHistoryDetail detail = new RequestHistoryDetail
            {
                GUID = Guid.NewGuid(),
                RequestId = Guid.NewGuid().ToString(),
                CorrelationId = ContentFactory.RequestCorrelationId,
                TraceId = _Content.RandomHex(32),
                CreatedUtc = createdUtc,
                CompletedUtc = createdUtc.AddMilliseconds(processingMs),
                Method = method,
                Path = path,
                Url = "http://localhost:8701" + path,
                SourceIp = _Content.PickSourceIp(),
                TenantGUID = _Settings.TenantGuid,
                UserGUID = _SyntheticUsers.Count > 0 ? _SyntheticUsers[_Random.Next(_SyntheticUsers.Count)].GUID : null,
                StatusCode = statusCode,
                Success = success,
                ProcessingTimeMs = processingMs,
                RequestBodyLength = requestLength,
                ResponseBodyLength = responseLength,
                RequestContentType = requestLength > 0 ? "application/json" : null,
                ResponseContentType = responseLength > 0 ? "application/json" : null
            };

            detail.RequestHeaders = new Dictionary<string, string>
            {
                { "User-Agent", _Content.PickUserAgent() },
                { "Accept", "application/json" }
            };

            detail.ResponseHeaders = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" }
            };

            return detail;
        }

        private string PickMethod()
        {
            double roll = _Random.NextDouble();
            if (roll < 0.68) return "GET";
            if (roll < 0.83) return "PUT";
            if (roll < 0.90) return "POST";
            if (roll < 0.95) return "DELETE";
            return "HEAD";
        }

        private string PickPath(string method)
        {
            string tenantSegment = "/v1.0/tenants/" + _Settings.TenantGuid;

            if (_SyntheticGraphs.Count < 1 || _Random.NextDouble() < 0.1)
            {
                double roll = _Random.NextDouble();
                if (roll < 0.4) return tenantSegment + "/graphs";
                if (roll < 0.7) return "/v1.0/token";
                return tenantSegment + "/users";
            }

            Graph graph = _SyntheticGraphs[_Random.Next(_SyntheticGraphs.Count)];
            string graphSegment = tenantSegment + "/graphs/" + graph.GUID;

            double pathRoll = _Random.NextDouble();
            if (pathRoll < 0.25) return graphSegment + "/nodes";
            if (pathRoll < 0.45) return graphSegment + "/nodes/" + Guid.NewGuid();
            if (pathRoll < 0.6) return graphSegment + "/edges";
            if (pathRoll < 0.72) return graphSegment + "/edges/" + Guid.NewGuid();
            if (pathRoll < 0.85 && method != "DELETE") return graphSegment + "/nodes/search";
            if (pathRoll < 0.93 && method != "DELETE") return graphSegment + "/vectors/search";
            return graphSegment;
        }

        private int PickStatusCode(string method, bool success)
        {
            if (success)
            {
                if (method == "PUT" || method == "POST") return _Random.NextDouble() < 0.7 ? 200 : 201;
                if (method == "DELETE") return _Random.NextDouble() < 0.5 ? 200 : 204;
                return 200;
            }

            double roll = _Random.NextDouble();
            if (roll < 0.45) return 404;
            if (roll < 0.75) return 400;
            if (roll < 0.9) return 500;
            return 401;
        }

        private async Task SeedChatAsync(SeedSummary summary, CancellationToken token)
        {
            if (_Settings.ChatThreadCount < 1)
            {
                Console.WriteLine("Chat: skipped (thread count is 0)");
                return;
            }

            if (_SyntheticUsers.Count < 1)
            {
                Console.WriteLine("Chat: skipped (no synthetic users available)");
                return;
            }

            for (int t = 0; t < _Settings.ChatThreadCount; t++)
            {
                token.ThrowIfCancellationRequested();

                UserMaster user = _SyntheticUsers[_Random.Next(_SyntheticUsers.Count)];
                Graph? graph = _SyntheticGraphs.Count > 0 && _Random.NextDouble() < 0.8
                    ? _SyntheticGraphs[_Random.Next(_SyntheticGraphs.Count)]
                    : null;

                int turnCount = Math.Max(1, _Content.SamplePoisson(Math.Max(0.0, _Settings.ChatTurnsAverage - 1.0)) + 1);

                DateTime threadStart = _Clock.NextTimestamp();
                if (graph != null && threadStart <= graph.CreatedUtc) threadStart = graph.CreatedUtc.AddMinutes(5 + _Random.Next(0, 600));
                if (threadStart >= _WindowEndUtc) threadStart = _WindowEndUtc.AddMinutes(-30);

                ChatThread thread = new ChatThread
                {
                    TenantGUID = _Settings.TenantGuid,
                    UserGUID = user.GUID,
                    GraphGUID = graph != null ? graph.GUID : null,
                    Title = _Chat.PickThreadTitle(t),
                    CreatedUtc = threadStart
                };

                List<ChatTurn> turns = new List<ChatTurn>();
                DateTime turnTime = threadStart;

                for (int s = 0; s < turnCount; s++)
                {
                    ChatTurn turn = _Chat.BuildTurn();
                    turn.TenantGUID = _Settings.TenantGuid;
                    turn.ThreadGUID = thread.GUID;
                    turn.Sequence = s;
                    turn.CreatedUtc = turnTime;
                    turns.Add(turn);

                    turnTime = turnTime.AddMilliseconds(turn.TotalDurationMs).AddSeconds(20 + _Random.Next(0, 280));
                    if (turnTime >= _WindowEndUtc) turnTime = _WindowEndUtc.AddSeconds(-1);
                }

                thread.LastUpdateUtc = turnTime;

                ChatThread createdThread = await _Client.ChatThread.Create(thread, token).ConfigureAwait(false);
                summary.ChatThreadsCreated++;

                int feedbackCount = 0;

                foreach (ChatTurn turn in turns)
                {
                    await _Client.ChatTurn.Create(turn, token).ConfigureAwait(false);
                    summary.ChatTurnsCreated++;

                    if (_Random.NextDouble() < 0.3)
                    {
                        bool positive = _Random.NextDouble() < 0.8;

                        ChatFeedback feedback = new ChatFeedback
                        {
                            TenantGUID = _Settings.TenantGuid,
                            ThreadGUID = createdThread.GUID,
                            TurnGUID = turn.GUID,
                            UserGUID = user.GUID,
                            Rating = positive ? ChatFeedbackRatingEnum.ThumbsUp : ChatFeedbackRatingEnum.ThumbsDown,
                            FeedbackText = _Chat.PickFeedbackComment(positive),
                            CreatedUtc = turn.CreatedUtc.AddSeconds(20 + _Random.Next(0, 120))
                        };

                        await _Client.ChatFeedback.Create(feedback, token).ConfigureAwait(false);
                        summary.ChatFeedbackCreated++;
                        feedbackCount++;
                    }
                }

                Console.WriteLine(
                    "Chat thread " + (t + 1) + "/" + _Settings.ChatThreadCount + ": '" + createdThread.Title + "' with "
                    + turnCount + " turn(s), " + feedbackCount + " feedback entrie(s)");
            }
        }

        private NameValueCollection BuildTags(string typeValue)
        {
            NameValueCollection tags = new NameValueCollection();
            tags.Add(ContentFactory.GeneratorTagKey, ContentFactory.GeneratorTagValue);
            tags.Add("type", typeValue.ToLowerInvariant());
            return tags;
        }

        #endregion
    }
}
