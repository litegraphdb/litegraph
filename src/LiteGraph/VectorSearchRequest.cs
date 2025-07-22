namespace LiteGraph
{
    using ExpressionTree;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;

    /// <summary>
    /// Vector search request.
    /// </summary>
    public class VectorSearchRequest
    {
        #region Public-Members

        /// <summary>
        /// Tenant GUID.
        /// </summary>
        public Guid TenantGUID { get; set; } = default(Guid);

        /// <summary>
        /// Graph GUID.
        /// </summary>
        public Guid? GraphGUID { get; set; } = null;

        /// <summary>
        /// Vector search domain.
        /// </summary>
        public VectorSearchDomainEnum Domain { get; set; } = VectorSearchDomainEnum.Node;

        /// <summary>
        /// Vector search type.
        /// </summary>
        public VectorSearchTypeEnum SearchType { get; set; } = VectorSearchTypeEnum.CosineSimilarity;

        /// <summary>
        /// Specify the top K number of results to return.  Default is 100.  Value can be null, but if not null, must be greater than zero.
        /// </summary>
        public int? TopK
        {
            get
            {
                return _TopK;
            }
            set
            {
                if (value != null && value.Value < 1) throw new ArgumentOutOfRangeException(nameof(TopK));
                _TopK = value;
            }
        }

        /// <summary>
        /// Minimum score.  Default value is 0.  Value must be null, or, zero or greater.
        /// </summary>
        public float? MinimumScore
        {
            get
            {
                return _MinimumScore;
            }
            set
            {
                if (value != null && value.Value < 0) throw new ArgumentOutOfRangeException(nameof(MinimumScore));
                _MinimumScore = value;
            }
        }

        /// <summary>
        /// Maximum distance.  Default value is the maximum float value.  Value must be null, or, greater than zero.
        /// </summary>
        public float? MaximumDistance
        {
            get
            {
                return _MaximumDistance;
            }
            set
            {
                if (value != null && value.Value <= 0) throw new ArgumentOutOfRangeException(nameof(MaximumDistance));
                _MaximumDistance = value;
            }
        }

        /// <summary>
        /// Minimum inner product.  Default value is 0.  Value must be null, or, zero or greater.
        /// </summary>
        public float? MinimumInnerProduct
        {
            get
            {
                return _MinimumInnerProduct;
            }
            set
            {
                if (value != null && value.Value < 0) throw new ArgumentOutOfRangeException(nameof(MinimumInnerProduct));
                _MinimumInnerProduct = value;
            }
        }

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

        /// <summary>
        /// Embeddings.
        /// </summary>
        public List<float> Embeddings { get; set; } = null;

        #endregion

        #region Private-Members

        private int? _TopK = 100;
        private float? _MinimumScore = 0.0f;
        private float? _MaximumDistance = float.MaxValue;
        private float? _MinimumInnerProduct = 0.0f;

        private List<string> _Labels = new List<string>();
        private NameValueCollection _Tags = new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);
        private List<float> _Embeddings = new List<float>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public VectorSearchRequest()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
