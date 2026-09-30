namespace LiteGraph.Coordination
{
    using System;

    /// <summary>
    /// Catalogue of lock keys used by LiteGraph.  Distributed providers prefix these with the cluster name.
    /// </summary>
    public static class LockKeys
    {
        /// <summary>
        /// Schema creation and migrations.
        /// </summary>
        public static string Schema = "schema";

        /// <summary>
        /// Server settings file writes.
        /// </summary>
        public static string Settings = "settings";

        /// <summary>
        /// Rolling restart coordination.
        /// </summary>
        public static string Restart = "restart";

        /// <summary>
        /// Singleton background job.
        /// </summary>
        /// <param name="jobName">Job name.</param>
        /// <returns>Key.</returns>
        public static string Job(string jobName)
        {
            if (String.IsNullOrEmpty(jobName)) throw new ArgumentNullException(nameof(jobName));
            return "job/" + jobName;
        }

        /// <summary>
        /// Vector index build for a dimensionality.
        /// </summary>
        /// <param name="dimensions">Dimensions.</param>
        /// <returns>Key.</returns>
        public static string VectorIndex(int dimensions)
        {
            if (dimensions < 1) throw new ArgumentOutOfRangeException(nameof(dimensions));
            return "vectorindex/cosine/" + dimensions;
        }

        /// <summary>
        /// Key class (first segment) used as a low-cardinality metric label.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>Key class.</returns>
        public static string KeyClass(string key)
        {
            if (String.IsNullOrEmpty(key)) return "unknown";
            int slash = key.IndexOf('/');
            return slash < 0 ? key : key.Substring(0, slash);
        }
    }
}
