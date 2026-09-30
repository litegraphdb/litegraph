namespace LiteGraph.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Encodings.Web;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Serialization;
    using LiteGraph.Server.Classes;

    /// <summary>
    /// Reads and writes the settings file on behalf of the settings API.
    /// At startup the running settings are compared with the file to find every value that did not come from the file:
    /// environment variable overrides (node identity, secrets, database connection, ports) and values the server derives at
    /// startup.  Reads return the file, so every node sharing the file returns the same settings; writes keep the file's
    /// own value for each of those paths, so environment-supplied secrets and one node's identity are never written to the
    /// shared file.  CreatedUtc always keeps its file value.
    /// Thread safety: safe for concurrent use; writes are serialized within the process.
    /// </summary>
    public class SettingsFileService
    {
        #region Public-Members

        /// <summary>
        /// Settings file path.
        /// </summary>
        public string Filename { get; }

        /// <summary>
        /// Dotted paths of the settings whose running value did not come from the file, for example "Encryption.Key".
        /// </summary>
        public IReadOnlyList<string> OverriddenPaths
        {
            get
            {
                return _Overridden.Select(p => String.Join(".", p)).ToList();
            }
        }

        #endregion

        #region Private-Members

        private static readonly JsonSerializerOptions _Compact = new JsonSerializerOptions { WriteIndented = false };
        private static readonly JsonSerializerOptions _Indented = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
        private static readonly List<string[]> _LivePaths = new List<string[]> { new[] { "RequestTimeoutSeconds" } };
        private static readonly List<string[]> _FileOwnedPaths = new List<string[]> { new[] { "CreatedUtc" } };

        private readonly Serializer _Serializer;
        private readonly List<string[]> _Overridden;
        private readonly string _StartupJson;
        private readonly SemaphoreSlim _WriteLock = new SemaphoreSlim(1, 1);

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate, comparing the running settings with the settings file as it is now.
        /// </summary>
        /// <param name="filename">Settings file path.</param>
        /// <param name="serializer">Serializer.</param>
        /// <param name="running">Settings the server is running with, after environment overrides.</param>
        /// <exception cref="ArgumentNullException">A required argument is null.</exception>
        /// <exception cref="IOException">The settings file could not be read.</exception>
        public SettingsFileService(string filename, Serializer serializer, Settings running)
        {
            if (String.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));
            if (running == null) throw new ArgumentNullException(nameof(running));
            _Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            Filename = filename;

            _StartupJson = ReadNormalizedJson();
            _Overridden = Diff(JsonNode.Parse(_StartupJson), JsonNode.Parse(_Serializer.SerializeJson(running, false)));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Read the settings file.
        /// </summary>
        /// <returns>Settings as stored in the file.</returns>
        /// <exception cref="IOException">The settings file could not be read.</exception>
        public Settings Read()
        {
            return _Serializer.DeserializeJson<Settings>(File.ReadAllText(Filename));
        }

        /// <summary>
        /// Write settings to the file, keeping the file's own value for every overridden path.
        /// The file is rewritten in place so that a bind-mounted file stays shared between containers.
        /// </summary>
        /// <param name="incoming">Settings to write.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Settings as written.</returns>
        /// <exception cref="ArgumentNullException">incoming is null.</exception>
        /// <exception cref="IOException">The settings file could not be read or written.</exception>
        public async Task<Settings> WriteAsync(Settings incoming, CancellationToken token = default)
        {
            if (incoming == null) throw new ArgumentNullException(nameof(incoming));

            await _WriteLock.WaitAsync(token).ConfigureAwait(false);
            try
            {
                // Values kept from the file are copied from its raw text rather than a deserialized copy, so they are
                // written back exactly as they were.
                JsonNode merged = JsonNode.Parse(_Serializer.SerializeJson(incoming, false));
                JsonNode file = JsonNode.Parse(File.ReadAllText(Filename));

                foreach (string[] path in _Overridden.Concat(_FileOwnedPaths))
                {
                    CopyValue(file, merged, path);
                }

                string json = merged.ToJsonString(_Indented);
                Settings result = _Serializer.DeserializeJson<Settings>(json);
                await File.WriteAllBytesAsync(Filename, Encoding.UTF8.GetBytes(json), token).ConfigureAwait(false);
                return result;
            }
            finally
            {
                _WriteLock.Release();
            }
        }

        /// <summary>
        /// Check whether a setting's running value did not come from the file.
        /// </summary>
        /// <param name="path">Property names from the root, for example "Encryption", "Key".</param>
        /// <returns>True if overridden.</returns>
        public bool IsOverridden(params string[] path)
        {
            if (path == null || path.Length == 0) return false;
            return _Overridden.Any(p => IsPrefix(p, path) || IsPrefix(path, p));
        }

        /// <summary>
        /// Check whether the file has changed since startup in a way that takes effect only after a restart.
        /// Changes to settings applied live (RequestTimeoutSeconds) are ignored.
        /// </summary>
        /// <returns>True if a restart is needed to apply the file.</returns>
        /// <exception cref="IOException">The settings file could not be read.</exception>
        public bool RestartNeeded()
        {
            return RestartRequiredChanges().Count > 0;
        }

        /// <summary>
        /// List the settings changed in the file since startup that take effect only after a restart.
        /// </summary>
        /// <returns>Dotted paths of the changed settings.</returns>
        /// <exception cref="IOException">The settings file could not be read.</exception>
        public List<string> RestartRequiredChanges()
        {
            List<string[]> changed = Diff(JsonNode.Parse(_StartupJson), JsonNode.Parse(ReadNormalizedJson()));
            return changed.Where(c => !_LivePaths.Any(l => IsPrefix(l, c))).Select(c => String.Join(".", c)).ToList();
        }

        /// <summary>
        /// List the paths at which two JSON documents differ.  Objects are compared property by property; any other value,
        /// including arrays, is compared as a whole.
        /// </summary>
        /// <param name="left">First document.</param>
        /// <param name="right">Second document.</param>
        /// <returns>Paths of differing values.</returns>
        public static List<string[]> Diff(JsonNode left, JsonNode right)
        {
            List<string[]> ret = new List<string[]>();
            DiffInto(left, right, new List<string>(), ret);
            return ret;
        }

        #endregion

        #region Private-Methods

        private string ReadNormalizedJson()
        {
            Settings settings = _Serializer.DeserializeJson<Settings>(File.ReadAllText(Filename));
            return _Serializer.SerializeJson(settings, false);
        }

        private static void DiffInto(JsonNode left, JsonNode right, List<string> path, List<string[]> diffs)
        {
            if (left is JsonObject leftObject && right is JsonObject rightObject)
            {
                IEnumerable<string> names = leftObject.Select(p => p.Key).Union(rightObject.Select(p => p.Key), StringComparer.Ordinal);
                foreach (string name in names)
                {
                    leftObject.TryGetPropertyValue(name, out JsonNode leftValue);
                    rightObject.TryGetPropertyValue(name, out JsonNode rightValue);
                    path.Add(name);
                    DiffInto(leftValue, rightValue, path, diffs);
                    path.RemoveAt(path.Count - 1);
                }
                return;
            }

            string leftText = left?.ToJsonString(_Compact);
            string rightText = right?.ToJsonString(_Compact);
            if (!String.Equals(leftText, rightText, StringComparison.Ordinal) && path.Count > 0) diffs.Add(path.ToArray());
        }

        private static void CopyValue(JsonNode from, JsonNode to, string[] path)
        {
            JsonNode source = from;
            JsonNode target = to;

            for (int i = 0; i < path.Length - 1; i++)
            {
                source = (source as JsonObject)?[path[i]];
                JsonObject targetObject = target as JsonObject;
                if (targetObject == null) return;
                if (!(targetObject[path[i]] is JsonObject))
                {
                    if (!(source is JsonObject)) return;
                    targetObject[path[i]] = new JsonObject();
                }
                target = targetObject[path[i]];
            }

            string last = path[path.Length - 1];
            JsonObject destination = target as JsonObject;
            if (destination == null) return;

            JsonObject sourceObject = source as JsonObject;
            if (sourceObject != null && sourceObject.TryGetPropertyValue(last, out JsonNode value))
                destination[last] = value?.DeepClone();
            else
                destination.Remove(last);
        }

        private static bool IsPrefix(string[] prefix, string[] path)
        {
            if (prefix.Length > path.Length) return false;
            for (int i = 0; i < prefix.Length; i++)
            {
                if (!String.Equals(prefix[i], path[i], StringComparison.Ordinal)) return false;
            }
            return true;
        }

        #endregion
    }
}
