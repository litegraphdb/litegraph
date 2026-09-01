import { ChatEndpointType, ChatProviderType, validateProviderTypeCombo } from '@/lib/sdk/chat';

/** Fields validated by the endpoint form. */
export type EndpointFormValues = {
  Name?: string | null;
  EndpointType?: ChatEndpointType | null;
  Provider?: ChatProviderType | null;
  Endpoint?: string | null;
  Model?: string | null;
  MaxConcurrentRequests?: number | null;
};

/** Machine-readable validation error codes keyed by field. */
export type EndpointFormErrors = {
  Name?: 'required';
  Endpoint?: 'required' | 'invalidUrl' | 'notBaseUrl';
  Model?: 'required';
  Provider?: 'anthropicEmbedding' | 'voyageCompletion';
  MaxConcurrentRequests?: 'min';
};

/** True when the value is an absolute http/https URL. */
export const isAbsoluteHttpUrl = (value?: string | null): boolean => {
  if (!value) return false;
  try {
    const parsed = new URL(value);
    return parsed.protocol === 'http:' || parsed.protocol === 'https:';
  } catch {
    return false;
  }
};

/**
 * PolyPrompt expects a bare base URL and appends provider paths itself
 * (`/v1/...` for OpenAI-compatible, `/api/...` for Ollama, and so on), so a
 * query string, fragment, or unexpected path means the request URL would be
 * malformed. OpenAI-compatible endpoints may end in `/v1` because PolyPrompt
 * detects and keeps it; every other provider must be host-only.
 */
export const validateEndpointUrlForProvider = (
  provider: ChatProviderType | null | undefined,
  value?: string | null
): 'invalidUrl' | 'notBaseUrl' | undefined => {
  if (!value) return undefined;
  if (!isAbsoluteHttpUrl(value)) return 'invalidUrl';
  const parsed = new URL(value);
  if (parsed.search || parsed.hash) return 'notBaseUrl';
  const path = parsed.pathname.replace(/\/+$/, '');
  if (path === '') return undefined;
  if (provider === 'OpenAI' && path === '/v1') return undefined;
  return 'notBaseUrl';
};

/**
 * Client-side mirror of the server's endpoint validation rules: required
 * Name/Endpoint/Model, absolute http(s) endpoint URL, valid provider/type
 * combo (Anthropic offers no embeddings; VoyageAI offers no completions), and
 * MaxConcurrentRequests >= 1. Returns an empty object when everything passes.
 */
export const validateEndpointForm = (values: EndpointFormValues): EndpointFormErrors => {
  const errors: EndpointFormErrors = {};
  if (!values.Name || !values.Name.trim()) errors.Name = 'required';
  if (!values.Endpoint || !values.Endpoint.trim()) {
    errors.Endpoint = 'required';
  } else {
    const urlError = validateEndpointUrlForProvider(values.Provider, values.Endpoint);
    if (urlError) errors.Endpoint = urlError;
  }
  if (!values.Model || !values.Model.trim()) errors.Model = 'required';
  if (values.Provider && values.EndpointType) {
    const combo = validateProviderTypeCombo(values.Provider, values.EndpointType);
    if (combo) errors.Provider = combo;
  }
  if (values.MaxConcurrentRequests != null && values.MaxConcurrentRequests < 1) {
    errors.MaxConcurrentRequests = 'min';
  }
  return errors;
};
