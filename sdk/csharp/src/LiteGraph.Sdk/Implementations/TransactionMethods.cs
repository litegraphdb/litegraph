namespace LiteGraph.Sdk.Implementations
{
    using System;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Sdk.Interfaces;
    using RestWrapper;

    /// <summary>
    /// Graph transaction methods.
    /// </summary>
    public class TransactionMethods : ITransactionMethods
    {
        private LiteGraphSdk _Sdk = null;

        /// <summary>
        /// Transaction methods.
        /// </summary>
        /// <param name="sdk">LiteGraph SDK.</param>
        public TransactionMethods(LiteGraphSdk sdk)
        {
            _Sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
        }

        /// <inheritdoc />
        public GraphTransactionBuilder CreateRequestBuilder()
        {
            return new GraphTransactionBuilder();
        }

        /// <inheritdoc />
        public async Task<TransactionResult> Execute(
            Guid tenantGuid,
            Guid graphGuid,
            TransactionRequest request,
            CancellationToken token = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/transaction";

            string json = null;
            if (!Serializer.TrySerializeJson(request, true, out json))
                throw new ArgumentException("Supplied object is not serializable to JSON.");

            using (RestRequest req = new RestRequest(url, HttpMethod.Post))
            {
                req.TimeoutMilliseconds = _Sdk.TimeoutMs;
                req.ContentType = "application/json";
                req.Authorization.BearerToken = _Sdk.BearerToken;

                using (RestResponse resp = await req.SendAsync(json, token).ConfigureAwait(false))
                {
                    if (resp != null)
                    {
                        if ((resp.StatusCode >= 200 && resp.StatusCode <= 299) || resp.StatusCode == 400 || resp.StatusCode == 409)
                        {
                            _Sdk.Log(SeverityEnum.Debug, "transaction result reported from " + url + ": " + resp.StatusCode + ", " + resp.ContentLength + " bytes");

                            if (!String.IsNullOrEmpty(resp.DataAsString))
                            {
                                return Serializer.DeserializeJson<TransactionResult>(resp.DataAsString);
                            }

                            return null;
                        }

                        _Sdk.Log(SeverityEnum.Warn, "non-success reported from " + url + ": " + resp.StatusCode + ", " + resp.ContentLength + " bytes");
                        return null;
                    }

                    _Sdk.Log(SeverityEnum.Warn, "no response from " + url);
                    return null;
                }
            }
        }
    }
}
