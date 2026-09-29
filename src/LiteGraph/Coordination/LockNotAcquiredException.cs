namespace LiteGraph.Coordination
{
    using System;

    /// <summary>
    /// Thrown when a lock is not granted within the allowed time.
    /// </summary>
    public class LockNotAcquiredException : Exception
    {
        /// <summary>
        /// Lock key.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Requested mode.
        /// </summary>
        public LockModeEnum Mode { get; }

        /// <summary>
        /// Reason, for example Denied or Timeout.
        /// </summary>
        public string Reason { get; }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="key">Lock key.</param>
        /// <param name="mode">Requested mode.</param>
        /// <param name="reason">Reason.</param>
        public LockNotAcquiredException(string key, LockModeEnum mode, string reason)
            : base("Lock '" + key + "' (" + mode + ") was not acquired: " + reason + ".")
        {
            Key = key;
            Mode = mode;
            Reason = reason;
        }
    }
}
