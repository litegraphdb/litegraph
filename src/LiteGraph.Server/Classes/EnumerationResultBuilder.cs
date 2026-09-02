namespace LiteGraph.Server.Classes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using LiteGraph;

    /// <summary>
    /// Helper that wraps fully-materialized lists in the standard enumeration result envelope.
    /// Used for computed or sub-scoped result sets that do not have a database-backed enumeration,
    /// applying skip and max-results slicing over the full list.  Continuation tokens are not
    /// produced by this helper; callers page using skip.
    /// </summary>
    internal static class EnumerationResultBuilder
    {
        #region Public-Members

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        /// <summary>
        /// Build an enumeration result from a fully-materialized list.
        /// </summary>
        /// <typeparam name="T">Object type.</typeparam>
        /// <param name="objects">Full list of matching objects.  Null is treated as an empty list.</param>
        /// <param name="skip">Number of records to skip.  Minimum is zero.  Default is zero.</param>
        /// <param name="maxResults">Maximum number of results to return.  Minimum is one.  Default is one thousand.</param>
        /// <returns>Enumeration result where TotalRecords reflects the full list count and Objects contains the requested page.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when skip is negative or maxResults is less than one.</exception>
        internal static EnumerationResult<T> FromList<T>(List<T> objects, int skip = 0, int maxResults = 1000)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            if (maxResults < 1) throw new ArgumentOutOfRangeException(nameof(maxResults));
            if (objects == null) objects = new List<T>();

            EnumerationResult<T> ret = new EnumerationResult<T>
            {
                MaxResults = maxResults
            };

            ret.TotalRecords = objects.Count;
            ret.Objects = objects.Skip(skip).Take(maxResults).ToList();
            ret.RecordsRemaining = Math.Max(0, objects.Count - skip - ret.Objects.Count);
            ret.EndOfResults = (ret.RecordsRemaining < 1);
            ret.ContinuationToken = null;
            ret.Timestamp.End = DateTime.UtcNow;
            return ret;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
