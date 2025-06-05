namespace LiteGraph.Server.Classes
{
    using System.Runtime.Serialization;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Request type.
    /// </summary>
    public enum RequestTypeEnum
    {
        #region General

        /// <summary>
        /// Root
        /// </summary>
        [EnumMember(Value = "Root")]
        Root,
        /// <summary>
        /// Loopback
        /// </summary>
        [EnumMember(Value = "Loopback")]
        Loopback,
        /// <summary>
        /// Favicon
        /// </summary>
        [EnumMember(Value = "Favicon")]
        Favicon,
        /// <summary>
        /// Unknown
        /// </summary>
        [EnumMember(Value = "Unknown")]
        Unknown,

        #endregion

        #region Admin

        /// <summary>
        /// FlushDatabase.
        /// </summary>
        [EnumMember(Value = "FlushDatabase")]
        FlushDatabase,

        /// <summary>
        /// Backup.
        /// </summary>
        [EnumMember(Value = "Backup")]
        Backup,
        /// <summary>
        /// BackupExists.
        /// </summary>
        [EnumMember(Value = "BackupExists")]
        BackupExists,
        /// <summary>
        /// BackupRead.
        /// </summary>
        [EnumMember(Value = "BackupRead")]
        BackupRead,
        /// <summary>
        /// BackupEnumerate.
        /// </summary>
        [EnumMember(Value = "BackupEnumerate")]
        BackupEnumerate,
        /// <summary>
        /// BackupDelete.
        /// </summary>
        [EnumMember(Value = "BackupDelete")]
        BackupDelete,

        #endregion

        #region Tenants

        /// <summary>
        /// TenantCreate
        /// </summary>
        [EnumMember(Value = "TenantCreate")]
        TenantCreate,
        /// <summary>
        /// TenantDelete
        /// </summary>
        [EnumMember(Value = "TenantDelete")]
        TenantDelete,
        /// <summary>
        /// TenantExists
        /// </summary>
        [EnumMember(Value = "TenantExists")]
        TenantExists,
        /// <summary>
        /// TenantRead
        /// </summary>
        [EnumMember(Value = "TenantRead")]
        TenantRead,
        /// <summary>
        /// TenantReadMany
        /// </summary>
        [EnumMember(Value = "TenantReadMany")]
        TenantReadMany,
        /// <summary>
        /// TenantUpdate
        /// </summary>
        [EnumMember(Value = "TenantUpdate")]
        TenantUpdate,

        #endregion

        #region Users

        /// <summary>
        /// UserCreate
        /// </summary>
        [EnumMember(Value = "UserCreate")]
        UserCreate,
        /// <summary>
        /// UserDelete
        /// </summary>
        [EnumMember(Value = "UserDelete")]
        UserDelete,
        /// <summary>
        /// UserExists
        /// </summary>
        [EnumMember(Value = "UserExists")]
        UserExists,
        /// <summary>
        /// UserRead
        /// </summary>
        [EnumMember(Value = "UserRead")]
        UserRead,
        /// <summary>
        /// UserReadMany
        /// </summary>
        [EnumMember(Value = "UserReadMany")]
        UserReadMany,
        /// <summary>
        /// UserReadTenants
        /// </summary>
        [EnumMember(Value = "UserReadTenants")]
        UserReadTenants,
        /// <summary>
        /// UserUpdate
        /// </summary>
        [EnumMember(Value = "UserUpdate")]
        UserUpdate,

        #endregion

        #region Credentials

        /// <summary>
        /// CredentialCreate
        /// </summary>
        [EnumMember(Value = "CredentialCreate")]
        CredentialCreate,
        /// <summary>
        /// CredentialDelete
        /// </summary>
        [EnumMember(Value = "CredentialDelete")]
        CredentialDelete,
        /// <summary>
        /// CredentialExists
        /// </summary>
        [EnumMember(Value = "CredentialExists")]
        CredentialExists,
        /// <summary>
        /// CredentialRead
        /// </summary>
        [EnumMember(Value = "CredentialRead")]
        CredentialRead,
        /// <summary>
        /// CredentialReadMany
        /// </summary>
        [EnumMember(Value = "CredentialReadMany")]
        CredentialReadMany,
        /// <summary>
        /// CredentialUpdate
        /// </summary>
        [EnumMember(Value = "CredentialUpdate")]
        CredentialUpdate,

        #endregion

        #region Labels

        /// <summary>
        /// LabelCreate
        /// </summary>
        [EnumMember(Value = "LabelCreate")]
        LabelCreate,
        /// <summary>
        /// LabelCreateMany
        /// </summary>
        [EnumMember(Value = "LabelCreateMany")]
        LabelCreateMany,
        /// <summary>
        /// LabelDelete
        /// </summary>
        [EnumMember(Value = "LabelDelete")]
        LabelDelete,
        /// <summary>
        /// LabelDeleteMany
        /// </summary>
        [EnumMember(Value = "LabelDeleteMany")]
        LabelDeleteMany,
        /// <summary>
        /// LabelExists
        /// </summary>
        [EnumMember(Value = "LabelExists")]
        LabelExists,
        /// <summary>
        /// LabelRead
        /// </summary>
        [EnumMember(Value = "LabelRead")]
        LabelRead,
        /// <summary>
        /// LabelReadMany
        /// </summary>
        [EnumMember(Value = "LabelReadMany")]
        LabelReadMany,
        /// <summary>
        /// LabelUpdate
        /// </summary>
        [EnumMember(Value = "LabelUpdate")]
        LabelUpdate,

        #endregion

        #region Vectors

        /// <summary>
        /// VectorCreate
        /// </summary>
        [EnumMember(Value = "VectorCreate")]
        VectorCreate,
        /// <summary>
        /// VectorCreateMany
        /// </summary>
        [EnumMember(Value = "VectorCreateMany")]
        VectorCreateMany,
        /// <summary>
        /// VectorDelete
        /// </summary>
        [EnumMember(Value = "VectorDelete")]
        VectorDelete,
        /// <summary>
        /// VectorDeleteMany
        /// </summary>
        [EnumMember(Value = "VectorDeleteMany")]
        VectorDeleteMany,
        /// <summary>
        /// VectorExists
        /// </summary>
        [EnumMember(Value = "VectorExists")]
        VectorExists,
        /// <summary>
        /// VectorRead
        /// </summary>
        [EnumMember(Value = "VectorRead")]
        VectorRead,
        /// <summary>
        /// VectorReadMany
        /// </summary>
        [EnumMember(Value = "VectorReadMany")]
        VectorReadMany,
        /// <summary>
        /// VectorSearch
        /// </summary>
        [EnumMember(Value = "VectorSearch")]
        VectorSearch,
        /// <summary>
        /// VectorUpdate
        /// </summary>
        [EnumMember(Value = "VectorUpdate")]
        VectorUpdate,

        #endregion

        #region Tags

        /// <summary>
        /// TagCreate
        /// </summary>
        [EnumMember(Value = "TagCreate")]
        TagCreate,
        /// <summary>
        /// TagCreateMany
        /// </summary>
        [EnumMember(Value = "TagCreateMany")]
        TagCreateMany,
        /// <summary>
        /// TagDelete
        /// </summary>
        [EnumMember(Value = "TagDelete")]
        TagDelete,
        /// <summary>
        /// TagDeleteMany
        /// </summary>
        [EnumMember(Value = "TagDeleteMany")]
        TagDeleteMany,
        /// <summary>
        /// TagExists
        /// </summary>
        [EnumMember(Value = "TagExists")]
        TagExists,
        /// <summary>
        /// TagRead
        /// </summary>
        [EnumMember(Value = "TagRead")]
        TagRead,
        /// <summary>
        /// TagReadMany
        /// </summary>
        [EnumMember(Value = "TagReadMany")]
        TagReadMany,
        /// <summary>
        /// TagUpdate
        /// </summary>
        [EnumMember(Value = "TagUpdate")]
        TagUpdate,

        #endregion

        #region Graphs

        /// <summary>
        /// GraphCreate
        /// </summary>
        [EnumMember(Value = "GraphCreate")]
        GraphCreate,
        /// <summary>
        /// GraphDelete
        /// </summary>
        [EnumMember(Value = "GraphDelete")]
        GraphDelete,
        /// <summary>
        /// GraphExistence
        /// </summary>
        [EnumMember(Value = "GraphExistence")]
        GraphExistence,
        /// <summary>
        /// GraphExists
        /// </summary>
        [EnumMember(Value = "GraphExists")]
        GraphExists,
        /// <summary>
        /// GraphExport
        /// </summary>
        [EnumMember(Value = "GraphExport")]
        GraphExport,
        /// <summary>
        /// GraphRead
        /// </summary>
        [EnumMember(Value = "GraphRead")]
        GraphRead,
        /// <summary>
        /// GraphReadMany
        /// </summary>
        [EnumMember(Value = "GraphReadMany")]
        GraphReadMany,
        /// <summary>
        /// GraphReadFirst
        /// </summary>
        [EnumMember(Value = "GraphReadFirst")]
        GraphReadFirst,
        /// <summary>
        /// GraphSearch
        /// </summary>
        [EnumMember(Value = "GraphSearch")]
        GraphSearch,
        /// <summary>
        /// GraphUpdate
        /// </summary>
        [EnumMember(Value = "GraphUpdate")]
        GraphUpdate,

        #endregion

        #region Nodes

        /// <summary>
        /// NodeCreate
        /// </summary>
        [EnumMember(Value = "NodeCreate")]
        NodeCreate,
        /// <summary>
        /// NodeCreateMany
        /// </summary>
        [EnumMember(Value = "NodeCreateMany")]
        NodeCreateMany,
        /// <summary>
        /// NodeDelete
        /// </summary>
        [EnumMember(Value = "NodeDelete")]
        NodeDelete,
        /// <summary>
        /// NodeDeleteAll
        /// </summary>
        [EnumMember(Value = "NodeDeleteAll")]
        NodeDeleteAll,
        /// <summary>
        /// NodeDeleteMany
        /// </summary>
        [EnumMember(Value = "NodeDeleteMany")]
        NodeDeleteMany,
        /// <summary>
        /// NodeExists
        /// </summary>
        [EnumMember(Value = "NodeExists")]
        NodeExists,
        /// <summary>
        /// NodeRead
        /// </summary>
        [EnumMember(Value = "NodeRead")]
        NodeRead,
        /// <summary>
        /// NodeReadMany
        /// </summary>
        [EnumMember(Value = "NodeReadMany")]
        NodeReadMany,
        /// <summary>
        /// NodeReadFirst
        /// </summary>
        [EnumMember(Value = "NodeReadFirst")]
        NodeReadFirst,
        /// <summary>
        /// NodeSearch
        /// </summary>
        [EnumMember(Value = "NodeSearch")]
        NodeSearch,
        /// <summary>
        /// NodeUpdate
        /// </summary>
        [EnumMember(Value = "NodeUpdate")]
        NodeUpdate,

        #endregion

        #region Edges

        /// <summary>
        /// EdgeBetween
        /// </summary>
        [EnumMember(Value = "EdgeBetween")]
        EdgeBetween,
        /// <summary>
        /// EdgeCreate
        /// </summary>
        [EnumMember(Value = "EdgeCreate")]
        EdgeCreate,
        /// <summary>
        /// EdgeCreateMany
        /// </summary>
        [EnumMember(Value = "EdgeCreateMany")]
        EdgeCreateMany,
        /// <summary>
        /// EdgeDelete
        /// </summary>
        [EnumMember(Value = "EdgeDelete")]
        EdgeDelete,
        /// <summary>
        /// EdgeDeleteAll
        /// </summary>
        [EnumMember(Value = "EdgeDeleteAll")]
        EdgeDeleteAll,
        /// <summary>
        /// EdgeDeleteMany
        /// </summary>
        [EnumMember(Value = "EdgeDeleteMany")]
        EdgeDeleteMany,
        /// <summary>
        /// EdgeExists
        /// </summary>
        [EnumMember(Value = "EdgeExists")]
        EdgeExists,
        /// <summary>
        /// EdgeRead
        /// </summary>
        [EnumMember(Value = "EdgeRead")]
        EdgeRead,
        /// <summary>
        /// EdgeReadMany
        /// </summary>
        [EnumMember(Value = "EdgeReadMany")]
        EdgeReadFirst,
        /// <summary>
        /// EdgeReadFirst
        /// </summary>
        [EnumMember(Value = "EdgeReadFirst")]
        EdgeReadMany,
        /// <summary>
        /// EdgeSearch
        /// </summary>
        [EnumMember(Value = "EdgeSearch")]
        EdgeSearch,
        /// <summary>
        /// EdgeUpdate
        /// </summary>
        [EnumMember(Value = "EdgeUpdate")]
        EdgeUpdate,

        #endregion

        #region Topology

        /// <summary>
        /// EdgesFromNode
        /// </summary>
        [EnumMember(Value = "EdgesFromNode")]
        EdgesFromNode,
        /// <summary>
        /// EdgesToNode
        /// </summary>
        [EnumMember(Value = "EdgesToNode")]
        EdgesToNode,
        /// <summary>
        /// AllEdgesToNode
        /// </summary>
        [EnumMember(Value = "AllEdgesToNode")]
        AllEdgesToNode,
        /// <summary>
        /// NodeChildren
        /// </summary>
        [EnumMember(Value = "NodeChildren")]
        NodeChildren,
        /// <summary>
        /// NodeParents
        /// </summary>
        [EnumMember(Value = "NodeParents")]
        NodeParents,
        /// <summary>
        /// NodeNeighbors
        /// </summary>
        [EnumMember(Value = "NodeNeighbors")]
        NodeNeighbors,
        /// <summary>
        /// GetRoutes
        /// </summary>
        [EnumMember(Value = "GetRoutes")]
        GetRoutes

        #endregion
    }
}
