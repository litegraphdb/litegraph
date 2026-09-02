import { http, HttpResponse } from 'msw';
import {
  mockChatEndpointGuid,
  mockThreadGuid,
  mockTurnGuid,
  mockFeedbackGuid,
  chatEndpointData,
  chatEndpointsMockApiResponse,
  chatEndpointHealthData,
  chatEndpointHealthMockApiResponse,
  chatEndpointTestResultData,
  chatThreadData,
  chatThreadsMockApiResponse,
  chatTurnsMockApiResponse,
  chatFeedbackData,
  chatFeedbackMockApiResponse,
  chatSettingsData,
  chatModelsMockApiResponse,
  chatCompletionResultData,
  sseStreamBody,
} from './mockData';
import { mockEndpoint, mockTenantId } from '../setupTest';
import { toEnumerationEnvelope } from '../enumerationEnvelope';

const encoder = new TextEncoder();

// Streams the SSE body in small chunks (including mid-frame splits) to exercise buffering.
export const buildSseResponse = (body, chunkSize = 17) => {
  const stream = new ReadableStream({
    start(controller) {
      for (let i = 0; i < body.length; i += chunkSize) {
        controller.enqueue(encoder.encode(body.slice(i, i + chunkSize)));
      }
      controller.close();
    },
  });
  return new HttpResponse(stream, {
    headers: { 'Content-Type': 'text/event-stream' },
  });
};

export const handlers = [
  // Create a chat endpoint
  http.put(`${mockEndpoint}v1.0/tenants/${mockTenantId}/chat/endpoints`, () => {
    return HttpResponse.json(chatEndpointData);
  }),

  // Read all chat endpoints (enumeration envelope, optionally filtered by endpointType)
  http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/chat/endpoints`, ({ request }) => {
    const url = new URL(request.url);
    const endpointType = url.searchParams.get('endpointType');
    if (endpointType && endpointType !== chatEndpointData.EndpointType) {
      return HttpResponse.json(toEnumerationEnvelope([]));
    }
    return HttpResponse.json(toEnumerationEnvelope(chatEndpointsMockApiResponse));
  }),

  // Read health for all chat endpoints (enumeration envelope)
  http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/chat/endpoints/health`, () => {
    return HttpResponse.json(toEnumerationEnvelope(chatEndpointHealthMockApiResponse));
  }),

  // Read health for one chat endpoint
  http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/chat/endpoints/${mockChatEndpointGuid}/health`, () => {
    return HttpResponse.json(chatEndpointHealthData);
  }),

  // Check if a chat endpoint exists by GUID
  http.head(`${mockEndpoint}v1.0/tenants/${mockTenantId}/chat/endpoints/${mockChatEndpointGuid}`, () => {
    return HttpResponse.text('true');
  }),

  // Read a specific chat endpoint
  http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/chat/endpoints/${mockChatEndpointGuid}`, () => {
    return HttpResponse.json(chatEndpointData);
  }),

  // Update a chat endpoint
  http.put(`${mockEndpoint}v1.0/tenants/${mockTenantId}/chat/endpoints/${mockChatEndpointGuid}`, () => {
    return HttpResponse.json(chatEndpointData);
  }),

  // Delete a chat endpoint
  http.delete(`${mockEndpoint}v1.0/tenants/${mockTenantId}/chat/endpoints/${mockChatEndpointGuid}`, () => {
    return HttpResponse.json(chatEndpointData);
  }),

  // Test a chat endpoint
  http.post(`${mockEndpoint}v1.0/tenants/${mockTenantId}/chat/endpoints/${mockChatEndpointGuid}/test`, () => {
    return HttpResponse.json(chatEndpointTestResultData);
  }),

  // Read the model catalog (enumeration envelope)
  http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/chat/models`, () => {
    return HttpResponse.json(toEnumerationEnvelope(chatModelsMockApiResponse));
  }),

  // Chat completions (streaming and non-streaming, discriminated by Stream flag)
  http.post(`${mockEndpoint}v1.0/tenants/${mockTenantId}/chat/completions`, async ({ request }) => {
    const body = await request.json();
    if (body && body.Stream === true) {
      return buildSseResponse(sseStreamBody);
    }
    return HttpResponse.json(chatCompletionResultData);
  }),

  // Create a chat thread
  http.put(`${mockEndpoint}v1.0/tenants/${mockTenantId}/chat/threads`, () => {
    return HttpResponse.json(chatThreadData);
  }),

  // Read chat threads (enumeration envelope, optionally all users')
  http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/chat/threads`, () => {
    return HttpResponse.json(toEnumerationEnvelope(chatThreadsMockApiResponse));
  }),

  // Read a specific chat thread
  http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/chat/threads/${mockThreadGuid}`, () => {
    return HttpResponse.json(chatThreadData);
  }),

  // Read turns of a chat thread (enumeration envelope)
  http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/chat/threads/${mockThreadGuid}/turns`, () => {
    return HttpResponse.json(toEnumerationEnvelope(chatTurnsMockApiResponse));
  }),

  // Update a chat thread
  http.put(`${mockEndpoint}v1.0/tenants/${mockTenantId}/chat/threads/${mockThreadGuid}`, () => {
    return HttpResponse.json(chatThreadData);
  }),

  // Delete a chat thread
  http.delete(`${mockEndpoint}v1.0/tenants/${mockTenantId}/chat/threads/${mockThreadGuid}`, () => {
    return HttpResponse.json(chatThreadData);
  }),

  // Submit feedback for a chat turn
  http.post(`${mockEndpoint}v1.0/tenants/${mockTenantId}/chat/turns/${mockTurnGuid}/feedback`, () => {
    return HttpResponse.json(chatFeedbackData);
  }),

  // Read all chat feedback (enumeration envelope)
  http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/chat/feedback`, () => {
    return HttpResponse.json(toEnumerationEnvelope(chatFeedbackMockApiResponse));
  }),

  // Read a specific chat feedback record
  http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/chat/feedback/${mockFeedbackGuid}`, () => {
    return HttpResponse.json(chatFeedbackData);
  }),

  // Delete a chat feedback record
  http.delete(`${mockEndpoint}v1.0/tenants/${mockTenantId}/chat/feedback/${mockFeedbackGuid}`, () => {
    return HttpResponse.json(chatFeedbackData);
  }),

  // Read tenant chat settings
  http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/chat/settings`, () => {
    return HttpResponse.json(chatSettingsData);
  }),

  // Upsert tenant chat settings
  http.put(`${mockEndpoint}v1.0/tenants/${mockTenantId}/chat/settings`, () => {
    return HttpResponse.json(chatSettingsData);
  }),
];
