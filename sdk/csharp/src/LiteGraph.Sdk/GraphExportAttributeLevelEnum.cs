namespace LiteGraph.Sdk
{
    /// <summary>
    /// Level of node/edge attribute detail included in a graph projection export.
    /// </summary>
    public enum GraphExportAttributeLevelEnum
    {
        /// <summary>
        /// Identifiers and structure only.
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
