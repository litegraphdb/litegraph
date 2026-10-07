namespace LiteGraphConsole
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;
    using LiteGraph.GraphRepositories;
    using LiteGraph.Serialization;

    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            try
            {
                ConsoleOptions options = ConsoleOptions.Parse(args);
                if (options.ShowHelp)
                {
                    WriteUsage();
                    return 0;
                }

                using (QueryConsole console = new QueryConsole(options))
                {
                    if (!String.IsNullOrEmpty(options.Execute))
                    {
                        await console.ExecuteAndPrint(options.Execute, CancellationToken.None).ConfigureAwait(false);
                        return 0;
                    }

                    if (!String.IsNullOrEmpty(options.ScriptFile))
                    {
                        string script = await File.ReadAllTextAsync(options.ScriptFile, CancellationToken.None).ConfigureAwait(false);
                        foreach (string statement in SplitStatements(script))
                        {
                            if (!String.IsNullOrWhiteSpace(statement))
                                await console.ExecuteAndPrint(statement, CancellationToken.None).ConfigureAwait(false);
                        }

                        return 0;
                    }

                    await console.RunInteractive(CancellationToken.None).ConfigureAwait(false);
                    return 0;
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("error: " + e.Message);
                return 1;
            }
        }

        internal static IEnumerable<string> SplitStatements(string script)
        {
            if (String.IsNullOrWhiteSpace(script)) yield break;

            StringBuilder current = new StringBuilder();
            bool singleQuoted = false;
            bool doubleQuoted = false;
            bool escaped = false;

            foreach (char c in script)
            {
                current.Append(c);

                if (escaped)
                {
                    escaped = false;
                    continue;
                }

                if (c == '\\')
                {
                    escaped = true;
                    continue;
                }

                if (c == '\'' && !doubleQuoted)
                {
                    singleQuoted = !singleQuoted;
                    continue;
                }

                if (c == '"' && !singleQuoted)
                {
                    doubleQuoted = !doubleQuoted;
                    continue;
                }

                if (c == ';' && !singleQuoted && !doubleQuoted)
                {
                    yield return current.ToString();
                    current.Clear();
                }
            }

            if (current.Length > 0) yield return current.ToString();
        }

        private static void WriteUsage()
        {
            Console.WriteLine("LiteGraphConsole");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("  lg --database litegraph.db --tenant <guid> --graph <guid>");
            Console.WriteLine("  lg --endpoint http://localhost:8000 --tenant <guid> --graph <guid> --token <bearer>");
            Console.WriteLine("  lg --database litegraph.db --tenant <guid> --graph <guid> --execute \"MATCH (n) RETURN n LIMIT 5\"");
            Console.WriteLine("  lg --database litegraph.db --tenant <guid> --graph <guid> --script queries.lg");
            Console.WriteLine();
            Console.WriteLine("Arguments:");
            Console.WriteLine("  --database, --file, -d <path>       SQLite database file");
            Console.WriteLine("  --endpoint <url>                    LiteGraph REST endpoint");
            Console.WriteLine("  --tenant <guid>                     Tenant GUID");
            Console.WriteLine("  --graph <guid>                      Graph GUID");
            Console.WriteLine("  --token <token>                     Bearer token for endpoint mode");
            Console.WriteLine("  --security-token <token>            x-token value for endpoint mode");
            Console.WriteLine("  --execute, -e <query>               Execute one query and exit");
            Console.WriteLine("  --script, -s <path>                 Execute semicolon-delimited queries from a file");
            Console.WriteLine("  --param <name=json>                 Add a query parameter");
            Console.WriteLine("  --max-results <count>               Request MaxResults value, default 100");
            Console.WriteLine("  --timeout <seconds>                 Request TimeoutSeconds value, default 30");
            Console.WriteLine("  --compact                           Emit compact JSON");
            Console.WriteLine("  --help, -h                          Show help");
            Console.WriteLine();
            Console.WriteLine("Interactive commands:");
            Console.WriteLine("  .help                               Show shell commands");
            Console.WriteLine("  .show                               Show current connection settings");
            Console.WriteLine("  .tenant <guid>                      Set tenant GUID");
            Console.WriteLine("  .graph <guid>                       Set graph GUID");
            Console.WriteLine("  .database <path>                    Switch to local SQLite database mode");
            Console.WriteLine("  .endpoint <url>                     Switch to REST endpoint mode");
            Console.WriteLine("  .token <token>                      Set bearer token");
            Console.WriteLine("  .param set <name> <json>            Set a query parameter");
            Console.WriteLine("  .param unset <name>                 Remove a query parameter");
            Console.WriteLine("  .param clear                        Clear query parameters");
            Console.WriteLine("  .read <path>                        Execute queries from a file");
            Console.WriteLine("  .mode pretty|compact                Set JSON output mode");
            Console.WriteLine("  .quit, .exit                        Exit");
        }
    }

    internal sealed class QueryConsole : IDisposable
    {
        private readonly ConsoleOptions _Options;
        private readonly Serializer _Serializer = new Serializer();
        private LiteGraphClient _Client;
        private HttpClient _HttpClient;
        private bool _Disposed;

        internal QueryConsole(ConsoleOptions options)
        {
            _Options = options ?? throw new ArgumentNullException(nameof(options));
            _Options.ValidateConnection();
        }

        internal async Task RunInteractive(CancellationToken token)
        {
            Console.WriteLine("LiteGraphConsole. Enter .help for commands. End queries with ';'.");
            WriteConnectionSummary();

            StringBuilder buffer = new StringBuilder();
            while (true)
            {
                token.ThrowIfCancellationRequested();
                Console.Write(buffer.Length == 0 ? "lg> " : "...> ");
                string line = Console.ReadLine();
                if (line == null) break;

                if (buffer.Length == 0 && line.TrimStart().StartsWith(".", StringComparison.Ordinal))
                {
                    bool shouldContinue = await HandleCommand(line.Trim(), token).ConfigureAwait(false);
                    if (!shouldContinue) break;
                    continue;
                }

                buffer.AppendLine(line);
                if (!EndsStatement(line)) continue;

                string query = buffer.ToString();
                buffer.Clear();
                await ExecuteAndPrint(query, token).ConfigureAwait(false);
            }
        }

        internal async Task ExecuteAndPrint(string query, CancellationToken token)
        {
            if (String.IsNullOrWhiteSpace(query)) return;

            string json = await Execute(query.Trim(), token).ConfigureAwait(false);
            if (_Options.PrettyJson) Console.WriteLine(PrettyJson(json));
            else Console.WriteLine(CompactJson(json));
        }

        public void Dispose()
        {
            if (_Disposed) return;
            _Disposed = true;
            _Client?.Dispose();
            _HttpClient?.Dispose();
        }

        private async Task<bool> HandleCommand(string command, CancellationToken token)
        {
            string[] parts = SplitCommand(command);
            if (parts.Length == 0) return true;

            string verb = parts[0].ToLowerInvariant();
            switch (verb)
            {
                case ".help":
                    WriteShellHelp();
                    return true;

                case ".quit":
                case ".exit":
                    return false;

                case ".show":
                    WriteConnectionSummary();
                    WriteParameterSummary();
                    return true;

                case ".tenant":
                    RequireParts(parts, 2, ".tenant <guid>");
                    _Options.TenantGuid = Guid.Parse(parts[1]);
                    return true;

                case ".graph":
                    RequireParts(parts, 2, ".graph <guid>");
                    _Options.GraphGuid = Guid.Parse(parts[1]);
                    return true;

                case ".database":
                    RequireParts(parts, 2, ".database <path>");
                    _Options.DatabaseFile = parts[1];
                    _Options.Endpoint = null;
                    ResetLocalClient();
                    return true;

                case ".endpoint":
                    RequireParts(parts, 2, ".endpoint <url>");
                    _Options.Endpoint = parts[1];
                    _Options.DatabaseFile = null;
                    ResetLocalClient();
                    return true;

                case ".token":
                    RequireParts(parts, 2, ".token <token>");
                    _Options.BearerToken = parts[1];
                    ResetHttpClient();
                    return true;

                case ".maxresults":
                    RequireParts(parts, 2, ".maxresults <count>");
                    _Options.MaxResults = Int32.Parse(parts[1], CultureInfo.InvariantCulture);
                    return true;

                case ".timeout":
                    RequireParts(parts, 2, ".timeout <seconds>");
                    _Options.TimeoutSeconds = Int32.Parse(parts[1], CultureInfo.InvariantCulture);
                    return true;

                case ".mode":
                    RequireParts(parts, 2, ".mode pretty|compact");
                    _Options.PrettyJson = parts[1].Equals("pretty", StringComparison.OrdinalIgnoreCase);
                    if (!parts[1].Equals("pretty", StringComparison.OrdinalIgnoreCase)
                        && !parts[1].Equals("compact", StringComparison.OrdinalIgnoreCase))
                        throw new ArgumentException(".mode expects pretty or compact.");
                    return true;

                case ".param":
                    HandleParamCommand(parts);
                    return true;

                case ".read":
                    RequireParts(parts, 2, ".read <path>");
                    string script = await File.ReadAllTextAsync(parts[1], token).ConfigureAwait(false);
                    foreach (string statement in Program.SplitStatements(script))
                    {
                        if (!String.IsNullOrWhiteSpace(statement))
                            await ExecuteAndPrint(statement, token).ConfigureAwait(false);
                    }
                    return true;

                default:
                    Console.WriteLine("Unknown command. Enter .help for commands.");
                    return true;
            }
        }

        private async Task<string> Execute(string query, CancellationToken token)
        {
            _Options.ValidateExecution();
            GraphQueryRequest request = new GraphQueryRequest
            {
                Query = query,
                MaxResults = _Options.MaxResults,
                TimeoutSeconds = _Options.TimeoutSeconds,
                Parameters = new Dictionary<string, object>(_Options.Parameters, StringComparer.OrdinalIgnoreCase)
            };

            if (!String.IsNullOrEmpty(_Options.Endpoint))
                return await ExecuteRemote(request, token).ConfigureAwait(false);

            LiteGraphClient client = GetLocalClient();
            GraphQueryResult result = await client.Query.Execute(_Options.TenantGuid.Value, _Options.GraphGuid.Value, request, token).ConfigureAwait(false);
            return _Serializer.SerializeJson(result, _Options.PrettyJson);
        }

        private async Task<string> ExecuteRemote(GraphQueryRequest request, CancellationToken token)
        {
            HttpClient http = GetHttpClient();
            string url = _Options.Endpoint.TrimEnd('/')
                + "/v1.0/tenants/"
                + Uri.EscapeDataString(_Options.TenantGuid.Value.ToString())
                + "/graphs/"
                + Uri.EscapeDataString(_Options.GraphGuid.Value.ToString())
                + "/query";

            using (StringContent content = new StringContent(_Serializer.SerializeJson(request, false), Encoding.UTF8, "application/json"))
            using (HttpResponseMessage response = await http.PostAsync(url, content, token).ConfigureAwait(false))
            {
                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException("Endpoint returned " + (int)response.StatusCode + " " + response.ReasonPhrase + ": " + body);

                return body;
            }
        }

        private LiteGraphClient GetLocalClient()
        {
            if (_Client != null) return _Client;
            GraphRepositoryBase repo = GraphRepositoryFactory.Create(new DatabaseSettings
            {
                Filename = _Options.DatabaseFile
            });
            repo.InitializeRepository();
            _Client = new LiteGraphClient(repo);
            return _Client;
        }

        private HttpClient GetHttpClient()
        {
            if (_HttpClient != null) return _HttpClient;

            _HttpClient = new HttpClient();
            if (!String.IsNullOrEmpty(_Options.BearerToken))
                _HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _Options.BearerToken);
            if (!String.IsNullOrEmpty(_Options.SecurityToken))
                _HttpClient.DefaultRequestHeaders.Add("x-token", _Options.SecurityToken);

            return _HttpClient;
        }

        private void ResetLocalClient()
        {
            _Client?.Dispose();
            _Client = null;
        }

        private void ResetHttpClient()
        {
            _HttpClient?.Dispose();
            _HttpClient = null;
        }

        private void HandleParamCommand(string[] parts)
        {
            if (parts.Length == 1 || (parts.Length == 2 && parts[1].Equals("list", StringComparison.OrdinalIgnoreCase)))
            {
                WriteParameterSummary();
                return;
            }

            if (parts[1].Equals("clear", StringComparison.OrdinalIgnoreCase))
            {
                _Options.Parameters.Clear();
                return;
            }

            if (parts[1].Equals("unset", StringComparison.OrdinalIgnoreCase))
            {
                RequireParts(parts, 3, ".param unset <name>");
                _Options.Parameters.Remove(parts[2]);
                return;
            }

            if (parts[1].Equals("set", StringComparison.OrdinalIgnoreCase))
            {
                RequireParts(parts, 4, ".param set <name> <json>");
                _Options.Parameters[parts[2]] = ParseJsonValue(String.Join(" ", parts.Skip(3)));
                return;
            }

            throw new ArgumentException("Unsupported .param command.");
        }

        private void WriteConnectionSummary()
        {
            string mode = !String.IsNullOrEmpty(_Options.Endpoint) ? "endpoint " + _Options.Endpoint : "database " + _Options.DatabaseFile;
            Console.WriteLine("Connected to " + mode);
            Console.WriteLine("Tenant: " + (_Options.TenantGuid != null ? _Options.TenantGuid.Value.ToString() : "(not set)"));
            Console.WriteLine("Graph:  " + (_Options.GraphGuid != null ? _Options.GraphGuid.Value.ToString() : "(not set)"));
        }

        private void WriteParameterSummary()
        {
            if (_Options.Parameters.Count < 1)
            {
                Console.WriteLine("Parameters: (none)");
                return;
            }

            Console.WriteLine("Parameters:");
            foreach (KeyValuePair<string, object> kvp in _Options.Parameters.OrderBy(k => k.Key, StringComparer.OrdinalIgnoreCase))
            {
                Console.WriteLine("  " + kvp.Key + " = " + _Serializer.SerializeJson(kvp.Value, false));
            }
        }

        private static void WriteShellHelp()
        {
            Console.WriteLine(".help                         Show commands");
            Console.WriteLine(".show                         Show connection and parameters");
            Console.WriteLine(".tenant <guid>                Set tenant GUID");
            Console.WriteLine(".graph <guid>                 Set graph GUID");
            Console.WriteLine(".database <path>              Use a local SQLite database");
            Console.WriteLine(".endpoint <url>               Use a LiteGraph REST endpoint");
            Console.WriteLine(".token <token>                Set endpoint bearer token");
            Console.WriteLine(".maxresults <count>           Set MaxResults");
            Console.WriteLine(".timeout <seconds>            Set TimeoutSeconds");
            Console.WriteLine(".param list                   List parameters");
            Console.WriteLine(".param set <name> <json>      Set parameter");
            Console.WriteLine(".param unset <name>           Remove parameter");
            Console.WriteLine(".param clear                  Clear parameters");
            Console.WriteLine(".read <path>                  Execute semicolon-delimited query file");
            Console.WriteLine(".mode pretty|compact          Set JSON output mode");
            Console.WriteLine(".quit, .exit                  Exit");
        }

        private static bool EndsStatement(string line)
        {
            if (line == null) return false;
            return line.TrimEnd().EndsWith(";", StringComparison.Ordinal);
        }

        // Indented JsonElement metadata (no reflection, so the console also runs under Native AOT).
        private static readonly System.Text.Json.Serialization.Metadata.JsonTypeInfo<JsonElement> _IndentedElement =
            (System.Text.Json.Serialization.Metadata.JsonTypeInfo<JsonElement>)new JsonSerializerOptions
            {
                WriteIndented = true,
                TypeInfoResolver = LiteGraphJsonContext.Default
            }.GetTypeInfo(typeof(JsonElement));

        private static string PrettyJson(string json)
        {
            if (String.IsNullOrWhiteSpace(json)) return json;
            try
            {
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    return JsonSerializer.Serialize(document.RootElement, _IndentedElement);
                }
            }
            catch
            {
                return json;
            }
        }

        private static string CompactJson(string json)
        {
            if (String.IsNullOrWhiteSpace(json)) return json;
            try
            {
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    return JsonSerializer.Serialize(document.RootElement, LiteGraphJsonContext.Default.JsonElement);
                }
            }
            catch
            {
                return json;
            }
        }

        private static string[] SplitCommand(string command)
        {
            if (String.IsNullOrWhiteSpace(command)) return Array.Empty<string>();

            List<string> ret = new List<string>();
            StringBuilder current = new StringBuilder();
            bool quoted = false;
            char quote = '\0';

            foreach (char c in command)
            {
                if (quoted)
                {
                    if (c == quote)
                    {
                        quoted = false;
                    }
                    else
                    {
                        current.Append(c);
                    }

                    continue;
                }

                if (c == '\'' || c == '"')
                {
                    quoted = true;
                    quote = c;
                    continue;
                }

                if (Char.IsWhiteSpace(c))
                {
                    if (current.Length > 0)
                    {
                        ret.Add(current.ToString());
                        current.Clear();
                    }
                    continue;
                }

                current.Append(c);
            }

            if (current.Length > 0) ret.Add(current.ToString());
            return ret.ToArray();
        }

        private static void RequireParts(string[] parts, int count, string usage)
        {
            if (parts.Length < count) throw new ArgumentException("Usage: " + usage);
        }

        private static object ParseJsonValue(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return null;
            try
            {
                return JsonSerializer.Deserialize(value, LiteGraphJsonContext.Default.Object);
            }
            catch
            {
                return value;
            }
        }
    }

    internal sealed class ConsoleOptions
    {
        internal string DatabaseFile { get; set; }
        internal string Endpoint { get; set; }
        internal Guid? TenantGuid { get; set; }
        internal Guid? GraphGuid { get; set; }
        internal string BearerToken { get; set; }
        internal string SecurityToken { get; set; }
        internal string Execute { get; set; }
        internal string ScriptFile { get; set; }
        internal int MaxResults { get; set; } = 100;
        internal int TimeoutSeconds { get; set; } = 30;
        internal bool PrettyJson { get; set; } = true;
        internal bool ShowHelp { get; set; }
        internal Dictionary<string, object> Parameters { get; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        internal static ConsoleOptions Parse(string[] args)
        {
            ConsoleOptions options = new ConsoleOptions();

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                switch (arg)
                {
                    case "--help":
                    case "-h":
                        options.ShowHelp = true;
                        break;

                    case "--database":
                    case "--file":
                    case "-d":
                        options.DatabaseFile = RequireValue(args, ref i, arg);
                        break;

                    case "--endpoint":
                        options.Endpoint = RequireValue(args, ref i, arg);
                        break;

                    case "--tenant":
                        options.TenantGuid = Guid.Parse(RequireValue(args, ref i, arg));
                        break;

                    case "--graph":
                        options.GraphGuid = Guid.Parse(RequireValue(args, ref i, arg));
                        break;

                    case "--token":
                        options.BearerToken = RequireValue(args, ref i, arg);
                        break;

                    case "--security-token":
                        options.SecurityToken = RequireValue(args, ref i, arg);
                        break;

                    case "--execute":
                    case "-e":
                        options.Execute = RequireValue(args, ref i, arg);
                        break;

                    case "--script":
                    case "-s":
                        options.ScriptFile = RequireValue(args, ref i, arg);
                        break;

                    case "--max-results":
                        options.MaxResults = Int32.Parse(RequireValue(args, ref i, arg), CultureInfo.InvariantCulture);
                        break;

                    case "--timeout":
                        options.TimeoutSeconds = Int32.Parse(RequireValue(args, ref i, arg), CultureInfo.InvariantCulture);
                        break;

                    case "--compact":
                        options.PrettyJson = false;
                        break;

                    case "--param":
                        string parameter = RequireValue(args, ref i, arg);
                        int split = parameter.IndexOf('=');
                        if (split < 1) throw new ArgumentException("--param expects name=json.");
                        options.Parameters[parameter.Substring(0, split)] = ParseJsonValue(parameter.Substring(split + 1));
                        break;

                    default:
                        throw new ArgumentException("Unknown argument '" + arg + "'. Use --help.");
                }
            }

            if (String.IsNullOrEmpty(options.DatabaseFile) && String.IsNullOrEmpty(options.Endpoint))
                options.DatabaseFile = "litegraph.db";

            return options;
        }

        internal void ValidateConnection()
        {
            if (!String.IsNullOrEmpty(DatabaseFile) && !String.IsNullOrEmpty(Endpoint))
                throw new ArgumentException("Specify either --database/--file or --endpoint, not both.");

            if (MaxResults < 1 || MaxResults > 10000) throw new ArgumentOutOfRangeException(nameof(MaxResults));
            if (TimeoutSeconds < 1 || TimeoutSeconds > 3600) throw new ArgumentOutOfRangeException(nameof(TimeoutSeconds));
        }

        internal void ValidateExecution()
        {
            ValidateConnection();
            if (TenantGuid == null) throw new ArgumentException("Tenant GUID is required. Use --tenant or .tenant.");
            if (GraphGuid == null) throw new ArgumentException("Graph GUID is required. Use --graph or .graph.");
        }

        private static string RequireValue(string[] args, ref int index, string name)
        {
            if (index + 1 >= args.Length) throw new ArgumentException(name + " requires a value.");
            index++;
            return args[index];
        }

        private static object ParseJsonValue(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return null;
            try
            {
                return JsonSerializer.Deserialize(value, LiteGraphJsonContext.Default.Object);
            }
            catch
            {
                return value;
            }
        }
    }
}
