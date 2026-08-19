'use client';

import React, { useMemo, useState } from 'react';
import { useTranslations } from 'next-intl';
import {
  Alert,
  Button,
  Dropdown,
  Form,
  Input,
  Modal,
  Popconfirm,
  Select,
  Space,
  Switch,
  Tabs,
  Tag,
  Typography,
} from 'antd';
import {
  CodeOutlined,
  DeleteOutlined,
  EditOutlined,
  MoreOutlined,
  PlusSquareOutlined,
} from '@ant-design/icons';
import type { TableProps } from 'antd';
import PageContainer from '@/components/base/pageContainer/PageContainer';
import LitegraphButton from '@/components/base/button/Button';
import LitegraphTable from '@/components/base/table/Table';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';
import FallBack from '@/components/base/fallback/FallBack';
import ViewJsonModal from '@/components/base/view-json-modal/ViewJsonModal';
import { useSelectedTenant } from '@/hooks/entityHooks';
import { useAppSelector } from '@/lib/store/hooks';
import { RootState } from '@/lib/store/store';
import { canManageAuthorization } from '@/lib/authorization/permissions';
import {
  AuthorizationEffectiveGrant,
  AuthorizationPermission,
  AuthorizationResourceScope,
  AuthorizationResourceType,
  AuthorizationRole,
  CredentialScopeAssignment,
  UserRoleAssignment,
} from '@/lib/sdk/authorization';
import {
  useCreateAuthorizationRoleMutation,
  useCreateCredentialScopeAssignmentMutation,
  useCreateUserRoleAssignmentMutation,
  useDeleteAuthorizationRoleMutation,
  useDeleteCredentialScopeAssignmentMutation,
  useDeleteUserRoleAssignmentMutation,
  useGetAllCredentialsQuery,
  useGetAllGraphsQuery,
  useGetAllUsersQuery,
  useGetCredentialEffectivePermissionsQuery,
  useGetUserEffectivePermissionsQuery,
  useListAuthorizationRolesQuery,
  useListCredentialScopeAssignmentsQuery,
  useListUserRoleAssignmentsQuery,
  useUpdateAuthorizationRoleMutation,
} from '@/lib/store/slice/slice';

const permissionOptions: AuthorizationPermission[] = ['Read', 'Write', 'Delete', 'Admin'];
const resourceTypeOptions: AuthorizationResourceType[] = [
  'Admin',
  'Graph',
  'Node',
  'Edge',
  'Label',
  'Tag',
  'Vector',
  'Query',
  'Transaction',
];
const scopeOptions: AuthorizationResourceScope[] = ['Tenant', 'Graph'];

const sectionStyle: React.CSSProperties = {
  marginBottom: 24,
};

const formGridStyle: React.CSSProperties = {
  display: 'grid',
  gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))',
  gap: 12,
  alignItems: 'end',
};

const tags = (values?: string[]) => (
  <Space size={[4, 4]} wrap>
    {(values || []).map((value) => (
      <Tag key={value}>{value}</Tag>
    ))}
  </Space>
);

const guidLabel = (guid?: string | null) => {
  if (!guid) return 'Tenant scope';
  return guid;
};

const roleLabel = (role: AuthorizationRole) => {
  if (role.DisplayName && role.DisplayName !== role.Name) return `${role.DisplayName} (${role.Name})`;
  return role.Name;
};

type RoleModalProps = {
  open: boolean;
  role: AuthorizationRole | null;
  saving: boolean;
  onCancel: () => void;
  onSave: (role: Partial<AuthorizationRole>) => Promise<void>;
};

const RoleModal = ({ open, role, saving, onCancel, onSave }: RoleModalProps) => {
  const [form] = Form.useForm();

  React.useEffect(() => {
    if (!open) return;
    if (role) {
      form.setFieldsValue({
        Name: role.Name,
        DisplayName: role.DisplayName,
        Description: role.Description,
        ResourceScope: role.ResourceScope,
        Permissions: role.Permissions,
        ResourceTypes: role.ResourceTypes,
        InheritsToGraphs: role.InheritsToGraphs,
      });
    } else {
      form.resetFields();
      form.setFieldsValue({
        ResourceScope: 'Graph',
        Permissions: ['Read'],
        ResourceTypes: ['Graph'],
        InheritsToGraphs: false,
      });
    }
  }, [form, open, role]);

  const submit = async () => {
    const values = await form.validateFields();
    await onSave({
      ...role,
      ...values,
      BuiltIn: false,
      BuiltInRole: 'Custom',
      Permissions: values.Permissions || [],
      ResourceTypes: values.ResourceTypes || [],
      InheritsToGraphs: !!values.InheritsToGraphs,
    });
  };

  return (
    <Modal
      title={role ? 'Edit Role' : 'Create Role'}
      open={open}
      onCancel={onCancel}
      onOk={submit}
      confirmLoading={saving}
      okText={role ? 'Update' : 'Create'}
      destroyOnHidden
      maskClosable
    >
      <Form form={form} layout="vertical">
        <Form.Item name="Name" label="Name" rules={[{ required: true }]}>
          <Input placeholder="GraphAuditor" disabled={!!role} />
        </Form.Item>
        <Form.Item name="DisplayName" label="Display Name">
          <Input placeholder="Graph Auditor" />
        </Form.Item>
        <Form.Item name="Description" label="Description">
          <Input.TextArea rows={3} placeholder="Read-only audit access for selected graphs" />
        </Form.Item>
        <Form.Item name="ResourceScope" label="Default Scope" rules={[{ required: true }]}>
          <Select options={scopeOptions.map((value) => ({ label: value, value }))} />
        </Form.Item>
        <Form.Item name="Permissions" label="Permissions" rules={[{ required: true }]}>
          <Select
            mode="multiple"
            options={permissionOptions.map((value) => ({ label: value, value }))}
          />
        </Form.Item>
        <Form.Item name="ResourceTypes" label="Resource Types" rules={[{ required: true }]}>
          <Select
            mode="multiple"
            options={resourceTypeOptions.map((value) => ({ label: value, value }))}
          />
        </Form.Item>
        <Form.Item
          name="InheritsToGraphs"
          label="Tenant Scope Inherits To Graphs"
          valuePropName="checked"
        >
          <Switch />
        </Form.Item>
      </Form>
    </Modal>
  );
};

const AuthorizationPage = () => {
  // TODO(i18n): page not yet fully migrated — only title and primary actions are localized.
  const t = useTranslations('authorization');
  const selectedTenant = useSelectedTenant();
  const tenantGuid = selectedTenant?.GUID || '';
  const adminAccessKey = useAppSelector((state: RootState) => state.liteGraph.adminAccessKey);
  const currentUserGuid = useAppSelector(
    (state: RootState) => state.liteGraph.user?.GUID || state.liteGraph.token?.UserGUID || ''
  );

  const [roleModalOpen, setRoleModalOpen] = useState(false);
  const [editingRole, setEditingRole] = useState<AuthorizationRole | null>(null);
  const [jsonRole, setJsonRole] = useState<AuthorizationRole | null>(null);
  const [selectedUserGuid, setSelectedUserGuid] = useState<string>('');
  const [selectedCredentialGuid, setSelectedCredentialGuid] = useState<string>('');
  const [userRoleForm] = Form.useForm();
  const [credentialScopeForm] = Form.useForm();

  const {
    data: rolesResult,
    refetch: refetchRoles,
    isLoading: rolesLoading,
    isFetching: rolesFetching,
    error: rolesError,
  } = useListAuthorizationRolesQuery(
    { tenantGuid, params: { includeBuiltIns: true, page: 0, pageSize: 1000 } },
    { skip: !tenantGuid }
  );
  const roles = rolesResult?.Objects || [];

  const {
    data: users = [],
    isLoading: usersLoading,
    refetch: refetchUsers,
  } = useGetAllUsersQuery(undefined, {
    skip: !tenantGuid,
  });
  const {
    data: credentials = [],
    isLoading: credentialsLoading,
    refetch: refetchCredentials,
  } = useGetAllCredentialsQuery(undefined, { skip: !tenantGuid });
  const {
    data: graphs = [],
    isLoading: graphsLoading,
    refetch: refetchGraphs,
  } = useGetAllGraphsQuery(undefined, {
    skip: !tenantGuid,
  });

  const effectiveUserGuid = selectedUserGuid || users[0]?.GUID || '';
  const effectiveCredentialGuid = selectedCredentialGuid || credentials[0]?.GUID || '';

  const { data: currentEffectivePermissions, isLoading: currentPermissionsLoading } =
    useGetUserEffectivePermissionsQuery(
      { tenantGuid, userGuid: currentUserGuid },
      { skip: !tenantGuid || !currentUserGuid || !!adminAccessKey }
    );
  const canManage = canManageAuthorization(currentEffectivePermissions, !!adminAccessKey);

  const {
    data: userAssignmentsResult,
    isLoading: userAssignmentsLoading,
    isFetching: userAssignmentsFetching,
    refetch: refetchUserAssignments,
  } = useListUserRoleAssignmentsQuery(
    { tenantGuid, userGuid: effectiveUserGuid, params: { page: 0, pageSize: 1000 } },
    { skip: !tenantGuid || !effectiveUserGuid }
  );
  const userAssignments = userAssignmentsResult?.Objects || [];

  const {
    data: credentialScopesResult,
    isLoading: credentialScopesLoading,
    isFetching: credentialScopesFetching,
    refetch: refetchCredentialScopes,
  } = useListCredentialScopeAssignmentsQuery(
    { tenantGuid, credentialGuid: effectiveCredentialGuid, params: { page: 0, pageSize: 1000 } },
    { skip: !tenantGuid || !effectiveCredentialGuid }
  );
  const credentialScopes = credentialScopesResult?.Objects || [];

  const {
    data: userEffectivePermissions,
    isLoading: userEffectivePermissionsLoading,
    isFetching: userEffectivePermissionsFetching,
    refetch: refetchUserEffectivePermissions,
  } = useGetUserEffectivePermissionsQuery(
    { tenantGuid, userGuid: effectiveUserGuid },
    { skip: !tenantGuid || !effectiveUserGuid }
  );
  const {
    data: credentialEffectivePermissions,
    isLoading: credentialEffectivePermissionsLoading,
    isFetching: credentialEffectivePermissionsFetching,
    refetch: refetchCredentialEffectivePermissions,
  } = useGetCredentialEffectivePermissionsQuery(
    { tenantGuid, credentialGuid: effectiveCredentialGuid },
    { skip: !tenantGuid || !effectiveCredentialGuid }
  );

  const [createRole, { isLoading: creatingRole }] = useCreateAuthorizationRoleMutation();
  const [updateRole, { isLoading: updatingRole }] = useUpdateAuthorizationRoleMutation();
  const [deleteRole, { isLoading: deletingRole }] = useDeleteAuthorizationRoleMutation();
  const [createUserRole, { isLoading: creatingUserRole }] = useCreateUserRoleAssignmentMutation();
  const [deleteUserRole, { isLoading: deletingUserRole }] = useDeleteUserRoleAssignmentMutation();
  const [createCredentialScope, { isLoading: creatingCredentialScope }] =
    useCreateCredentialScopeAssignmentMutation();
  const [deleteCredentialScope, { isLoading: deletingCredentialScope }] =
    useDeleteCredentialScopeAssignmentMutation();

  const roleOptions = useMemo(
    () =>
      roles
        .filter((role) => role.Name && role.Name !== 'Custom')
        .map((role) => ({ label: roleLabel(role), value: role.Name })),
    [roles]
  );
  const userOptions = users.map((user) => ({
    label: `${user.FirstName} ${user.LastName}`,
    value: user.GUID,
  }));
  const credentialOptions = credentials.map((credential) => ({
    label: credential.Name,
    value: credential.GUID,
  }));
  const graphOptions = graphs.map((graph) => ({ label: graph.Name, value: graph.GUID }));

  const refreshAuthorizationData = () => {
    const refreshes: PromiseLike<unknown>[] = [refetchRoles()];

    if (tenantGuid) {
      refreshes.push(refetchUsers(), refetchCredentials(), refetchGraphs());
    }
    if (tenantGuid && effectiveUserGuid) {
      refreshes.push(refetchUserAssignments(), refetchUserEffectivePermissions());
    }
    if (tenantGuid && effectiveCredentialGuid) {
      refreshes.push(refetchCredentialScopes(), refetchCredentialEffectivePermissions());
    }

    void Promise.allSettled(refreshes);
  };

  React.useEffect(() => {
    if (effectiveUserGuid) {
      userRoleForm.setFieldsValue({ UserGUID: effectiveUserGuid });
    }
  }, [effectiveUserGuid, userRoleForm]);

  React.useEffect(() => {
    if (effectiveCredentialGuid) {
      credentialScopeForm.setFieldsValue({ CredentialGUID: effectiveCredentialGuid });
    }
  }, [credentialScopeForm, effectiveCredentialGuid]);

  const closeRoleModal = () => {
    setRoleModalOpen(false);
    setEditingRole(null);
  };

  const saveRole = async (role: Partial<AuthorizationRole>) => {
    if (!tenantGuid) return;
    if (role.GUID) {
      await updateRole({ tenantGuid, role: role as AuthorizationRole }).unwrap();
    } else {
      await createRole({ tenantGuid, role }).unwrap();
    }
    closeRoleModal();
  };

  const confirmDeleteRole = (role: AuthorizationRole) => {
    Modal.confirm({
      title: 'Delete role?',
      content: `Delete "${role.DisplayName || role.Name}"?`,
      okText: 'Delete',
      okButtonProps: { danger: true, loading: deletingRole },
      maskClosable: true,
      onOk: () => deleteRole({ tenantGuid, roleGuid: role.GUID }).unwrap(),
    });
  };

  const createUserAssignment = async () => {
    if (!tenantGuid) return;
    const values = await userRoleForm.validateFields();
    const userGuid = values.UserGUID || effectiveUserGuid;
    await createUserRole({
      tenantGuid,
      userGuid,
      assignment: {
        RoleName: values.RoleName,
        ResourceScope: values.ResourceScope,
        GraphGUID: values.ResourceScope === 'Graph' ? values.GraphGUID : null,
      },
    }).unwrap();
    setSelectedUserGuid(userGuid);
    userRoleForm.resetFields();
    userRoleForm.setFieldsValue({ ResourceScope: 'Graph' });
  };

  const createCredentialAssignment = async () => {
    if (!tenantGuid) return;
    const values = await credentialScopeForm.validateFields();
    const credentialGuid = values.CredentialGUID || effectiveCredentialGuid;
    await createCredentialScope({
      tenantGuid,
      credentialGuid,
      assignment: {
        RoleName: values.RoleName || null,
        ResourceScope: values.ResourceScope,
        GraphGUID: values.ResourceScope === 'Graph' ? values.GraphGUID : null,
        Permissions: values.Permissions || [],
        ResourceTypes: values.ResourceTypes || [],
      },
    }).unwrap();
    setSelectedCredentialGuid(credentialGuid);
    credentialScopeForm.resetFields();
    credentialScopeForm.setFieldsValue({
      ResourceScope: 'Graph',
      Permissions: [],
      ResourceTypes: [],
    });
  };

  const roleColumns: TableProps<AuthorizationRole>['columns'] = [
    {
      title: 'Name',
      dataIndex: 'Name',
      key: 'Name',
      render: (_: string, role) => (
        <Space direction="vertical" size={0}>
          <Typography.Text strong>{role.DisplayName || role.Name}</Typography.Text>
          <Typography.Text type="secondary">{role.Name}</Typography.Text>
        </Space>
      ),
    },
    {
      title: 'Scope',
      dataIndex: 'ResourceScope',
      key: 'ResourceScope',
      width: 110,
    },
    {
      title: 'Permissions',
      dataIndex: 'Permissions',
      key: 'Permissions',
      render: tags,
    },
    {
      title: 'Resources',
      dataIndex: 'ResourceTypes',
      key: 'ResourceTypes',
      render: tags,
    },
    {
      title: 'Built In',
      dataIndex: 'BuiltIn',
      key: 'BuiltIn',
      width: 100,
      render: (builtIn: boolean) => (builtIn ? <Tag color="blue">Built in</Tag> : <Tag>Custom</Tag>),
    },
    {
      title: 'Actions',
      key: 'Actions',
      width: 150,
      render: (_, role) => {
        if (role.BuiltIn) return <Typography.Text type="secondary">Immutable</Typography.Text>;

        const items = [
          {
            key: 'edit',
            label: 'Edit',
            icon: <EditOutlined />,
            disabled: !canManage,
            onClick: () => {
              setEditingRole(role);
              setRoleModalOpen(true);
            },
          },
          {
            key: 'view-json',
            label: 'View JSON',
            icon: <CodeOutlined />,
            onClick: () => setJsonRole(role),
          },
          {
            key: 'delete',
            label: 'Delete',
            icon: <DeleteOutlined />,
            danger: true,
            disabled: !canManage || deletingRole,
            onClick: () => confirmDeleteRole(role),
          },
        ];

        return (
          <span
            data-row-click-ignore="true"
            onClick={(event) => event.stopPropagation()}
          >
            <Dropdown menu={{ items }} trigger={['click']} placement="bottomRight">
              <LitegraphTooltip title="Actions">
                <Button
                  type="text"
                  role="authorization-role-action-menu"
                  icon={<MoreOutlined style={{ fontSize: 20 }} />}
                />
              </LitegraphTooltip>
            </Dropdown>
          </span>
        );
      },
    },
  ];

  const assignmentColumns: TableProps<UserRoleAssignment>['columns'] = [
    {
      title: 'Role',
      dataIndex: 'RoleName',
      key: 'RoleName',
      render: (value: string, record) => value || record.RoleGUID || 'Direct role',
    },
    { title: 'Scope', dataIndex: 'ResourceScope', key: 'ResourceScope', width: 110 },
    {
      title: 'Graph',
      dataIndex: 'GraphGUID',
      key: 'GraphGUID',
      render: guidLabel,
    },
    {
      title: 'Actions',
      key: 'Actions',
      width: 120,
      render: (_, assignment) => (
        <Popconfirm
          title="Revoke role?"
          okText="Revoke"
          onConfirm={() =>
            deleteUserRole({
              tenantGuid,
              userGuid: effectiveUserGuid,
              assignmentGuid: assignment.GUID,
            })
          }
          disabled={!canManage}
        >
          <Button size="small" danger disabled={!canManage || deletingUserRole}>
            Revoke
          </Button>
        </Popconfirm>
      ),
    },
  ];

  const credentialScopeColumns: TableProps<CredentialScopeAssignment>['columns'] = [
    {
      title: 'Role',
      dataIndex: 'RoleName',
      key: 'RoleName',
      render: (value: string, record) => value || record.RoleGUID || 'Direct grant',
    },
    { title: 'Scope', dataIndex: 'ResourceScope', key: 'ResourceScope', width: 110 },
    {
      title: 'Graph',
      dataIndex: 'GraphGUID',
      key: 'GraphGUID',
      render: guidLabel,
    },
    {
      title: 'Permissions',
      dataIndex: 'Permissions',
      key: 'Permissions',
      render: tags,
    },
    {
      title: 'Resources',
      dataIndex: 'ResourceTypes',
      key: 'ResourceTypes',
      render: tags,
    },
    {
      title: 'Actions',
      key: 'Actions',
      width: 120,
      render: (_, assignment) => (
        <Popconfirm
          title="Revoke credential scope?"
          okText="Revoke"
          onConfirm={() =>
            deleteCredentialScope({
              tenantGuid,
              credentialGuid: effectiveCredentialGuid,
              assignmentGuid: assignment.GUID,
            })
          }
          disabled={!canManage}
        >
          <Button size="small" danger disabled={!canManage || deletingCredentialScope}>
            Revoke
          </Button>
        </Popconfirm>
      ),
    },
  ];

  const grantColumns: TableProps<AuthorizationEffectiveGrant>['columns'] = [
    { title: 'Source', dataIndex: 'Source', key: 'Source' },
    {
      title: 'Role',
      dataIndex: 'RoleName',
      key: 'RoleName',
      render: (value: string, record) => value || record.RoleGUID || 'Direct grant',
    },
    { title: 'Scope', dataIndex: 'ResourceScope', key: 'ResourceScope', width: 110 },
    {
      title: 'Graph',
      dataIndex: 'GraphGUID',
      key: 'GraphGUID',
      render: guidLabel,
    },
    {
      title: 'Permissions',
      dataIndex: 'Permissions',
      key: 'Permissions',
      render: tags,
    },
    {
      title: 'Resources',
      dataIndex: 'ResourceTypes',
      key: 'ResourceTypes',
      render: tags,
    },
  ];

  if (!tenantGuid) {
    return (
      <PageContainer id="authorization" pageTitle={t('title')}>
        <Alert type="info" showIcon message="Select a tenant to manage authorization." />
      </PageContainer>
    );
  }

  const loading = rolesLoading || rolesFetching;
  const authorizationRefreshing =
    loading ||
    usersLoading ||
    credentialsLoading ||
    graphsLoading ||
    userAssignmentsLoading ||
    userAssignmentsFetching ||
    credentialScopesLoading ||
    credentialScopesFetching ||
    userEffectivePermissionsLoading ||
    userEffectivePermissionsFetching ||
    credentialEffectivePermissionsLoading ||
    credentialEffectivePermissionsFetching;

  return (
    <PageContainer
      id="authorization"
      pageTitle={t('title')}
      pageTitleRightContent={
        <LitegraphTooltip
          title={canManage ? t('createTooltip') : t('adminRequired')}
        >
          <LitegraphButton
            type="link"
            icon={<PlusSquareOutlined />}
            onClick={() => {
              setEditingRole(null);
              setRoleModalOpen(true);
            }}
            disabled={!canManage || currentPermissionsLoading}
            weight={500}
          >
            {t('createRole')}
          </LitegraphButton>
        </LitegraphTooltip>
      }
    >
      {!canManage && (
        <Alert
          style={{ marginBottom: 16 }}
          type="warning"
          showIcon
          message="Admin permission is required to change roles or credential scopes."
        />
      )}

      {rolesError && !loading ? (
        <FallBack retry={refetchRoles}>Unable to load authorization data.</FallBack>
      ) : (
        <Tabs
          items={[
            {
              key: 'roles',
              label: 'Roles',
              children: (
                <section style={sectionStyle}>
                  <LitegraphTable
                    rowKey="GUID"
                    columns={roleColumns}
                    dataSource={roles}
                    loading={loading}
                    onRefresh={refreshAuthorizationData}
                    isRefreshing={authorizationRefreshing}
                  />
                </section>
              ),
            },
            {
              key: 'users',
              label: 'User Roles',
              forceRender: true,
              children: (
                <section style={sectionStyle}>
                  <Form
                    form={userRoleForm}
                    layout="vertical"
                    style={formGridStyle}
                    initialValues={{ ResourceScope: 'Graph' }}
                  >
                    <Form.Item name="UserGUID" label="User" rules={[{ required: true }]}>
                      <Select
                        showSearch
                        optionFilterProp="label"
                        loading={usersLoading}
                        options={userOptions}
                        value={effectiveUserGuid || undefined}
                        onChange={setSelectedUserGuid}
                        placeholder="Select user"
                      />
                    </Form.Item>
                    <Form.Item name="RoleName" label="Role" rules={[{ required: true }]}>
                      <Select
                        showSearch
                        optionFilterProp="label"
                        options={roleOptions}
                        placeholder="Select role"
                      />
                    </Form.Item>
                    <Form.Item name="ResourceScope" label="Scope" rules={[{ required: true }]}>
                      <Select options={scopeOptions.map((value) => ({ label: value, value }))} />
                    </Form.Item>
                    <Form.Item shouldUpdate noStyle>
                      {() => (
                        <Form.Item
                          name="GraphGUID"
                          label="Graph"
                          rules={[
                            {
                              required: userRoleForm.getFieldValue('ResourceScope') === 'Graph',
                              message: 'Select graph for graph-scoped assignments.',
                            },
                          ]}
                        >
                          <Select
                            showSearch
                            optionFilterProp="label"
                            loading={graphsLoading}
                            options={graphOptions}
                            placeholder="Select graph"
                            disabled={userRoleForm.getFieldValue('ResourceScope') === 'Tenant'}
                            allowClear
                          />
                        </Form.Item>
                      )}
                    </Form.Item>
                    <Form.Item label=" ">
                      <Button
                        type="primary"
                        onClick={createUserAssignment}
                        disabled={!canManage}
                        loading={creatingUserRole}
                      >
                        Assign Role
                      </Button>
                    </Form.Item>
                  </Form>
                  <LitegraphTable
                    rowKey="GUID"
                    columns={assignmentColumns}
                    dataSource={userAssignments}
                    loading={userAssignmentsLoading || userAssignmentsFetching || usersLoading}
                    onRefresh={refreshAuthorizationData}
                    isRefreshing={authorizationRefreshing}
                  />
                  <Typography.Title level={5} style={{ marginTop: 24 }}>
                    Effective User Permissions
                  </Typography.Title>
                  <LitegraphTable
                    rowKey="AssignmentGUID"
                    columns={grantColumns}
                    dataSource={userEffectivePermissions?.Grants || []}
                    size="small"
                    onRefresh={refreshAuthorizationData}
                    isRefreshing={authorizationRefreshing}
                  />
                </section>
              ),
            },
            {
              key: 'credentials',
              label: 'Credential Scopes',
              forceRender: true,
              children: (
                <section style={sectionStyle}>
                  <Form
                    form={credentialScopeForm}
                    layout="vertical"
                    style={formGridStyle}
                    initialValues={{ ResourceScope: 'Graph', Permissions: [], ResourceTypes: [] }}
                  >
                    <Form.Item
                      name="CredentialGUID"
                      label="Credential"
                      rules={[{ required: true }]}
                    >
                      <Select
                        showSearch
                        optionFilterProp="label"
                        loading={credentialsLoading}
                        options={credentialOptions}
                        value={effectiveCredentialGuid || undefined}
                        onChange={setSelectedCredentialGuid}
                        placeholder="Select credential"
                      />
                    </Form.Item>
                    <Form.Item name="RoleName" label="Role">
                      <Select
                        showSearch
                        optionFilterProp="label"
                        options={roleOptions}
                        placeholder="Optional role"
                        allowClear
                      />
                    </Form.Item>
                    <Form.Item name="Permissions" label="Direct Permissions">
                      <Select
                        mode="multiple"
                        options={permissionOptions.map((value) => ({ label: value, value }))}
                      />
                    </Form.Item>
                    <Form.Item name="ResourceTypes" label="Direct Resources">
                      <Select
                        mode="multiple"
                        options={resourceTypeOptions.map((value) => ({ label: value, value }))}
                      />
                    </Form.Item>
                    <Form.Item name="ResourceScope" label="Scope" rules={[{ required: true }]}>
                      <Select options={scopeOptions.map((value) => ({ label: value, value }))} />
                    </Form.Item>
                    <Form.Item shouldUpdate noStyle>
                      {() => (
                        <Form.Item
                          name="GraphGUID"
                          label="Graph"
                          rules={[
                            {
                              required:
                                credentialScopeForm.getFieldValue('ResourceScope') === 'Graph',
                              message: 'Select graph for graph-scoped scopes.',
                            },
                          ]}
                        >
                          <Select
                            showSearch
                            optionFilterProp="label"
                            loading={graphsLoading}
                            options={graphOptions}
                            placeholder="Select graph"
                            disabled={
                              credentialScopeForm.getFieldValue('ResourceScope') === 'Tenant'
                            }
                            allowClear
                          />
                        </Form.Item>
                      )}
                    </Form.Item>
                    <Form.Item
                      label=" "
                      shouldUpdate={(previous, current) =>
                        previous.RoleName !== current.RoleName ||
                        previous.Permissions !== current.Permissions ||
                        previous.ResourceTypes !== current.ResourceTypes
                      }
                    >
                      {() => {
                        const roleName = credentialScopeForm.getFieldValue('RoleName');
                        const permissions = credentialScopeForm.getFieldValue('Permissions') || [];
                        const resourceTypes = credentialScopeForm.getFieldValue('ResourceTypes') || [];
                        const hasGrant =
                          !!roleName || (permissions.length > 0 && resourceTypes.length > 0);
                        return (
                          <Button
                            type="primary"
                            onClick={createCredentialAssignment}
                            disabled={!canManage || !hasGrant}
                            loading={creatingCredentialScope}
                          >
                            Assign Scope
                          </Button>
                        );
                      }}
                    </Form.Item>
                  </Form>
                  <LitegraphTable
                    rowKey="GUID"
                    columns={credentialScopeColumns}
                    dataSource={credentialScopes}
                    loading={
                      credentialScopesLoading || credentialScopesFetching || credentialsLoading
                    }
                    onRefresh={refreshAuthorizationData}
                    isRefreshing={authorizationRefreshing}
                  />
                  <Typography.Title level={5} style={{ marginTop: 24 }}>
                    Effective Credential Permissions
                  </Typography.Title>
                  <LitegraphTable
                    rowKey="AssignmentGUID"
                    columns={grantColumns}
                    dataSource={credentialEffectivePermissions?.Grants || []}
                    size="small"
                    onRefresh={refreshAuthorizationData}
                    isRefreshing={authorizationRefreshing}
                  />
                </section>
              ),
            },
          ]}
        />
      )}

      {roleModalOpen && (
        <RoleModal
          open={roleModalOpen}
          role={editingRole}
          saving={creatingRole || updatingRole}
          onCancel={closeRoleModal}
          onSave={saveRole}
        />
      )}
      <ViewJsonModal
        open={!!jsonRole}
        onClose={() => setJsonRole(null)}
        data={jsonRole}
        title="Authorization Role JSON"
      />
    </PageContainer>
  );
};

export default AuthorizationPage;
