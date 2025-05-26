namespace LiteGraph.Server.Classes
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Backup request.
    /// </summary>
    public class BackupRequest
    {
        #region Public-Members

        /// <summary>
        /// File to which the backup should be written.
        /// </summary>
        public string Filename
        {
            get
            {
                return _Filename;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(Filename));
                if (value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                    throw new ArgumentException("The specified filename contains invalid characters.");
                _Filename = value;
            }
        }

        #endregion

        #region Private-Members

        private string _Filename = "litegraph-backup-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".db";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Backup requets.
        /// </summary>
        public BackupRequest()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
