namespace LiteGraph.Coordination
{
    using System;

    /// <summary>
    /// Thrown when a lock provider cannot currently grant locks, for example because the distributed lock service is unreachable.
    /// </summary>
    public class LockProviderUnavailableException : Exception
    {
        /// <summary>
        /// Provider name.
        /// </summary>
        public string Provider { get; }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="provider">Provider name.</param>
        /// <param name="message">Message.</param>
        /// <param name="inner">Inner exception.</param>
        public LockProviderUnavailableException(string provider, string message, Exception inner = null)
            : base(message, inner)
        {
            Provider = provider;
        }
    }
}
