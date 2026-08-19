namespace LiteGraph
{
    /// <summary>
    /// Strategy used to resolve GUID collisions during a JSONL import.
    /// </summary>
    public enum GraphImportGuidStrategyEnum
    {
        /// <summary>
        /// Keep the original GUIDs from the file.  A collision with an existing record is an error.
        /// This is the default for creating a new graph.
        /// </summary>
        Preserve,
        /// <summary>
        /// Assign fresh GUIDs to every imported graph, node, and edge, remapping all references.
        /// This is the default for merging into an existing graph and can never collide.
        /// </summary>
        Regenerate,
        /// <summary>
        /// Leave records whose GUIDs already exist in the target untouched and import the remainder.
        /// </summary>
        Skip,
        /// <summary>
        /// Update records whose GUIDs already exist in the target in place, and create the remainder.
        /// </summary>
        Overwrite
    }
}
