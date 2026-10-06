namespace LiteGraph.Coordination
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Lock mode.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter<LockModeEnum>))]
    public enum LockModeEnum
    {
        /// <summary>
        /// Shared read lock.  Compatible with other read locks.
        /// </summary>
        Read,
        /// <summary>
        /// Write lock.  Excludes other writers.
        /// </summary>
        Write,
        /// <summary>
        /// Fully exclusive lock.  Excludes every other holder.
        /// </summary>
        Exclusive
    }
}
