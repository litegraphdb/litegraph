namespace LiteGraph.Coordination
{
    using System;

    /// <summary>
    /// Options controlling lock acquisition.
    /// </summary>
    public class LockAcquireOptions
    {
        #region Public-Members

        /// <summary>
        /// True to wait for the lock up to TimeoutMs; false to fail immediately if the lock is held.
        /// Default is true.
        /// </summary>
        public bool Wait { get; set; } = true;

        /// <summary>
        /// Maximum time to wait for the lock, in milliseconds, when Wait is true.
        /// Default is 30000.  Minimum is 0 (fail immediately), maximum is 3600000.
        /// </summary>
        public int TimeoutMs
        {
            get
            {
                return _TimeoutMs;
            }
            set
            {
                if (value < 0 || value > 3600000) throw new ArgumentOutOfRangeException(nameof(TimeoutMs), "TimeoutMs must be between 0 and 3600000.");
                _TimeoutMs = value;
            }
        }

        /// <summary>
        /// Lease duration in milliseconds for distributed locks.  The provider renews the lease while the handle is held.
        /// Ignored by in-process providers.  Default is 30000.  Minimum is 1000, maximum is 3600000.
        /// </summary>
        public int LeaseMs
        {
            get
            {
                return _LeaseMs;
            }
            set
            {
                if (value < 1000 || value > 3600000) throw new ArgumentOutOfRangeException(nameof(LeaseMs), "LeaseMs must be between 1000 and 3600000.");
                _LeaseMs = value;
            }
        }

        #endregion

        #region Private-Members

        private int _TimeoutMs = 30000;
        private int _LeaseMs = 30000;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate with defaults (wait up to 30 seconds, 30 second lease).
        /// </summary>
        public LockAcquireOptions()
        {
        }

        /// <summary>
        /// Options that fail immediately when the lock is held.
        /// </summary>
        /// <returns>Options.</returns>
        public static LockAcquireOptions FailFast()
        {
            return new LockAcquireOptions { Wait = false, TimeoutMs = 0 };
        }

        /// <summary>
        /// Options that wait up to the supplied timeout.
        /// </summary>
        /// <param name="timeoutMs">Timeout in milliseconds.</param>
        /// <returns>Options.</returns>
        public static LockAcquireOptions WaitUpTo(int timeoutMs)
        {
            return new LockAcquireOptions { Wait = true, TimeoutMs = timeoutMs };
        }

        #endregion
    }
}
