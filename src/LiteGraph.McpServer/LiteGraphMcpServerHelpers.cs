namespace LiteGraph.McpServer
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;

    /// <summary>
    /// Helper methods for LiteGraph MCP Server.
    /// </summary>
    internal static class LiteGraphMcpServerHelpers
    {
        /// <summary>
        /// Converts an async enumerable to a list synchronously.
        /// </summary>
        public static List<T> ToListSync<T>(IAsyncEnumerable<T> enumerable)
        {
            List<T> list = new List<T>();
            var enumerator = enumerable.GetAsyncEnumerator();
            try
            {
                while (enumerator.MoveNextAsync().AsTask().GetAwaiter().GetResult())
                {
                    list.Add(enumerator.Current);
                }
            }
            finally
            {
                enumerator.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
            return list;
        }

        /// <summary>
        /// Gets a GUID from JSON element, returning null if not present.
        /// </summary>
        public static Guid? GetGuidOrNull(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out JsonElement prop))
            {
                string? guidStr = prop.GetString();
                if (!string.IsNullOrEmpty(guidStr) && Guid.TryParse(guidStr, out Guid guid))
                    return guid;
            }
            return null;
        }

        /// <summary>
        /// Gets a GUID from JSON element, throwing if not present.
        /// </summary>
        public static Guid GetGuidRequired(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement prop))
                throw new ArgumentException($"Required parameter '{propertyName}' is missing");
            
            string? guidStr = prop.GetString();
            if (string.IsNullOrEmpty(guidStr) || !Guid.TryParse(guidStr, out Guid guid))
                throw new ArgumentException($"Invalid GUID format for '{propertyName}'");
            
            return guid;
        }

        /// <summary>
        /// Gets a string from JSON element, returning null if not present.
        /// </summary>
        public static string? GetStringOrNull(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out JsonElement prop))
                return prop.GetString();
            return null;
        }

        /// <summary>
        /// Gets a boolean from JSON element, returning default if not present.
        /// </summary>
        public static bool GetBoolOrDefault(JsonElement element, string propertyName, bool defaultValue = false)
        {
            if (element.TryGetProperty(propertyName, out JsonElement prop))
                return prop.GetBoolean();
            return defaultValue;
        }

        /// <summary>
        /// Gets an integer from JSON element, returning default if not present.
        /// </summary>
        public static int GetIntOrDefault(JsonElement element, string propertyName, int defaultValue = 0)
        {
            if (element.TryGetProperty(propertyName, out JsonElement prop))
                return prop.GetInt32();
            return defaultValue;
        }

        /// <summary>
        /// Gets a list of strings from JSON element.
        /// </summary>
        public static List<string?>? GetStringListOrNull(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement prop))
                return null;
            
            List<string?> list = new List<string?>();
            foreach (JsonElement item in prop.EnumerateArray())
            {
                list.Add(item.GetString());
            }
            return list.Count > 0 ? list : null;
        }

        /// <summary>
        /// Gets a list of GUIDs from JSON element.
        /// </summary>
        public static List<Guid>? GetGuidListOrNull(JsonElement element, string propertyName)
        {
            if (!element.TryGetProperty(propertyName, out JsonElement prop))
                return null;
            
            List<Guid> list = new List<Guid>();
            foreach (JsonElement item in prop.EnumerateArray())
            {
                string? guidStr = item.GetString();
                if (guidStr != null && Guid.TryParse(guidStr, out Guid guid))
                    list.Add(guid);
            }
            return list.Count > 0 ? list : null;
        }
    }
}

