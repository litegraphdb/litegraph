namespace LiteGraph.Sdk
{
    /// <summary>
    /// Mode governing how a JSONL import is applied to the target tenant.
    /// </summary>
    public enum GraphImportModeEnum
    {
        /// <summary>
        /// Create a brand-new graph populated from the imported records.
        /// </summary>
        CreateNew,
        /// <summary>
        /// Merge the imported records into an existing target graph.
        /// </summary>
        MergeIntoExisting
    }
}
