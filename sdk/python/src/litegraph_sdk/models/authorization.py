import uuid
from datetime import datetime, timezone
from typing import List, Optional

from pydantic import BaseModel, ConfigDict, Field


class AuthorizationRoleModel(BaseModel):
    """
    Authorization role metadata.
    """

    guid: str = Field(default_factory=lambda: str(uuid.uuid4()), alias="GUID")
    tenant_guid: Optional[str] = Field(default=None, alias="TenantGUID")
    name: str = Field(default="", alias="Name")
    display_name: Optional[str] = Field(default=None, alias="DisplayName")
    description: Optional[str] = Field(default=None, alias="Description")
    built_in: bool = Field(default=False, alias="BuiltIn")
    built_in_role: Optional[str] = Field(default="Custom", alias="BuiltInRole")
    resource_scope: str = Field(default="Tenant", alias="ResourceScope")
    permissions: List[str] = Field(default_factory=list, alias="Permissions")
    resource_types: List[str] = Field(default_factory=list, alias="ResourceTypes")
    inherits_to_graphs: bool = Field(default=False, alias="InheritsToGraphs")
    created_utc: datetime = Field(
        default_factory=lambda: datetime.now(timezone.utc), alias="CreatedUtc"
    )
    last_update_utc: datetime = Field(
        default_factory=lambda: datetime.now(timezone.utc), alias="LastUpdateUtc"
    )

    model_config = ConfigDict(populate_by_name=True, from_attributes=True)


class UserRoleAssignmentModel(BaseModel):
    """
    User role assignment.
    """

    guid: str = Field(default_factory=lambda: str(uuid.uuid4()), alias="GUID")
    tenant_guid: str = Field(alias="TenantGUID")
    user_guid: str = Field(alias="UserGUID")
    role_guid: Optional[str] = Field(default=None, alias="RoleGUID")
    role_name: Optional[str] = Field(default=None, alias="RoleName")
    resource_scope: str = Field(default="Tenant", alias="ResourceScope")
    graph_guid: Optional[str] = Field(default=None, alias="GraphGUID")
    created_utc: datetime = Field(
        default_factory=lambda: datetime.now(timezone.utc), alias="CreatedUtc"
    )
    last_update_utc: datetime = Field(
        default_factory=lambda: datetime.now(timezone.utc), alias="LastUpdateUtc"
    )

    model_config = ConfigDict(populate_by_name=True, from_attributes=True)


class CredentialScopeAssignmentModel(BaseModel):
    """
    Credential scope assignment.
    """

    guid: str = Field(default_factory=lambda: str(uuid.uuid4()), alias="GUID")
    tenant_guid: str = Field(alias="TenantGUID")
    credential_guid: str = Field(alias="CredentialGUID")
    role_guid: Optional[str] = Field(default=None, alias="RoleGUID")
    role_name: Optional[str] = Field(default=None, alias="RoleName")
    resource_scope: str = Field(default="Tenant", alias="ResourceScope")
    graph_guid: Optional[str] = Field(default=None, alias="GraphGUID")
    permissions: List[str] = Field(default_factory=list, alias="Permissions")
    resource_types: List[str] = Field(default_factory=list, alias="ResourceTypes")
    created_utc: datetime = Field(
        default_factory=lambda: datetime.now(timezone.utc), alias="CreatedUtc"
    )
    last_update_utc: datetime = Field(
        default_factory=lambda: datetime.now(timezone.utc), alias="LastUpdateUtc"
    )

    model_config = ConfigDict(populate_by_name=True, from_attributes=True)


class AuthorizationEffectiveGrantModel(BaseModel):
    """
    Effective authorization grant resolved for a user or credential.
    """

    source: str = Field(alias="Source")
    assignment_guid: str = Field(alias="AssignmentGUID")
    role_guid: Optional[str] = Field(default=None, alias="RoleGUID")
    role_name: Optional[str] = Field(default=None, alias="RoleName")
    resource_scope: str = Field(alias="ResourceScope")
    graph_guid: Optional[str] = Field(default=None, alias="GraphGUID")
    permissions: List[str] = Field(default_factory=list, alias="Permissions")
    resource_types: List[str] = Field(default_factory=list, alias="ResourceTypes")
    inherits_to_graphs: bool = Field(default=False, alias="InheritsToGraphs")
    applies_to_requested_graph: bool = Field(
        default=False, alias="AppliesToRequestedGraph"
    )

    model_config = ConfigDict(populate_by_name=True, from_attributes=True)


class AuthorizationEffectivePermissionsResultModel(BaseModel):
    """
    Effective authorization permissions for a user or credential.
    """

    tenant_guid: str = Field(alias="TenantGUID")
    user_guid: Optional[str] = Field(default=None, alias="UserGUID")
    credential_guid: Optional[str] = Field(default=None, alias="CredentialGUID")
    graph_guid: Optional[str] = Field(default=None, alias="GraphGUID")
    grants: List[AuthorizationEffectiveGrantModel] = Field(
        default_factory=list, alias="Grants"
    )
    user_role_assignments: List[UserRoleAssignmentModel] = Field(
        default_factory=list, alias="UserRoleAssignments"
    )
    credential_scope_assignments: List[CredentialScopeAssignmentModel] = Field(
        default_factory=list, alias="CredentialScopeAssignments"
    )
    roles: List[AuthorizationRoleModel] = Field(default_factory=list, alias="Roles")

    model_config = ConfigDict(populate_by_name=True, from_attributes=True)
