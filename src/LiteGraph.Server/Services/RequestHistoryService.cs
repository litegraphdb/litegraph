namespace LiteGraph.Server.Services
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Text;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;
    using LiteGraph;
    using LiteGraph.GraphRepositories;
    using LiteGraph.Server.Classes;
    using SyslogLogging;
    using WatsonWebserver.Core;

    /// <summary>
    /// Service responsible for persisting request history records and running the retention purge loop.
    /// </summary>
    public class RequestHistoryService : IDisposable
    {
        #region Public-Members

        /// <summary>
        /// Redacted value used in place of sensitive header contents.
        /// </summary>
        public const string RedactedValue = "***";

        #endregion

        #region Private-Members

        private readonly string _Header = "[RequestHistoryService] ";
        private const int CaptureQueueCapacity = 2048;
        private readonly Settings _Settings;
        private readonly LoggingModule _Logging;
        private readonly GraphRepositoryBase _Repo;

        private readonly HashSet<string> _SensitiveHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "authorization",
            "x-password",
            "x-token",
            "x-api-key",
            "cookie",
            "set-cookie"
        };

        private readonly HashSet<string> _SkippedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "/favicon.ico"
        };

        private readonly Channel<RequestHistoryDetail> _CaptureChannel;
        private readonly CancellationTokenSource _CaptureCts = new CancellationTokenSource();
        private readonly CancellationTokenSource _PurgeCts = new CancellationTokenSource();
        private Task _CaptureTask;
        private Task _PurgeTask;
        private long _DroppedCaptures = 0;
        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="settings">Settings.</param>
        /// <param name="logging">Logging.</param>
        /// <param name="repo">Repository.</param>
        public RequestHistoryService(Settings settings, LoggingModule logging, GraphRepositoryBase repo)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));

            _CaptureChannel = Channel.CreateBounded<RequestHistoryDetail>(new BoundedChannelOptions(CaptureQueueCapacity)
            {
                AllowSynchronousContinuations = false,
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = true,
                SingleWriter = false
            });

            _CaptureTask = Task.Run(() => ProcessCaptureQueueAsync(_CaptureCts.Token));
            _PurgeTask = Task.Run(() => PurgeLoopAsync(_PurgeCts.Token));
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Fire-and-forget capture of a completed request.  Failures are logged and swallowed so
        /// the capture never interferes with the response path.
        /// </summary>
        /// <param name="detail">Detail to persist.</param>
        public void Capture(RequestHistoryDetail detail)
        {
            if (!_Settings.RequestHistory.Enable) return;
            if (detail == null) return;

            if (!_CaptureChannel.Writer.TryWrite(detail))
            {
                long dropped = Interlocked.Increment(ref _DroppedCaptures);
                if (dropped == 1 || dropped % 100 == 0)
                {
                    _Logging.Warn(_Header + "capture queue full, dropped " + dropped + " request history item(s)");
                }
            }
        }

        /// <summary>
        /// Returns true if the request should be skipped (not captured at all).
        /// </summary>
        /// <param name="path">Request path.</param>
        /// <returns>True if the path should be skipped.</returns>
        public bool ShouldSkip(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return _SkippedPaths.Contains(path);
        }

        /// <summary>
        /// Redact a headers collection into a dictionary suitable for persistence.
        /// </summary>
        /// <param name="headers">Headers.</param>
        /// <returns>Dictionary.</returns>
        public Dictionary<string, string> RedactHeaders(NameValueCollection headers)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (headers == null) return ret;

            foreach (string key in headers.AllKeys)
            {
                if (string.IsNullOrEmpty(key)) continue;
                if (_SensitiveHeaders.Contains(key))
                    ret[key] = RedactedValue;
                else
                    ret[key] = headers[key];
            }
            return ret;
        }

        /// <summary>
        /// Redact a header string dictionary directly.
        /// </summary>
        /// <param name="headers">Headers.</param>
        /// <returns>Redacted copy.</returns>
        public Dictionary<string, string> RedactHeaders(IDictionary<string, string> headers)
        {
            Dictionary<string, string> ret = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (headers == null) return ret;

            foreach (KeyValuePair<string, string> kvp in headers)
            {
                if (string.IsNullOrEmpty(kvp.Key)) continue;
                if (_SensitiveHeaders.Contains(kvp.Key))
                    ret[kvp.Key] = RedactedValue;
                else
                    ret[kvp.Key] = kvp.Value;
            }
            return ret;
        }

        /// <summary>
        /// Capture bytes as a body string with truncation enforcement.
        /// </summary>
        /// <param name="bytes">Raw bytes.</param>
        /// <param name="limit">Maximum bytes to keep.</param>
        /// <param name="truncated">Output: whether truncation occurred.</param>
        /// <returns>Body string.</returns>
        public string CaptureBody(byte[] bytes, int limit, out bool truncated)
        {
            truncated = false;
            if (bytes == null || bytes.Length == 0) return null;
            if (limit <= 0)
            {
                truncated = true;
                return null;
            }

            if (bytes.Length > limit)
            {
                truncated = true;
                return SafeDecode(bytes, limit);
            }

            return SafeDecode(bytes);
        }

        /// <summary>
        /// Capture string content as a body string with truncation enforcement based on UTF-8 byte length.
        /// </summary>
        /// <param name="body">Body string.</param>
        /// <param name="limit">Maximum bytes to keep.</param>
        /// <param name="truncated">Output: whether truncation occurred.</param>
        /// <param name="byteLength">Output: UTF-8 byte length.</param>
        /// <returns>Body string.</returns>
        public string CaptureBody(string body, int limit, out bool truncated, out long byteLength)
        {
            truncated = false;
            byteLength = 0;
            if (string.IsNullOrEmpty(body)) return null;

            byteLength = Encoding.UTF8.GetByteCount(body);
            if (limit <= 0)
            {
                truncated = true;
                return null;
            }

            if (byteLength <= limit) return body;

            truncated = true;
            byte[] bytes = Encoding.UTF8.GetBytes(body);
            return SafeDecode(bytes, limit);
        }

        /// <summary>
        /// Dispose of resources.
        /// </summary>
        public void Dispose()
        {
            if (_Disposed) return;
            _Disposed = true;

            try { _CaptureChannel.Writer.TryComplete(); } catch { }
            try
            {
                if (_CaptureTask != null && !_CaptureTask.Wait(TimeSpan.FromSeconds(5)))
                {
                    _CaptureCts.Cancel();
                    _CaptureTask.Wait(TimeSpan.FromSeconds(1));
                }
            }
            catch { }

            try { _PurgeCts.Cancel(); } catch { }
            try { _PurgeTask?.Wait(TimeSpan.FromSeconds(5)); } catch { }
            try { _CaptureCts.Dispose(); } catch { }
            try { _PurgeCts.Dispose(); } catch { }
        }

        #endregion

        #region Private-Methods

        private static string SafeDecode(byte[] bytes)
        {
            try
            {
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return Convert.ToBase64String(bytes);
            }
        }

        private static string SafeDecode(byte[] bytes, int count)
        {
            try
            {
                return Encoding.UTF8.GetString(bytes, 0, count);
            }
            catch
            {
                byte[] slice = new byte[count];
                Array.Copy(bytes, slice, count);
                return Convert.ToBase64String(slice);
            }
        }

        private async Task ProcessCaptureQueueAsync(CancellationToken token)
        {
            try
            {
                await foreach (RequestHistoryDetail detail in _CaptureChannel.Reader.ReadAllAsync(token).ConfigureAwait(false))
                {
                    try
                    {
                        await _Repo.RequestHistory.Insert(detail, token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                    catch (Exception e)
                    {
                        _Logging.Warn(_Header + "capture failed for request " + detail.GUID + ": " + e.Message);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        private async Task PurgeLoopAsync(CancellationToken token)
        {
            TimeSpan initialDelay = TimeSpan.FromSeconds(30);
            try { await Task.Delay(initialDelay, token).ConfigureAwait(false); }
            catch (OperationCanceledException) { return; }

            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (_Settings.RequestHistory.Enable)
                    {
                        DateTime cutoff = DateTime.UtcNow.AddDays(-_Settings.RequestHistory.RetentionDays);
                        int deleted = await _Repo.RequestHistory.DeleteOlderThan(cutoff, token).ConfigureAwait(false);
                        if (deleted > 0)
                            _Logging.Debug(_Header + "purged " + deleted + " request history records older than " + cutoff.ToString("O"));
                    }
                }
                catch (OperationCanceledException) { return; }
                catch (Exception e)
                {
                    _Logging.Warn(_Header + "purge pass failed: " + e.Message);
                }

                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(_Settings.RequestHistory.PurgeIntervalMinutes), token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) { return; }
            }
        }

        #endregion
    }
}
