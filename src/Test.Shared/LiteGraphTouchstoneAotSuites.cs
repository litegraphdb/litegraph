namespace Test.Shared
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using ExpressionTree;
    using LiteGraph;
    using LiteGraph.Algorithms;
    using LiteGraph.Gexf;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.Indexing.Vector;
    using LiteGraph.Serialization;
    using LiteGraph.Storage;
    using Touchstone.Core;

    /// <summary>
    /// Touchstone test cases that pin LiteGraph's JSON and GEXF output so the Native AOT work cannot change it.
    /// The baselines under Baselines/ were captured with the reflection-based serializer before the change.
    /// Set LITEGRAPH_CAPTURE_AOT_BASELINES to a directory to write fresh baselines there instead of comparing.
    /// </summary>
    public static partial class LiteGraphTouchstoneSuites
    {
        #region Private-Members

        private const string _AotSuiteId = "Aot.Serialization";
        private const string _AotCaptureEnvironmentVariable = "LITEGRAPH_CAPTURE_AOT_BASELINES";
        private const string _SerializationBaselineFile = "serialization-baseline.json";
        private const string _GexfBaselineFile = "gexf-baseline.xml";
        private const string _AotSampleDataJson = "{\"name\":\"sample\",\"count\":3,\"ratio\":0.25,\"enabled\":true,\"missing\":null,\"items\":[1,\"two\",{\"three\":3}],\"nested\":{\"deep\":{\"value\":\"x\"}}}";

        private static readonly DateTime _AotSampleTimestamp = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc).AddTicks(1234560);

        // Assemblies whose classes the sample populator fills in when they appear as property types.
        private static readonly HashSet<Assembly> _AotSampleAssemblies = new HashSet<Assembly>
        {
            typeof(Node).Assembly,
            typeof(LiteGraph.Server.Classes.Settings).Assembly,
            typeof(WatsonWebserver.Core.WebserverSettings).Assembly
        };

        private static readonly Type[] _AotParityTypes = new Type[]
        {
            typeof(AuthenticationToken),
            typeof(AuthorizationAuditEntry),
            typeof(AuthorizationAuditSearchRequest),
            typeof(AuthorizationAuditSearchResult),
            typeof(AuthorizationEffectiveGrant),
            typeof(AuthorizationEffectivePermissionsResult),
            typeof(AuthorizationRole),
            typeof(AuthorizationRoleSearchRequest),
            typeof(AuthorizationRoleSearchResult),
            typeof(BackupFile),
            typeof(CachingSettings),
            typeof(ChatEndpoint),
            typeof(ChatFeedback),
            typeof(ChatSettings),
            typeof(ChatThread),
            typeof(ChatTurn),
            typeof(Credential),
            typeof(CredentialScopeAssignment),
            typeof(CredentialScopeAssignmentSearchRequest),
            typeof(CredentialScopeAssignmentSearchResult),
            typeof(DatabaseSettings),
            typeof(Edge),
            typeof(EdgeBetween),
            typeof(EnumerationRequest),
            typeof(EnumerationResult<Node>),
            typeof(EnumerationResult<Edge>),
            typeof(EnumerationResult<Graph>),
            typeof(EnumerationResult<RouteDetail>),
            typeof(Dictionary<Guid, GraphStatistics>),
            typeof(Dictionary<Guid, TenantStatistics>),
            typeof(ExistenceRequest),
            typeof(ExistenceResult),
            typeof(Graph),
            typeof(GraphImportRequest),
            typeof(GraphImportResult),
            typeof(GraphQueryExecutionProfile),
            typeof(GraphQueryPlanSummary),
            typeof(GraphQueryRequest),
            typeof(GraphQueryResponse),
            typeof(GraphQueryResult),
            typeof(GraphStatistics),
            typeof(JsonlExportMetadata),
            typeof(JsonlRecord),
            typeof(LabelMetadata),
            typeof(LoggingSettings),
            typeof(Node),
            typeof(RequestHistoryDetail),
            typeof(RequestHistoryEntry),
            typeof(RequestHistorySearchRequest),
            typeof(RequestHistorySearchResult),
            typeof(RequestHistorySummary),
            typeof(RequestHistorySummaryBucket),
            typeof(RoleDefinition),
            typeof(RouteDetail),
            typeof(SearchRequest),
            typeof(SearchResult),
            typeof(StorageSettings),
            typeof(SubgraphExtractionRequest),
            typeof(SyslogServer),
            typeof(TagMetadata),
            typeof(TenantMetadata),
            typeof(TenantStatistics),
            typeof(TransactionExecutionOptions),
            typeof(TransactionOperation),
            typeof(TransactionOperationResult),
            typeof(TransactionRequest),
            typeof(TransactionResult),
            typeof(UserMaster),
            typeof(UserRoleAssignment),
            typeof(UserRoleAssignmentSearchRequest),
            typeof(UserRoleAssignmentSearchResult),
            typeof(VectorIndexConfiguration),
            typeof(VectorMetadata),
            typeof(VectorScoreResult),
            typeof(VectorSearchRequest),
            typeof(VectorSearchResult),
            typeof(GraphAlgorithmConfiguration),
            typeof(GraphAlgorithmImportRequest),
            typeof(GraphAlgorithmNodeResult),
            typeof(GraphAlgorithmRequest),
            typeof(GraphAlgorithmResult),
            typeof(HnswIndexState),
            typeof(HnswNodeState),
            typeof(VectorDistanceResult),
            typeof(VectorIndexEntry),
            typeof(VectorIndexStatistics),
            typeof(StorageEntityCounts),
            typeof(StorageMigrationResult),
            typeof(StorageVerificationResult),
            typeof(Expr)
        };

        #endregion

        #region Private-Methods

        private static TestSuiteDescriptor CreateAotSerializationSuite()
        {
            return new TestSuiteDescriptor(
                suiteId: _AotSuiteId,
                displayName: "Serialization and export output is unchanged by the Native AOT work",
                cases: new List<TestCaseDescriptor>
                {
                    new TestCaseDescriptor(
                        suiteId: _AotSuiteId,
                        caseId: "Aot.SerializationParity.Compact",
                        displayName: "Compact JSON for every model type matches the reflection-serializer baseline",
                        executeAsync: TestAotSerializationParityCompact),
                    new TestCaseDescriptor(
                        suiteId: _AotSuiteId,
                        caseId: "Aot.SerializationParity.Pretty",
                        displayName: "Indented JSON for every model type matches the reflection-serializer baseline",
                        executeAsync: TestAotSerializationParityPretty),
                    new TestCaseDescriptor(
                        suiteId: _AotSuiteId,
                        caseId: "Aot.SerializationParity.RoundTrip",
                        displayName: "Baseline JSON deserializes and reserializes exactly as the reflection serializer did",
                        executeAsync: TestAotSerializationParityRoundTrip),
                    new TestCaseDescriptor(
                        suiteId: _AotSuiteId,
                        caseId: "Aot.ContextCoverage",
                        displayName: "LiteGraphJsonContext supplies metadata for every model type without reflection",
                        executeAsync: TestAotContextCoverage),
                    new TestCaseDescriptor(
                        suiteId: _AotSuiteId,
                        caseId: "Aot.JitCompatibility",
                        displayName: "Under the JIT, exceptions and unregistered types serialize as they did before",
                        executeAsync: TestAotJitCompatibility),
                    new TestCaseDescriptor(
                        suiteId: _AotSuiteId,
                        caseId: "Aot.DateTimeUtc",
                        displayName: "Timestamps keep their UTC value through JSON in any machine time zone",
                        executeAsync: TestAotDateTimeUtc),
                    new TestCaseDescriptor(
                        suiteId: _AotSuiteId,
                        caseId: "Aot.GexfParity",
                        displayName: "GEXF export matches the XmlSerializer baseline",
                        executeAsync: TestAotGexfParity)
                });
        }

        private static async Task TestAotSerializationParityCompact(CancellationToken token)
        {
            await CompareAotSerializationBaseline("compact", token).ConfigureAwait(false);
        }

        private static async Task TestAotSerializationParityPretty(CancellationToken token)
        {
            await CompareAotSerializationBaseline("pretty", token).ConfigureAwait(false);
        }

        private static async Task TestAotSerializationParityRoundTrip(CancellationToken token)
        {
            await CompareAotSerializationBaseline("roundtrip", token).ConfigureAwait(false);
        }

        private static async Task CompareAotSerializationBaseline(string kind, CancellationToken token)
        {
            SortedDictionary<string, Dictionary<string, string>> actual = BuildAotSerializationSnapshot();

            // Indented JSON uses the platform's line ending (CRLF on Windows); baselines use LF.
            foreach (Dictionary<string, string> entry in actual.Values)
            {
                foreach (string key in entry.Keys.ToList()) entry[key] = entry[key].Replace("\r\n", "\n");
            }

            string? captureDirectory = Environment.GetEnvironmentVariable(_AotCaptureEnvironmentVariable);
            if (!String.IsNullOrEmpty(captureDirectory))
            {
                Directory.CreateDirectory(captureDirectory);
                string json = JsonSerializer.Serialize(actual, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(Path.Combine(captureDirectory, _SerializationBaselineFile), json, token).ConfigureAwait(false);
                return;
            }

            string baselinePath = Path.Combine(AppContext.BaseDirectory, "Baselines", _SerializationBaselineFile);
            AssertTrue(File.Exists(baselinePath), "Serialization baseline exists at " + baselinePath);
            string baselineJson = await File.ReadAllTextAsync(baselinePath, token).ConfigureAwait(false);
            Dictionary<string, Dictionary<string, string>> expected =
                JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(baselineJson)
                ?? new Dictionary<string, Dictionary<string, string>>();

            List<string> failures = new List<string>();
            foreach (KeyValuePair<string, Dictionary<string, string>> entry in expected)
            {
                if (!actual.TryGetValue(entry.Key, out Dictionary<string, string>? actualEntry))
                {
                    failures.Add(entry.Key + ": no longer produced");
                    continue;
                }

                string expectedValue = entry.Value[kind];
                string actualValue = actualEntry[kind];
                if (!String.Equals(expectedValue, actualValue, StringComparison.Ordinal))
                {
                    failures.Add(entry.Key + ":\n    expected " + expectedValue + "\n    actual   " + actualValue);
                }
            }

            foreach (string key in actual.Keys)
            {
                if (!expected.ContainsKey(key)) failures.Add(key + ": not in baseline (recapture baselines to add it)");
            }

            AssertTrue(failures.Count == 0, kind + " serialization differs from baseline for " + failures.Count + " type(s):\n  " + String.Join("\n  ", failures));
        }

        private static SortedDictionary<string, Dictionary<string, string>> BuildAotSerializationSnapshot()
        {
            Serializer serializer = new Serializer();
            SortedDictionary<string, Dictionary<string, string>> snapshot = new SortedDictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);

            foreach (Type type in _AotParityTypes)
            {
                object instance = CreateAotSample(type, 0);
                AddAotSnapshotEntry(snapshot, AotTypeKey(type), type, instance, serializer);
            }

            foreach (KeyValuePair<string, object?> data in CreateAotDataVariants())
            {
                Node node = (Node)CreateAotSample(typeof(Node), 0);
                node.Data = data.Value;
                AddAotSnapshotEntry(snapshot, "Node.Data=" + data.Key, typeof(Node), node, serializer);
            }

            return snapshot;
        }

        private static void AddAotSnapshotEntry(
            SortedDictionary<string, Dictionary<string, string>> snapshot,
            string key,
            Type type,
            object instance,
            Serializer serializer)
        {
            string compact = serializer.SerializeJson(instance, false);
            string pretty = serializer.SerializeJson(instance, true);

            MethodInfo deserialize = typeof(Serializer).GetMethods()
                .Single(m => m.Name == nameof(Serializer.DeserializeJson) && m.GetParameters().Length == 1)
                .MakeGenericMethod(type);
            object? roundTripped = deserialize.Invoke(serializer, new object[] { compact });
            string roundTrip = MaskAotClockValues(serializer.SerializeJson(roundTripped!, false));

            snapshot[key] = new Dictionary<string, string>
            {
                { "compact", compact },
                { "pretty", pretty },
                { "roundtrip", roundTrip }
            };
        }

        private static string MaskAotClockValues(string json)
        {
            // Types that create a Timestamps.Timestamp in their constructor stamp the current time on deserialization.
            json = Regex.Replace(json, "\"(Start|End)\":\"[^\"]*\"", "\"$1\":\"MASKED\"");
            return Regex.Replace(json, "\"TotalMs\":[-0-9.Ee+]+", "\"TotalMs\":0");
        }

        private static List<KeyValuePair<string, object?>> CreateAotDataVariants()
        {
            Dictionary<string, object> dictionary = new Dictionary<string, object>
            {
                { "name", "sample" },
                { "count", 3L },
                { "ratio", 0.25d },
                { "enabled", true },
                { "items", new List<object> { 1L, "two" } }
            };

            return new List<KeyValuePair<string, object?>>
            {
                new KeyValuePair<string, object?>("JsonElement", JsonDocument.Parse(_AotSampleDataJson).RootElement.Clone()),
                new KeyValuePair<string, object?>("JsonObject", JsonNode.Parse(_AotSampleDataJson)),
                new KeyValuePair<string, object?>("Dictionary", dictionary),
                new KeyValuePair<string, object?>("String", "plain text"),
                new KeyValuePair<string, object?>("Long", 42L),
                new KeyValuePair<string, object?>("Double", 1.5d),
                new KeyValuePair<string, object?>("Bool", true),
                new KeyValuePair<string, object?>("Guid", AotGuid("data-guid")),
                new KeyValuePair<string, object?>("ListOfObject", new List<object> { "a", 2L, true }),
                new KeyValuePair<string, object?>("Model", CreateAotSample(typeof(TagMetadata), 1)),
                new KeyValuePair<string, object?>("Null", null)
            };
        }

        private static string AotTypeKey(Type type)
        {
            if (!type.IsGenericType) return type.FullName ?? type.Name;
            string name = type.GetGenericTypeDefinition().FullName ?? type.Name;
            name = name.Substring(0, name.IndexOf('`'));
            return name + "<" + String.Join(",", type.GetGenericArguments().Select(AotTypeKey)) + ">";
        }

        private static object CreateAotSample(Type type, int depth)
        {
            if (type == typeof(Expr))
            {
                return new Expr(
                    new Expr("Name", OperatorEnum.Equals, "sample"),
                    OperatorEnum.And,
                    new Expr("Count", OperatorEnum.GreaterThan, 3L));
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>) && type.GetGenericArguments()[0] == typeof(Guid))
            {
                // A dictionary keyed by GUID as the root sample (statistics results). Properties of this shape inside other
                // types stay empty, as in the 10.1 baselines.
                string seed = AotTypeKey(type);
                IDictionary dictionary = (IDictionary)Activator.CreateInstance(type)!;
                List<object?> values = AotCollectionItems(type.GetGenericArguments()[1], seed, depth);
                for (int i = 0; i < values.Count; i++) dictionary[AotGuid(seed + "#key" + i)] = values[i];
                return dictionary;
            }

            object instance = Activator.CreateInstance(type)!;
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).OrderBy(p => p.Name, StringComparer.Ordinal))
            {
                if (property.GetIndexParameters().Length > 0) continue;
                MethodInfo? setter = property.GetSetMethod();
                if (setter == null) continue;

                foreach (object? candidate in AotCandidateValues(property.PropertyType, type.Name + "." + property.Name, depth))
                {
                    try
                    {
                        property.SetValue(instance, candidate);
                        break;
                    }
                    catch (TargetInvocationException)
                    {
                    }
                    catch (ArgumentException)
                    {
                    }
                }
            }

            return instance;
        }

        private static IEnumerable<object?> AotCandidateValues(Type type, string seed, int depth)
        {
            Type? nullable = Nullable.GetUnderlyingType(type);
            if (nullable != null) type = nullable;

            uint hash = AotHash(seed);

            if (type == typeof(string))
            {
                yield return "s-" + seed.Substring(seed.LastIndexOf('.') + 1).ToLowerInvariant();
                yield return "http://localhost:8000/";
                yield return "sample@example.com";
                yield break;
            }

            if (type == typeof(Guid)) { yield return AotGuid(seed); yield break; }
            if (type == typeof(bool)) { yield return true; yield break; }
            if (type == typeof(DateTime)) { yield return _AotSampleTimestamp; yield break; }
            if (type == typeof(DateTimeOffset)) { yield return new DateTimeOffset(_AotSampleTimestamp); yield break; }
            if (type == typeof(TimeSpan)) { yield return TimeSpan.FromSeconds(90); yield break; }
            if (type == typeof(byte[])) { yield return new byte[] { 1, 2, 3, (byte)(hash & 0xFF) }; yield break; }
            if (type == typeof(IPAddress)) { yield return IPAddress.Loopback; yield break; }

            if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte)
                || type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort))
            {
                foreach (long value in new long[] { 2 + (hash % 50), 1, 10, 100, 1000, 0 })
                    yield return Convert.ChangeType(value, type);
                yield break;
            }

            if (type == typeof(double) || type == typeof(float) || type == typeof(decimal))
            {
                foreach (double value in new double[] { 0.25 + (hash % 10) / 100d, 0.5, 1, 10, 0 })
                    yield return Convert.ChangeType(value, type);
                yield break;
            }

            if (type.IsEnum)
            {
                Array values = Enum.GetValues(type);
                if (values.Length > 1) yield return values.GetValue(1);
                if (values.Length > 0) yield return values.GetValue(0);
                yield break;
            }

            if (type == typeof(object))
            {
                yield return JsonDocument.Parse(_AotSampleDataJson).RootElement.Clone();
                yield break;
            }

            if (type == typeof(NameValueCollection))
            {
                NameValueCollection collection = new NameValueCollection();
                collection.Add("key", "value");
                collection.Add("other", "second");
                yield return collection;
                yield break;
            }

            if (type == typeof(Expr))
            {
                yield return CreateAotSample(typeof(Expr), depth + 1);
                yield break;
            }

            if (type.IsArray)
            {
                Type element = type.GetElementType()!;
                List<object?> items = AotCollectionItems(element, seed, depth);
                Array array = Array.CreateInstance(element, items.Count);
                for (int i = 0; i < items.Count; i++) array.SetValue(items[i], i);
                yield return array;
                yield break;
            }

            if (type.IsGenericType)
            {
                Type definition = type.GetGenericTypeDefinition();
                Type[] arguments = type.GetGenericArguments();

                if (definition == typeof(List<>) || definition == typeof(IList<>) || definition == typeof(IEnumerable<>)
                    || definition == typeof(ICollection<>) || definition == typeof(IReadOnlyList<>) || definition == typeof(IReadOnlyCollection<>)
                    || definition == typeof(HashSet<>))
                {
                    Type concrete = definition == typeof(HashSet<>) ? type : typeof(List<>).MakeGenericType(arguments[0]);
                    object collection = Activator.CreateInstance(concrete)!;
                    MethodInfo add = concrete.GetMethod("Add")!;
                    foreach (object? item in AotCollectionItems(arguments[0], seed, depth)) add.Invoke(collection, new object?[] { item });
                    yield return collection;
                    yield break;
                }

                if ((definition == typeof(Dictionary<,>) || definition == typeof(IDictionary<,>) || definition == typeof(IReadOnlyDictionary<,>))
                    && arguments[0] == typeof(string))
                {
                    Type concrete = typeof(Dictionary<,>).MakeGenericType(arguments);
                    IDictionary dictionary = (IDictionary)Activator.CreateInstance(concrete)!;
                    List<object?> values = AotCollectionItems(arguments[1], seed, depth);
                    for (int i = 0; i < values.Count; i++) dictionary["key" + i] = values[i];
                    yield return dictionary;
                    yield break;
                }
            }

            if (depth < 2 && type.IsClass && !type.IsAbstract && _AotSampleAssemblies.Contains(type.Assembly)
                && type.GetConstructor(Type.EmptyTypes) != null)
            {
                yield return CreateAotSample(type, depth + 1);
                yield break;
            }

            yield return null;
        }

        private static List<object?> AotCollectionItems(Type element, string seed, int depth)
        {
            List<object?> items = new List<object?>();
            if (depth >= 2 && element.IsClass && element != typeof(string) && element != typeof(object)) return items;

            for (int i = 0; i < 2; i++)
            {
                object? item = AotCandidateValues(element, seed + "[" + i + "]", depth + 1).FirstOrDefault();
                if (item != null) items.Add(item);
            }

            return items;
        }

        private static Guid AotGuid(string seed)
        {
            byte[] bytes = new byte[16];
            uint hash = AotHash(seed);
            for (int i = 0; i < 16; i++)
            {
                hash = unchecked(hash * 16777619u) ^ (uint)i;
                bytes[i] = (byte)(hash >> 8);
            }

            return new Guid(bytes);
        }

        private static uint AotHash(string value)
        {
            uint hash = 2166136261u;
            foreach (byte b in Encoding.UTF8.GetBytes(value))
            {
                hash = unchecked((hash ^ b) * 16777619u);
            }

            return hash;
        }

        private static Task TestAotContextCoverage(CancellationToken token)
        {
            List<string> missing = new List<string>();
            foreach (Type type in _AotParityTypes)
            {
                if (LiteGraphJsonContext.Default.GetTypeInfo(type) == null) missing.Add(AotTypeKey(type));
            }

            AssertTrue(missing.Count == 0, "LiteGraphJsonContext is missing: " + String.Join(", ", missing));
            return Task.CompletedTask;
        }

        private static Task TestAotJitCompatibility(CancellationToken token)
        {
            AssertTrue(JsonSerializer.IsReflectionEnabledByDefault, "Test.Automated runs with reflection-based serialization enabled");

            Serializer serializer = new Serializer();

            // Captured from the reflection-based ExceptionConverter before the AOT work.
            AssertEqual(
                "{\"Message\":\"plain\",\"Data\":{},\"HResult\":-2146233088}",
                serializer.SerializeJson(new Exception("plain"), false),
                "Plain exception JSON");
            AssertEqual(
                "{\"Error\":{\"Message\":\"wrapped\",\"Data\":{},\"HResult\":-2146233088}}",
                serializer.SerializeJson(new { Error = new Exception("wrapped") }, false),
                "Exception inside an anonymous object");

            string argumentJson = serializer.SerializeJson(new ArgumentException("outer message", "paramX", new InvalidOperationException("inner")), false);
            AssertEqual(
                "{\"Message\":\"outer message (Parameter \\u0027paramX\\u0027)\",\"ParamName\":\"paramX\",\"Data\":{},\"InnerException\":{\"Message\":\"inner\",\"Data\":{},\"HResult\":-2146233079},\"HResult\":-2147024809}",
                argumentJson,
                "Argument exception JSON");

            Node node = new Node { Name = "anonymous", Data = new { Level = SeverityEnum.Warn, Values = new[] { 1, 2 } } };
            string nodeJson = serializer.SerializeJson(node, false);
            AssertTrue(nodeJson.Contains("\"Data\":{\"Level\":\"Warn\",\"Values\":[1,2]}"), "Anonymous data with an enum serializes through reflection: " + nodeJson);

            TagMetadata copy = serializer.CopyObject(new TagMetadata { Key = "k", Value = "v" });
            AssertTrue(copy != null && copy.Key == "k", "CopyObject of a model type");
            return Task.CompletedTask;
        }

        private static Task TestAotDateTimeUtc(CancellationToken token)
        {
            Serializer serializer = new Serializer();
            DateTime expected = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc).AddTicks(1234560);

            Node zulu = serializer.DeserializeJson<Node>("{\"CreatedUtc\":\"2026-01-02T03:04:05.123456Z\"}");
            AssertEqual(expected.Ticks, zulu.CreatedUtc.Ticks, "Z timestamp keeps its value");
            AssertEqual(DateTimeKind.Utc, zulu.CreatedUtc.Kind, "Z timestamp is UTC");

            Node offset = serializer.DeserializeJson<Node>("{\"CreatedUtc\":\"2026-01-02T05:04:05.123456+02:00\"}");
            AssertEqual(expected.Ticks, offset.CreatedUtc.Ticks, "Offset timestamp is converted to UTC");

            Node unzoned = serializer.DeserializeJson<Node>("{\"CreatedUtc\":\"2026-01-02 03:04:05.123456\"}");
            AssertEqual(expected.Ticks, unzoned.CreatedUtc.Ticks, "Timestamp without a zone is taken as UTC");

            string json = serializer.SerializeJson(new Node { CreatedUtc = expected, LastUpdateUtc = expected.ToLocalTime() }, false);
            AssertTrue(json.Contains("\"CreatedUtc\":\"2026-01-02T03:04:05.123456Z\""), "UTC timestamp written unchanged: " + json);
            AssertTrue(json.Contains("\"LastUpdateUtc\":\"2026-01-02T03:04:05.123456Z\""), "Local timestamp written as UTC: " + json);

            Node roundTripped = serializer.DeserializeJson<Node>(serializer.SerializeJson(zulu, false));
            AssertEqual(expected.Ticks, roundTripped.CreatedUtc.Ticks, "Round trip keeps the value");
            return Task.CompletedTask;
        }

        private static async Task TestAotGexfParity(CancellationToken token)
        {
            string filename = Path.Combine(Path.GetTempPath(), "litegraph-aot-gexf-" + Guid.NewGuid().ToString("N") + ".db");
            try
            {
                using (LiteGraphClient client = new LiteGraphClient(new SqliteGraphRepository(filename, true)))
                {
                    client.InitializeRepository();

                    TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata { GUID = AotGuid("gexf-tenant"), Name = "Gexf" }, token).ConfigureAwait(false);
                    Graph graph = await client.Graph.Create(new Graph { TenantGUID = tenant.GUID, GUID = AotGuid("gexf-graph"), Name = "Gexf" }, token).ConfigureAwait(false);

                    // One label and one tag per object: SQLite returns several in no fixed order.
                    NameValueCollection tags = new NameValueCollection();
                    tags.Add("color", "red");

                    Node first = await client.Node.Create(new Node
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graph.GUID,
                        GUID = AotGuid("gexf-node-1"),
                        Name = "First <&> \"node\"",
                        Labels = new List<string> { "person" },
                        Tags = tags,
                        Data = JsonDocument.Parse(_AotSampleDataJson).RootElement.Clone(),
                        CreatedUtc = _AotSampleTimestamp,
                        LastUpdateUtc = _AotSampleTimestamp
                    }, token).ConfigureAwait(false);

                    Node second = await client.Node.Create(new Node
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graph.GUID,
                        GUID = AotGuid("gexf-node-2"),
                        Name = "Second",
                        CreatedUtc = _AotSampleTimestamp.AddSeconds(1),
                        LastUpdateUtc = _AotSampleTimestamp.AddSeconds(1)
                    }, token).ConfigureAwait(false);

                    Node third = await client.Node.Create(new Node
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graph.GUID,
                        GUID = AotGuid("gexf-node-3"),
                        CreatedUtc = _AotSampleTimestamp.AddSeconds(2),
                        LastUpdateUtc = _AotSampleTimestamp.AddSeconds(2)
                    }, token).ConfigureAwait(false);

                    await client.Edge.Create(new Edge
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graph.GUID,
                        GUID = AotGuid("gexf-edge-1"),
                        Name = "knows",
                        From = first.GUID,
                        To = second.GUID,
                        Cost = 7,
                        Labels = new List<string> { "relationship" },
                        Tags = tags,
                        Data = new Dictionary<string, object> { { "since", 2020L } },
                        CreatedUtc = _AotSampleTimestamp,
                        LastUpdateUtc = _AotSampleTimestamp
                    }, token).ConfigureAwait(false);

                    await client.Edge.Create(new Edge
                    {
                        TenantGUID = tenant.GUID,
                        GraphGUID = graph.GUID,
                        GUID = AotGuid("gexf-edge-2"),
                        From = second.GUID,
                        To = third.GUID,
                        CreatedUtc = _AotSampleTimestamp.AddSeconds(1),
                        LastUpdateUtc = _AotSampleTimestamp.AddSeconds(1)
                    }, token).ConfigureAwait(false);

                    GexfWriter writer = new GexfWriter();
                    string withData = await writer.RenderAsGexf(client, tenant.GUID, graph.GUID, true, true, token).ConfigureAwait(false);
                    string withoutData = await writer.RenderAsGexf(client, tenant.GUID, graph.GUID, false, false, token).ConfigureAwait(false);
                    string actual = MaskGexfTimestamp(withData) + "\n<!-- without data -->\n" + MaskGexfTimestamp(withoutData);

                    string? captureDirectory = Environment.GetEnvironmentVariable(_AotCaptureEnvironmentVariable);
                    if (!String.IsNullOrEmpty(captureDirectory))
                    {
                        Directory.CreateDirectory(captureDirectory);
                        await File.WriteAllTextAsync(Path.Combine(captureDirectory, _GexfBaselineFile), actual, token).ConfigureAwait(false);
                        return;
                    }

                    string baselinePath = Path.Combine(AppContext.BaseDirectory, "Baselines", _GexfBaselineFile);
                    AssertTrue(File.Exists(baselinePath), "GEXF baseline exists at " + baselinePath);
                    string expected = await File.ReadAllTextAsync(baselinePath, token).ConfigureAwait(false);
                    AssertTrue(
                        String.Equals(expected.Replace("\r\n", "\n"), actual.Replace("\r\n", "\n"), StringComparison.Ordinal),
                        "GEXF output matches baseline.\nExpected:\n" + expected + "\nActual:\n" + actual);
                }
            }
            finally
            {
                if (File.Exists(filename)) File.Delete(filename);
            }
        }

        private static string MaskGexfTimestamp(string xml)
        {
            return Regex.Replace(xml, "lastmodifieddate=\"[^\"]*\"", "lastmodifieddate=\"MASKED\"");
        }

        #endregion
    }
}
