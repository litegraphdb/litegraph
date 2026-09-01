/**
 * Enum for chat provider types.
 *
 * @readonly
 * @enum {string}
 */
export const ChatProviderTypeEnum = Object.freeze({
  /** OpenAI provider. */
  OpenAI: 'OpenAI',

  /** Ollama provider. */
  Ollama: 'Ollama',

  /** Google Gemini provider. */
  Gemini: 'Gemini',

  /** Anthropic provider. */
  Anthropic: 'Anthropic',

  /** Voyage AI provider (embeddings only). */
  VoyageAI: 'VoyageAI',
});
