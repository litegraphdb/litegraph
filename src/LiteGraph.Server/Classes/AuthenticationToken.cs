namespace LiteGraph.Server.Classes
{
    using System;
    using System.Security.Cryptography;
    using System.Text;
    using PrettyId;

    /// <summary>
    /// Authentication token details.
    /// </summary>
    public class AuthenticationToken
    {
        #region Public-Members

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
                return TimestampUtc > ExpirationUtc;
            }
        }

        /// <summary>
        /// Random string.
        /// </summary>
        public string Random { get; set; } = IdGenerator.GenerateBase64();

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

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public AuthenticationToken()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
