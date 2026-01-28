import { http, HttpResponse } from 'msw';
const sampleGraphs: any[] = [];

export const handlers = [
  http.get('/api/graphs', () => {
    return HttpResponse.json(sampleGraphs);
  }),

  http.post('/api/graphs', async ({ request }) => {
    const data: any = await request.json();
    return HttpResponse.json({
      id: 'new-graph-id',
      ...data,
    });
  }),

  http.put('/api/graphs/:id', async ({ params, request }) => {
    const data: any = await request.json();
    return HttpResponse.json({
      id: params.id,
      ...data,
    });
  }),

  http.delete('/api/graphs/:id', () => {
    return HttpResponse.json({ success: true });
  }),
];
