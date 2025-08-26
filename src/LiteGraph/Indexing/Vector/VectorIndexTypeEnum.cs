namespace LiteGraph.Indexing.Vector
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

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
