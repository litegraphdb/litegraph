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
  Endpoint?: 'required' | 'invalidUrl';
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
  } else if (!isAbsoluteHttpUrl(values.Endpoint)) {
    errors.Endpoint = 'invalidUrl';
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
