/**
 * ChatEndpointPreloadResult class representing the result of a chat endpoint model preload (warm-up) request.
 */
export default class ChatEndpointPreloadResult {
    /**
     * @param {Object} result - Information about the preload result.
     * @param {string|null} [result.EndpointGUID=null] - Chat endpoint GUID (default is null).
     * @param {string|null} [result.Model=null] - Model configured on the endpoint (default is null).
     * @param {string} [result.Provider='OpenAI'] - Provider type of the endpoint (default is 'OpenAI').
     * @param {boolean} [result.Supported=false] - Indicates whether the provider supports model preloading (default is false).
     * @param {boolean} [result.Started=false] - Indicates whether a background warm-up was started (default is false).
     * @param {boolean} [result.AlreadyInProgress=false] - Indicates whether a warm-up was already in flight (default is false).
     */
    constructor(result?: {
        EndpointGUID?: string | null;
        Model?: string | null;
        Provider?: string;
        Supported?: boolean;
        Started?: boolean;
        AlreadyInProgress?: boolean;
    });
    EndpointGUID: string;
    Model: string;
    Provider: string;
    Supported: boolean;
    Started: boolean;
    AlreadyInProgress: boolean;
}
