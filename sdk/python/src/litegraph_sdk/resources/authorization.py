from typing import Any, Dict, Optional, Union
from urllib.parse import urlencode

from pydantic import BaseModel

from ..configuration import get_client
from ..exceptions import TENANT_REQUIRED_ERROR
from ..mixins import JSON_CONTENT_TYPE
from ..models.authorization import (
    AuthorizationEffectivePermissionsResultModel,
    AuthorizationRoleModel,
    CredentialScopeAssignmentModel,
    UserRoleAssignmentModel,
)
from ..models.enumeration_result import EnumerationResultModel, parse_enumeration_result


def _to_json(payload: Union[BaseModel, Dict[str, Any]]) -> Dict[str, Any]:
    if isinstance(payload, BaseModel):
        return payload.model_dump(mode="json", by_alias=True, exclude_none=True)
    return dict(payload)


def _query(params: Dict[str, Any]) -> str:
    filtered = {
        key: value
        for key, value in params.items()
        if value is not None and value != ""
    }
    if not filtered:
        return ""
    return "?" + urlencode(filtered)


class Authorization:
    """
    Authorization role, user-role, credential-scope, and effective-permission helpers.
    """

    @classmethod
    def _tenant_guid(cls, tenant_guid: Optional[str] = None) -> str:
        client = get_client()
        tid = tenant_guid or client.tenant_guid
        if tid is None:
            raise ValueError(TENANT_REQUIRED_ERROR)
        return tid

    @classmethod
    def list_roles(
        cls,
        tenant_guid: Optional[str] = None,
        page: int = 0,
        page_size: int = 1000,
        include_built_ins: bool = True,
        built_in: Optional[bool] = None,
        name: Optional[str] = None,
        resource_scope: Optional[str] = None,
        permission: Optional[str] = None,
        resource_type: Optional[str] = None,
        max_keys: Optional[int] = None,
        skip: Optional[int] = None,
    ) -> EnumerationResultModel:
        """
        List authorization roles as an EnumerationResult envelope whose
        ``objects`` are AuthorizationRoleModel items. The enumeration-style
        ``max_keys`` and ``skip`` parameters take precedence over the legacy
        ``page`` and ``page_size`` parameters on the server.
        """
        client = get_client()
        tid = cls._tenant_guid(tenant_guid)
        url = f"v1.0/tenants/{tid}/roles" + _query(
            {
                "page": page,
                "pageSize": page_size,
                "includeBuiltIns": str(include_built_ins).lower(),
                "builtIn": None if built_in is None else str(built_in).lower(),
                "name": name,
                "resourceScope": resource_scope,
                "permission": permission,
                "resourceType": resource_type,
                "max-keys": max_keys,
                "skip": skip,
            }
        )
        return parse_enumeration_result(
            client.request("GET", url), AuthorizationRoleModel
        )

    @classmethod
    def create_role(
        cls,
        role: Union[AuthorizationRoleModel, Dict[str, Any]],
        tenant_guid: Optional[str] = None,
    ) -> AuthorizationRoleModel:
        client = get_client()
        tid = cls._tenant_guid(tenant_guid)
        response = client.request(
            "PUT",
            f"v1.0/tenants/{tid}/roles",
            json=_to_json(role),
            headers=JSON_CONTENT_TYPE,
        )
        return AuthorizationRoleModel.model_validate(response)

    @classmethod
    def read_role(
        cls,
        role_guid: str,
        tenant_guid: Optional[str] = None,
    ) -> AuthorizationRoleModel:
        client = get_client()
        tid = cls._tenant_guid(tenant_guid)
        return AuthorizationRoleModel.model_validate(
            client.request("GET", f"v1.0/tenants/{tid}/roles/{role_guid}")
        )

    @classmethod
    def update_role(
        cls,
        role: Union[AuthorizationRoleModel, Dict[str, Any]],
        tenant_guid: Optional[str] = None,
        role_guid: Optional[str] = None,
    ) -> AuthorizationRoleModel:
        client = get_client()
        tid = cls._tenant_guid(tenant_guid)
        payload = _to_json(role)
        rid = role_guid or payload.get("GUID")
        if not rid:
            raise ValueError("Role GUID is required")
        response = client.request(
            "PUT",
            f"v1.0/tenants/{tid}/roles/{rid}",
            json=payload,
            headers=JSON_CONTENT_TYPE,
        )
        return AuthorizationRoleModel.model_validate(response)

    @classmethod
    def delete_role(cls, role_guid: str, tenant_guid: Optional[str] = None) -> None:
        client = get_client()
        tid = cls._tenant_guid(tenant_guid)
        client.request("DELETE", f"v1.0/tenants/{tid}/roles/{role_guid}")

    @classmethod
    def list_user_roles(
        cls,
        user_guid: str,
        tenant_guid: Optional[str] = None,
        page: int = 0,
        page_size: int = 1000,
        role_name: Optional[str] = None,
        resource_scope: Optional[str] = None,
        graph_guid: Optional[str] = None,
        max_keys: Optional[int] = None,
        skip: Optional[int] = None,
    ) -> EnumerationResultModel:
        """
        List a user's role assignments as an EnumerationResult envelope whose
        ``objects`` are UserRoleAssignmentModel items. The enumeration-style
        ``max_keys`` and ``skip`` parameters take precedence over the legacy
        ``page`` and ``page_size`` parameters on the server.
        """
        client = get_client()
        tid = cls._tenant_guid(tenant_guid)
        url = f"v1.0/tenants/{tid}/users/{user_guid}/roles" + _query(
            {
                "page": page,
                "pageSize": page_size,
                "roleName": role_name,
                "resourceScope": resource_scope,
                "graphGuid": graph_guid,
                "max-keys": max_keys,
                "skip": skip,
            }
        )
        return parse_enumeration_result(
            client.request("GET", url), UserRoleAssignmentModel
        )

    @classmethod
    def create_user_role(
        cls,
        user_guid: str,
        assignment: Union[UserRoleAssignmentModel, Dict[str, Any]],
        tenant_guid: Optional[str] = None,
    ) -> UserRoleAssignmentModel:
        client = get_client()
        tid = cls._tenant_guid(tenant_guid)
        response = client.request(
            "PUT",
            f"v1.0/tenants/{tid}/users/{user_guid}/roles",
            json=_to_json(assignment),
            headers=JSON_CONTENT_TYPE,
        )
        return UserRoleAssignmentModel.model_validate(response)

    @classmethod
    def read_user_role(
        cls,
        user_guid: str,
        assignment_guid: str,
        tenant_guid: Optional[str] = None,
    ) -> UserRoleAssignmentModel:
        client = get_client()
        tid = cls._tenant_guid(tenant_guid)
        return UserRoleAssignmentModel.model_validate(
            client.request(
                "GET",
                f"v1.0/tenants/{tid}/users/{user_guid}/roles/{assignment_guid}",
            )
        )

    @classmethod
    def update_user_role(
        cls,
        user_guid: str,
        assignment: Union[UserRoleAssignmentModel, Dict[str, Any]],
        tenant_guid: Optional[str] = None,
        assignment_guid: Optional[str] = None,
    ) -> UserRoleAssignmentModel:
        client = get_client()
        tid = cls._tenant_guid(tenant_guid)
        payload = _to_json(assignment)
        aid = assignment_guid or payload.get("GUID")
        if not aid:
            raise ValueError("Assignment GUID is required")
        response = client.request(
            "PUT",
            f"v1.0/tenants/{tid}/users/{user_guid}/roles/{aid}",
            json=payload,
            headers=JSON_CONTENT_TYPE,
        )
        return UserRoleAssignmentModel.model_validate(response)

    @classmethod
    def delete_user_role(
        cls,
        user_guid: str,
        assignment_guid: str,
        tenant_guid: Optional[str] = None,
    ) -> None:
        client = get_client()
        tid = cls._tenant_guid(tenant_guid)
        client.request(
            "DELETE",
            f"v1.0/tenants/{tid}/users/{user_guid}/roles/{assignment_guid}",
        )

    @classmethod
    def get_user_effective_permissions(
        cls,
        user_guid: str,
        tenant_guid: Optional[str] = None,
        graph_guid: Optional[str] = None,
    ) -> AuthorizationEffectivePermissionsResultModel:
        client = get_client()
        tid = cls._tenant_guid(tenant_guid)
        url = f"v1.0/tenants/{tid}/users/{user_guid}/permissions" + _query(
            {"graphGuid": graph_guid}
        )
        return AuthorizationEffectivePermissionsResultModel.model_validate(
            client.request("GET", url)
        )

    @classmethod
    def list_credential_scopes(
        cls,
        credential_guid: str,
        tenant_guid: Optional[str] = None,
        page: int = 0,
        page_size: int = 1000,
        role_name: Optional[str] = None,
        resource_scope: Optional[str] = None,
        graph_guid: Optional[str] = None,
        permission: Optional[str] = None,
        resource_type: Optional[str] = None,
        max_keys: Optional[int] = None,
        skip: Optional[int] = None,
    ) -> EnumerationResultModel:
        """
        List a credential's scope assignments as an EnumerationResult envelope
        whose ``objects`` are CredentialScopeAssignmentModel items. The
        enumeration-style ``max_keys`` and ``skip`` parameters take precedence
        over the legacy ``page`` and ``page_size`` parameters on the server.
        """
        client = get_client()
        tid = cls._tenant_guid(tenant_guid)
        url = f"v1.0/tenants/{tid}/credentials/{credential_guid}/scopes" + _query(
            {
                "page": page,
                "pageSize": page_size,
                "roleName": role_name,
                "resourceScope": resource_scope,
                "graphGuid": graph_guid,
                "permission": permission,
                "resourceType": resource_type,
                "max-keys": max_keys,
                "skip": skip,
            }
        )
        return parse_enumeration_result(
            client.request("GET", url), CredentialScopeAssignmentModel
        )

    @classmethod
    def create_credential_scope(
        cls,
        credential_guid: str,
        assignment: Union[CredentialScopeAssignmentModel, Dict[str, Any]],
        tenant_guid: Optional[str] = None,
    ) -> CredentialScopeAssignmentModel:
        client = get_client()
        tid = cls._tenant_guid(tenant_guid)
        response = client.request(
            "PUT",
            f"v1.0/tenants/{tid}/credentials/{credential_guid}/scopes",
            json=_to_json(assignment),
            headers=JSON_CONTENT_TYPE,
        )
        return CredentialScopeAssignmentModel.model_validate(response)

    @classmethod
    def read_credential_scope(
        cls,
        credential_guid: str,
        assignment_guid: str,
        tenant_guid: Optional[str] = None,
    ) -> CredentialScopeAssignmentModel:
        client = get_client()
        tid = cls._tenant_guid(tenant_guid)
        return CredentialScopeAssignmentModel.model_validate(
            client.request(
                "GET",
                f"v1.0/tenants/{tid}/credentials/{credential_guid}/scopes/{assignment_guid}",
            )
        )

    @classmethod
    def update_credential_scope(
        cls,
        credential_guid: str,
        assignment: Union[CredentialScopeAssignmentModel, Dict[str, Any]],
        tenant_guid: Optional[str] = None,
        assignment_guid: Optional[str] = None,
    ) -> CredentialScopeAssignmentModel:
        client = get_client()
        tid = cls._tenant_guid(tenant_guid)
        payload = _to_json(assignment)
        aid = assignment_guid or payload.get("GUID")
        if not aid:
            raise ValueError("Assignment GUID is required")
        response = client.request(
            "PUT",
            f"v1.0/tenants/{tid}/credentials/{credential_guid}/scopes/{aid}",
            json=payload,
            headers=JSON_CONTENT_TYPE,
        )
        return CredentialScopeAssignmentModel.model_validate(response)

    @classmethod
    def delete_credential_scope(
        cls,
        credential_guid: str,
        assignment_guid: str,
        tenant_guid: Optional[str] = None,
    ) -> None:
        client = get_client()
        tid = cls._tenant_guid(tenant_guid)
        client.request(
            "DELETE",
            f"v1.0/tenants/{tid}/credentials/{credential_guid}/scopes/{assignment_guid}",
        )

    @classmethod
    def get_credential_effective_permissions(
        cls,
        credential_guid: str,
        tenant_guid: Optional[str] = None,
        graph_guid: Optional[str] = None,
    ) -> AuthorizationEffectivePermissionsResultModel:
        client = get_client()
        tid = cls._tenant_guid(tenant_guid)
        url = f"v1.0/tenants/{tid}/credentials/{credential_guid}/permissions" + _query(
            {"graphGuid": graph_guid}
        )
        return AuthorizationEffectivePermissionsResultModel.model_validate(
            client.request("GET", url)
        )
