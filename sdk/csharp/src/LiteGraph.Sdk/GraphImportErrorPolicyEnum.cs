namespace LiteGraph.Sdk
{
    /// <summary>
    /// Policy governing how the importer reacts to a malformed or unrecognized JSONL line.
    /// </summary>
    public enum GraphImportErrorPolicyEnum
    {
        /// <summary>
        /// Abort the entire import on the first bad line, rolling back any staged writes.  This is the default.
        /// </summary>
        Abort,
        /// <summary>
        /// Skip bad lines, collect them as warnings, and import the remaining well-formed records.
        /// </summary>
        Skip
    }
}
