namespace LiteGraph.GraphRepositories.Postgresql.Queries
{
    using System;
    using System.Text;
    using LiteGraph.Serialization;

    internal static class AuthorizationRoleQueries
    {
        internal const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        private static readonly Serializer Serializer = new Serializer();

        internal static string InsertRole(AuthorizationRole role)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO 'authorizationroles' (");
            sb.Append("guid, tenantguid, name, displayname, description, builtin, builtinrole, resourcescope, permissions, resourcetypes, inheritstographs, createdutc, lastupdateutc");
            sb.Append(") VALUES (");
            sb.Append(SqlString(role.GUID.ToString())).Append(", ");
            sb.Append(GuidOrNull(role.TenantGUID)).Append(", ");
            sb.Append(SqlString(role.Name)).Append(", ");
            sb.Append(SqlString(role.DisplayName)).Append(", ");
            sb.Append(SqlString(role.Description)).Append(", ");
            sb.Append(role.BuiltIn ? "1" : "0").Append(", ");
            sb.Append(SqlString(role.BuiltInRole.ToString())).Append(", ");
            sb.Append(SqlString(role.ResourceScope.ToString())).Append(", ");
            sb.Append(SqlJson(Serializer.SerializeJson(role.Permissions ?? new System.Collections.Generic.List<AuthorizationPermissionEnum>(), false))).Append(", ");
            sb.Append(SqlJson(Serializer.SerializeJson(role.ResourceTypes ?? new System.Collections.Generic.List<AuthorizationResourceTypeEnum>(), false))).Append(", ");
            sb.Append(role.InheritsToGraphs ? "1" : "0").Append(", ");
            sb.Append(SqlString(role.CreatedUtc.ToString(TimestampFormat))).Append(", ");
            sb.Append(SqlString(role.LastUpdateUtc.ToString(TimestampFormat)));
            sb.Append(") RETURNING *;");
            return sb.ToString();
        }

        internal static string SelectRoleByGuid(Guid guid)
        {
            return "SELECT * FROM 'authorizationroles' WHERE guid = '" + guid + "';";
        }

        internal static string SelectRoleByName(Guid? tenantGuid, string name)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT * FROM 'authorizationroles' WHERE ");
            if (tenantGuid.HasValue) sb.Append("tenantguid = '").Append(tenantGuid.Value).Append("' ");
            else sb.Append("tenantguid IS NULL ");
            sb.Append("AND name = ").Append(SqlString(name)).Append(" ");
            sb.Append("ORDER BY createdutc DESC LIMIT 1;");
            return sb.ToString();
        }

        internal static string SearchRoles(AuthorizationRoleSearchRequest search, bool countOnly)
        {
            StringBuilder sb = new StringBuilder();
            if (countOnly) sb.Append("SELECT COUNT(*) AS record_count FROM 'authorizationroles' WHERE 1=1 ");
            else sb.Append("SELECT * FROM 'authorizationroles' WHERE 1=1 ");

            AppendRoleFilters(sb, search);

            if (!countOnly)
            {
                sb.Append("ORDER BY createdutc DESC, guid DESC ");
                sb.Append("LIMIT ").Append(search.PageSize).Append(" OFFSET ").Append(search.Page * search.PageSize).Append(";");
            }
            else
            {
                sb.Append(";");
            }

            return sb.ToString();
        }

        internal static string UpdateRole(AuthorizationRole role)
        {
            return
                "UPDATE 'authorizationroles' SET "
                + "tenantguid = " + GuidOrNull(role.TenantGUID) + ", "
                + "name = " + SqlString(role.Name) + ", "
                + "displayname = " + SqlString(role.DisplayName) + ", "
                + "description = " + SqlString(role.Description) + ", "
                + "builtin = " + (role.BuiltIn ? "1" : "0") + ", "
                + "builtinrole = " + SqlString(role.BuiltInRole.ToString()) + ", "
                + "resourcescope = " + SqlString(role.ResourceScope.ToString()) + ", "
                + "permissions = " + SqlJson(Serializer.SerializeJson(role.Permissions ?? new System.Collections.Generic.List<AuthorizationPermissionEnum>(), false)) + ", "
                + "resourcetypes = " + SqlJson(Serializer.SerializeJson(role.ResourceTypes ?? new System.Collections.Generic.List<AuthorizationResourceTypeEnum>(), false)) + ", "
                + "inheritstographs = " + (role.InheritsToGraphs ? "1" : "0") + ", "
                + "lastupdateutc = " + SqlString(DateTime.UtcNow.ToString(TimestampFormat)) + " "
                + "WHERE guid = '" + role.GUID + "' "
                + "RETURNING *;";
        }

        internal static string DeleteRole(Guid guid)
        {
            return "DELETE FROM 'authorizationroles' WHERE guid = '" + guid + "';";
        }

        internal static string InsertUserRole(UserRoleAssignment assignment)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO 'userroleassignments' (");
            sb.Append("guid, tenantguid, userguid, roleguid, rolename, resourcescope, graphguid, createdutc, lastupdateutc");
            sb.Append(") VALUES (");
            sb.Append(SqlString(assignment.GUID.ToString())).Append(", ");
            sb.Append(SqlString(assignment.TenantGUID.ToString())).Append(", ");
            sb.Append(SqlString(assignment.UserGUID.ToString())).Append(", ");
            sb.Append(GuidOrNull(assignment.RoleGUID)).Append(", ");
            sb.Append(SqlString(assignment.RoleName)).Append(", ");
            sb.Append(SqlString(assignment.ResourceScope.ToString())).Append(", ");
            sb.Append(GuidOrNull(assignment.GraphGUID)).Append(", ");
            sb.Append(SqlString(assignment.CreatedUtc.ToString(TimestampFormat))).Append(", ");
            sb.Append(SqlString(assignment.LastUpdateUtc.ToString(TimestampFormat)));
            sb.Append(") RETURNING *;");
            return sb.ToString();
        }

        internal static string SelectUserRoleByGuid(Guid guid)
        {
            return "SELECT * FROM 'userroleassignments' WHERE guid = '" + guid + "';";
        }

        internal static string SearchUserRoles(UserRoleAssignmentSearchRequest search, bool countOnly)
        {
            StringBuilder sb = new StringBuilder();
            if (countOnly) sb.Append("SELECT COUNT(*) AS record_count FROM 'userroleassignments' WHERE 1=1 ");
            else sb.Append("SELECT * FROM 'userroleassignments' WHERE 1=1 ");

            AppendUserRoleFilters(sb, search);

            if (!countOnly)
            {
                sb.Append("ORDER BY createdutc DESC, guid DESC ");
                sb.Append("LIMIT ").Append(search.PageSize).Append(" OFFSET ").Append(search.Page * search.PageSize).Append(";");
            }
            else
            {
                sb.Append(";");
            }

            return sb.ToString();
        }

        internal static string DeleteUserRole(Guid guid)
        {
            return "DELETE FROM 'userroleassignments' WHERE guid = '" + guid + "';";
        }

        internal static string UpdateUserRole(UserRoleAssignment assignment)
        {
            return
                "UPDATE 'userroleassignments' SET "
                + "tenantguid = " + SqlString(assignment.TenantGUID.ToString()) + ", "
                + "userguid = " + SqlString(assignment.UserGUID.ToString()) + ", "
                + "roleguid = " + GuidOrNull(assignment.RoleGUID) + ", "
                + "rolename = " + SqlString(assignment.RoleName) + ", "
                + "resourcescope = " + SqlString(assignment.ResourceScope.ToString()) + ", "
                + "graphguid = " + GuidOrNull(assignment.GraphGUID) + ", "
                + "lastupdateutc = " + SqlString(DateTime.UtcNow.ToString(TimestampFormat)) + " "
                + "WHERE guid = '" + assignment.GUID + "' "
                + "RETURNING *;";
        }

        internal static string DeleteUserRoles(UserRoleAssignmentSearchRequest search)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("DELETE FROM 'userroleassignments' WHERE 1=1 ");
            AppendUserRoleFilters(sb, search);
            sb.Append(";");
            return sb.ToString();
        }

        internal static string InsertCredentialScope(CredentialScopeAssignment assignment)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO 'credentialscopeassignments' (");
            sb.Append("guid, tenantguid, credentialguid, roleguid, rolename, resourcescope, graphguid, permissions, resourcetypes, createdutc, lastupdateutc");
            sb.Append(") VALUES (");
            sb.Append(SqlString(assignment.GUID.ToString())).Append(", ");
            sb.Append(SqlString(assignment.TenantGUID.ToString())).Append(", ");
            sb.Append(SqlString(assignment.CredentialGUID.ToString())).Append(", ");
            sb.Append(GuidOrNull(assignment.RoleGUID)).Append(", ");
            sb.Append(SqlString(assignment.RoleName)).Append(", ");
            sb.Append(SqlString(assignment.ResourceScope.ToString())).Append(", ");
            sb.Append(GuidOrNull(assignment.GraphGUID)).Append(", ");
            sb.Append(SqlJson(Serializer.SerializeJson(assignment.Permissions ?? new System.Collections.Generic.List<AuthorizationPermissionEnum>(), false))).Append(", ");
            sb.Append(SqlJson(Serializer.SerializeJson(assignment.ResourceTypes ?? new System.Collections.Generic.List<AuthorizationResourceTypeEnum>(), false))).Append(", ");
            sb.Append(SqlString(assignment.CreatedUtc.ToString(TimestampFormat))).Append(", ");
            sb.Append(SqlString(assignment.LastUpdateUtc.ToString(TimestampFormat)));
            sb.Append(") RETURNING *;");
            return sb.ToString();
        }

        internal static string SelectCredentialScopeByGuid(Guid guid)
        {
            return "SELECT * FROM 'credentialscopeassignments' WHERE guid = '" + guid + "';";
        }

        internal static string SearchCredentialScopes(CredentialScopeAssignmentSearchRequest search, bool countOnly)
        {
            StringBuilder sb = new StringBuilder();
            if (countOnly) sb.Append("SELECT COUNT(*) AS record_count FROM 'credentialscopeassignments' WHERE 1=1 ");
            else sb.Append("SELECT * FROM 'credentialscopeassignments' WHERE 1=1 ");

            AppendCredentialScopeFilters(sb, search);

            if (!countOnly)
            {
                sb.Append("ORDER BY createdutc DESC, guid DESC ");
                sb.Append("LIMIT ").Append(search.PageSize).Append(" OFFSET ").Append(search.Page * search.PageSize).Append(";");
            }
            else
            {
                sb.Append(";");
            }

            return sb.ToString();
        }

        internal static string DeleteCredentialScope(Guid guid)
        {
            return "DELETE FROM 'credentialscopeassignments' WHERE guid = '" + guid + "';";
        }

        internal static string UpdateCredentialScope(CredentialScopeAssignment assignment)
        {
            return
                "UPDATE 'credentialscopeassignments' SET "
                + "tenantguid = " + SqlString(assignment.TenantGUID.ToString()) + ", "
                + "credentialguid = " + SqlString(assignment.CredentialGUID.ToString()) + ", "
                + "roleguid = " + GuidOrNull(assignment.RoleGUID) + ", "
                + "rolename = " + SqlString(assignment.RoleName) + ", "
                + "resourcescope = " + SqlString(assignment.ResourceScope.ToString()) + ", "
                + "graphguid = " + GuidOrNull(assignment.GraphGUID) + ", "
                + "permissions = " + SqlJson(Serializer.SerializeJson(assignment.Permissions ?? new System.Collections.Generic.List<AuthorizationPermissionEnum>(), false)) + ", "
                + "resourcetypes = " + SqlJson(Serializer.SerializeJson(assignment.ResourceTypes ?? new System.Collections.Generic.List<AuthorizationResourceTypeEnum>(), false)) + ", "
                + "lastupdateutc = " + SqlString(DateTime.UtcNow.ToString(TimestampFormat)) + " "
                + "WHERE guid = '" + assignment.GUID + "' "
                + "RETURNING *;";
        }

        internal static string DeleteCredentialScopes(CredentialScopeAssignmentSearchRequest search)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("DELETE FROM 'credentialscopeassignments' WHERE 1=1 ");
            AppendCredentialScopeFilters(sb, search);
            sb.Append(";");
            return sb.ToString();
        }

        private static void AppendRoleFilters(StringBuilder sb, AuthorizationRoleSearchRequest search)
        {
            if (search.TenantGUID.HasValue)
                sb.Append("AND tenantguid = '").Append(search.TenantGUID.Value).Append("' ");

            if (!String.IsNullOrEmpty(search.Name))
                sb.Append("AND name = ").Append(SqlString(search.Name)).Append(" ");

            if (search.BuiltIn.HasValue)
                sb.Append("AND builtin = ").Append(search.BuiltIn.Value ? "1" : "0").Append(" ");

            if (search.BuiltInRole.HasValue)
                sb.Append("AND builtinrole = ").Append(SqlString(search.BuiltInRole.Value.ToString())).Append(" ");

            if (search.ResourceScope.HasValue)
                sb.Append("AND resourcescope = ").Append(SqlString(search.ResourceScope.Value.ToString())).Append(" ");

            if (search.Permission.HasValue)
                sb.Append("AND permissions LIKE ").Append(SqlString("%" + search.Permission.Value + "%")).Append(" ");

            if (search.ResourceType.HasValue)
                sb.Append("AND resourcetypes LIKE ").Append(SqlString("%" + search.ResourceType.Value + "%")).Append(" ");

            if (search.FromUtc.HasValue)
                sb.Append("AND createdutc >= ").Append(SqlString(search.FromUtc.Value.ToString(TimestampFormat))).Append(" ");

            if (search.ToUtc.HasValue)
                sb.Append("AND createdutc < ").Append(SqlString(search.ToUtc.Value.ToString(TimestampFormat))).Append(" ");
        }

        private static void AppendUserRoleFilters(StringBuilder sb, UserRoleAssignmentSearchRequest search)
        {
            if (search.TenantGUID.HasValue)
                sb.Append("AND tenantguid = '").Append(search.TenantGUID.Value).Append("' ");

            if (search.UserGUID.HasValue)
                sb.Append("AND userguid = '").Append(search.UserGUID.Value).Append("' ");

            if (search.RoleGUID.HasValue)
                sb.Append("AND roleguid = '").Append(search.RoleGUID.Value).Append("' ");

            if (!String.IsNullOrEmpty(search.RoleName))
                sb.Append("AND rolename = ").Append(SqlString(search.RoleName)).Append(" ");

            if (search.ResourceScope.HasValue)
                sb.Append("AND resourcescope = ").Append(SqlString(search.ResourceScope.Value.ToString())).Append(" ");

            if (search.GraphGUID.HasValue)
                sb.Append("AND graphguid = '").Append(search.GraphGUID.Value).Append("' ");

            if (search.FromUtc.HasValue)
                sb.Append("AND createdutc >= ").Append(SqlString(search.FromUtc.Value.ToString(TimestampFormat))).Append(" ");

            if (search.ToUtc.HasValue)
                sb.Append("AND createdutc < ").Append(SqlString(search.ToUtc.Value.ToString(TimestampFormat))).Append(" ");
        }

        private static void AppendCredentialScopeFilters(StringBuilder sb, CredentialScopeAssignmentSearchRequest search)
        {
            if (search.TenantGUID.HasValue)
                sb.Append("AND tenantguid = '").Append(search.TenantGUID.Value).Append("' ");

            if (search.CredentialGUID.HasValue)
                sb.Append("AND credentialguid = '").Append(search.CredentialGUID.Value).Append("' ");

            if (search.RoleGUID.HasValue)
                sb.Append("AND roleguid = '").Append(search.RoleGUID.Value).Append("' ");

            if (!String.IsNullOrEmpty(search.RoleName))
                sb.Append("AND rolename = ").Append(SqlString(search.RoleName)).Append(" ");

            if (search.ResourceScope.HasValue)
                sb.Append("AND resourcescope = ").Append(SqlString(search.ResourceScope.Value.ToString())).Append(" ");

            if (search.GraphGUID.HasValue)
                sb.Append("AND graphguid = '").Append(search.GraphGUID.Value).Append("' ");

            if (search.Permission.HasValue)
                sb.Append("AND permissions LIKE ").Append(SqlString("%" + search.Permission.Value + "%")).Append(" ");

            if (search.ResourceType.HasValue)
                sb.Append("AND resourcetypes LIKE ").Append(SqlString("%" + search.ResourceType.Value + "%")).Append(" ");

            if (search.FromUtc.HasValue)
                sb.Append("AND createdutc >= ").Append(SqlString(search.FromUtc.Value.ToString(TimestampFormat))).Append(" ");

            if (search.ToUtc.HasValue)
                sb.Append("AND createdutc < ").Append(SqlString(search.ToUtc.Value.ToString(TimestampFormat))).Append(" ");
        }

        private static string GuidOrNull(Guid? guid)
        {
            return guid.HasValue ? SqlString(guid.Value.ToString()) : "NULL";
        }

        private static string SqlString(string value)
        {
            if (value == null) return "NULL";
            return "'" + EscapeQuotes(value) + "'";
        }

        private static string SqlJson(string json)
        {
            if (String.IsNullOrEmpty(json)) return "NULL";
            return "'" + Sanitizer.SanitizeJson(json).Replace("'", "''") + "'";
        }

        private static string EscapeQuotes(string value)
        {
            if (value == null) return "";
            return Sanitizer.Sanitize(value).Replace("'", "''");
        }
    }
}

