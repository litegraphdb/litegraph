namespace LoadGenerator
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Produces plausible themed content for synthetic graphs, nodes, edges, and API requests.
    /// This class is not thread-safe; use one instance per thread.
    /// </summary>
    public class ContentFactory
    {
        #region Public-Members

        /// <summary>
        /// Label attached to every synthetic entity so wipe operations can find them.
        /// </summary>
        public const string SyntheticLabel = "synthetic";

        /// <summary>
        /// Tag key attached to every synthetic entity.
        /// </summary>
        public const string GeneratorTagKey = "generator";

        /// <summary>
        /// Tag value attached to every synthetic entity.
        /// </summary>
        public const string GeneratorTagValue = "loadgen";

        /// <summary>
        /// Correlation ID stamped on every synthetic request-history entry so wipe operations can find them.
        /// </summary>
        public const string RequestCorrelationId = "loadgen-synthetic";

        /// <summary>
        /// Email domain of synthetic users so wipe operations can find them.
        /// </summary>
        public const string SyntheticEmailDomain = "loadgen.synthetic";

        #endregion

        #region Private-Members

        private Random _Random;
        private List<GraphTheme> _Themes = new List<GraphTheme>();

        private static readonly string[] _FirstNames = new string[] { "Ava", "Marcus", "Priya", "Diego", "Yuki", "Lena", "Tomás", "Nadia" };
        private static readonly string[] _LastNames = new string[] { "Chen", "Webb", "Sharma", "Alvarez", "Tanaka", "Fischer", "Okafor", "Petrov" };

        private static readonly string[] _SourceIps = new string[]
        {
            "10.0.4.17", "10.0.4.22", "10.0.7.3", "172.16.2.41", "172.16.2.58",
            "192.168.14.9", "192.168.14.30", "10.1.9.104", "10.1.9.221", "172.20.0.6"
        };

        private static readonly string[] _UserAgents = new string[]
        {
            "litegraph-dashboard/8.1.0", "curl/8.5.0", "python-requests/2.32.3",
            "litegraph-sdk-dotnet/8.1.0", "litegraph-sdk-python/1.2.1", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)"
        };

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="random">Random number generator.</param>
        /// <exception cref="ArgumentNullException">Thrown when the random number generator is null.</exception>
        public ContentFactory(Random random)
        {
            _Random = random ?? throw new ArgumentNullException(nameof(random));
            BuildThemes();
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Get the theme for a graph by ordinal, rotating through the available templates.
        /// </summary>
        /// <param name="graphOrdinal">Zero-based graph ordinal.</param>
        /// <returns>Graph theme.</returns>
        public GraphTheme GetTheme(int graphOrdinal)
        {
            return _Themes[((graphOrdinal % _Themes.Count) + _Themes.Count) % _Themes.Count];
        }

        /// <summary>
        /// Build a graph name for the given ordinal, appending a cycle suffix when themes repeat.
        /// </summary>
        /// <param name="graphOrdinal">Zero-based graph ordinal.</param>
        /// <returns>Graph name.</returns>
        public string BuildGraphName(int graphOrdinal)
        {
            GraphTheme theme = GetTheme(graphOrdinal);
            int cycle = graphOrdinal / _Themes.Count;
            if (cycle == 0) return theme.GraphName;
            return theme.GraphName + " " + (cycle + 1).ToString();
        }

        /// <summary>
        /// Build a plausible node name for a theme.
        /// </summary>
        /// <param name="theme">Graph theme.</param>
        /// <param name="nodeOrdinal">Zero-based node ordinal within the graph.</param>
        /// <returns>Node name.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the theme is null.</exception>
        public string BuildNodeName(GraphTheme theme, int nodeOrdinal)
        {
            if (theme == null) throw new ArgumentNullException(nameof(theme));

            if (theme.ThemeLabel == "social")
            {
                string first = _FirstNames[_Random.Next(_FirstNames.Length)];
                string last = _LastNames[_Random.Next(_LastNames.Length)];
                return first + " " + last + " (" + nodeOrdinal.ToString("00") + ")";
            }

            string stem = theme.NodeNameStems[_Random.Next(theme.NodeNameStems.Length)];
            return stem + "-" + nodeOrdinal.ToString("00");
        }

        /// <summary>
        /// Pick a node type label from a theme.
        /// </summary>
        /// <param name="theme">Graph theme.</param>
        /// <returns>Node type label.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the theme is null.</exception>
        public string PickNodeType(GraphTheme theme)
        {
            if (theme == null) throw new ArgumentNullException(nameof(theme));
            return theme.NodeTypes[_Random.Next(theme.NodeTypes.Length)];
        }

        /// <summary>
        /// Pick an edge relationship name from a theme.
        /// </summary>
        /// <param name="theme">Graph theme.</param>
        /// <returns>Edge relationship name.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the theme is null.</exception>
        public string PickEdgeType(GraphTheme theme)
        {
            if (theme == null) throw new ArgumentNullException(nameof(theme));
            return theme.EdgeTypes[_Random.Next(theme.EdgeTypes.Length)];
        }

        /// <summary>
        /// Build a plausible JSON-serializable data payload for a node.
        /// </summary>
        /// <param name="theme">Graph theme.</param>
        /// <param name="nodeType">Node type label.</param>
        /// <returns>Dictionary payload.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the theme or node type is null.</exception>
        public Dictionary<string, object> BuildNodeData(GraphTheme theme, string nodeType)
        {
            if (theme == null) throw new ArgumentNullException(nameof(theme));
            if (nodeType == null) throw new ArgumentNullException(nameof(nodeType));

            Dictionary<string, object> data = new Dictionary<string, object>();
            data["type"] = nodeType;
            data["qualifier"] = theme.Qualifiers[_Random.Next(theme.Qualifiers.Length)];

            switch (theme.ThemeLabel)
            {
                case "infrastructure":
                    data["cpuCores"] = 2 << _Random.Next(0, 4);
                    data["memoryGb"] = 4 << _Random.Next(0, 5);
                    data["status"] = _Random.NextDouble() < 0.93 ? "healthy" : "degraded";
                    data["uptimePercent"] = Math.Round(97.5 + (_Random.NextDouble() * 2.49), 2);
                    break;
                case "knowledge":
                    data["wordCount"] = 300 + _Random.Next(0, 4200);
                    data["views"] = _Random.Next(5, 9000);
                    data["helpfulVotes"] = _Random.Next(0, 250);
                    break;
                case "catalog":
                    data["sku"] = "SKU-" + _Random.Next(100000, 999999).ToString();
                    data["priceUsd"] = Math.Round(4.99 + (_Random.NextDouble() * 495.0), 2);
                    data["inStock"] = _Random.NextDouble() < 0.85;
                    data["rating"] = Math.Round(2.5 + (_Random.NextDouble() * 2.5), 1);
                    break;
                case "social":
                    data["followers"] = _Random.Next(3, 25000);
                    data["postsPerWeek"] = Math.Round(_Random.NextDouble() * 20.0, 1);
                    data["verified"] = _Random.NextDouble() < 0.1;
                    break;
                case "supplychain":
                    data["capacityUnits"] = _Random.Next(500, 120000);
                    data["leadTimeDays"] = _Random.Next(1, 45);
                    data["onTimeRate"] = Math.Round(0.75 + (_Random.NextDouble() * 0.249), 3);
                    break;
                default:
                    data["value"] = _Random.Next(0, 1000);
                    break;
            }

            return data;
        }

        /// <summary>
        /// Build a short content snippet used for a node vector.
        /// </summary>
        /// <param name="theme">Graph theme.</param>
        /// <param name="nodeName">Node name.</param>
        /// <param name="nodeType">Node type label.</param>
        /// <returns>Content snippet.</returns>
        public string BuildVectorContent(GraphTheme? theme, string nodeName, string nodeType)
        {
            string label = theme != null ? theme.ThemeLabel : "generic";
            return nodeName + " is a " + nodeType + " entity in the " + label + " domain, tracked by LiteGraph synthetic load.";
        }

        /// <summary>
        /// Generate a random unit vector of the requested dimensionality.
        /// </summary>
        /// <param name="dimensions">Number of dimensions.  Minimum is 1.</param>
        /// <returns>List of floats normalized to unit length.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when dimensions is less than 1.</exception>
        public List<float> BuildUnitVector(int dimensions)
        {
            if (dimensions < 1) throw new ArgumentOutOfRangeException(nameof(dimensions));

            List<float> values = new List<float>(dimensions);
            double sumOfSquares = 0.0;

            for (int i = 0; i < dimensions; i++)
            {
                double value = (_Random.NextDouble() * 2.0) - 1.0;
                values.Add((float)value);
                sumOfSquares += value * value;
            }

            double magnitude = Math.Sqrt(sumOfSquares);
            if (magnitude < 0.000001) magnitude = 1.0;

            for (int i = 0; i < dimensions; i++)
            {
                values[i] = (float)(values[i] / magnitude);
            }

            return values;
        }

        /// <summary>
        /// Pick a synthetic user full name by ordinal.
        /// </summary>
        /// <param name="ordinal">Zero-based ordinal.</param>
        /// <returns>Tuple-free two-element array: first name at index 0, last name at index 1.</returns>
        public string[] PickUserName(int ordinal)
        {
            string first = _FirstNames[((ordinal % _FirstNames.Length) + _FirstNames.Length) % _FirstNames.Length];
            string last = _LastNames[((ordinal % _LastNames.Length) + _LastNames.Length) % _LastNames.Length];
            return new string[] { first, last };
        }

        /// <summary>
        /// Pick a plausible internal source IP address.
        /// </summary>
        /// <returns>IP address string.</returns>
        public string PickSourceIp()
        {
            return _SourceIps[_Random.Next(_SourceIps.Length)];
        }

        /// <summary>
        /// Pick a plausible user agent string.
        /// </summary>
        /// <returns>User agent string.</returns>
        public string PickUserAgent()
        {
            return _UserAgents[_Random.Next(_UserAgents.Length)];
        }

        /// <summary>
        /// Generate a random lowercase hexadecimal string, suitable for trace identifiers.
        /// </summary>
        /// <param name="length">Number of hexadecimal characters.  Minimum is 1.</param>
        /// <returns>Hexadecimal string.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when length is less than 1.</exception>
        public string RandomHex(int length)
        {
            if (length < 1) throw new ArgumentOutOfRangeException(nameof(length));

            const string chars = "0123456789abcdef";
            char[] buffer = new char[length];

            for (int i = 0; i < length; i++)
            {
                buffer[i] = chars[_Random.Next(chars.Length)];
            }

            return new string(buffer);
        }

        /// <summary>
        /// Sample from a Poisson distribution.
        /// </summary>
        /// <param name="lambda">Mean of the distribution.  Minimum is 0.</param>
        /// <returns>Sampled value.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when lambda is negative.</exception>
        public int SamplePoisson(double lambda)
        {
            if (lambda < 0.0) throw new ArgumentOutOfRangeException(nameof(lambda));
            if (lambda == 0.0) return 0;

            double limit = Math.Exp(-lambda);
            double product = 1.0;
            int count = 0;

            do
            {
                count++;
                product *= _Random.NextDouble();
            }
            while (product > limit);

            return count - 1;
        }

        #endregion

        #region Private-Methods

        private void BuildThemes()
        {
            _Themes.Add(new GraphTheme
            {
                GraphName = "Production Infrastructure",
                ThemeLabel = "infrastructure",
                NodeNameStems = new string[] { "web", "api", "db-primary", "db-replica", "redis-cache", "queue", "worker", "lb", "auth-svc", "metrics" },
                NodeTypes = new string[] { "server", "database", "load-balancer", "service", "cache", "queue" },
                EdgeTypes = new string[] { "DEPENDS_ON", "ROUTES_TO", "REPLICATES_TO", "CONNECTS_TO" },
                Qualifiers = new string[] { "us-west-2", "us-east-1", "eu-central-1", "ap-southeast-1" }
            });

            _Themes.Add(new GraphTheme
            {
                GraphName = "Engineering Knowledge Base",
                ThemeLabel = "knowledge",
                NodeNameStems = new string[] { "runbook", "postmortem", "design-doc", "faq", "howto", "adr", "glossary", "onboarding" },
                NodeTypes = new string[] { "article", "topic", "author", "playbook" },
                EdgeTypes = new string[] { "LINKS_TO", "AUTHORED_BY", "RELATED_TO", "SUPERSEDES" },
                Qualifiers = new string[] { "platform", "backend", "frontend", "sre", "security" }
            });

            _Themes.Add(new GraphTheme
            {
                GraphName = "Product Catalog",
                ThemeLabel = "catalog",
                NodeNameStems = new string[] { "widget", "gadget", "adapter", "sensor", "bracket", "module", "kit", "cable" },
                NodeTypes = new string[] { "product", "category", "supplier", "bundle" },
                EdgeTypes = new string[] { "BELONGS_TO", "SUPPLIED_BY", "SIMILAR_TO", "BUNDLED_WITH" },
                Qualifiers = new string[] { "hardware", "electronics", "accessories", "industrial" }
            });

            _Themes.Add(new GraphTheme
            {
                GraphName = "Customer Community",
                ThemeLabel = "social",
                NodeNameStems = new string[] { "member" },
                NodeTypes = new string[] { "member", "moderator", "advocate", "newcomer" },
                EdgeTypes = new string[] { "FOLLOWS", "MENTIONS", "REPLIES_TO", "COLLABORATES_WITH" },
                Qualifiers = new string[] { "forum", "slack", "discord", "meetup" }
            });

            _Themes.Add(new GraphTheme
            {
                GraphName = "Global Supply Chain",
                ThemeLabel = "supplychain",
                NodeNameStems = new string[] { "warehouse", "plant", "port", "dc", "supplier", "carrier", "hub", "depot" },
                NodeTypes = new string[] { "warehouse", "factory", "port", "distribution-center", "carrier" },
                EdgeTypes = new string[] { "SHIPS_TO", "SOURCES_FROM", "STOCKS", "ROUTES_TO" },
                Qualifiers = new string[] { "emea", "apac", "amer", "latam" }
            });
        }

        #endregion
    }
}
