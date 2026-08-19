namespace LiteGraph
{
    using System;

    /// <summary>
    /// Exception raised when a JSONL record line cannot be parsed or has an unrecognized type.
    /// </summary>
    public class JsonlFormatException : Exception
    {
        #region Public-Members

        /// <summary>
        /// One-based line number of the offending line within the input.
        /// </summary>
        public long LineNumber { get; set; } = 0;

        /// <summary>
        /// The offending line content.
        /// </summary>
        public string Line { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        /// <param name="lineNumber">One-based line number.</param>
        /// <param name="line">Offending line content.</param>
        /// <param name="message">Message.</param>
        /// <param name="inner">Inner exception.</param>
        public JsonlFormatException(long lineNumber, string line, string message, Exception inner = null)
            : base(message, inner)
        {
            LineNumber = lineNumber;
            Line = line;
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
