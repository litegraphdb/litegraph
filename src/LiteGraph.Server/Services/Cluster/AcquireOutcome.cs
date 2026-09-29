namespace LiteGraph.Server.Services.Cluster
{
    /// <summary>
    /// Result of one acquisition attempt against Clutch: a handle when granted, otherwise the denial reason.
    /// </summary>
    internal sealed class AcquireOutcome
    {
        internal ClutchLockHandle Handle { get; }

        internal string Reason { get; }

        internal AcquireOutcome(ClutchLockHandle handle, string reason)
        {
            Handle = handle;
            Reason = reason;
        }
    }
}
