namespace LiteGraph.Serialization
{
    using System;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Net;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Text.Json.Serialization.Metadata;

    /// <summary>
    /// Supplies JSON metadata for root-level values that LiteGraph handles entirely with a custom converter
    /// (exceptions, name-value collections, IP addresses), so they serialize without reflection.
    /// The converter is taken from the options the metadata is requested for.
    /// Thread safety: stateless and safe to use from multiple threads.
    /// </summary>
    internal class ConverterTypeInfoResolver : IJsonTypeInfoResolver
    {
        #region Public-Members

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ConverterTypeInfoResolver()
        {
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Get metadata for a type, or null when the type is not one this resolver handles.
        /// </summary>
        /// <param name="type">Type.</param>
        /// <param name="options">Options that hold the converters.</param>
        /// <returns>Type metadata, or null.</returns>
        public JsonTypeInfo GetTypeInfo(Type type, JsonSerializerOptions options)
        {
            if (type == null || options == null) return null;
            if (type == typeof(Exception)) return Create<Exception>(options);
            if (type == typeof(NameValueCollection)) return Create<NameValueCollection>(options);
            if (type == typeof(IPAddress)) return Create<IPAddress>(options);
            return null;
        }

        #endregion

        #region Private-Methods

        private static JsonTypeInfo Create<T>(JsonSerializerOptions options)
        {
            JsonConverter<T> converter = options.Converters.OfType<JsonConverter<T>>().FirstOrDefault();
            if (converter == null) return null;
            return JsonMetadataServices.CreateValueInfo<T>(options, converter);
        }

        #endregion
    }
}
