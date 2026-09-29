namespace LiteGraph.GraphRepositories
{
    using System;
    using System.Text;

    /// <summary>
    /// Builds deterministic ORDER BY clauses and matching continuation (marker) predicates for enumeration queries.
    /// Every ordering must end with a unique column (the row GUID) so that rows sharing a sort value, such as objects
    /// created in the same microsecond, are returned in a stable order. Without a unique final key, databases may return
    /// tied rows in a different order on each query, so LIMIT/OFFSET pages and marker-based continuation can skip or
    /// repeat rows.
    /// Thread safety: all methods are stateless and safe to call concurrently.
    /// </summary>
    internal static class KeysetOrdering
    {
        #region Public-Members

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        /// <summary>
        /// Build an ORDER BY clause over the supplied columns, all in the same direction.
        /// </summary>
        /// <param name="descending">True to sort descending, false to sort ascending.</param>
        /// <param name="columns">Sort columns, most significant first. The last column must be unique.</param>
        /// <returns>ORDER BY clause with a trailing space.</returns>
        /// <exception cref="ArgumentException">Thrown when no columns are supplied.</exception>
        internal static string OrderBy(bool descending, params string[] columns)
        {
            if (columns == null || columns.Length < 1) throw new ArgumentException("At least one sort column is required.", nameof(columns));

            string direction = descending ? " DESC" : " ASC";
            StringBuilder sb = new StringBuilder("ORDER BY ");
            for (int i = 0; i < columns.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(columns[i]).Append(direction);
            }

            return sb.Append(' ').ToString();
        }

        /// <summary>
        /// Build a predicate selecting the rows that follow a marker row in the ordering produced by
        /// <see cref="OrderBy(bool, string[])"/> with the same direction and columns. For columns (a, b) this yields
        /// ((a &gt; va) OR (a = va AND b &gt; vb)) for ascending order, and the same with &lt; for descending order.
        /// </summary>
        /// <param name="descending">True if the ordering is descending, false if ascending.</param>
        /// <param name="columns">Sort columns, most significant first, matching the ORDER BY clause.</param>
        /// <param name="literals">SQL literals holding the marker row's value for each column, already quoted and sanitized.</param>
        /// <returns>Parenthesized predicate.</returns>
        /// <exception cref="ArgumentException">Thrown when no columns are supplied or the column and literal counts differ.</exception>
        internal static string After(bool descending, string[] columns, string[] literals)
        {
            if (columns == null || columns.Length < 1) throw new ArgumentException("At least one sort column is required.", nameof(columns));
            if (literals == null || literals.Length != columns.Length) throw new ArgumentException("A marker literal is required for each sort column.", nameof(literals));

            string comparison = descending ? " < " : " > ";
            StringBuilder sb = new StringBuilder("(");
            for (int i = 0; i < columns.Length; i++)
            {
                if (i > 0) sb.Append(" OR ");
                sb.Append('(');
                for (int j = 0; j < i; j++)
                {
                    sb.Append(columns[j]).Append(" = ").Append(literals[j]).Append(" AND ");
                }

                sb.Append(columns[i]).Append(comparison).Append(literals[i]).Append(')');
            }

            return sb.Append(')').ToString();
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
