namespace LiteGraph.Client.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;
    using LiteGraph.Client.Interfaces;
    using LiteGraph.GraphRepositories;

    /// <summary>
    /// Chat endpoint methods.
    /// Client implementations are responsible for input validation and cross-cutting logic.
    /// </summary>
    public class ChatEndpointMethods : IChatEndpointMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LiteGraphClient _Client = null;
        private GraphRepositoryBase _Repo = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Chat endpoint methods.
        /// </summary>
        /// <param name="client">LiteGraph client.</param>
        /// <param name="repo">Graph repository.</param>
        public ChatEndpointMethods(LiteGraphClient client, GraphRepositoryBase repo)
        {
            _Client = client ?? throw new ArgumentNullException(nameof(client));
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<ChatEndpoint> Create(ChatEndpoint endpoint, CancellationToken token = default)
        {
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));
            token.ThrowIfCancellationRequested();
            ValidateEndpoint(endpoint);
            await _Client.ValidateTenantExists(endpoint.TenantGUID, token).ConfigureAwait(false);
            ChatEndpoint created = await _Repo.ChatEndpoint.Create(endpoint, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "created chat endpoint " + created.GUID);
            return created;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<ChatEndpoint> ReadAllInTenant(
            Guid tenantGuid,
            ChatEndpointTypeEnum? endpointType = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving chat endpoints");
            await foreach (ChatEndpoint endpoint in _Repo.ChatEndpoint.ReadAllInTenant(tenantGuid, endpointType, order, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                yield return endpoint;
            }
        }

        /// <inheritdoc />
        public async Task<ChatEndpoint> ReadByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return await _Repo.ChatEndpoint.ReadByGuid(tenantGuid, guid, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<ChatEndpoint>> Enumerate(EnumerationRequest query, ChatEndpointTypeEnum? endpointType = null, CancellationToken token = default)
        {
            if (query == null) query = new EnumerationRequest();
            token.ThrowIfCancellationRequested();
            return await _Repo.ChatEndpoint.Enumerate(query, endpointType, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<int> GetRecordCount(
            Guid? tenantGuid,
            ChatEndpointTypeEnum? endpointType = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            Guid? markerGuid = null,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return await _Repo.ChatEndpoint.GetRecordCount(tenantGuid, endpointType, order, markerGuid, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<ChatEndpoint> Update(ChatEndpoint endpoint, CancellationToken token = default)
        {
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));
            token.ThrowIfCancellationRequested();
            ValidateEndpoint(endpoint);
            ChatEndpoint existing = await _Repo.ChatEndpoint.ReadByGuid(endpoint.TenantGUID, endpoint.GUID, token).ConfigureAwait(false);
            if (existing == null) throw new KeyNotFoundException("The specified chat endpoint could not be found.");
            if (ChatEndpoint.IsRedactedApiKey(endpoint.ApiKey)) endpoint.ApiKey = existing.ApiKey;
            ChatEndpoint updated = await _Repo.ChatEndpoint.Update(endpoint, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "updated chat endpoint " + updated.GUID);
            return updated;
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ChatEndpoint.DeleteByGuid(tenantGuid, guid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted chat endpoint " + guid);
        }

        /// <inheritdoc />
        public async Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ChatEndpoint.DeleteAllInTenant(tenantGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted chat endpoints in tenant " + tenantGuid);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return await _Repo.ChatEndpoint.ExistsByGuid(tenantGuid, guid, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        private void ValidateEndpoint(ChatEndpoint endpoint)
        {
            if (String.IsNullOrEmpty(endpoint.Name)) throw new ArgumentException("Chat endpoint name is required.");
            if (String.IsNullOrEmpty(endpoint.Endpoint)) throw new ArgumentException("Chat endpoint URL is required.");
            if (String.IsNullOrEmpty(endpoint.Model)) throw new ArgumentException("Chat endpoint model is required.");

            Uri parsed = null;
            if (!Uri.TryCreate(endpoint.Endpoint, UriKind.Absolute, out parsed)
                || (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
                throw new ArgumentException("Chat endpoint URL must be an absolute http or https URL.");

            // PolyPrompt appends provider API paths itself (/v1/... for OpenAI-compatible, /api/... for
            // Ollama, and so on).  A path prefix on the base URL is permitted to support reverse proxies
            // and model-pinned gateways (for example a LiteGraph Ollama-compatible proxy at
            // /v1.0/api/{model}/); the provider client concatenates its API suffix onto whatever base is
            // supplied.  Only reject a base that already ends in a known API path, which would cause the
            // client to double-append (for example .../api/embeddings/api/embeddings).
            if (!String.IsNullOrEmpty(parsed.Query) || !String.IsNullOrEmpty(parsed.Fragment))
                throw new ArgumentException("Chat endpoint URL must be a base URL without a query string or fragment.");

            string trimmedPath = parsed.AbsolutePath.TrimEnd('/');
            string lowerPath = trimmedPath.ToLowerInvariant();
            bool endsWithApiPath = lowerPath.EndsWith("/embeddings", StringComparison.Ordinal)
                || lowerPath.EndsWith("/api/embed", StringComparison.Ordinal)
                || lowerPath.EndsWith("/chat/completions", StringComparison.Ordinal)
                || lowerPath.EndsWith("/api/chat", StringComparison.Ordinal)
                || lowerPath.EndsWith("/api/generate", StringComparison.Ordinal);
            if (endsWithApiPath)
                throw new ArgumentException("Chat endpoint URL must be a base URL; the provider client appends the API path (for example /api/embeddings) itself. Remove the trailing API path from the endpoint URL.");

            if (endpoint.Provider == ChatProviderTypeEnum.Anthropic && endpoint.EndpointType == ChatEndpointTypeEnum.Embedding)
                throw new ArgumentException("Anthropic has no embeddings API and cannot be used for an embedding endpoint.");

            if (endpoint.Provider == ChatProviderTypeEnum.VoyageAI && endpoint.EndpointType == ChatEndpointTypeEnum.Completion)
                throw new ArgumentException("VoyageAI is an embeddings-only provider and cannot be used for a completion endpoint.");

            if (!String.IsNullOrEmpty(endpoint.HealthCheckUrl))
            {
                Uri healthParsed = null;
                if (!Uri.TryCreate(endpoint.HealthCheckUrl, UriKind.Absolute, out healthParsed)
                    || (healthParsed.Scheme != Uri.UriSchemeHttp && healthParsed.Scheme != Uri.UriSchemeHttps))
                    throw new ArgumentException("Health check URL must be an absolute http or https URL.");
            }
        }

        #endregion
    }
}
