namespace LiteGraph.Server.Classes
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using WatsonWebserver.Core;

    /// <summary>
    /// Write-only stream adapter that forwards writes to a Watson response using chunked transfer-encoding.
    /// Each write becomes a non-final chunk; the caller is responsible for sending the final chunk after use.
    /// </summary>
    public class ChunkedResponseStream : Stream
    {
        #region Public-Members

        /// <inheritdoc />
        public override bool CanRead
        {
            get { return false; }
        }

        /// <inheritdoc />
        public override bool CanSeek
        {
            get { return false; }
        }

        /// <inheritdoc />
        public override bool CanWrite
        {
            get { return true; }
        }

        /// <inheritdoc />
        public override long Length
        {
            get { throw new NotSupportedException(); }
        }

        /// <inheritdoc />
        public override long Position
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        #endregion

        #region Private-Members

        private readonly HttpResponseBase _Response;
        private readonly CancellationToken _Token;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="response">Watson HTTP response with chunked transfer already enabled.</param>
        /// <param name="token">Cancellation token.</param>
        /// <exception cref="ArgumentNullException">Thrown when the response is null.</exception>
        public ChunkedResponseStream(HttpResponseBase response, CancellationToken token = default)
        {
            _Response = response ?? throw new ArgumentNullException(nameof(response));
            _Token = token;
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public override void Flush()
        {
        }

        /// <inheritdoc />
        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count)
        {
            WriteAsync(buffer, offset, count, _Token).GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (count <= 0) return;

            byte[] chunk;
            if (offset == 0 && count == buffer.Length)
            {
                chunk = buffer;
            }
            else
            {
                chunk = new byte[count];
                Buffer.BlockCopy(buffer, offset, chunk, 0, count);
            }

            await _Response.SendChunk(chunk, false, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
