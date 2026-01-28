import { http, HttpResponse } from 'msw';
import { mockEndpoint } from './setupTest';

export const handlers = [
  // Check if a edge exists by GUID
  http.head(mockEndpoint, ({ request, params, cookies }) => {
    return HttpResponse.text('Hello'); // Simulating edge exists
  }),
];
