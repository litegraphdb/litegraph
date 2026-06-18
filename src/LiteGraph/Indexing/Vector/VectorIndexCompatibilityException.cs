namespace LiteGraph.Indexing.Vector
{
    using System;

    /// <summary>
    /// Exception raised when a persisted vector index artifact cannot be safely used by the current index implementation.
    /// </summary>
    public class VectorIndexCompatibilityException : Exception
    {
        /// <summary>
        /// Instantiate the exception.
        /// </summary>
        /// <param name="message">Compatibility failure message.</param>
        public VectorIndexCompatibilityException(string message) : base(message)
        {
        }
    }
}
