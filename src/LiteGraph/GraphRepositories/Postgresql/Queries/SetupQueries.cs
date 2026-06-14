namespace LiteGraph.GraphRepositories.Postgresql.Queries
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using ExpressionTree;
    using LiteGraph.Serialization;

    internal static class SetupQueries
    {
        internal static string CreateTablesAndIndices()
        {
            StringBuilder sql = new StringBuilder();

            #region Tenants

            sql.AppendLine(
                "CREATE TABLE IF NOT EXISTS 'tenants' ("
                + "guid VARCHAR(64) NOT NULL UNIQUE, "
                + "name VARCHAR(128), "
                + "active INT, "
                + "createdutc VARCHAR(64), "
                + "lastupdateutc VARCHAR(64) "
                + ");");

            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_tenants_guid' ON 'tenants' (guid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_tenants_name' ON 'tenants' (name ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_tenants_createdutc' ON 'tenants' ('createdutc' ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_tenants_lastupdateutc' ON 'tenants' ('lastupdateutc' ASC);");

            #endregion

            #region Users

            sql.AppendLine(
                "CREATE TABLE IF NOT EXISTS 'users' ("
                + "guid VARCHAR(64) NOT NULL UNIQUE, "
                + "tenantguid VARCHAR(64) NOT NULL, "
                + "firstname VARCHAR(64), "
                + "lastname VARCHAR(64), "
                + "email VARCHAR(128), "
                + "password VARCHAR(128), "
                + "active INT, "
                + "createdutc VARCHAR(64), "
                + "lastupdateutc VARCHAR(64) "
                + ");");

            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_users_guid' ON 'users' (guid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_users_tenantguid' ON 'users' (tenantguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_users_email' ON 'users' (email ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_users_password' ON 'users' (password ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_users_createdutc' ON 'users' ('createdutc' ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_users_lastupdateutc' ON 'users' ('lastupdateutc' ASC);");

            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_users_tenantguid_guid' ON 'users' (tenantguid ASC, guid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_users_email_password' ON 'users' (email ASC, password ASC);");

            #endregion

            #region Credentials

            sql.AppendLine(
                "CREATE TABLE IF NOT EXISTS 'creds' ("
                + "guid VARCHAR(64) NOT NULL UNIQUE, "
                + "tenantguid VARCHAR(64) NOT NULL, "
                + "userguid VARCHAR(64) NOT NULL, "
                + "name VARCHAR(64), "
                + "bearertoken VARCHAR(64), "
                + "active INT, "
                + "scopes TEXT, "
                + "graphguids TEXT, "
                + "createdutc VARCHAR(64), "
                + "lastupdateutc VARCHAR(64) "
                + ");");

            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_creds_guid' ON 'creds' (guid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_creds_tenantguid' ON 'creds' (tenantguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_creds_userguid' ON 'creds' (userguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_creds_bearertoken' ON 'creds' (bearertoken ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_creds_createdutc' ON 'creds' ('createdutc' ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_creds_lastupdateutc' ON 'creds' ('lastupdateutc' ASC);");

            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_creds_tenantguid_guid' ON 'creds' (tenantguid ASC, guid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_creds_tenantguid_userguid' ON 'creds' (tenantguid ASC, userguid ASC);");

            #endregion

            #region Authorization-Roles

            sql.AppendLine(
                "CREATE TABLE IF NOT EXISTS 'authorizationroles' ("
                + "guid VARCHAR(64) NOT NULL UNIQUE, "
                + "tenantguid VARCHAR(64), "
                + "name VARCHAR(128) NOT NULL, "
                + "displayname VARCHAR(128), "
                + "description TEXT, "
                + "builtin INT NOT NULL DEFAULT 0, "
                + "builtinrole VARCHAR(64), "
                + "resourcescope VARCHAR(64) NOT NULL, "
                + "permissions TEXT, "
                + "resourcetypes TEXT, "
                + "inheritstographs INT NOT NULL DEFAULT 0, "
                + "createdutc VARCHAR(64), "
                + "lastupdateutc VARCHAR(64) "
                + ");");

            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_authorizationroles_guid' ON 'authorizationroles' (guid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_authorizationroles_tenantguid' ON 'authorizationroles' (tenantguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_authorizationroles_name' ON 'authorizationroles' (name ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_authorizationroles_tenantguid_name' ON 'authorizationroles' (tenantguid ASC, name ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_authorizationroles_builtin' ON 'authorizationroles' (builtin ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_authorizationroles_builtinrole' ON 'authorizationroles' (builtinrole ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_authorizationroles_resourcescope' ON 'authorizationroles' (resourcescope ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_authorizationroles_createdutc' ON 'authorizationroles' (createdutc DESC);");

            sql.AppendLine(
                "CREATE TABLE IF NOT EXISTS 'userroleassignments' ("
                + "guid VARCHAR(64) NOT NULL UNIQUE, "
                + "tenantguid VARCHAR(64) NOT NULL, "
                + "userguid VARCHAR(64) NOT NULL, "
                + "roleguid VARCHAR(64), "
                + "rolename VARCHAR(128), "
                + "resourcescope VARCHAR(64) NOT NULL, "
                + "graphguid VARCHAR(64), "
                + "createdutc VARCHAR(64), "
                + "lastupdateutc VARCHAR(64) "
                + ");");

            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_userroleassignments_guid' ON 'userroleassignments' (guid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_userroleassignments_tenantguid' ON 'userroleassignments' (tenantguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_userroleassignments_userguid' ON 'userroleassignments' (userguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_userroleassignments_roleguid' ON 'userroleassignments' (roleguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_userroleassignments_rolename' ON 'userroleassignments' (rolename ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_userroleassignments_resourcescope' ON 'userroleassignments' (resourcescope ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_userroleassignments_graphguid' ON 'userroleassignments' (graphguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_userroleassignments_tenantguid_userguid' ON 'userroleassignments' (tenantguid ASC, userguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_userroleassignments_tenantguid_graphguid' ON 'userroleassignments' (tenantguid ASC, graphguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_userroleassignments_createdutc' ON 'userroleassignments' (createdutc DESC);");

            sql.AppendLine(
                "CREATE TABLE IF NOT EXISTS 'credentialscopeassignments' ("
                + "guid VARCHAR(64) NOT NULL UNIQUE, "
                + "tenantguid VARCHAR(64) NOT NULL, "
                + "credentialguid VARCHAR(64) NOT NULL, "
                + "roleguid VARCHAR(64), "
                + "rolename VARCHAR(128), "
                + "resourcescope VARCHAR(64) NOT NULL, "
                + "graphguid VARCHAR(64), "
                + "permissions TEXT, "
                + "resourcetypes TEXT, "
                + "createdutc VARCHAR(64), "
                + "lastupdateutc VARCHAR(64) "
                + ");");

            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_credentialscopeassignments_guid' ON 'credentialscopeassignments' (guid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_credentialscopeassignments_tenantguid' ON 'credentialscopeassignments' (tenantguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_credentialscopeassignments_credentialguid' ON 'credentialscopeassignments' (credentialguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_credentialscopeassignments_roleguid' ON 'credentialscopeassignments' (roleguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_credentialscopeassignments_rolename' ON 'credentialscopeassignments' (rolename ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_credentialscopeassignments_resourcescope' ON 'credentialscopeassignments' (resourcescope ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_credentialscopeassignments_graphguid' ON 'credentialscopeassignments' (graphguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_credentialscopeassignments_tenantguid_credentialguid' ON 'credentialscopeassignments' (tenantguid ASC, credentialguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_credentialscopeassignments_tenantguid_graphguid' ON 'credentialscopeassignments' (tenantguid ASC, graphguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_credentialscopeassignments_createdutc' ON 'credentialscopeassignments' (createdutc DESC);");

            #endregion

            #region Labels

            sql.AppendLine(
                "CREATE TABLE IF NOT EXISTS 'labels' ("
                + "guid VARCHAR(64) NOT NULL UNIQUE, "
                + "tenantguid VARCHAR(64) NOT NULL, "
                + "graphguid VARCHAR(64), "
                + "nodeguid VARCHAR(64), "
                + "edgeguid VARCHAR(64), "
                + "label VARCHAR(256), "
                + "createdutc VARCHAR(64), "
                + "lastupdateutc VARCHAR(64) "
                + ");");

            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_labels_guid' ON 'labels' (guid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_labels_tenantguid' ON 'labels' (tenantguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_labels_graphguid' ON 'labels' (graphguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_labels_nodeguid' ON 'labels' (nodeguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_labels_edgeguid' ON 'labels' (edgeguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_labels_label' ON 'labels' (label ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_labels_createdutc' ON 'labels' ('createdutc' ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_labels_lastupdateutc' ON 'labels' ('lastupdateutc' ASC);");

            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_labels_tenantguid_guid' ON 'labels' (tenantguid ASC, guid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_labels_tenantguid_graphguid' ON 'labels' (tenantguid ASC, graphguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_labels_tenantguid_graphguid_nodeguid' ON 'labels' (tenantguid ASC, graphguid ASC, nodeguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_labels_tenantguid_graphguid_edgeguid' ON 'labels' (tenantguid ASC, graphguid ASC, edgeguid ASC);");

            #endregion

            #region Tags

            sql.AppendLine(
                "CREATE TABLE IF NOT EXISTS 'tags' ("
                + "guid VARCHAR(64) NOT NULL UNIQUE, "
                + "tenantguid VARCHAR(64) NOT NULL, "
                + "graphguid VARCHAR(64), "
                + "nodeguid VARCHAR(64), "
                + "edgeguid VARCHAR(64), "
                + "tagkey VARCHAR(256), "
                + "tagvalue TEXT, "
                + "createdutc VARCHAR(64), "
                + "lastupdateutc VARCHAR(64) "
                + ");");

            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_tags_guid' ON 'tags' (guid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_tags_tenantguid' ON 'tags' (tenantguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_tags_graphguid' ON 'tags' (graphguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_tags_nodeguid' ON 'tags' (nodeguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_tags_edgeguid' ON 'tags' (edgeguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_tags_tagkey' ON 'tags' (tagkey ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_tags_tagvalue' ON 'tags' (tagvalue ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_tags_createdutc' ON 'tags' ('createdutc' ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_tags_lastupdateutc' ON 'tags' ('lastupdateutc' ASC);");

            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_tags_tenantguid_graphguid' ON 'tags' (tenantguid ASC, graphguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_tags_tenantguid_graphguid_nodeguid' ON 'tags' (tenantguid ASC, graphguid ASC, nodeguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_tags_tenantguid_graphguid_edgeguid' ON 'tags' (tenantguid ASC, graphguid ASC, edgeguid ASC);");

            #endregion

            #region Vectors

            sql.AppendLine(
                "CREATE TABLE IF NOT EXISTS 'vectors' ("
                + "guid VARCHAR(64) NOT NULL UNIQUE, "
                + "tenantguid VARCHAR(64) NOT NULL, "
                + "graphguid VARCHAR(64), "
                + "nodeguid VARCHAR(64), "
                + "edgeguid VARCHAR(64), "
                + "model VARCHAR(256), "
                + "dimensionality INT, "
                + "content TEXT, "
                + "embeddings BLOB, "
                + "createdutc VARCHAR(64), "
                + "lastupdateutc VARCHAR(64) "
                + ");");

            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_vectors_guid' ON 'vectors' (guid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_vectors_tenantguid' ON 'vectors' (tenantguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_vectors_graphguid' ON 'vectors' (graphguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_vectors_nodeguid' ON 'vectors' (nodeguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_vectors_edgeguid' ON 'vectors' (edgeguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_vectors_model' ON 'vectors' (model ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_vectors_dimensionality' ON 'vectors' (dimensionality ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_vectors_createdutc' ON 'vectors' ('createdutc' ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_vectors_lastupdateutc' ON 'vectors' ('lastupdateutc' ASC);");

            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_vectors_tenantguid_graphguid' ON 'vectors' (tenantguid ASC, graphguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_vectors_tenantguid_graphguid_nodeguid' ON 'vectors' (tenantguid ASC, graphguid ASC, nodeguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_vectors_tenantguid_graphguid_edgeguid' ON 'vectors' (tenantguid ASC, graphguid ASC, edgeguid ASC);");

            #endregion

            #region Graphs

            sql.AppendLine(
                "CREATE TABLE IF NOT EXISTS 'graphs' ("
                + "guid VARCHAR(64) NOT NULL UNIQUE, "
                + "tenantguid VARCHAR(64) NOT NULL, "
                + "name VARCHAR(128), "
                + "vectorindextype VARCHAR(16), "
                + "vectorindexfile VARCHAR(256), "
                + "vectorindexthreshold INT DEFAULT NULL, "
                + "vectordimensionality INT DEFAULT NULL, "
                + "vectorindexm INT DEFAULT NULL, "
                + "vectorindexef INT DEFAULT NULL, "
                + "vectorindexefconstruction INT DEFAULT NULL, "
                + "vectorindexdirty INT NOT NULL DEFAULT 0, "
                + "vectorindexdirtyutc VARCHAR(64), "
                + "vectorindexdirtyreason TEXT, "
                + "data TEXT, "
                + "createdutc VARCHAR(64), "
                + "lastupdateutc VARCHAR(64) "
                + ");");

            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_graphs_guid' ON 'graphs' (guid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_graphs_tenantguid' ON 'graphs' (tenantguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_graphs_name' ON 'graphs' (name ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_graphs_createdutc' ON 'graphs' ('createdutc' ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_graphs_lastupdateutc' ON 'graphs' ('lastupdateutc' ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_graphs_vectorindexdirty' ON 'graphs' (vectorindexdirty ASC);");
            sql.AppendLine("DROP INDEX IF EXISTS 'idx_graphs_data';");

            #endregion

            #region Nodes

            sql.AppendLine(
                "CREATE TABLE IF NOT EXISTS 'nodes' ("
                + "guid VARCHAR(64) NOT NULL UNIQUE, "
                + "tenantguid VARCHAR(64) NOT NULL, "
                + "graphguid VARCHAR(64) NOT NULL, "
                + "name VARCHAR(128), "
                + "data TEXT, "
                + "createdutc VARCHAR(64), "
                + "lastupdateutc VARCHAR(64) "
                + ");");

            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_nodes_guid' ON 'nodes' (guid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_nodes_tenantguid' ON 'nodes' (tenantguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_nodes_graphguid' ON 'nodes' (graphguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_nodes_name' ON 'nodes' (name ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_nodes_createdutc' ON 'nodes' ('createdutc' ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_nodes_lastupdateutc' ON 'nodes' ('lastupdateutc' ASC);");

            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_nodes_tenantguid_graphguid' ON 'nodes' (tenantguid ASC, graphguid ASC);");
            sql.AppendLine("DROP INDEX IF EXISTS 'idx_nodes_data';");

            #endregion

            #region Edges

            sql.AppendLine(
                "CREATE TABLE IF NOT EXISTS 'edges' ("
                + "guid VARCHAR(64) NOT NULL UNIQUE, "
                + "tenantguid VARCHAR(64) NOT NULL, "
                + "graphguid VARCHAR(64) NOT NULL, "
                + "name VARCHAR(128), "
                + "fromguid VARCHAR(64) NOT NULL, "
                + "toguid VARCHAR(64) NOT NULL, "
                + "cost INT NOT NULL, "
                + "data TEXT, "
                + "createdutc VARCHAR(64), "
                + "lastupdateutc VARCHAR(64) "
                + ");");

            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_edges_guid' ON 'edges' (guid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_edges_tenantguid' ON 'edges' (tenantguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_edges_graphguid' ON 'edges' (graphguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_edges_name' ON 'edges' (name ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_edges_fromguid' ON 'edges' (fromguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_edges_toguid' ON 'edges' (toguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_edges_createdutc' ON 'edges' ('createdutc' ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_edges_lastupdateutc' ON 'edges' ('lastupdateutc' ASC);");

            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_edges_tenantguid_graphguid' ON 'edges' (tenantguid ASC, graphguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_edges_tenantguid_graphguid_fromguid' ON 'edges' (tenantguid ASC, graphguid ASC, fromguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_edges_tenantguid_graphguid_toguid' ON 'edges' (tenantguid ASC, graphguid ASC, toguid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_edges_tenantguid_graphguid_fromguid_toguid' ON 'edges' (tenantguid ASC, graphguid ASC, fromguid ASC, toguid ASC);");
            sql.AppendLine("DROP INDEX IF EXISTS 'idx_edges_data';");

            #endregion

            #region Request-History

            sql.AppendLine(
                "CREATE TABLE IF NOT EXISTS 'requesthistory' ("
                + "guid VARCHAR(64) NOT NULL UNIQUE, "
                + "requestid VARCHAR(128), "
                + "correlationid VARCHAR(128), "
                + "traceid VARCHAR(128), "
                + "createdutc VARCHAR(64) NOT NULL, "
                + "completedutc VARCHAR(64), "
                + "method VARCHAR(16) NOT NULL, "
                + "path TEXT NOT NULL, "
                + "url TEXT NOT NULL, "
                + "sourceip VARCHAR(64), "
                + "tenantguid VARCHAR(64), "
                + "userguid VARCHAR(64), "
                + "statuscode INT NOT NULL, "
                + "success INT NOT NULL, "
                + "processingtimems REAL NOT NULL DEFAULT 0, "
                + "requestbodylength INT NOT NULL DEFAULT 0, "
                + "responsebodylength INT NOT NULL DEFAULT 0, "
                + "requestbodytruncated INT NOT NULL DEFAULT 0, "
                + "responsebodytruncated INT NOT NULL DEFAULT 0, "
                + "requestcontenttype TEXT, "
                + "responsecontenttype TEXT, "
                + "requestheadersjson TEXT, "
                + "requestbodyb64 TEXT, "
                + "responseheadersjson TEXT, "
                + "responsebodyb64 TEXT "
                + ");");

            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_requesthistory_guid' ON 'requesthistory' (guid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_requesthistory_requestid' ON 'requesthistory' (requestid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_requesthistory_correlationid_createdutc' ON 'requesthistory' (correlationid ASC, createdutc DESC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_requesthistory_traceid_createdutc' ON 'requesthistory' (traceid ASC, createdutc DESC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_requesthistory_createdutc' ON 'requesthistory' (createdutc DESC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_requesthistory_tenantguid_createdutc' ON 'requesthistory' (tenantguid ASC, createdutc DESC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_requesthistory_method_createdutc' ON 'requesthistory' (method ASC, createdutc DESC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_requesthistory_statuscode_createdutc' ON 'requesthistory' (statuscode ASC, createdutc DESC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_requesthistory_success_createdutc' ON 'requesthistory' (success ASC, createdutc DESC);");

            #endregion

            #region Authorization-Audit

            sql.AppendLine(
                "CREATE TABLE IF NOT EXISTS 'authorizationaudit' ("
                + "guid VARCHAR(64) NOT NULL UNIQUE, "
                + "createdutc VARCHAR(64) NOT NULL, "
                + "requestid VARCHAR(128), "
                + "correlationid VARCHAR(128), "
                + "traceid VARCHAR(128), "
                + "tenantguid VARCHAR(64), "
                + "graphguid VARCHAR(64), "
                + "userguid VARCHAR(64), "
                + "credentialguid VARCHAR(64), "
                + "requesttype VARCHAR(128), "
                + "method VARCHAR(16), "
                + "path TEXT, "
                + "sourceip VARCHAR(64), "
                + "authenticationresult VARCHAR(64), "
                + "authorizationresult VARCHAR(64), "
                + "reason VARCHAR(128), "
                + "requiredscope VARCHAR(64), "
                + "isadmin INT NOT NULL DEFAULT 0, "
                + "statuscode INT NOT NULL DEFAULT 0, "
                + "description TEXT "
                + ");");

            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_authorizationaudit_guid' ON 'authorizationaudit' (guid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_authorizationaudit_createdutc' ON 'authorizationaudit' (createdutc DESC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_authorizationaudit_requestid' ON 'authorizationaudit' (requestid ASC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_authorizationaudit_correlationid_createdutc' ON 'authorizationaudit' (correlationid ASC, createdutc DESC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_authorizationaudit_traceid_createdutc' ON 'authorizationaudit' (traceid ASC, createdutc DESC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_authorizationaudit_tenantguid_createdutc' ON 'authorizationaudit' (tenantguid ASC, createdutc DESC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_authorizationaudit_graphguid_createdutc' ON 'authorizationaudit' (graphguid ASC, createdutc DESC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_authorizationaudit_userguid_createdutc' ON 'authorizationaudit' (userguid ASC, createdutc DESC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_authorizationaudit_credentialguid_createdutc' ON 'authorizationaudit' (credentialguid ASC, createdutc DESC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_authorizationaudit_reason_createdutc' ON 'authorizationaudit' (reason ASC, createdutc DESC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_authorizationaudit_requiredscope_createdutc' ON 'authorizationaudit' (requiredscope ASC, createdutc DESC);");
            sql.AppendLine("CREATE INDEX IF NOT EXISTS 'idx_authorizationaudit_requesttype_createdutc' ON 'authorizationaudit' (requesttype ASC, createdutc DESC);");

            #endregion

            return sql.ToString();
        }
    }
}

