namespace LiteGraph.Sdk
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Vector index type.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum VectorIndexTypeEnum
    {
        /// <summary>
        /// None.
        /// </summary>
        None,
        /// <summary>
        /// Hierarchical navigable small world, in RAM only.
        /// </summary>
        HnswRam,
        /// <summary>
        /// Hierarchical navigable small world, in a separate Sqlite database.
        /// </summary>
        HnswSqlite
    }
}