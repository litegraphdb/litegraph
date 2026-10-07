namespace LiteGraph.Server.Classes
{
    using System.Text.Json.Serialization.Metadata;
    using System.Threading;
    using LiteGraph.Serialization;

    /// <summary>
    /// JSON metadata for LiteGraph.Server.
    /// <see cref="Register"/> adds the server's source-generated metadata to the LiteGraph serializer, so the server needs
    /// no reflection-based serialization and runs the same under the JIT and Native AOT. The server calls it at startup;
    /// code that uses server types in another process (tests, tools) calls it before serializing them.
    /// Thread safety: <see cref="Register"/> is thread-safe and registers once however often it is called.
    /// </summary>
    public static class ServerJson
    {
        #region Public-Members

        /// <summary>
        /// Resolver with the server's metadata.
        /// </summary>
        public static IJsonTypeInfoResolver Resolver
        {
            get
            {
                return _Resolver;
            }
        }

        #endregion

        #region Private-Members

        private static readonly IJsonTypeInfoResolver _Resolver = JsonTypeInfoResolver.Combine(
            new ServerConverterTypeInfoResolver(),
            LiteGraphServerJsonContext.Default);
        private static int _Registered = 0;

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        /// <summary>
        /// Add the server's metadata to the LiteGraph serializer (see <see cref="Serializer.AddTypeInfoResolver"/>).
        /// Calls after the first do nothing.
        /// </summary>
        public static void Register()
        {
            if (Interlocked.Exchange(ref _Registered, 1) == 1) return;
            Serializer.AddTypeInfoResolver(_Resolver);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
