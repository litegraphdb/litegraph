namespace LiteGraph.Jsonl
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Serialization;

    /// <summary>
    /// Reads the LiteGraph JSONL interchange format from a stream or string.
    /// The reader streams line by line and never buffers the whole input.
    /// </summary>
    public class JsonlGraphReader
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private Serializer _Serializer = new Serializer();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="serializer">Serializer.  When null, a default serializer is used.</param>
        public JsonlGraphReader(Serializer serializer = null)
        {
            if (serializer != null) _Serializer = serializer;
            else _Serializer = new Serializer();
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Parse a single line into a record.  Comment lines (beginning with '#') and blank lines return null.
        /// </summary>
        /// <param name="line">Line content.</param>
        /// <param name="lineNumber">One-based line number, used for error reporting.</param>
        /// <returns>Record, or null if the line is a comment or blank.</returns>
        /// <exception cref="JsonlFormatException">Thrown when the line is not a valid record.</exception>
        public JsonlRecord ParseLine(string line, long lineNumber)
        {
            if (line == null) return null;
            if (String.IsNullOrWhiteSpace(line)) return null;
            if (line[0] == '#') return null;

            JsonlRecord record;
            try
            {
                record = _Serializer.DeserializeJson<JsonlRecord>(line);
            }
            catch (Exception e)
            {
                throw new JsonlFormatException(lineNumber, line, "Unable to parse JSONL record on line " + lineNumber + ": " + e.Message, e);
            }

            if (record == null)
                throw new JsonlFormatException(lineNumber, line, "JSONL record on line " + lineNumber + " deserialized to null.");
            if (record.Object == null)
                throw new JsonlFormatException(lineNumber, line, "JSONL record on line " + lineNumber + " has no object payload.");

            return record;
        }

        /// <summary>
        /// Read records from a stream, skipping comment and blank lines.
        /// </summary>
        /// <param name="stream">Source stream.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Async enumerable of records.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the stream is null.</exception>
        /// <exception cref="JsonlFormatException">Thrown when a record line is malformed.</exception>
        public async IAsyncEnumerable<JsonlRecord> ReadAsync(
            Stream stream,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8, true, 65536, true))
            {
                long lineNumber = 0;
                string line;
                while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) != null)
                {
                    token.ThrowIfCancellationRequested();
                    lineNumber++;
                    JsonlRecord record = ParseLine(line, lineNumber);
                    if (record != null) yield return record;
                }
            }
        }

        /// <summary>
        /// Read records from a string, skipping comment and blank lines.
        /// </summary>
        /// <param name="content">JSONL content.</param>
        /// <returns>Enumerable of records.</returns>
        /// <exception cref="ArgumentNullException">Thrown when content is null.</exception>
        /// <exception cref="JsonlFormatException">Thrown when a record line is malformed.</exception>
        public IEnumerable<JsonlRecord> Read(string content)
        {
            if (content == null) throw new ArgumentNullException(nameof(content));

            long lineNumber = 0;
            using (StringReader reader = new StringReader(content))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    JsonlRecord record = ParseLine(line, lineNumber);
                    if (record != null) yield return record;
                }
            }
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
