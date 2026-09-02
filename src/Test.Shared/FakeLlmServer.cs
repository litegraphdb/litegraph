namespace Test.Shared
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// In-process OpenAI-compatible fake LLM server for deterministic chat tests.
    /// Speaks /v1/chat/completions (streaming and non-streaming), /v1/embeddings, and /v1/models.
    /// Behaviors are scripted per request via a queue; when the queue is empty a plain text
    /// response is produced, which also absorbs incidental requests such as thread title generation.
    /// </summary>
    public sealed class FakeLlmServer : IDisposable
    {
        #region Public-Members

        /// <summary>
        /// Base URL, for example http://127.0.0.1:53412.
        /// </summary>
        public string Endpoint
        {
            get
            {
                return "http://127.0.0.1:" + _Port;
            }
        }

        /// <summary>
        /// Total requests received.
        /// </summary>
        public int RequestCount
        {
            get
            {
                return _RequestCount;
            }
        }

        /// <summary>
        /// Captured request bodies for /v1/chat/completions, oldest first.
        /// </summary>
        public ConcurrentQueue<string> CapturedCompletionBodies { get; } = new ConcurrentQueue<string>();

        /// <summary>
        /// Captured request bodies for the Ollama-style /api/generate route, oldest first.
        /// Used to assert model preload (warm-up) requests.
        /// </summary>
        public ConcurrentQueue<string> CapturedGenerateBodies { get; } = new ConcurrentQueue<string>();

        #endregion

        #region Private-Members

        private readonly HttpListener _Listener;
        private readonly int _Port;
        private readonly CancellationTokenSource _TokenSource = new CancellationTokenSource();
        private readonly ConcurrentQueue<FakeLlmBehavior> _Behaviors = new ConcurrentQueue<FakeLlmBehavior>();
        private Task _LoopTask;
        private int _RequestCount = 0;
        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate and start listening on a free loopback port.
        /// </summary>
        public FakeLlmServer()
        {
            int attempts = 0;

            while (true)
            {
                attempts++;
                int port = AllocatePort();
                HttpListener listener = new HttpListener();
                listener.Prefixes.Add("http://127.0.0.1:" + port + "/");

                try
                {
                    listener.Start();
                    _Listener = listener;
                    _Port = port;
                    break;
                }
                catch (HttpListenerException)
                {
                    if (attempts >= 10) throw;
                }
            }

            _LoopTask = Task.Run(AcceptLoop);
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Queue a plain text completion response.
        /// </summary>
        /// <param name="text">Response text.</param>
        /// <param name="promptTokens">Prompt tokens to report.</param>
        /// <param name="completionTokens">Completion tokens to report.</param>
        public void EnqueueText(string text, int promptTokens = 10, int completionTokens = 5)
        {
            _Behaviors.Enqueue(new FakeLlmBehavior { Kind = FakeLlmBehaviorKind.Text, Text = text, PromptTokens = promptTokens, CompletionTokens = completionTokens });
        }

        /// <summary>
        /// Queue a tool call response.
        /// </summary>
        /// <param name="toolName">Tool name.</param>
        /// <param name="argumentsJson">Arguments JSON string.</param>
        public void EnqueueToolCall(string toolName, string argumentsJson)
        {
            _Behaviors.Enqueue(new FakeLlmBehavior { Kind = FakeLlmBehaviorKind.ToolCall, Text = toolName, ArgumentsJson = argumentsJson });
        }

        /// <summary>
        /// Queue an HTTP failure response.
        /// </summary>
        /// <param name="statusCode">Status code to return, for example 429 or 500.</param>
        public void EnqueueFailure(int statusCode)
        {
            _Behaviors.Enqueue(new FakeLlmBehavior { Kind = FakeLlmBehaviorKind.Failure, StatusCode = statusCode });
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            if (_Disposed) return;
            _Disposed = true;
            _TokenSource.Cancel();
            try { _Listener.Stop(); } catch { }
            try { _Listener.Close(); } catch { }
            _TokenSource.Dispose();
        }

        #endregion

        #region Private-Methods

        private static int AllocatePort()
        {
            System.Net.Sockets.TcpListener probe = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            int port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();
            return port;
        }

        private async Task AcceptLoop()
        {
            while (!_TokenSource.IsCancellationRequested)
            {
                HttpListenerContext ctx;

                try
                {
                    ctx = await _Listener.GetContextAsync().ConfigureAwait(false);
                }
                catch (Exception)
                {
                    return;
                }

                _ = Task.Run(() => HandleRequest(ctx));
            }
        }

        private async Task HandleRequest(HttpListenerContext ctx)
        {
            Interlocked.Increment(ref _RequestCount);

            try
            {
                string path = ctx.Request.Url != null ? ctx.Request.Url.AbsolutePath : String.Empty;

                if (path.EndsWith("/v1/models", StringComparison.OrdinalIgnoreCase))
                {
                    await SendJson(ctx, 200, "{\"object\":\"list\",\"data\":[{\"id\":\"fake-model\",\"object\":\"model\"}]}").ConfigureAwait(false);
                    return;
                }

                string body = String.Empty;
                using (StreamReader reader = new StreamReader(ctx.Request.InputStream, Encoding.UTF8))
                {
                    body = await reader.ReadToEndAsync().ConfigureAwait(false);
                }

                if (path.EndsWith("/api/generate", StringComparison.OrdinalIgnoreCase))
                {
                    CapturedGenerateBodies.Enqueue(body);
                    await SendJson(ctx, 200, "{\"model\":\"fake-model\",\"done\":true}").ConfigureAwait(false);
                    return;
                }

                if (path.EndsWith("/v1/embeddings", StringComparison.OrdinalIgnoreCase))
                {
                    await SendJson(ctx, 200, BuildEmbeddingResponse(body)).ConfigureAwait(false);
                    return;
                }

                if (path.EndsWith("/v1/chat/completions", StringComparison.OrdinalIgnoreCase))
                {
                    CapturedCompletionBodies.Enqueue(body);

                    if (!_Behaviors.TryDequeue(out FakeLlmBehavior? behavior) || behavior == null)
                    {
                        behavior = new FakeLlmBehavior { Kind = FakeLlmBehaviorKind.Text, Text = "ok", PromptTokens = 1, CompletionTokens = 1 };
                    }

                    if (behavior.Kind == FakeLlmBehaviorKind.Failure)
                    {
                        await SendJson(ctx, behavior.StatusCode, "{\"error\":{\"message\":\"scripted failure\"}}").ConfigureAwait(false);
                        return;
                    }

                    bool stream = BodyRequestsStreaming(body);

                    if (stream) await SendStreamingCompletion(ctx, behavior).ConfigureAwait(false);
                    else await SendJson(ctx, 200, BuildNonStreamingCompletion(behavior)).ConfigureAwait(false);
                    return;
                }

                await SendJson(ctx, 404, "{\"error\":{\"message\":\"not found\"}}").ConfigureAwait(false);
            }
            catch (Exception)
            {
                try { ctx.Response.Abort(); } catch { }
            }
        }

        private static bool BodyRequestsStreaming(string body)
        {
            if (String.IsNullOrEmpty(body)) return false;

            try
            {
                using (JsonDocument doc = JsonDocument.Parse(body))
                {
                    if (doc.RootElement.TryGetProperty("stream", out JsonElement streamProp))
                    {
                        return streamProp.ValueKind == JsonValueKind.True;
                    }
                }
            }
            catch (JsonException)
            {
            }

            return false;
        }

        private static string BuildEmbeddingResponse(string body)
        {
            int seed = (body != null ? body.Length : 1);
            StringBuilder floats = new StringBuilder();

            for (int i = 0; i < 8; i++)
            {
                if (i > 0) floats.Append(',');
                floats.Append(((seed % 17) + i + 1) / 100.0);
            }

            return "{\"object\":\"list\",\"model\":\"fake-embed\",\"data\":[{\"object\":\"embedding\",\"index\":0,\"embedding\":[" + floats + "]}],\"usage\":{\"prompt_tokens\":2,\"total_tokens\":2}}";
        }

        private static string BuildNonStreamingCompletion(FakeLlmBehavior behavior)
        {
            if (behavior.Kind == FakeLlmBehaviorKind.ToolCall)
            {
                return "{\"id\":\"fake\",\"object\":\"chat.completion\",\"model\":\"fake-model\",\"choices\":[{\"index\":0,\"message\":{\"role\":\"assistant\",\"content\":null,\"tool_calls\":[{\"id\":\"call_1\",\"type\":\"function\",\"function\":{\"name\":\"" + behavior.Text + "\",\"arguments\":" + JsonSerializer.Serialize(behavior.ArgumentsJson) + "}}]},\"finish_reason\":\"tool_calls\"}],\"usage\":{\"prompt_tokens\":10,\"completion_tokens\":5,\"total_tokens\":15}}";
            }

            return "{\"id\":\"fake\",\"object\":\"chat.completion\",\"model\":\"fake-model\",\"choices\":[{\"index\":0,\"message\":{\"role\":\"assistant\",\"content\":" + JsonSerializer.Serialize(behavior.Text) + "},\"finish_reason\":\"stop\"}],\"usage\":{\"prompt_tokens\":" + behavior.PromptTokens + ",\"completion_tokens\":" + behavior.CompletionTokens + ",\"total_tokens\":" + (behavior.PromptTokens + behavior.CompletionTokens) + "}}";
        }

        private async Task SendStreamingCompletion(HttpListenerContext ctx, FakeLlmBehavior behavior)
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "text/event-stream";
            ctx.Response.SendChunked = true;

            if (behavior.Kind == FakeLlmBehaviorKind.ToolCall)
            {
                string half = behavior.ArgumentsJson.Length > 1
                    ? behavior.ArgumentsJson.Substring(0, behavior.ArgumentsJson.Length / 2)
                    : behavior.ArgumentsJson;
                string rest = behavior.ArgumentsJson.Length > 1
                    ? behavior.ArgumentsJson.Substring(behavior.ArgumentsJson.Length / 2)
                    : String.Empty;

                await WriteSse(ctx, "{\"id\":\"fake\",\"object\":\"chat.completion.chunk\",\"choices\":[{\"index\":0,\"delta\":{\"role\":\"assistant\",\"tool_calls\":[{\"index\":0,\"id\":\"call_1\",\"type\":\"function\",\"function\":{\"name\":\"" + behavior.Text + "\",\"arguments\":" + JsonSerializer.Serialize(half) + "}}]},\"finish_reason\":null}]}").ConfigureAwait(false);
                await WriteSse(ctx, "{\"id\":\"fake\",\"object\":\"chat.completion.chunk\",\"choices\":[{\"index\":0,\"delta\":{\"tool_calls\":[{\"index\":0,\"function\":{\"arguments\":" + JsonSerializer.Serialize(rest) + "}}]},\"finish_reason\":null}]}").ConfigureAwait(false);
                await WriteSse(ctx, "{\"id\":\"fake\",\"object\":\"chat.completion.chunk\",\"choices\":[{\"index\":0,\"delta\":{},\"finish_reason\":\"tool_calls\"}],\"usage\":{\"prompt_tokens\":10,\"completion_tokens\":5,\"total_tokens\":15}}").ConfigureAwait(false);
            }
            else
            {
                await WriteSse(ctx, "{\"id\":\"fake\",\"object\":\"chat.completion.chunk\",\"choices\":[{\"index\":0,\"delta\":{\"role\":\"assistant\"},\"finish_reason\":null}]}").ConfigureAwait(false);

                string text = behavior.Text ?? String.Empty;
                int mid = Math.Max(1, text.Length / 2);
                foreach (string piece in new string[] { text.Substring(0, Math.Min(mid, text.Length)), (text.Length > mid ? text.Substring(mid) : String.Empty) })
                {
                    if (String.IsNullOrEmpty(piece)) continue;
                    await WriteSse(ctx, "{\"id\":\"fake\",\"object\":\"chat.completion.chunk\",\"choices\":[{\"index\":0,\"delta\":{\"content\":" + JsonSerializer.Serialize(piece) + "},\"finish_reason\":null}]}").ConfigureAwait(false);
                }

                await WriteSse(ctx, "{\"id\":\"fake\",\"object\":\"chat.completion.chunk\",\"choices\":[{\"index\":0,\"delta\":{},\"finish_reason\":\"stop\"}],\"usage\":{\"prompt_tokens\":" + behavior.PromptTokens + ",\"completion_tokens\":" + behavior.CompletionTokens + ",\"total_tokens\":" + (behavior.PromptTokens + behavior.CompletionTokens) + "}}").ConfigureAwait(false);
            }

            await WriteSse(ctx, "[DONE]").ConfigureAwait(false);
            try { ctx.Response.Close(); } catch { }
        }

        private static async Task WriteSse(HttpListenerContext ctx, string data)
        {
            byte[] bytes = Encoding.UTF8.GetBytes("data: " + data + "\n\n");
            await ctx.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
            await ctx.Response.OutputStream.FlushAsync().ConfigureAwait(false);
        }

        private static async Task SendJson(HttpListenerContext ctx, int statusCode, string json)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            ctx.Response.StatusCode = statusCode;
            ctx.Response.ContentType = "application/json";
            ctx.Response.ContentLength64 = bytes.Length;
            await ctx.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
            try { ctx.Response.Close(); } catch { }
        }

        #endregion

        #region Private-Classes

        private enum FakeLlmBehaviorKind
        {
            Text,
            ToolCall,
            Failure
        }

        private sealed class FakeLlmBehavior
        {
            internal FakeLlmBehaviorKind Kind = FakeLlmBehaviorKind.Text;
            internal string? Text = null;
            internal string ArgumentsJson = "{}";
            internal int StatusCode = 500;
            internal int PromptTokens = 10;
            internal int CompletionTokens = 5;
        }

        #endregion
    }
}
