namespace LiteGraph.Server.Classes
{
    using System;

    /// <summary>
    /// Constants.
    /// </summary>
    internal static class Constants
    {
        /// <summary>
        /// Logo.
        /// </summary>
        public static string Logo =
            @"  _ _ _                          _     " + Environment.NewLine +
            @" | (_) |_ ___ __ _ _ _ __ _ _ __| |_   " + Environment.NewLine +
            @" | | |  _/ -_) _` | '_/ _` | '_ \ ' \  " + Environment.NewLine +
            @" |_|_|\__\___\__, |_| \__,_| .__/_||_| " + Environment.NewLine +
            @"             |___/         |_|         " + Environment.NewLine;

        /// <summary>
        /// Product name.
        /// </summary>
        public static string ProductName = " LiteGraph Server";

        /// <summary>
        /// Copyright.
        /// </summary>
        public static string Copyright = " (c)2025 Joel Christner";

        /// <summary>
        /// Settings file.
        /// </summary>
        public static string SettingsFile = "./litegraph.json";

        /// <summary>
        /// Log file directory.
        /// </summary>
        public static string LogDirectory = "./logs/";

        /// <summary>
        /// Log filename.
        /// </summary>
        public static string LogFilename = "litegraph.log";

        #region Environment-Variables

        /// <summary>
        /// Webserver port environment variable.
        /// </summary>
        public static string WebserverPortEnvironmentVariable = "LITEGRAPH_PORT";

        /// <summary>
        /// Data database filename environment variable.
        /// </summary>
        public static string DatabaseFilenameEnvironmentVariable = "LITEGRAPH_DB";

        /// <summary>
        /// Database provider type environment variable.
        /// </summary>
        public static string DatabaseTypeEnvironmentVariable = "LITEGRAPH_DB_TYPE";

        /// <summary>
        /// Database filename environment variable.
        /// </summary>
        public static string DatabaseFilenameEnvironmentVariableAlternate = "LITEGRAPH_DB_FILENAME";

        /// <summary>
        /// Database hostname environment variable.
        /// </summary>
        public static string DatabaseHostnameEnvironmentVariable = "LITEGRAPH_DB_HOST";

        /// <summary>
        /// Database port environment variable.
        /// </summary>
        public static string DatabasePortEnvironmentVariable = "LITEGRAPH_DB_PORT";

        /// <summary>
        /// Database name environment variable.
        /// </summary>
        public static string DatabaseNameEnvironmentVariable = "LITEGRAPH_DB_NAME";

        /// <summary>
        /// Database username environment variable.
        /// </summary>
        public static string DatabaseUsernameEnvironmentVariable = "LITEGRAPH_DB_USERNAME";

        /// <summary>
        /// Database password environment variable.
        /// </summary>
        public static string DatabasePasswordEnvironmentVariable = "LITEGRAPH_DB_PASSWORD";

        /// <summary>
        /// Database schema environment variable.
        /// </summary>
        public static string DatabaseSchemaEnvironmentVariable = "LITEGRAPH_DB_SCHEMA";

        /// <summary>
        /// Database connection string environment variable.
        /// </summary>
        public static string DatabaseConnectionStringEnvironmentVariable = "LITEGRAPH_DB_CONNECTION_STRING";

        /// <summary>
        /// Database maximum connection count environment variable.
        /// </summary>
        public static string DatabaseMaxConnectionsEnvironmentVariable = "LITEGRAPH_DB_MAX_CONNECTIONS";

        /// <summary>
        /// Database command timeout environment variable.
        /// </summary>
        public static string DatabaseCommandTimeoutEnvironmentVariable = "LITEGRAPH_DB_COMMAND_TIMEOUT_SECONDS";

        /// <summary>
        /// REST request timeout environment variable.
        /// </summary>
        public static string RequestTimeoutEnvironmentVariable = "LITEGRAPH_REQUEST_TIMEOUT_SECONDS";

        /// <summary>
        /// Maximum graph transaction operation count environment variable.
        /// </summary>
        public static string TransactionMaxOperationsEnvironmentVariable = "LITEGRAPH_TRANSACTION_MAX_OPERATIONS";

        /// <summary>
        /// Maximum graph transaction timeout environment variable.
        /// </summary>
        public static string TransactionMaxTimeoutEnvironmentVariable = "LITEGRAPH_TRANSACTION_MAX_TIMEOUT_SECONDS";

        /// <summary>
        /// Enable built-in OTLP exporter environment variable.
        /// </summary>
        public static string OtlpExporterEnableEnvironmentVariable = "LITEGRAPH_OTLP_ENABLE";

        /// <summary>
        /// LiteGraph-specific OTLP endpoint environment variable.
        /// </summary>
        public static string OtlpEndpointEnvironmentVariable = "LITEGRAPH_OTLP_ENDPOINT";

        /// <summary>
        /// LiteGraph-specific OTLP protocol environment variable.
        /// </summary>
        public static string OtlpProtocolEnvironmentVariable = "LITEGRAPH_OTLP_PROTOCOL";

        /// <summary>
        /// LiteGraph-specific OTLP headers environment variable.
        /// </summary>
        public static string OtlpHeadersEnvironmentVariable = "LITEGRAPH_OTLP_HEADERS";

        /// <summary>
        /// LiteGraph-specific OTLP timeout environment variable.
        /// </summary>
        public static string OtlpTimeoutEnvironmentVariable = "LITEGRAPH_OTLP_TIMEOUT_MILLISECONDS";

        /// <summary>
        /// Standard OpenTelemetry service name environment variable.
        /// </summary>
        public static string OTelServiceNameEnvironmentVariable = "OTEL_SERVICE_NAME";

        /// <summary>
        /// Standard OpenTelemetry OTLP endpoint environment variable.
        /// </summary>
        public static string OTelOtlpEndpointEnvironmentVariable = "OTEL_EXPORTER_OTLP_ENDPOINT";

        /// <summary>
        /// Standard OpenTelemetry OTLP protocol environment variable.
        /// </summary>
        public static string OTelOtlpProtocolEnvironmentVariable = "OTEL_EXPORTER_OTLP_PROTOCOL";

        /// <summary>
        /// Standard OpenTelemetry OTLP headers environment variable.
        /// </summary>
        public static string OTelOtlpHeadersEnvironmentVariable = "OTEL_EXPORTER_OTLP_HEADERS";

        /// <summary>
        /// Standard OpenTelemetry OTLP timeout environment variable.
        /// </summary>
        public static string OTelOtlpTimeoutEnvironmentVariable = "OTEL_EXPORTER_OTLP_TIMEOUT";

        #endregion

        #region Content-Types

        /// <summary>
        /// Content-type value for XML.
        /// </summary>
        public static string XmlContentType = "application/xml";

        /// <summary>
        /// Content-type value for JSON.
        /// </summary>
        public static string JsonContentType = "application/json";

        /// <summary>
        /// Content-type value for HTML.
        /// </summary>
        public static string HtmlContentType = "text/html";

        /// <summary>
        /// Favicon content type.
        /// </summary>
        public static string FaviconContentType = "image/x-icon";

        #endregion

        #region HTML

        /// <summary>
        /// Default homepage contents.
        /// </summary>
        public static string DefaultHomepage =
            "<html>"
            + "<head><title>LiteGraph</title></head>"
            + "<body>"
            + "<div><pre>" + Logo + Environment.NewLine
            + " Your LiteGraph node is operational</p></div>"
            + "</body>"
            + "</html>";

        /// <summary>
        /// Favicon file.
        /// </summary>
        public static string FaviconFile = "./assets/favicon.ico";

        #endregion

        #region Headers

        /// <summary>
        /// Hostname header key.
        /// </summary>
        public static string HostnameHeader = "x-hostname";

        /// <summary>
        /// Request ID header.
        /// </summary>
        public static string RequestIdHeader = "x-request-id";

        /// <summary>
        /// Correlation ID header.
        /// </summary>
        public static string CorrelationIdHeader = "x-correlation-id";

        /// <summary>
        /// W3C trace context header.
        /// </summary>
        public static string TraceparentHeader = "traceparent";

        /// <summary>
        /// W3C trace state header.
        /// </summary>
        public static string TracestateHeader = "tracestate";

        /// <summary>
        /// Authorization header.
        /// </summary>
        public static string AuthorizationHeader = "Authorization";

        /// <summary>
        /// Email header.
        /// </summary>
        public static string EmailHeader = "x-email";

        /// <summary>
        /// Password header.
        /// </summary>
        public static string PasswordHeader = "x-password";

        /// <summary>
        /// Tenant GUID header.
        /// </summary>
        public static string TenantGuidHeader = "x-tenant-guid";

        /// <summary>
        /// Token header.
        /// </summary>
        public static string TokenHeader = "x-token";

        #endregion

        #region Querystring

        /// <summary>
        /// Enumeration order querystring.
        /// </summary>
        public static string EnumerationOrderQuerystring = "order";

        /// <summary>
        /// Skip querystring key.
        /// </summary>
        public static string SkipQuerystring = "skip";

        /// <summary>
        /// Max-keys querystring key.
        /// </summary>
        public static string MaxKeysQuerystring = "max-keys";

        /// <summary>
        /// Alternate max-keys querystring key.
        /// </summary>
        public static string MaxKeysQuerystringAlternate = "maxKeys";

        /// <summary>
        /// Force querystring key.
        /// </summary>
        public static string ForceQuerystring = "force";

        /// <summary>
        /// Include data querystring key.
        /// </summary>
        public static string IncludeDataQuerystring = "incldata";

        /// <summary>
        /// Include subordinates querystring key.
        /// </summary>
        public static string IncludeSubordinatesQuerystring = "inclsub";

        /// <summary>
        /// Bulk create return mode querystring key.
        /// </summary>
        public static string ReturnQuerystring = "return";

        /// <summary>
        /// Full bulk create return mode querystring value.
        /// </summary>
        public static string FullReturnValue = "full";

        /// <summary>
        /// Minimal bulk create return mode querystring value.
        /// </summary>
        public static string MinimalReturnValue = "minimal";

        /// <summary>
        /// From GUID querystring key.
        /// </summary>
        public static string FromGuidQuerystring = "from";

        /// <summary>
        /// To GUID querystring key.
        /// </summary>
        public static string ToGuidQuerystring = "to";

        /// <summary>
        /// Continuation token querystring key.
        /// </summary>
        public static string ContinuationTokenQuerystring = "token";

        /// <summary>
        /// GUIDs querystring key.
        /// </summary>
        public static string GuidsQuerystring = "guids";

        /// <summary>
        /// MaxDepth querystring key.
        /// </summary>
        public static string MaxDepth = "maxDepth";

        /// <summary>
        /// MaxNodes querystring key.
        /// </summary>
        public static string MaxNodes = "maxNodes";

        /// <summary>
        /// MaxEdges querystring key.
        /// </summary>
        public static string MaxEdges = "maxEdges";

        #endregion
    }
}
