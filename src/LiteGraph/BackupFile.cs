namespace LiteGraph
{
    using System;
    using System.Collections.Generic;
    using System.Security.Cryptography;
    using System.Text.Json.Serialization;
    using PrettyId;

    /// <summary>
    /// Backup file.
    /// </summary>
    public class BackupFile
    {
        #region Public-Members

        /// <summary>
        /// Filename.
        /// </summary>
        public string Filename { get; set; } = null;

        /// <summary>
        /// File length.
        /// </summary>
        public long Length
        {
            get
            {
                return _Length;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(Length));
                _Length = value;
            }
        }

        /// <summary>
        /// MD5 hash.
        /// </summary>
        public string MD5Hash { get; set; } = null;

        /// <summary>
        /// SHA1 hash.
        /// </summary>
        public string SHA1Hash { get; set; } = null;

        /// <summary>
        /// SHA256 hash.
        /// </summary>
        public string SHA256Hash { get; set; } = null;

        /// <summary>
        /// Creation timestamp, in UTC.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp from last update, in UTC.
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp from last access, in UTC.
        /// </summary>
        public DateTime LastAccessUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// File contents.
        /// </summary>
        public byte[] Data { get; set; } = null;

        #endregion

        #region Private-Members

        private long _Length = 0;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public BackupFile()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}