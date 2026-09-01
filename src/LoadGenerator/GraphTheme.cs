namespace LoadGenerator
{
    using System;

    /// <summary>
    /// Template describing a themed synthetic graph: its name, node vocabulary, and edge relationship types.
    /// </summary>
    public class GraphTheme
    {
        #region Public-Members

        /// <summary>
        /// Graph display name, for example 'Production Infrastructure'.
        /// </summary>
        public string GraphName { get; set; } = "Untitled Graph";

        /// <summary>
        /// Short theme label attached to graphs and nodes, for example 'infrastructure'.
        /// </summary>
        public string ThemeLabel { get; set; } = "generic";

        /// <summary>
        /// Pool of node name stems, combined with suffixes to produce node names.
        /// </summary>
        public string[] NodeNameStems { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Pool of node type labels, for example 'server' or 'article'.
        /// </summary>
        public string[] NodeTypes { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Pool of edge relationship names, for example 'DEPENDS_ON'.
        /// </summary>
        public string[] EdgeTypes { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Pool of environment or region qualifiers used in node data payloads.
        /// </summary>
        public string[] Qualifiers { get; set; } = Array.Empty<string>();

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public GraphTheme()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
