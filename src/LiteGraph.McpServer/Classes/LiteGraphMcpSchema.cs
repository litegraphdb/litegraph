namespace LiteGraph.McpServer.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;

    /// <summary>
    /// JSON schemas for MCP tool arguments.
    /// Tool schemas are written as JSON text and parsed once at registration. Voltaic stores every schema as a
    /// <see cref="JsonElement"/>, so a parsed schema reaches clients exactly as written and needs no reflection, which
    /// keeps the MCP server compatible with Native AOT (anonymous objects cannot be serialized there).
    /// Thread safety: all methods are stateless and thread-safe.
    /// </summary>
    public static class LiteGraphMcpSchema
    {
        #region Public-Members

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        /// <summary>
        /// Parse a JSON schema.
        /// </summary>
        /// <param name="json">Schema as JSON text; must be a JSON object.</param>
        /// <returns>Detached JSON element holding the schema.</returns>
        /// <exception cref="ArgumentNullException">Thrown when json is null.</exception>
        /// <exception cref="JsonException">Thrown when json is not valid JSON.</exception>
        /// <exception cref="ArgumentException">Thrown when json is not a JSON object.</exception>
        public static JsonElement Parse(string json)
        {
            ArgumentNullException.ThrowIfNull(json);

            using (JsonDocument document = JsonDocument.Parse(json))
            {
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                    throw new ArgumentException("A tool schema must be a JSON object.", nameof(json));

                return document.RootElement.Clone();
            }
        }

        /// <summary>
        /// Build a schema for one property with a type and a description, for schemas assembled in code.
        /// </summary>
        /// <param name="type">JSON schema type, such as string, integer, or boolean; must not be null.</param>
        /// <param name="description">Property description; must not be null.</param>
        /// <returns>Property schema with "type" and "description", in that order.</returns>
        /// <exception cref="ArgumentNullException">Thrown when type or description is null.</exception>
        public static Dictionary<string, object> Property(string type, string description)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(description);

            return new Dictionary<string, object>
            {
                { "type", type },
                { "description", description }
            };
        }

        /// <summary>
        /// Build an object schema from properties assembled in code.
        /// </summary>
        /// <param name="properties">Property schemas by name, in the order they are listed; must not be null.</param>
        /// <param name="required">Names of the required properties; must not be null.</param>
        /// <returns>Object schema with "type", "properties", and "required", in that order.</returns>
        /// <exception cref="ArgumentNullException">Thrown when properties or required is null.</exception>
        public static Dictionary<string, object> Object(Dictionary<string, object> properties, IEnumerable<string> required)
        {
            ArgumentNullException.ThrowIfNull(properties);
            ArgumentNullException.ThrowIfNull(required);

            return new Dictionary<string, object>
            {
                { "type", "object" },
                { "properties", properties },
                { "required", new List<string>(required).ToArray() }
            };
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
