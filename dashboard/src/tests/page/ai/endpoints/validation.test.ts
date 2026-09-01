import { isAbsoluteHttpUrl, validateEndpointForm } from '@/page/ai/endpoints/validation';
import { providersForType, validateProviderTypeCombo } from '@/lib/sdk/chat';

const validValues = {
  Name: 'Prod GPT',
  EndpointType: 'Completion' as const,
  Provider: 'OpenAI' as const,
  Endpoint: 'https://api.openai.com/v1',
  Model: 'gpt-4o-mini',
  MaxConcurrentRequests: 2,
};

describe('validateProviderTypeCombo', () => {
  it('blocks Anthropic embedding endpoints', () => {
    expect(validateProviderTypeCombo('Anthropic', 'Embedding')).toBe('anthropicEmbedding');
  });
  it('blocks VoyageAI completion endpoints', () => {
    expect(validateProviderTypeCombo('VoyageAI', 'Completion')).toBe('voyageCompletion');
  });
  it('allows every other combination', () => {
    expect(validateProviderTypeCombo('Anthropic', 'Completion')).toBeNull();
    expect(validateProviderTypeCombo('VoyageAI', 'Embedding')).toBeNull();
    expect(validateProviderTypeCombo('OpenAI', 'Completion')).toBeNull();
    expect(validateProviderTypeCombo('OpenAI', 'Embedding')).toBeNull();
    expect(validateProviderTypeCombo('Ollama', 'Completion')).toBeNull();
    expect(validateProviderTypeCombo('Gemini', 'Embedding')).toBeNull();
  });
});

describe('providersForType', () => {
  it('omits VoyageAI for completion and Anthropic for embedding', () => {
    expect(providersForType('Completion')).toEqual(['OpenAI', 'Ollama', 'Gemini', 'Anthropic']);
    expect(providersForType('Embedding')).toEqual(['OpenAI', 'Ollama', 'Gemini', 'VoyageAI']);
  });
});

describe('isAbsoluteHttpUrl', () => {
  it('accepts absolute http and https URLs', () => {
    expect(isAbsoluteHttpUrl('http://localhost:11434')).toBe(true);
    expect(isAbsoluteHttpUrl('https://api.openai.com/v1')).toBe(true);
  });
  it('rejects relative, empty, and non-http URLs', () => {
    expect(isAbsoluteHttpUrl('')).toBe(false);
    expect(isAbsoluteHttpUrl(null)).toBe(false);
    expect(isAbsoluteHttpUrl('/v1')).toBe(false);
    expect(isAbsoluteHttpUrl('ftp://example.com')).toBe(false);
    expect(isAbsoluteHttpUrl('api.openai.com')).toBe(false);
  });
});

describe('validateEndpointForm', () => {
  it('passes a fully valid form', () => {
    expect(validateEndpointForm(validValues)).toEqual({});
  });

  it('requires name, endpoint, and model', () => {
    const errors = validateEndpointForm({});
    expect(errors.Name).toBe('required');
    expect(errors.Endpoint).toBe('required');
    expect(errors.Model).toBe('required');
  });

  it('flags a relative endpoint URL', () => {
    expect(validateEndpointForm({ ...validValues, Endpoint: '/v1' }).Endpoint).toBe('invalidUrl');
  });

  it('flags invalid provider/type combos', () => {
    expect(
      validateEndpointForm({ ...validValues, Provider: 'VoyageAI', EndpointType: 'Completion' })
        .Provider
    ).toBe('voyageCompletion');
    expect(
      validateEndpointForm({ ...validValues, Provider: 'Anthropic', EndpointType: 'Embedding' })
        .Provider
    ).toBe('anthropicEmbedding');
  });

  it('enforces MaxConcurrentRequests >= 1', () => {
    expect(validateEndpointForm({ ...validValues, MaxConcurrentRequests: 0 }).MaxConcurrentRequests).toBe('min');
    expect(validateEndpointForm({ ...validValues, MaxConcurrentRequests: 1 }).MaxConcurrentRequests).toBeUndefined();
  });
});
