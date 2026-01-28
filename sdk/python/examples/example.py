import litegraph_sdk

litegraph_sdk.configure(
    endpoint="http://192.168.101.63:8701",
    tenant_guid="00000000-0000-0000-0000-000000000000",
    graph_guid="00000000-0000-0000-0000-000000000000",
    access_key="litegraphadmin",
)

# Check if tenant exists
exists = litegraph_sdk.Graph.exists(
    resource_guid="00000000-0000-0000-0000-000000000000"
)
print(f"Tenant exists: {exists}")


# Create a new credential
new_tenant = litegraph_sdk.Credential.create(
    **{
        "UserGUID": "00000000-0000-0000-0000-000000000000",
        "Name": "New credential",
        "BearerToken": "foobar",
        "Active": True,
    }
)
print(f"Created tenant: {new_tenant}")

# Update a credential
updated_tenant = litegraph_sdk.Credential.update(
    resource_guid="cea747d4-9af5-4d79-917f-351a88f1c738",
    **{
        "UserGUID": "00000000-0000-0000-0000-000000000000",
        "Name": "Updated credential",
        "BearerToken": "default",
        "Active": True,
    },
)
print(f"Updated tenant: {updated_tenant}")

# Retrieve a specific credential
tenant = litegraph_sdk.User.retrieve(
    resource_guid="00000000-0000-0000-0000-000000000000"
)
print(f"Retrieved tenant: {tenant}")
# Delete a credential
litegraph_sdk.Credential.delete(resource_guid="1deacf52-c1ea-4866-a78b-ed5a46828930")
print("Tenant deleted")

# Search
all_tenants = litegraph_sdk.Graph.search(
    graph_guid="00000000-0000-0000-0000-000000000000",
    **{
        "Ordering": "CreatedDescending",
        "Labels": ["updated", "test"],
        "Tags": {"type": "graph"},
    },
)
print(f"All tenants: {all_tenants}")
