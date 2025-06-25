namespace LiteGraph
{
    using ExpressionTree;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Reflection.Emit;
    using System.Text.Json.Serialization;
    using Timestamps;

    /// <summary>
    /// Object used to request enumeration.
    /// </summary>
    public class EnumerationQuery
    {
        #region Public-Members

        /// <summary>
        /// Tenant GUID.
        /// </summary>
        public Guid? TenantGUID { get; set; } = null;

        /// <summary>
        /// User GUID.
        /// </summary>
        public Guid? UserGUID { get; set; } = null;

        /// <summary>
        /// Graph GUID.
        /// </summary>
        public Guid? GraphGUID { get; set; } = null;

        /// <summary>
        /// Order by.
        /// </summary>
        public EnumerationOrderEnum Ordering { get; set; } = EnumerationOrderEnum.CreatedDescending;

        /// <summary>
        /// Boolean indicating if an object's data property should be included.
        /// </summary>
        public bool IncludeData { get; set; } = false;

        /// <summary>
        /// Boolean indicating if an object's subordinate objects (i.e. labels, tags, vectors) should be included.
        /// </summary>
        public bool IncludeSubordinates { get; set; } = false;

        /// <summary>
        /// Maximum number of results to retrieve.
        /// </summary>
        public int MaxResults
        {
            get
            {
                return _MaxResults;
            }
            set
            {
                if (value < 1) throw new ArgumentException("MaxResults must be greater than zero.");
                if (value > 1000) throw new ArgumentException("MaxResults must be one thousand or less.");
                _MaxResults = value;
            }
        }

        /// <summary>
        /// The number of records to skip.
        /// </summary>
        public int Skip
        {
            get
            {
                return _Skip;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(Skip));
                _Skip = value;
            }
        }

        /// <summary>
        /// Continuation token.
        /// </summary>
        public Guid? ContinuationToken { get; set; } = null;

        /// <summary>
        /// Search labels.
        /// </summary>
        public List<string> Labels
        {
            get
            {
                return _Labels;
            }
            set
            {
                if (value == null) value = new List<string>();
                _Labels = value;
            }
        }

        /// <summary>
        /// Search tags.
        /// </summary>
        public NameValueCollection Tags
        {
            get
            {
                return _Tags;
            }
            set
            {
                if (value == null) value = new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);
                _Tags = value;
            }
        }

        /// <summary>
        /// Expression.
        /// </summary>
        public Expr Expr { get; set; } = null;

        #endregion

        #region Private-Members

        private int _MaxResults = 1000;
        private int _Skip = 0;
        private List<string> _Labels = new List<string>();
        private NameValueCollection _Tags = new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public EnumerationQuery()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion 
    }
}
