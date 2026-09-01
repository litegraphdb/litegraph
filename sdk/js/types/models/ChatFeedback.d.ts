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
    constructor(feedback?: {
        GUID?: string;
        TenantGUID?: string;
        ThreadGUID?: string;
        TurnGUID?: string;
        UserGUID?: string;
        Rating?: string;
        FeedbackText?: string | null;
        CreatedUtc?: Date | string;
    });
    GUID: string;
    TenantGUID: string;
    ThreadGUID: string;
    TurnGUID: string;
    UserGUID: string;
    Rating: string;
    FeedbackText: string;
    CreatedUtc: Date;
}
