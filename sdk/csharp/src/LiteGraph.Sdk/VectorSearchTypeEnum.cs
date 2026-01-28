namespace LiteGraph.Sdk
{
    /// <summary>
    /// Vector search type.
    /// </summary>
    public enum VectorSearchTypeEnum
    {
        /// <summary>
        /// Cosine distance, the inverse of cosine similarity.
        /// </summary>
        CosineDistance,
        /// <summary>
        /// Cosine similarity, the inverse of cosine distance.
        /// </summary>
        CosineSimilarity,
        /// <summary>
        /// Euclidian distance, also known as L2 distance, the inverse of Euclidian similarity or L2 similarity.
        /// </summary>
        EuclidianDistance,
        /// <summary>
        /// Euclidian simmilarity, also known as L2 similarity, the inverse of Euclidian distance or L2 distance.
        /// </summary>
        EuclidianSimilarity,
        /// <summary>
        /// Dot product similarity.  Equivalent to cosine similarity when vectors are normalized, that is, magnitudes are all 1.
        /// </summary>
        DotProduct
    }
}
