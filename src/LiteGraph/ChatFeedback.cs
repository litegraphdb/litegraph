namespace LiteGraph
{
    using System;

    /// <summary>
    /// Chat feedback.  A user's rating of a single assistant turn.
    /// </summary>
    public class ChatFeedback
    {
        #region Public-Members

        /// <summary>
        /// GUID.
        /// </summary>
        public Guid GUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Tenant GUID.
        /// </summary>
        public Guid TenantGUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Thread GUID.
        /// </summary>
        public Guid ThreadGUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Turn GUID.
        /// </summary>
        public Guid TurnGUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// GUID of the user that submitted the feedback.
        /// </summary>
        public Guid UserGUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Rating.  Default is ThumbsUp.
        /// </summary>
        public ChatFeedbackRatingEnum Rating { get; set; } = ChatFeedbackRatingEnum.ThumbsUp;

        /// <summary>
        /// Free-text feedback.  Null when the user supplied no comment.
        /// </summary>
        public string FeedbackText { get; set; } = null;

        /// <summary>
        /// Creation timestamp, in UTC.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ChatFeedback()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
