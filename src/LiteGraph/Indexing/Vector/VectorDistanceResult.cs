namespace LiteGraph.Indexing.Vector
{
    using System;

    /// <summary>
    /// Result from a vector search operation containing the vector identifier and its distance from the query.
    /// </summary>
    public class VectorDistanceResult
    {
        #region Public-Members

        /// <summary>
        /// Vector identifier (typically a node GUID).
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Distance from the query vector.
        /// Lower values indicate closer/more similar vectors.
        /// </summary>
        public float Distance { get; set; }

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public VectorDistanceResult()
        {
        }

        /// <summary>
        /// Instantiate with values.
        /// </summary>
        /// <param name="id">Vector identifier.</param>
        /// <param name="distance">Distance from query vector.</param>
        public VectorDistanceResult(Guid id, float distance)
        {
            Id = id;
            Distance = distance;
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
