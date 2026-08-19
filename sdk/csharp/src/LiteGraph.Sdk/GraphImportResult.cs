namespace LiteGraph.Sdk
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Outcome of a JSONL import operation.
    /// </summary>
    public class GraphImportResult
    {
        #region Public-Members

        /// <summary>
        /// True if the import completed successfully.
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// Tenant GUID into which records were imported.
        /// </summary>
        public Guid TenantGUID { get; set; } = default(Guid);

        /// <summary>
        /// GUID of the target graph, or the newly created graph.
        /// </summary>
        public Guid GraphGUID { get; set; } = default(Guid);

        /// <summary>
        /// Number of graphs created.
        /// </summary>
        public int GraphsCreated { get; set; } = 0;

        /// <summary>
        /// Number of nodes created.
        /// </summary>
        public int NodesCreated { get; set; } = 0;

        /// <summary>
        /// Number of nodes updated.
        /// </summary>
        public int NodesUpdated { get; set; } = 0;

        /// <summary>
        /// Number of nodes skipped.
        /// </summary>
        public int NodesSkipped { get; set; } = 0;

        /// <summary>
        /// Number of edges created.
        /// </summary>
        public int EdgesCreated { get; set; } = 0;

        /// <summary>
        /// Number of edges updated.
        /// </summary>
        public int EdgesUpdated { get; set; } = 0;

        /// <summary>
        /// Number of edges skipped.
        /// </summary>
        public int EdgesSkipped { get; set; } = 0;

        /// <summary>
        /// Total number of record lines read from the input.
        /// </summary>
        public int LinesRead { get; set; } = 0;

        /// <summary>
        /// Number of comment or blank lines ignored.
        /// </summary>
        public int LinesIgnored { get; set; } = 0;

        /// <summary>
        /// Warnings raised during the import, including dropped dangling edges and skipped malformed lines.
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// Mapping of original GUIDs to newly assigned GUIDs.  Populated only when the GUID strategy is Regenerate.
        /// </summary>
        public Dictionary<Guid, Guid> GuidMap { get; set; } = new Dictionary<Guid, Guid>();

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public GraphImportResult()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
