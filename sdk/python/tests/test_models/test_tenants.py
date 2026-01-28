import uuid
from datetime import datetime, timezone
from unittest.mock import Mock, patch

import pytest
from litegraph_sdk.models.tenant_metadata import TenantMetadataModel
from litegraph_sdk.resources.tenants import Tenant


@pytest.fixture
def valid_tenant_data():
    """Fixture providing valid tenant data."""
    return {
        "GUID": str(uuid.uuid4()),
        "Name": "Test Tenant",
        "CreatedUtc": datetime.now(timezone.utc).isoformat(),
        "Data": {"key": "value"},
    }


@pytest.fixture
def minimal_tenant_data():
    """Fixture providing minimal valid tenant data."""
    return {"GUID": str(uuid.uuid4())}


@pytest.fixture
def mock_client(monkeypatch):
    """Create a mock client and configure it."""
    client = Mock()
    client.base_url = "http://test-api.com"
    monkeypatch.setattr("litegraph_sdk.configuration._client", client)
    return client


class TestTenantMetadataModel:
    def test_valid_tenant_creation(self, valid_tenant_data):
        """Test creating a tenant with valid data."""
        tenant = TenantMetadataModel(**valid_tenant_data)
        assert tenant.guid == valid_tenant_data["GUID"]
        assert tenant.name == valid_tenant_data["Name"]
        assert isinstance(tenant.created_utc, datetime)

    def test_minimal_tenant_creation(self, minimal_tenant_data):
        """Test creating a tenant with minimal required data."""
        tenant = TenantMetadataModel(**minimal_tenant_data)
        assert tenant.guid == minimal_tenant_data["GUID"]
        assert tenant.name is None
        assert isinstance(tenant.created_utc, datetime)

    def test_auto_generated_fields(self):
        """Test auto-generated fields when creating tenant."""
        tenant = TenantMetadataModel()
        assert isinstance(tenant.guid, str)
        uuid.UUID(tenant.guid)  # Validates UUID format
        assert isinstance(tenant.created_utc, datetime)
        assert tenant.created_utc.tzinfo == timezone.utc

    def test_model_export(self):
        """Test model serialization."""
        test_data = {
            "GUID": str(uuid.uuid4()),
            "Name": "Test Tenant",
        }

        tenant = TenantMetadataModel(**test_data)
        exported = tenant.model_dump(by_alias=True)

        assert exported["GUID"] == test_data["GUID"]
        assert exported["Name"] == test_data["Name"]
        assert "CreatedUtc" in exported


class TestTenantResource:
    def test_delete_tenant(self, mock_client):
        """Test deleting a tenant."""
        # Test normal deletion
        Tenant.delete("test-guid")
        mock_client.request.assert_called_once_with(
            "DELETE", "v1.0/tenants/test-guid"
        )

        # Test force deletion
        mock_client.request.reset_mock()
        Tenant.delete("test-guid", force=True)
        mock_client.request.assert_called_once_with(
            "DELETE", "v1.0/tenants/test-guid?force"
        )

    def test_tenant_exists(self, mock_client):
        """Test checking if a tenant exists."""
        mock_client.request.return_value = True
        result = Tenant.exists("test-guid")
        assert result is True
        mock_client.request.assert_called_once()

    def test_retrieve_tenant(self, mock_client, valid_tenant_data):
        """Test retrieving a tenant."""
        mock_client.request.return_value = valid_tenant_data
        tenant = Tenant.retrieve("test-guid")
        assert isinstance(tenant, TenantMetadataModel)
        assert tenant.guid == valid_tenant_data["GUID"]
        mock_client.request.assert_called_once()

    def test_retrieve_all_tenants(self, mock_client, valid_tenant_data):
        """Test retrieving all tenants."""
        mock_client.request.return_value = [valid_tenant_data]
        tenants = Tenant.retrieve_all()
        assert isinstance(tenants, list)
        assert len(tenants) == 1
        assert isinstance(tenants[0], TenantMetadataModel)
        mock_client.request.assert_called_once()

    def test_create_tenant(self, mock_client, valid_tenant_data):
        """Test creating a tenant."""
        mock_client.request.return_value = valid_tenant_data
        tenant = Tenant.create(name="Test Tenant", data={"key": "value"})
        assert isinstance(tenant, TenantMetadataModel)
        assert tenant.name == valid_tenant_data["Name"]
        mock_client.request.assert_called_once()

    def test_update_tenant(self, mock_client, valid_tenant_data):
        """Test updating a tenant."""
        mock_client.request.return_value = valid_tenant_data
        tenant = Tenant.update("test-guid", name="Updated Tenant")
        assert isinstance(tenant, TenantMetadataModel)
        assert tenant.name == valid_tenant_data["Name"]
        mock_client.request.assert_called_once()

    @pytest.mark.parametrize(
        "field,value,expected_type",
        [
            ("name", "Test Tenant", str),
            ("name", None, type(None)),
            ("created_utc", datetime.now(timezone.utc), datetime),
        ],
    )
    def test_field_types(self, field, value, expected_type):
        """Test field type validation."""
        tenant = TenantMetadataModel(GUID=str(uuid.uuid4()), **{field: value})
        assert isinstance(getattr(tenant, field), expected_type)
