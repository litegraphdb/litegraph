/**
 * Enum for chat endpoint types.
 */
export type ChatEndpointTypeEnum = string;
/**
 * Enum for chat endpoint types.
 *
 * @readonly
 * @enum {string}
 */
export const ChatEndpointTypeEnum: Readonly<{
    /** Embedding endpoint. */
    Embedding: "Embedding";
    /** Completion endpoint. */
    Completion: "Completion";
}>;
