namespace LiteGraph
{
    using LiteGraph.Serialization;
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// User.
    /// </summary>
    public class UserMaster
    {
        #region Public-Members

        /// <summary>
        /// GUID.
        /// </summary>
        public Guid GUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Tenant GUID.
        /// </summary>
        public Guid TenantGUID { get; set; } = Guid.NewGuid();

        /// <summary>
        /// First name.
        /// </summary>
        public string FirstName
        {
            get
            {
                return _FirstName;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(FirstName));
                _FirstName = value;
            }
        }

        /// <summary>
        /// Last name.
        /// </summary>
        public string LastName
        {
            get
            {
                return _LastName;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(LastName));
                _LastName = value;
            }
        }

        /// <summary>
        /// Email.
        /// </summary>
        public string Email
        {
            get
            {
                return _Email;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(Email));
                _Email = value;
            }
        }

        /// <summary>
        /// Password.
        /// </summary>
        public string Password
        {
            get
            {
                return _Password;
            }
            set
            {
                _Password = value;
            }
        }

        /// <summary>
        /// Active.
        /// </summary>
        public bool Active { get; set; } = true;

        /// <summary>
        /// Creation time, in UTC.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp from last update, in UTC.
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private string _FirstName = "First";
        private string _LastName = "Last";
        private string _Email = "email@address.com";
        private string _Password = "password";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public UserMaster()
        {

        }

        /// <summary>
        /// Redact.
        /// </summary>
        /// <param name="serializer">Serializer.</param>
        /// <param name="user">User.</param>
        /// <returns>User.</returns>
        public static UserMaster Redact(ISerializer serializer, UserMaster user)
        {
            if (user == null) return null;
            UserMaster redacted = serializer.CopyObject<UserMaster>(user);

            if (!String.IsNullOrEmpty(user.Password))
            {
                int numAsterisks = user.Password.Length - 4;
                if (user.Password.Length < 5) user.Password = "****";
                else
                {
                    string password = "";
                    for (int i = 0; i < numAsterisks; i++) password += "*";
                    password += user.Password.Substring((user.Password.Length - 4), 4);
                    redacted.Password = password;
                }
            }

            return redacted;
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}