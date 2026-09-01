/**
 * Enum for chat provider types.
 */
export type ChatProviderTypeEnum = string;
/**
 * Enum for chat provider types.
 *
 * @readonly
 * @enum {string}
 */
export const ChatProviderTypeEnum: Readonly<{
    /** OpenAI provider. */
    OpenAI: "OpenAI";
    /** Ollama provider. */
    Ollama: "Ollama";
    /** Google Gemini provider. */
    Gemini: "Gemini";
    /** Anthropic provider. */
    Anthropic: "Anthropic";
    /** Voyage AI provider (embeddings only). */
    VoyageAI: "VoyageAI";
}>;
