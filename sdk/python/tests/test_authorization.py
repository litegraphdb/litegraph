from unittest.mock import Mock

import pytest

from litegraph_sdk.base import BaseClient
from litegraph_sdk.models.authorization import (
    AuthorizationRoleModel,
    CredentialScopeAssignmentModel,
    UserRoleAssignmentModel,
)
from litegraph_sdk.resources.authorization import Authorization


@pytest.fixture
def mock_client(monkeypatch):
    client = Mock(spec=BaseClient)
    client.tenant_guid = "tenant-1"
    monkeypatch.setattr("litegraph_sdk.configuration._client", client)
    return client


def test_authorization_role_model_serializes_permissions():
    role = AuthorizationRoleModel(
        GUID="role-1",
        TenantGUID="tenant-1",
        Name="GraphAuditor",
        DisplayName="Graph auditor",
        ResourceScope="Graph",
        Permissions=["Read"],
        ResourceTypes=["Graph", "Node", "Edge"],
    )

    payload = role.model_dump(mode="json", by_alias=True, exclude_none=True)

    assert payload["GUID"] == "role-1"
    assert payload["Name"] == "GraphAuditor"
    assert payload["Permissions"] == ["Read"]
    assert payload["ResourceTypes"] == ["Graph", "Node", "Edge"]


def test_list_roles_includes_built_ins_by_default(mock_client):
    mock_client.request.return_value = {
        "Success": True,
        "MaxResults": 1000,
        "ContinuationToken": None,
        "EndOfResults": True,
        "TotalRecords": 1,
        "RecordsRemaining": 0,
        "Objects": [
            {
                "GUID": "role-1",
                "TenantGUID": None,
                "Name": "Viewer",
                "BuiltIn": True,
                "ResourceScope": "Tenant",
                "Permissions": ["Read"],
                "ResourceTypes": ["Graph"],
            }
        ],
    }

    result = Authorization.list_roles()

    args, _ = mock_client.request.call_args
    assert args[0] == "GET"
    assert args[1] == (
        "v1.0/tenants/tenant-1/roles?"
        "page=0&pageSize=1000&includeBuiltIns=true"
    )
    assert result.total_records == 1
    assert result.end_of_results is True
    assert result.objects[0].name == "Viewer"
    assert result.objects[0].built_in is True


def test_list_roles_enumeration_paging_params(mock_client):
    mock_client.request.return_value = {
        "Success": True,
        "MaxResults": 10,
        "ContinuationToken": None,
        "EndOfResults": False,
        "TotalRecords": 25,
        "RecordsRemaining": 5,
        "Objects": [],
    }

    result = Authorization.list_roles(max_keys=10, skip=10)

    args, _ = mock_client.request.call_args
    assert "max-keys=10" in args[1]
    assert "skip=10" in args[1]
    assert result.records_remaining == 5
    assert result.end_of_results is False


def test_create_and_update_user_role_assignment_routes(mock_client):
    assignment = UserRoleAssignmentModel(
        GUID="assignment-1",
        TenantGUID="tenant-1",
        UserGUID="user-1",
        RoleName="Viewer",
        ResourceScope="Graph",
        GraphGUID="graph-1",
    )
    mock_client.request.return_value = assignment.model_dump(mode="json", by_alias=True)

    created = Authorization.create_user_role("user-1", assignment)
    updated = Authorization.update_user_role(
        "user-1",
        {"GUID": "assignment-1", "RoleName": "Editor", "ResourceScope": "Tenant"},
    )

    assert created.role_name == "Viewer"
    assert updated.role_name == "Viewer"
    assert mock_client.request.call_args_list[0].args[:2] == (
        "PUT",
        "v1.0/tenants/tenant-1/users/user-1/roles",
    )
    assert mock_client.request.call_args_list[1].args[:2] == (
        "PUT",
        "v1.0/tenants/tenant-1/users/user-1/roles/assignment-1",
    )


def test_credential_scope_and_effective_permission_routes(mock_client):
    scope = CredentialScopeAssignmentModel(
        GUID="scope-1",
        TenantGUID="tenant-1",
        CredentialGUID="credential-1",
        ResourceScope="Graph",
        GraphGUID="graph-1",
        Permissions=["Read"],
        ResourceTypes=["Graph", "Query"],
    )
    mock_client.request.side_effect = [
        scope.model_dump(mode="json", by_alias=True),
        {
            "TenantGUID": "tenant-1",
            "CredentialGUID": "credential-1",
            "GraphGUID": "graph-1",
            "Grants": [],
            "UserRoleAssignments": [],
            "CredentialScopeAssignments": [
                scope.model_dump(mode="json", by_alias=True)
            ],
            "Roles": [],
        },
        None,
    ]

    created = Authorization.create_credential_scope("credential-1", scope)
    effective = Authorization.get_credential_effective_permissions(
        "credential-1", graph_guid="graph-1"
    )
    Authorization.delete_credential_scope("credential-1", "scope-1")

    assert created.permissions == ["Read"]
    assert effective.credential_guid == "credential-1"
    assert mock_client.request.call_args_list[0].args[:2] == (
        "PUT",
        "v1.0/tenants/tenant-1/credentials/credential-1/scopes",
    )
    assert mock_client.request.call_args_list[1].args[:2] == (
        "GET",
        "v1.0/tenants/tenant-1/credentials/credential-1/permissions?graphGuid=graph-1",
    )
    assert mock_client.request.call_args_list[2].args[:2] == (
        "DELETE",
        "v1.0/tenants/tenant-1/credentials/credential-1/scopes/scope-1",
    )


def test_authorization_requires_tenant_guid(mock_client):
    mock_client.tenant_guid = None

    with pytest.raises(ValueError, match="Tenant GUID is required"):
        Authorization.list_roles()
