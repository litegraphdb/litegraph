import { v4 as uuidV4 } from 'uuid';

/**
 * ChatFeedback class representing user feedback for a chat turn.
 */
export default class ChatFeedback {
  /**
   * @param {Object} feedback - Information about the chat feedback.
   * @param {string} [feedback.GUID] - Globally unique identifier for the feedback (automatically generated if not provided).
   * @param {string} [feedback.TenantGUID] - Globally unique identifier for the tenant.
   * @param {string} [feedback.ThreadGUID] - Globally unique identifier for the thread.
   * @param {string} [feedback.TurnGUID] - Globally unique identifier for the turn.
   * @param {string} [feedback.UserGUID] - Globally unique identifier for the submitting user.
   * @param {string} [feedback.Rating='ThumbsUp'] - Rating: ThumbsUp or ThumbsDown (default is ThumbsUp).
   * @param {string|null} [feedback.FeedbackText=null] - Optional free-form feedback text (default is null).
   * @param {Date|string} [feedback.CreatedUtc] - Creation timestamp in UTC (defaults to current UTC time).
   */
  constructor(feedback = {}) {
    const {
      GUID = uuidV4(),
      TenantGUID = null,
      ThreadGUID = null,
      TurnGUID = null,
      UserGUID = null,
      Rating = 'ThumbsUp',
      FeedbackText = null,
      CreatedUtc = new Date().toISOString(),
    } = feedback;

    this.GUID = GUID; // Unique identifier for the feedback
    this.TenantGUID = TenantGUID; // Unique identifier for the tenant
    this.ThreadGUID = ThreadGUID; // Unique identifier for the thread
    this.TurnGUID = TurnGUID; // Unique identifier for the turn
    this.UserGUID = UserGUID; // Unique identifier for the submitting user
    this.Rating = Rating; // Rating (ThumbsUp or ThumbsDown)
    this.FeedbackText = FeedbackText; // Free-form feedback text
    this.CreatedUtc = new Date(CreatedUtc); // Creation timestamp
  }
}
