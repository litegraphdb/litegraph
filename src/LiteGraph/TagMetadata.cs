namespace LiteGraph
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;

    /// <summary>
    /// Tag metadata.
    /// </summary>
    public class TagMetadata
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
        /// Graph GUID.
        /// </summary>
        public Guid GraphGUID { get; set; } = default(Guid);

        /// <summary>
        /// Node GUID.
        /// </summary>
        public Guid? NodeGUID { get; set; } = null;

        /// <summary>
        /// Edge GUID.
        /// </summary>
        public Guid? EdgeGUID { get; set; } = null;

        /// <summary>
        /// Key.
        /// </summary>
        public string Key
        {   
            get
            {
                return _Key;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(Key));
                _Key = value;
            }
        }

        /// <summary>
        /// Value.
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Creation timestamp, in UTC.
        /// </summary>
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp from last update, in UTC.
        /// </summary>
        public DateTime LastUpdateUtc { get; set; } = DateTime.UtcNow;

        #endregion

        #region Private-Members

        private string _Key = "key";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public TagMetadata()
        {

        }

        /// <summary>
        /// Convert a list of tag metadata to a name value collection.
        /// </summary>
        /// <param name="tags">Tags.</param>
        /// <returns>Name value collection.</returns>
        public static NameValueCollection ToNameValueCollection(List<TagMetadata> tags)
        {
            if (tags == null) return null;

            NameValueCollection nvc = new NameValueCollection();

            foreach (TagMetadata tag in tags)
            {
                nvc.Add(tag.Key, tag.Value);
            }

            return nvc;
        }

        /// <summary>
        /// Convert a dictionary to a list of tags.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <param name="edgeGuid">Edge GUID.</param>
        /// <param name="nvc">Name value collection.</param>
        /// <returns>List of tags.</returns>
        public static List<TagMetadata> FromNameValueCollection(Guid tenantGuid, Guid graphGuid, Guid? nodeGuid, Guid? edgeGuid, NameValueCollection nvc)
        {
            if (nvc == null) return new List<TagMetadata>();

            List<TagMetadata> ret = new List<TagMetadata>();

            foreach (string key in nvc.AllKeys)
            {
                TagMetadata tag = new TagMetadata
                {
                    TenantGUID = tenantGuid,
                    GraphGUID = graphGuid,
                    NodeGUID = nodeGuid,
                    EdgeGUID = edgeGuid,
                    Key = key,
                    Value = nvc.Get(key)
                };

                ret.Add(tag);
            }

            return ret;
        }

        /// <summary>
        /// Retrieve tags from a given name value collection.
        /// </summary>
        /// <param name="tags">Tags.</param>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <param name="edgeGuid">Edge GUID.</param>
        /// <returns>Name value collection.</returns>
        public static NameValueCollection FromTags(
            List<TagMetadata> tags,
            Guid tenantGuid,
            Guid? graphGuid = null,
            Guid? nodeGuid = null,
            Guid? edgeGuid = null)
        {
            NameValueCollection nvc = new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);

            if (tags == null) return nvc;

            IEnumerable<TagMetadata> matchingTags = tags.Where(t => 
                t != null &&  
                t.TenantGUID == tenantGuid &&
                t.GraphGUID == graphGuid &&
                (nodeGuid == null ? t.NodeGUID == null : t.NodeGUID == nodeGuid) &&
                (edgeGuid == null ? t.EdgeGUID == null : t.EdgeGUID == edgeGuid));

            foreach (TagMetadata tag in matchingTags)
            {
                nvc.Add(tag.Key, tag.Value);
            }

            return nvc;
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}