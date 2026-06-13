namespace LiteGraph.Sdk
{
    /// <summary>
    /// Bulk create response shape.
    /// </summary>
    public enum BulkCreateReturnModeEnum
    {
        /// <summary>
        /// Return created objects with the existing full response shape.
        /// </summary>
        Full,

        /// <summary>
        /// Return only top-level created objects and skip optional response hydration.
        /// </summary>
        Minimal
    }
}
