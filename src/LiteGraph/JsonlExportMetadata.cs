namespace LiteGraph
{
    using System;

    /// <summary>
    /// Metadata rendered into the leading comment lines of a JSONL export.
    /// Comment lines begin with '#' and are ignored on import.
    /// </summary>
    public class JsonlExportMetadata
    {
        #region Public-Members

        /// <summary>
        /// JSONL format version string.  Default is "v1".
        /// </summary>
        public string FormatVersion { get; set; } = "v1";

        /// <summary>
        /// Kind of export, for example "subgraph" or "graph-backup".
        /// </summary>
        public string Kind { get; set; } = "graph-backup";

        /// <summary>
        /// Timestamp of the export, in UTC.
        /// </summary>
        public DateTime ExportedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Source tenant GUID.
        /// </summary>
        public Guid SourceTenantGUID { get; set; } = default(Guid);

        /// <summary>
        /// Source graph GUID.
        /// </summary>
        public Guid SourceGraphGUID { get; set; } = default(Guid);

        /// <summary>
        /// Source graph name, if known.
        /// </summary>
        public string SourceGraphName { get; set; } = null;

        /// <summary>
        /// Human-readable summary of the selection criteria, if this export was a filtered subgraph.
        /// </summary>
        public string SelectionSummary { get; set; } = null;

        /// <summary>
        /// Generator identifier, for example "LiteGraph 8.0.0".
        /// </summary>
        public string Generator { get; set; } = "LiteGraph";

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public JsonlExportMetadata()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
