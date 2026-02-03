namespace LiteGraph
{
    using System;

    /// <summary>
    /// Result from a vector search operation containing the identifier and a similarity score.
    /// </summary>
    public class VectorScoreResult
    {
        #region Public-Members

        /// <summary>
        /// Vector or node identifier.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Similarity score.
        /// The interpretation depends on the search type used.
        /// </summary>
        public float Score { get; set; }

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public VectorScoreResult()
        {
        }

        /// <summary>
        /// Instantiate with values.
        /// </summary>
        /// <param name="id">Vector or node identifier.</param>
        /// <param name="score">Similarity score.</param>
        public VectorScoreResult(Guid id, float score)
        {
            Id = id;
            Score = score;
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
