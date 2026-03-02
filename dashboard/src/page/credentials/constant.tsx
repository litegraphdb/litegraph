import React from 'react';
import { Button, Dropdown, TableProps } from 'antd';
import { MoreOutlined, CheckCircleFilled, CloseCircleFilled, CodeOutlined } from '@ant-design/icons';
import CopyButton from '@/components/base/copy-button/CopyButton';
import { CredentialType } from '@/types/types';
import { formatDateTime } from '@/utils/dateUtils';
import { FilterDropdownProps } from 'antd/es/table/interface';
import TableSearch from '@/components/table-search/TableSearch';
import { onGUIDFilter, onNameFilter } from '@/constants/table';
import { columnTooltip } from '@/utils/tooltipUtils';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';

export const tableColumns = (
  handleEdit: (user: CredentialType) => void,
  handleDelete: (user: CredentialType) => void,
  handleViewJson?: (record: CredentialType) => void
): TableProps<CredentialType>['columns'] => [
  {
    title: columnTooltip('GUID', 'Globally unique identifier'),
    dataIndex: 'GUID',
    key: 'GUID',
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder="Search GUID" />
    ),
    onFilter: (value, record) => onGUIDFilter(value, record.GUID),
    width: 350,
    render: (GUID: string) => <span style={{ display: 'inline-flex', alignItems: 'center', gap: 4, fontFamily: 'monospace', fontSize: 12, whiteSpace: 'nowrap' }}>{GUID}<CopyButton text={GUID} tooltipTitle="Copy GUID" /></span>,
  },
  {
    title: columnTooltip('User', 'Associated user name'),
    dataIndex: 'userName',
    key: 'userName',
    width: 250,
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder="Search User" />
    ),
    onFilter: (value, record) => onNameFilter(value, record.userName || ''),
    render: (userName: string) => <div>{userName}</div>,
  },
  {
    title: columnTooltip('Name', 'Credential display name'),
    dataIndex: 'Name',
    key: 'name',
    width: 200,
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder="Search Name" />
    ),
    onFilter: (value, record) => onNameFilter(value, record.Name),
    sorter: (a: CredentialType, b: CredentialType) => a.Name.localeCompare(b.Name),
    render: (name: string) => <div>{name}</div>,
  },
  {
    title: columnTooltip('Bearer Token', 'Authentication bearer token'),
    dataIndex: 'BearerToken',
    key: 'BearerToken',
    width: 200,
    render: (BearerToken: string) => <div>{BearerToken}</div>,
  },
  {
    title: columnTooltip('Active', 'Whether the credential is active'),
    dataIndex: 'Active',
    key: 'Active',
    width: 100,
    sorter: (a: CredentialType, b: CredentialType) => Number(b.Active) - Number(a.Active),
    render: (active: boolean) =>
      active ? (
        <CheckCircleFilled style={{ color: 'green' }} />
      ) : (
        <CloseCircleFilled style={{ color: 'red' }} />
      ),
  },
  {
    title: columnTooltip('Created UTC', 'Date and time of creation in UTC'),
    dataIndex: 'CreatedUtc',
    key: 'CreatedUtc',
    width: 200,
    sorter: (a: CredentialType, b: CredentialType) =>
      new Date(a.CreatedUtc).getTime() - new Date(b.CreatedUtc).getTime(),
    render: (CreatedUtc: string) => <div>{formatDateTime(CreatedUtc)}</div>,
  },
  {
    title: columnTooltip('Actions', 'Available operations'),
    key: 'actions',
    width: 100,
    render: (_: any, record: CredentialType) => {
      const items = [
        {
          key: 'edit',
          label: 'Edit',
          onClick: () => handleEdit(record),
        },
        {
          key: 'delete',
          label: 'Delete',
          onClick: () => handleDelete(record),
        },
        {
          icon: <CodeOutlined />,
          key: 'view-json',
          label: 'View JSON',
          onClick: () => handleViewJson?.(record),
        },
      ];
      return (
        <Dropdown menu={{ items }} trigger={['click']} placement="bottomRight">
          <LitegraphTooltip title="Actions">
            <Button
              type="text"
              icon={<MoreOutlined style={{ fontSize: '20px' }} />}
              role="credential-action-menu"
              style={{ fontSize: '16px' }}
            />
          </LitegraphTooltip>
        </Dropdown>
      );
    },
  },
];
