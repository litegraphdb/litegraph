namespace Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Server.Classes;
    using LiteGraph.Server.Services;
    using Touchstone.Core;

    public static partial class LiteGraphTouchstoneSuites
    {
        #region Observability-Suite

        private static TestSuiteDescriptor CreateObservabilitySuite()
        {
            return new TestSuiteDescriptor(
                suiteId: "Observability",
                displayName: "v8 REST and MCP Prometheus instrumentation",
                cases: new List<TestCaseDescriptor>
                {
                    Obs("Observability.RouteMappingComplete", "Every request type maps to a metric route label", TestObservabilityRouteMappingComplete),
                    Obs("Observability.RouteMappingUnique", "Request type route labels are unique and low-cardinality", TestObservabilityRouteMappingUnique),
                    Obs("Observability.RestMetricLabels", "REST metrics carry component, route, method, and status class labels", TestObservabilityRestMetricLabels),
                    Obs("Observability.RestErrorCounter", "A REST error response increments the error counter", TestObservabilityRestErrorCounter),
                    Obs("Observability.McpMetricLabels", "MCP metrics expose component=mcp with tool and transport labels", TestObservabilityMcpMetricLabels),
                    Obs("Observability.McpErrorCounter", "A failing MCP tool call increments the MCP error counter", TestObservabilityMcpErrorCounter)
                });
        }

        private static TestCaseDescriptor Obs(string caseId, string displayName, Func<CancellationToken, Task> executeAsync)
        {
            return new TestCaseDescriptor(suiteId: "Observability", caseId: caseId, displayName: displayName, executeAsync: executeAsync);
        }

        #endregion

        #region Observability-Cases

        private static Task TestObservabilityRouteMappingComplete(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Constructing the service runs the startup assertion that every RequestTypeEnum maps to a metric label.
            using (ObservabilityService observability = new ObservabilityService(new ObservabilitySettings()))
            {
                foreach (RequestTypeEnum requestType in (RequestTypeEnum[])Enum.GetValues(typeof(RequestTypeEnum)))
                {
                    string label = ObservabilityService.RouteLabel(requestType);
                    AssertTrue(!String.IsNullOrWhiteSpace(label), "Request type '" + requestType + "' maps to a non-empty route label");
                    AssertTrue(label == label.ToLowerInvariant(), "Route label for '" + requestType + "' is lower-case");
                }
            }

            return Task.CompletedTask;
        }

        private static Task TestObservabilityRouteMappingUnique(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            HashSet<string> labels = new HashSet<string>(StringComparer.Ordinal);
            int count = 0;
            foreach (RequestTypeEnum requestType in (RequestTypeEnum[])Enum.GetValues(typeof(RequestTypeEnum)))
            {
                count++;
                string label = ObservabilityService.RouteLabel(requestType);
                AssertTrue(labels.Add(label), "Route label '" + label + "' for '" + requestType + "' is unique");
            }

            AssertEqual(count, labels.Count, "Every request type has a distinct route label");

            // A representative low-cardinality template name.
            AssertEqual("graph.export.jsonl", ObservabilityService.RouteLabel(RequestTypeEnum.GraphExportJsonl), "Route label derivation is dotted lower-case");
            return Task.CompletedTask;
        }

        private static async Task TestObservabilityRestMetricLabels(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_McpEnvironment == null) throw new InvalidOperationException("MCP environment was not initialized.");

                // Generate a spread of REST traffic across distinct routes.
                await SendRestRequestAsync(HttpMethod.Get, _McpEnvironment.LiteGraphEndpoint + "/", null, cancellationToken).ConfigureAwait(false);
                await SendRestRequestAsync(HttpMethod.Get, _McpEnvironment.LiteGraphEndpoint + "/v1.0/tenants", "litegraphadmin", cancellationToken).ConfigureAwait(false);

                string metrics = await ScrapeAsync(_McpEnvironment.LiteGraphEndpoint + "/metrics", cancellationToken).ConfigureAwait(false);

                AssertTrue(metrics.Contains("litegraph_http_requests_total"), "REST request counter is exposed");
                AssertTrue(metrics.Contains("component=\"rest\""), "REST metrics carry component=rest");
                AssertTrue(metrics.Contains("route=\"tenant.read.all\""), "REST metrics carry the route template label");
                AssertTrue(metrics.Contains("method=\"GET\""), "REST metrics carry the HTTP method label");
                AssertTrue(metrics.Contains("status_class=\"2xx\""), "REST metrics carry the status class label");
                AssertTrue(metrics.Contains("# TYPE litegraph_http_request_duration_ms histogram"), "REST duration histogram is exposed");
                AssertTrue(metrics.Contains("litegraph_http_request_errors_total"), "REST error counter is exposed");
                AssertTrue(metrics.Contains("litegraph_http_requests_in_flight{component=\"rest\"}"), "REST in-flight gauge is exposed");
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        private static async Task TestObservabilityRestErrorCounter(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_McpEnvironment == null) throw new InvalidOperationException("MCP environment was not initialized.");

                // An unauthenticated tenant listing must fail closed with a 4xx status.
                int status = await SendRestRequestAsync(HttpMethod.Get, _McpEnvironment.LiteGraphEndpoint + "/v1.0/tenants", null, cancellationToken).ConfigureAwait(false);
                AssertTrue(status >= 400 && status < 500, "Unauthenticated tenant listing returns a client error status");

                string metrics = await ScrapeAsync(_McpEnvironment.LiteGraphEndpoint + "/metrics", cancellationToken).ConfigureAwait(false);

                double errorCount = SumMetricSeries(metrics, "litegraph_http_request_errors_total", "component=\"rest\"", "status_class=\"4xx\"");
                AssertTrue(errorCount >= 1, "REST error counter increments for a 4xx response");
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        private static async Task TestObservabilityMcpMetricLabels(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_McpEnvironment == null) throw new InvalidOperationException("MCP environment was not initialized.");
                if (_McpClient == null) throw new InvalidOperationException("MCP client was not initialized.");

                // Invoke a read-only tool over the HTTP transport.
                try
                {
                    await _McpClient.CallAsync<string>("tenant/all", new { }).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // Even a tool that returns an error still records a metric labeled with the tool name.
                }

                string metrics = await ScrapeUntilAsync(
                    _McpEnvironment.McpMetricsEndpoint + "/metrics",
                    "tool=\"tenant/all\"",
                    cancellationToken).ConfigureAwait(false);

                AssertTrue(metrics.Contains("litegraph_http_requests_total"), "MCP request counter is exposed");
                AssertTrue(metrics.Contains("component=\"mcp\""), "MCP metrics carry component=mcp");
                AssertTrue(metrics.Contains("transport=\"http\""), "MCP metrics carry the transport label");
                AssertTrue(metrics.Contains("tool=\"tenant/all\""), "MCP metrics carry the tool label");
                AssertTrue(metrics.Contains("# TYPE litegraph_http_request_duration_ms histogram"), "MCP duration histogram is exposed");
                AssertTrue(metrics.Contains("litegraph_http_requests_in_flight{component=\"mcp\""), "MCP in-flight gauge is exposed");
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        private static async Task TestObservabilityMcpErrorCounter(CancellationToken cancellationToken)
        {
            await EnsureMcpEnvironmentAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                if (_McpEnvironment == null) throw new InvalidOperationException("MCP environment was not initialized.");
                if (_McpClient == null) throw new InvalidOperationException("MCP client was not initialized.");

                // Invoking an unknown tool must fail and be recorded as an error.
                try
                {
                    await _McpClient.CallAsync<string>("observability/negative/probe", new { }).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    // Expected: the tool does not exist, so the server returns an error response.
                }

                string metrics = await ScrapeUntilAsync(
                    _McpEnvironment.McpMetricsEndpoint + "/metrics",
                    "litegraph_http_request_errors_total",
                    cancellationToken).ConfigureAwait(false);

                double errorCount = SumMetricSeries(metrics, "litegraph_http_request_errors_total", "component=\"mcp\"", "status_class=\"5xx\"");
                AssertTrue(errorCount >= 1, "MCP error counter increments for a failing tool call");
            }
            finally
            {
                await CleanupMcpServer().ConfigureAwait(false);
            }
        }

        #endregion

        #region Observability-Helpers

        private static async Task<int> SendRestRequestAsync(HttpMethod method, string url, string? bearerToken, CancellationToken cancellationToken)
        {
            using (HttpRequestMessage request = new HttpRequestMessage(method, url))
            {
                if (!String.IsNullOrEmpty(bearerToken)) request.Headers.Add("Authorization", "Bearer " + bearerToken);
                using (HttpResponseMessage response = await _ReadinessClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false))
                {
                    return (int)response.StatusCode;
                }
            }
        }

        private static async Task<string> ScrapeAsync(string url, CancellationToken cancellationToken)
        {
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url))
            using (HttpResponseMessage response = await _ReadinessClient.SendAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false))
            {
                AssertEqual(200, (int)response.StatusCode, "Metrics endpoint returns 200");
                return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        private static async Task<string> ScrapeUntilAsync(string url, string expectedSubstring, CancellationToken cancellationToken)
        {
            string metrics = String.Empty;
            for (int attempt = 0; attempt < 20; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                metrics = await ScrapeAsync(url, cancellationToken).ConfigureAwait(false);
                if (metrics.Contains(expectedSubstring)) return metrics;
                await Task.Delay(250, cancellationToken).ConfigureAwait(false);
            }

            return metrics;
        }

        private static double SumMetricSeries(string metricsText, string metricName, params string[] requiredLabels)
        {
            double total = 0;
            string[] lines = metricsText.Split('\n');
            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line[0] == '#') continue;
                if (!line.StartsWith(metricName + "{", StringComparison.Ordinal)) continue;

                bool matches = true;
                foreach (string label in requiredLabels)
                {
                    if (!line.Contains(label))
                    {
                        matches = false;
                        break;
                    }
                }

                if (!matches) continue;

                int spaceIndex = line.LastIndexOf(' ');
                if (spaceIndex < 0) continue;
                string valueText = line.Substring(spaceIndex + 1);
                if (Double.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out double value)) total += value;
            }

            return total;
        }

        #endregion
    }
}
