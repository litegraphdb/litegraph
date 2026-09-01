import { http, HttpResponse } from 'msw';
import {
  mockChatEndpointGuid,
  mockThreadGuid,
  mockTurnGuid,
  mockFeedbackGuid,
  chatCompletionResultData,
  sseStreamBodyWithMalformedFrame,
} from './mockData';
import { api, mockEndpoint, mockTenantId } from '../setupTest';
import { handlers, buildSseResponse } from './handlers';
import { getServer } from '../server';
import ChatEndpoint from '../../src/models/ChatEndpoint';
import ChatThread from '../../src/models/ChatThread';
import ChatTurn from '../../src/models/ChatTurn';
import ChatFeedback from '../../src/models/ChatFeedback';
import ChatSettings from '../../src/models/ChatSettings';
import ChatEndpointHealth from '../../src/models/ChatEndpointHealth';
import ChatEndpointTestResult from '../../src/models/ChatEndpointTestResult';
import ChatCompletionResult from '../../src/models/ChatCompletionResult';
import ApiErrorResponse from '../../src/models/ApiErrorResponse';

const server = getServer(handlers);

describe('chatRoute Tests', () => {
  beforeAll(() => {
    server.listen();
  });

  afterEach(() => {
    server.resetHandlers();
  });

  afterAll(() => {
    server.close();
  });

  describe('Chat Endpoint Routes', () => {
    test('should create a chat endpoint', async () => {
      const newEndpoint = {
        Name: 'OpenAI completions',
        EndpointType: 'Completion',
        Provider: 'OpenAI',
        Endpoint: 'https://api.openai.com/v1/',
        ApiKey: 'sk-abcdef',
        Model: 'gpt-4o-mini',
      };
      const response = await api.createChatEndpoint(mockTenantId, newEndpoint);
      expect(response instanceof ChatEndpoint).toBe(true);
      expect(response.GUID).toBe(mockChatEndpointGuid);
      expect(response.ApiKey).toBe('********cdef'); // redacted by the server
    });

    it('throws error when creating a chat endpoint without data', async () => {
      try {
        await api.createChatEndpoint(mockTenantId);
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: ArgumentNullException: endpoint is null or empty');
      }
    });

    test('should read all chat endpoints', async () => {
      const response = await api.readChatEndpoints(mockTenantId);
      response.forEach((endpoint) => {
        expect(endpoint instanceof ChatEndpoint).toBe(true);
      });
    });

    test('should read chat endpoints filtered by endpoint type', async () => {
      const completions = await api.readChatEndpoints(mockTenantId, 'Completion');
      expect(completions.length).toBe(1);
      const embeddings = await api.readChatEndpoints(mockTenantId, 'Embedding');
      expect(embeddings.length).toBe(0);
    });

    test('should read a specific chat endpoint by GUID', async () => {
      const response = await api.readChatEndpoint(mockTenantId, mockChatEndpointGuid);
      expect(response instanceof ChatEndpoint).toBe(true);
      expect(response.GUID).toBe(mockChatEndpointGuid);
    });

    test('should check if a chat endpoint exists by GUID', async () => {
      const response = await api.chatEndpointExists(mockTenantId, mockChatEndpointGuid);
      expect(response).toBe(true);
    });

    test('should update a chat endpoint', async () => {
      const updateEndpoint = {
        GUID: mockChatEndpointGuid,
        Name: 'OpenAI completions',
        EndpointType: 'Completion',
        Provider: 'OpenAI',
        Endpoint: 'https://api.openai.com/v1/',
        ApiKey: '********cdef',
        Model: 'gpt-4o-mini',
      };
      const response = await api.updateChatEndpoint(mockTenantId, updateEndpoint);
      expect(response instanceof ChatEndpoint).toBe(true);
      expect(response.GUID).toBe(mockChatEndpointGuid);
    });

    it('throws error when updating a chat endpoint without GUID', async () => {
      try {
        await api.updateChatEndpoint(mockTenantId, { Name: 'no guid' });
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: ArgumentNullException: endpoint.GUID is null or empty');
      }
    });

    test('should delete a chat endpoint', async () => {
      const response = await api.deleteChatEndpoint(mockTenantId, mockChatEndpointGuid);
      expect(response).toBeUndefined();
    });

    test('should test a chat endpoint', async () => {
      const response = await api.testChatEndpoint(mockTenantId, mockChatEndpointGuid);
      expect(response instanceof ChatEndpointTestResult).toBe(true);
      expect(response.Reachable).toBe(true);
      expect(response.ModelExists).toBe(true);
      expect(Array.isArray(response.Models)).toBe(true);
    });

    test('should read health for a specific chat endpoint', async () => {
      const response = await api.readChatEndpointHealth(mockTenantId, mockChatEndpointGuid);
      expect(response instanceof ChatEndpointHealth).toBe(true);
      expect(response.EndpointGUID).toBe(mockChatEndpointGuid);
      expect(response.Healthy).toBe(true);
    });

    test('should read health for all chat endpoints', async () => {
      const response = await api.readAllChatEndpointHealth(mockTenantId);
      response.forEach((health) => {
        expect(health instanceof ChatEndpointHealth).toBe(true);
      });
    });
  });

  describe('Chat Thread Routes', () => {
    test('should create a chat thread', async () => {
      const response = await api.createChatThread(mockTenantId, { Title: 'My chat thread' });
      expect(response instanceof ChatThread).toBe(true);
      expect(response.GUID).toBe(mockThreadGuid);
    });

    test('should create a chat thread without a body', async () => {
      const response = await api.createChatThread(mockTenantId);
      expect(response instanceof ChatThread).toBe(true);
    });

    test('should read chat threads', async () => {
      const response = await api.readChatThreads(mockTenantId);
      response.forEach((thread) => {
        expect(thread instanceof ChatThread).toBe(true);
      });
    });

    test('should read all users chat threads', async () => {
      const response = await api.readChatThreads(mockTenantId, true);
      expect(response.length).toBe(1);
    });

    test('should read a specific chat thread by GUID', async () => {
      const response = await api.readChatThread(mockTenantId, mockThreadGuid);
      expect(response instanceof ChatThread).toBe(true);
      expect(response.GUID).toBe(mockThreadGuid);
    });

    test('should read turns of a chat thread', async () => {
      const response = await api.readChatThreadTurns(mockTenantId, mockThreadGuid);
      response.forEach((turn) => {
        expect(turn instanceof ChatTurn).toBe(true);
      });
      expect(response[0].GUID).toBe(mockTurnGuid);
      expect(response[0].ThreadGUID).toBe(mockThreadGuid);
    });

    test('should update a chat thread title', async () => {
      const response = await api.updateChatThread(mockTenantId, mockThreadGuid, { Title: 'Renamed thread' });
      expect(response instanceof ChatThread).toBe(true);
      expect(response.GUID).toBe(mockThreadGuid);
    });

    test('should delete a chat thread', async () => {
      const response = await api.deleteChatThread(mockTenantId, mockThreadGuid);
      expect(response).toBeUndefined();
    });
  });

  describe('Chat Completion Routes', () => {
    test('should execute a non-streaming chat completion', async () => {
      const response = await api.chatCompletion(mockTenantId, {
        ThreadGUID: mockThreadGuid,
        Message: 'What nodes exist?',
      });
      expect(response instanceof ChatCompletionResult).toBe(true);
      expect(response.ThreadGUID).toBe(mockThreadGuid);
      expect(response.TurnGUID).toBe(mockTurnGuid);
      expect(response.Message).toBe('There are 3 nodes.');
      expect(response.PromptTokens).toBe(100);
    });

    it('throws error when executing a completion without a request', async () => {
      try {
        await api.chatCompletion(mockTenantId);
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: ArgumentNullException: request is null or empty');
      }
    });

    test('should stream a chat completion and yield parsed events', async () => {
      const events = [];
      for await (const event of api.chatCompletionStreaming(mockTenantId, {
        ThreadGUID: mockThreadGuid,
        Message: 'What nodes exist?',
      })) {
        events.push(event);
      }

      expect(events.map((e) => e.event)).toEqual([
        'started',
        'delta',
        'delta',
        'tool_call',
        'tool_result',
        'usage',
      ]);
      expect(events[0].threadGuid).toBe(mockThreadGuid);
      expect(events[0].turnGuid).toBe(mockTurnGuid);

      const message = events
        .filter((e) => e.event === 'delta')
        .map((e) => e.content)
        .join('');
      expect(message).toBe('There are 3 nodes.');

      const toolCall = events.find((e) => e.event === 'tool_call');
      expect(toolCall.name).toBe('node/search');
      expect(toolCall.iteration).toBe(1);

      const toolResult = events.find((e) => e.event === 'tool_result');
      expect(toolResult.success).toBe(true);

      const usage = events.find((e) => e.event === 'usage');
      expect(usage.usage.TurnGUID).toBe(chatCompletionResultData.TurnGUID);
    });

    test('should tolerate a malformed SSE frame and continue streaming', async () => {
      server.use(
        http.post(`${mockEndpoint}v1.0/tenants/${mockTenantId}/chat/completions`, () => {
          return buildSseResponse(sseStreamBodyWithMalformedFrame);
        })
      );

      const events = [];
      for await (const event of api.chatCompletionStreaming(mockTenantId, { Message: 'hi' })) {
        events.push(event);
      }

      expect(events.map((e) => e.event)).toEqual(['started', 'delta']);
      expect(events[1].content).toBe('Hello');
    });

    test('should surface a 401 on non-streaming completion as an SDK error', async () => {
      server.use(
        http.post(`${mockEndpoint}v1.0/tenants/${mockTenantId}/chat/completions`, () => {
          return HttpResponse.json(
            { Error: 'AuthenticationFailed', Context: null, Message: 'Your authentication material was not accepted.' },
            { status: 401 }
          );
        })
      );

      await expect(api.chatCompletion(mockTenantId, { Message: 'hi' })).rejects.toBeInstanceOf(ApiErrorResponse);
    });

    test('should surface a 401 on streaming completion as an SDK error', async () => {
      server.use(
        http.post(`${mockEndpoint}v1.0/tenants/${mockTenantId}/chat/completions`, () => {
          return HttpResponse.json(
            { Error: 'AuthenticationFailed', Context: null, Message: 'Your authentication material was not accepted.' },
            { status: 401 }
          );
        })
      );

      const iterate = async () => {
        // eslint-disable-next-line no-unused-vars
        for await (const event of api.chatCompletionStreaming(mockTenantId, { Message: 'hi' })) {
          // should not yield anything
        }
      };
      await expect(iterate()).rejects.toBeInstanceOf(ApiErrorResponse);
    });
  });

  describe('Chat Feedback Routes', () => {
    test('should submit feedback for a chat turn', async () => {
      const response = await api.submitChatFeedback(mockTenantId, mockTurnGuid, {
        Rating: 'ThumbsUp',
        FeedbackText: 'Great answer',
      });
      expect(response instanceof ChatFeedback).toBe(true);
      expect(response.GUID).toBe(mockFeedbackGuid);
      expect(response.Rating).toBe('ThumbsUp');
    });

    it('throws error when submitting feedback without data', async () => {
      try {
        await api.submitChatFeedback(mockTenantId, mockTurnGuid);
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: ArgumentNullException: feedback is null or empty');
      }
    });

    test('should read all chat feedback', async () => {
      const response = await api.readAllChatFeedback(mockTenantId);
      response.forEach((feedback) => {
        expect(feedback instanceof ChatFeedback).toBe(true);
      });
    });

    test('should read a specific chat feedback record by GUID', async () => {
      const response = await api.readChatFeedback(mockTenantId, mockFeedbackGuid);
      expect(response instanceof ChatFeedback).toBe(true);
      expect(response.GUID).toBe(mockFeedbackGuid);
    });

    test('should delete a chat feedback record', async () => {
      const response = await api.deleteChatFeedback(mockTenantId, mockFeedbackGuid);
      expect(response).toBeUndefined();
    });
  });

  describe('Chat Settings Routes', () => {
    test('should read tenant chat settings', async () => {
      const response = await api.readChatSettings(mockTenantId);
      expect(response instanceof ChatSettings).toBe(true);
      expect(response.EnableChat).toBe(true);
      expect(response.MaxToolIterations).toBe(10);
    });

    test('should update tenant chat settings', async () => {
      const response = await api.updateChatSettings(mockTenantId, {
        EnableChat: true,
        EnableTools: true,
        DefaultCompletionEndpointGUID: mockChatEndpointGuid,
      });
      expect(response instanceof ChatSettings).toBe(true);
      expect(response.DefaultCompletionEndpointGUID).toBe(mockChatEndpointGuid);
    });

    it('throws error when updating chat settings without data', async () => {
      try {
        await api.updateChatSettings(mockTenantId);
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: ArgumentNullException: settings is null or empty');
      }
    });
  });
});
