namespace LiteGraph.Server.Classes
{
    using System;
    using System.Text.Json;
    using System.Text.Json.Serialization.Metadata;
    using WatsonWebserver.Core.Settings;

    /// <summary>
    /// Supplies metadata backed by a hand-written converter for the server types whose generated metadata must not be used:
    /// Watson's <see cref="SslSettings"/>, written by <see cref="SslSettingsJsonConverter"/> so the certificate object is
    /// never read or serialized. Consulted before <see cref="LiteGraphServerJsonContext"/>, and works with any serializer
    /// options (a converter declared on the context itself applies only to the context's own options).
    /// Thread safety: stateless and thread-safe.
    /// </summary>
    internal sealed class ServerConverterTypeInfoResolver : IJsonTypeInfoResolver
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private static readonly SslSettingsJsonConverter _SslSettingsConverter = new SslSettingsJsonConverter();

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        /// <summary>
        /// Get metadata for a type handled by a converter.
        /// </summary>
        /// <param name="type">Type.</param>
        /// <param name="options">Serializer options.</param>
        /// <returns>Metadata, or null when the type is not handled here.</returns>
        public JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            if (type == typeof(SslSettings)) return JsonMetadataServices.CreateValueInfo<SslSettings>(options, _SslSettingsConverter);
            return null;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
