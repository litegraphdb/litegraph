import React from 'react';
import { Button, Dropdown, TableProps } from 'antd';
import { MoreOutlined, CheckCircleFilled, CloseCircleFilled } from '@ant-design/icons';
import { CredentialType } from '@/types/types';
import { formatDateTime } from '@/utils/dateUtils';
import { FilterDropdownProps } from 'antd/es/table/interface';
import TableSearch from '@/components/table-search/TableSearch';
import { onGUIDFilter, onNameFilter } from '@/constants/table';

export const tableColumns = (
  handleEdit: (user: CredentialType) => void,
  handleDelete: (user: CredentialType) => void
): TableProps<CredentialType>['columns'] => [
  {
    title: 'GUID',
    dataIndex: 'GUID',
    key: 'GUID',
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder="Search GUID" />
    ),
    onFilter: (value, record) => onGUIDFilter(value, record.GUID),
    width: 350,
    render: (GUID: string) => <div>{GUID}</div>,
  },
  {
    title: 'User',
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
    title: 'Name',
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
    title: 'Bearer Token',
    dataIndex: 'BearerToken',
    key: 'BearerToken',
    width: 200,
    render: (BearerToken: string) => <div>{BearerToken}</div>,
  },
  {
    title: 'Active',
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
    title: 'Created UTC',
    dataIndex: 'CreatedUtc',
    key: 'CreatedUtc',
    width: 200,
    sorter: (a: CredentialType, b: CredentialType) =>
      new Date(a.CreatedUtc).getTime() - new Date(b.CreatedUtc).getTime(),
    render: (CreatedUtc: string) => <div>{formatDateTime(CreatedUtc)}</div>,
  },
  {
    title: 'Actions',
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
      ];
      return (
        <Dropdown menu={{ items }} trigger={['click']} placement="bottomRight">
          <Button
            type="text"
            icon={<MoreOutlined style={{ fontSize: '20px' }} />}
            role="credential-action-menu"
            style={{ fontSize: '16px' }}
          />
        </Dropdown>
      );
    },
  },
];
