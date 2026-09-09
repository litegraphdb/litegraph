namespace LiteGraph.Algorithms
{
    using System;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Level of node/edge attribute detail included in a graph projection export.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum GraphExportAttributeLevelEnum
    {
        /// <summary>
        /// Identifiers and structure only (node GUID, edge endpoints, weight).
        /// </summary>
        None,
        /// <summary>
        /// Structure plus name, labels, and tags; excludes the JSON data payload.
        /// </summary>
        Meta,
        /// <summary>
        /// All attributes including the JSON data payload.
        /// </summary>
        Full
    }
}
