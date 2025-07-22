namespace LiteGraph.Indexing.Vector
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Vector index type.
    /// </summary>
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
        HnwsSqlite
    }
}
