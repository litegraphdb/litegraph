namespace LiteGraph.Sdk
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Chat feedback rating.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ChatFeedbackRatingEnum
    {
        /// <summary>
        /// Thumbs up.
        /// </summary>
        ThumbsUp,
        /// <summary>
        /// Thumbs down.
        /// </summary>
        ThumbsDown
    }
}
