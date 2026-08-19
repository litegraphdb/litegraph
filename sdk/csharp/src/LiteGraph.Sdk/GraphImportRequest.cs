namespace LiteGraph.Sdk
{
    using System;

    /// <summary>
    /// Options that accompany a JSONL import body and govern how records are applied.
    /// </summary>
    public class GraphImportRequest
    {
        #region Public-Members

        /// <summary>
        /// Import mode.  Default is CreateNew.
        /// </summary>
        public GraphImportModeEnum Mode { get; set; } = GraphImportModeEnum.CreateNew;

        /// <summary>
        /// GUID collision strategy.  Default is Preserve, which suits creating a new graph.
        /// For merging into an existing graph, Regenerate is recommended.
        /// </summary>
        public GraphImportGuidStrategyEnum GuidStrategy { get; set; } = GraphImportGuidStrategyEnum.Preserve;

        /// <summary>
        /// Policy applied when a malformed or unrecognized line is encountered.  Default is Abort.
        /// </summary>
        public GraphImportErrorPolicyEnum OnError { get; set; } = GraphImportErrorPolicyEnum.Abort;

        /// <summary>
        /// Target graph GUID.  Required when Mode is MergeIntoExisting; ignored when Mode is CreateNew.
        /// </summary>
        public Guid? TargetGraphGUID { get; set; } = null;

        /// <summary>
        /// Graph metadata to use when creating a new graph.  When null, the file's graph record is used,
        /// falling back to a minimal default.  Ignored when Mode is MergeIntoExisting.
        /// </summary>
        public Graph NewGraph { get; set; } = null;

        /// <summary>
        /// True to import vectors carried on nodes and edges.  Default is true.
        /// </summary>
        public bool IncludeVectors { get; set; } = true;

        /// <summary>
        /// Bulk create response shape used for imported nodes and edges.  Default is Minimal for import throughput.
        /// </summary>
        public BulkCreateReturnModeEnum ReturnMode { get; set; } = BulkCreateReturnModeEnum.Minimal;

        /// <summary>
        /// Number of nodes or edges written per bulk create call while streaming.  Default is 1000.  Minimum is 1.
        /// </summary>
        public int BatchSize
        {
            get
            {
                return _BatchSize;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(BatchSize));
                _BatchSize = value;
            }
        }

        /// <summary>
        /// True to write the entire import within a single transaction.  Default is true.
        /// For backup-scale restores that exceed a single transaction, the importer may commit in
        /// batches and report the downgrade as a warning.
        /// </summary>
        public bool SingleTransaction { get; set; } = true;

        #endregion

        #region Private-Members

        private int _BatchSize = 1000;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public GraphImportRequest()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
