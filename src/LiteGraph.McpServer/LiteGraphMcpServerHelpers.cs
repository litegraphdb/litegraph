namespace LiteGraph.McpServer
{
    using System;
    using System.Text.Json;
    using LiteGraph.Sdk;

    /// <summary>
    /// Helper methods for LiteGraph MCP Server.
    /// </summary>
    internal static class LiteGraphMcpServerHelpers
    {
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
        /// Gets an EnumerationOrderEnum from JSON element, returning default if not present or invalid.
        /// </summary>
        public static EnumerationOrderEnum GetEnumerationOrderOrDefault(JsonElement element, string propertyName, EnumerationOrderEnum defaultValue = EnumerationOrderEnum.CreatedDescending)
        {
            if (element.TryGetProperty(propertyName, out JsonElement prop))
            {
                string? orderStr = prop.GetString();
                if (!string.IsNullOrEmpty(orderStr) && Enum.TryParse<EnumerationOrderEnum>(orderStr, out EnumerationOrderEnum parsedOrder))
                    return parsedOrder;
            }
            return defaultValue;
        }

        /// <summary>
        /// Gets enumeration order and skip parameters from JSON element.
        /// </summary>
        public static (EnumerationOrderEnum order, int skip) GetEnumerationParams(JsonElement element, EnumerationOrderEnum defaultOrder = EnumerationOrderEnum.CreatedDescending, int defaultSkip = 0)
        {
            EnumerationOrderEnum order = GetEnumerationOrderOrDefault(element, "order", defaultOrder);
            int skip = GetIntOrDefault(element, "skip", defaultSkip);
            return (order, skip);
        }
    }
}

