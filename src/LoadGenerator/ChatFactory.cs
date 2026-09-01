namespace LoadGenerator
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using LiteGraph;

    /// <summary>
    /// Produces plausible chat threads, turns, telemetry, and feedback content.
    /// This class is not thread-safe; use one instance per thread.
    /// </summary>
    public class ChatFactory
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private Random _Random;

        private static readonly string[] _ThreadTitles = new string[]
        {
            "Mapping service dependencies",
            "Which nodes are unhealthy?",
            "Product recommendations deep dive",
            "Supply chain bottleneck analysis",
            "Who owns the auth runbook?",
            "Community influencers overview",
            "Capacity planning questions",
            "Edge weights and routing costs",
            "Vector search quality check",
            "Weekly graph health review"
        };

        private static readonly string[] _UserMessages = new string[]
        {
            "Which services depend on the primary database? I want to plan a maintenance window.",
            "Can you summarize the most connected nodes in this graph?",
            "What does the DEPENDS_ON relationship between the api and auth service imply for failover?",
            "List the top five products by rating and tell me which suppliers they come from.",
            "Are there any orphaned nodes or unusually expensive edges I should know about?",
            "How would a failure in the eu-central-1 region propagate through this graph?",
            "Give me a short overview of this graph: node counts, edge types, and anything unusual.",
            "Which warehouses ship to the apac distribution centers, and what are the lead times?",
            "Find articles related to the incident postmortem and summarize their key points.",
            "Who are the most active community members, and who do they collaborate with?"
        };

        private static readonly string[] _AssistantResponsesPlain = new string[]
        {
            "Based on the graph, three services declare a direct dependency: the API tier, the auth service, and the reporting worker. The API tier is the most sensitive because it has no replica fallback configured. I would schedule the window outside the 09:00-17:00 peak and fail traffic to the replica first.",
            "The failure would propagate along four DEPENDS_ON edges. The auth service is the critical path: its downstream consumers have no alternate route, so expect elevated error rates within one hop. The cache and queue tiers are isolated and should be unaffected.",
            "I traced the relationship chain and the failover implication is that requests will be re-routed through the secondary path with roughly 2x the edge cost. That is acceptable for reads but may add noticeable latency to writes.",
            "There are two nodes with degree zero relative to the rest of the component, and one edge whose cost (94) is far above the median (12). It may be worth reviewing whether that route is still intended."
        };

        private static readonly string[] _AssistantResponsesTable = new string[]
        {
            "Here are the most connected nodes:\n\n| Node | Type | Degree |\n|------|------|--------|\n| api-03 | service | 14 |\n| db-primary-01 | database | 11 |\n| lb-02 | load-balancer | 9 |\n| redis-cache-04 | cache | 7 |\n\nThe api tier dominates connectivity, which matches expectations for a hub-and-spoke layout.",
            "Top products by rating:\n\n| Product | Rating | Supplier |\n|---------|--------|----------|\n| sensor-12 | 4.9 | supplier-03 |\n| module-07 | 4.8 | supplier-01 |\n| adapter-19 | 4.7 | supplier-03 |\n| kit-02 | 4.6 | supplier-05 |\n| widget-33 | 4.5 | supplier-02 |\n\nSupplier-03 appears twice, so it may be worth prioritizing in negotiations."
        };

        private static readonly string[] _AssistantResponsesList = new string[]
        {
            "Quick overview of the graph:\n\n- **Nodes**: a healthy mix of services, databases, and caches\n- **Edge types**: DEPENDS_ON and ROUTES_TO dominate; REPLICATES_TO appears only on database pairs\n- **Unusual**: one node carries a degraded status flag and sits on the critical path\n- **Suggestion**: add a redundant route around the degraded node",
            "Key points from the related articles:\n\n1. The incident began with a saturated connection pool, not a network fault.\n2. The runbook's rollback step was out of date and cost roughly 18 minutes.\n3. Two follow-up actions remain open: pool monitoring and runbook review.\n\nI linked the three most relevant documents in the retrieval context."
        };

        private static readonly string[] _FailureErrors = new string[]
        {
            "Upstream provider returned 429 Too Many Requests; retries exhausted.",
            "Connection to inference endpoint timed out after 30000ms.",
            "Upstream provider returned 503 Service Unavailable."
        };

        private static readonly string[] _PositiveFeedback = new string[]
        {
            "Exactly what I needed, thanks.",
            "Great summary, saved me a lot of digging.",
            "The table format made this easy to act on.",
            ""
        };

        private static readonly string[] _NegativeFeedback = new string[]
        {
            "The degree counts do not match what I see in the graph view.",
            "Answer was too generic, I needed specifics for this graph.",
            ""
        };

        private static readonly ChatProviderTypeEnum[] _Providers = new ChatProviderTypeEnum[]
        {
            ChatProviderTypeEnum.OpenAI,
            ChatProviderTypeEnum.OpenAI,
            ChatProviderTypeEnum.Anthropic,
            ChatProviderTypeEnum.Ollama,
            ChatProviderTypeEnum.Gemini
        };

        private static readonly string[] _Models = new string[]
        {
            "gpt-4o",
            "gpt-4o-mini",
            "claude-sonnet-4-5",
            "llama3.1:8b",
            "gemini-2.0-flash"
        };

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="random">Random number generator.</param>
        /// <exception cref="ArgumentNullException">Thrown when the random number generator is null.</exception>
        public ChatFactory(Random random)
        {
            _Random = random ?? throw new ArgumentNullException(nameof(random));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Pick a thread title by ordinal.
        /// </summary>
        /// <param name="ordinal">Zero-based thread ordinal.</param>
        /// <returns>Thread title.</returns>
        public string PickThreadTitle(int ordinal)
        {
            int index = ((ordinal % _ThreadTitles.Length) + _ThreadTitles.Length) % _ThreadTitles.Length;
            int cycle = ordinal / _ThreadTitles.Length;
            if (cycle == 0) return _ThreadTitles[index];
            return _ThreadTitles[index] + " (" + (cycle + 1).ToString() + ")";
        }

        /// <summary>
        /// Build a fully populated chat turn with plausible content and correlated telemetry.
        /// The returned turn still requires GUID, tenant, thread, sequence, and timestamp assignment by the caller.
        /// </summary>
        /// <returns>Chat turn.</returns>
        public ChatTurn BuildTurn()
        {
            ChatTurn turn = new ChatTurn();

            int providerIndex = _Random.Next(_Providers.Length);
            turn.Provider = _Providers[providerIndex];
            turn.Model = _Models[providerIndex];
            turn.CompletionEndpointGUID = Guid.NewGuid();
            turn.TraceId = RandomHex(32);

            turn.UserMessage = _UserMessages[_Random.Next(_UserMessages.Length)];
            turn.AssistantResponse = PickAssistantResponse();

            bool failed = _Random.NextDouble() < 0.07;
            bool usedTools = !failed && _Random.NextDouble() < 0.2;
            bool usedRetrieval = _Random.NextDouble() < 0.6;

            int promptTokens = 300 + _Random.Next(0, 2700);
            int completionTokens = 80 + _Random.Next(0, 820);

            double ttftMs = 200.0 + (_Random.NextDouble() * 2800.0);
            double generationTps = 20.0 + (_Random.NextDouble() * 70.0);
            double generationMs = completionTokens / generationTps * 1000.0;
            double ttltMs = ttftMs + generationMs;
            double overheadMs = 50.0 + (_Random.NextDouble() * 350.0);

            turn.PromptTokens = promptTokens;
            turn.CompletionTokens = completionTokens;
            turn.LimiterWaitMs = Math.Round(_Random.NextDouble() * 40.0, 2);
            turn.InferenceConnectionMs = Math.Round(20.0 + (_Random.NextDouble() * 180.0), 2);
            turn.TimeToFirstTokenMs = Math.Round(ttftMs, 2);
            turn.TimeToLastTokenMs = Math.Round(ttltMs, 2);
            turn.TotalDurationMs = Math.Round(ttltMs + overheadMs, 2);
            turn.TokensPerSecondGeneration = Math.Round(completionTokens / (generationMs / 1000.0), 2);
            turn.TokensPerSecondOverall = Math.Round(completionTokens / (ttltMs / 1000.0), 2);
            turn.RetryCount = _Random.NextDouble() < 0.06 ? 1 : 0;

            if (usedRetrieval)
            {
                turn.EmbeddingEndpointGUID = Guid.NewGuid();
                turn.EmbeddingDurationMs = Math.Round(15.0 + (_Random.NextDouble() * 105.0), 2);
                turn.RetrievalDurationMs = Math.Round(5.0 + (_Random.NextDouble() * 55.0), 2);
                turn.RetrievedChunkCount = 1 + _Random.Next(0, 8);
                turn.TotalDurationMs = Math.Round(turn.TotalDurationMs + turn.EmbeddingDurationMs.Value + turn.RetrievalDurationMs.Value, 2);
            }

            if (usedTools)
            {
                turn.ToolLoopIterations = 1 + _Random.Next(0, 2);
                turn.ToolCallCount = turn.ToolLoopIterations;
                turn.ToolTranscriptJson = BuildToolTranscriptJson(turn.ToolCallCount);
            }

            if (failed)
            {
                turn.Success = false;
                turn.HttpStatus = PickFailureStatus();
                turn.Error = _FailureErrors[_Random.Next(_FailureErrors.Length)];
                turn.AssistantResponse = TruncateResponse(turn.AssistantResponse);
                turn.CompletionTokens = Math.Max(0, completionTokens / 4);
                turn.TokensPerSecondGeneration = null;
                turn.TokensPerSecondOverall = null;
                turn.TimeToLastTokenMs = null;
            }

            turn.TelemetryJson = BuildTelemetryJson(turn);

            return turn;
        }

        /// <summary>
        /// Pick a feedback comment matching the rating polarity.  May return null to represent feedback without a comment.
        /// </summary>
        /// <param name="positive">True for thumbs-up feedback.</param>
        /// <returns>Feedback text, or null.</returns>
        public string? PickFeedbackComment(bool positive)
        {
            string[] pool = positive ? _PositiveFeedback : _NegativeFeedback;
            string comment = pool[_Random.Next(pool.Length)];
            if (String.IsNullOrEmpty(comment)) return null;
            return comment;
        }

        #endregion

        #region Private-Methods

        private string PickAssistantResponse()
        {
            double roll = _Random.NextDouble();
            if (roll < 0.2) return _AssistantResponsesTable[_Random.Next(_AssistantResponsesTable.Length)];
            if (roll < 0.4) return _AssistantResponsesList[_Random.Next(_AssistantResponsesList.Length)];
            return _AssistantResponsesPlain[_Random.Next(_AssistantResponsesPlain.Length)];
        }

        private int PickFailureStatus()
        {
            double roll = _Random.NextDouble();
            if (roll < 0.5) return 429;
            if (roll < 0.8) return 503;
            return 500;
        }

        private string TruncateResponse(string? response)
        {
            if (String.IsNullOrEmpty(response)) return String.Empty;
            int cut = Math.Min(response.Length, 40 + _Random.Next(0, 80));
            return response.Substring(0, cut);
        }

        private string BuildToolTranscriptJson(int toolCallCount)
        {
            List<string> entries = new List<string>();

            for (int i = 0; i < toolCallCount; i++)
            {
                string name = _Random.NextDouble() < 0.5 ? "graph_search" : "node_lookup";
                int matches = _Random.Next(1, 8);
                int durationMs = 40 + _Random.Next(0, 500);
                entries.Add(
                    "{\"name\":\"" + name + "\"," +
                    "\"arguments\":{\"query\":\"dependency path\",\"limit\":10}," +
                    "\"result\":{\"matches\":" + matches.ToString(CultureInfo.InvariantCulture) + "}," +
                    "\"durationMs\":" + durationMs.ToString(CultureInfo.InvariantCulture) + "}");
            }

            return "[" + String.Join(",", entries) + "]";
        }

        private string BuildTelemetryJson(ChatTurn turn)
        {
            return
                "{\"limiterWaitMs\":" + FormatNullableDouble(turn.LimiterWaitMs) +
                ",\"connectionMs\":" + FormatNullableDouble(turn.InferenceConnectionMs) +
                ",\"embeddingMs\":" + FormatNullableDouble(turn.EmbeddingDurationMs) +
                ",\"retrievalMs\":" + FormatNullableDouble(turn.RetrievalDurationMs) +
                ",\"ttftMs\":" + FormatNullableDouble(turn.TimeToFirstTokenMs) +
                ",\"ttltMs\":" + FormatNullableDouble(turn.TimeToLastTokenMs) +
                ",\"totalMs\":" + turn.TotalDurationMs.ToString(CultureInfo.InvariantCulture) +
                "}";
        }

        private string FormatNullableDouble(double? value)
        {
            if (!value.HasValue) return "null";
            return value.Value.ToString(CultureInfo.InvariantCulture);
        }

        private string RandomHex(int length)
        {
            const string chars = "0123456789abcdef";
            char[] buffer = new char[length];

            for (int i = 0; i < length; i++)
            {
                buffer[i] = chars[_Random.Next(chars.Length)];
            }

            return new string(buffer);
        }

        #endregion
    }
}
