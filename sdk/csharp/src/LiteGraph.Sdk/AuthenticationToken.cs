namespace LiteGraph.Sdk
{
    using System;

    /// <summary>
    /// Authentication token details.
    /// </summary>
    public class AuthenticationToken
    {
        /// <summary>
        /// Timestamp when the token was issued, in UTC time.
        /// </summary>
        public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp when the token will expire, in UTC time.
        /// </summary>
        public DateTime ExpirationUtc { get; set; } = DateTime.UtcNow.AddHours(24);

        /// <summary>
        /// Boolean to indicate if the token is expired.
        /// </summary>
        public bool IsExpired
        {
            get
            {
                return DateTime.UtcNow > ExpirationUtc;
            }
        }

        /// <summary>
        /// Tenant GUID.
        /// </summary>
        public Guid? TenantGUID { get; set; } = null;

        /// <summary>
        /// Tenant.
        /// </summary>
        public TenantMetadata Tenant { get; set; } = null;

        /// <summary>
        /// User GUID.
        /// </summary>
        public Guid? UserGUID { get; set; } = null;

        /// <summary>
        /// User.
        /// </summary>
        public UserMaster User { get; set; } = null;

        /// <summary>
        /// Token.
        /// </summary>
        public string Token { get; set; } = null;

        /// <summary>
        /// Boolean indicating whether or not the token is valid.
        /// </summary>
        public bool Valid { get; set; } = true;
    }
}

