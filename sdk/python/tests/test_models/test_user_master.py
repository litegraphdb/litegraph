import uuid

from litegraph_sdk.models.user_master import UserMasterModel


class TestUserMasterModel:
    def test_flags_default_to_false(self):
        """v8.0 admin flags default to False."""
        user = UserMasterModel(GUID=str(uuid.uuid4()), Email="user@test.com")
        assert user.is_system_admin is False
        assert user.is_tenant_admin is False

    def test_flags_round_trip_via_alias(self):
        """The flags hydrate from PascalCase API payloads and export back to them."""
        payload = {
            "GUID": str(uuid.uuid4()),
            "TenantGUID": str(uuid.uuid4()),
            "Email": "admin@test.com",
            "Active": True,
            "IsSystemAdmin": True,
            "IsTenantAdmin": True,
        }

        user = UserMasterModel(**payload)
        assert user.is_system_admin is True
        assert user.is_tenant_admin is True

        exported = user.model_dump(by_alias=True)
        assert exported["IsSystemAdmin"] is True
        assert exported["IsTenantAdmin"] is True

    def test_tenant_admin_without_system_admin(self):
        """A tenant administrator is not implicitly a system administrator."""
        user = UserMasterModel(
            GUID=str(uuid.uuid4()),
            Email="tenantadmin@test.com",
            IsTenantAdmin=True,
        )
        assert user.is_tenant_admin is True
        assert user.is_system_admin is False
