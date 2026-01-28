import React from 'react';
import { Button, Dropdown, TableProps } from 'antd';
import { CheckCircleFilled, CloseCircleFilled, MoreOutlined } from '@ant-design/icons';
import { formatDateTime } from '@/utils/dateUtils';
import { FilterDropdownProps } from 'antd/es/table/interface';
import TableSearch from '@/components/table-search/TableSearch';
import { onGUIDFilter, onNameFilter } from '@/constants/table';
import { TenantMetaData } from 'litegraphdb/dist/types/types';

export const tableColumns = (
  handleEdit: (tenant: TenantMetaData) => void,
  handleDelete: (tenant: TenantMetaData) => void
): TableProps<TenantMetaData>['columns'] => [
  {
    title: 'GUID',
    dataIndex: 'GUID',
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder="Search GUID" />
    ),
    onFilter: (value, record) => onGUIDFilter(value, record.GUID),
    key: 'GUID',
    width: 450,
    render: (GUID: string) => <div>{GUID}</div>,
  },
  {
    title: 'Name',
    dataIndex: 'Name',
    key: 'Name',
    width: 350,
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder="Search Name" />
    ),
    onFilter: (value, record) => onNameFilter(value, record.Name),
    sorter: (a: TenantMetaData, b: TenantMetaData) => a.Name.localeCompare(b.Name),
    render: (Name: string) => <div>{Name}</div>,
  },

  {
    title: 'Active',
    dataIndex: 'Active',
    key: 'Active',
    width: 100,
    sorter: (a: TenantMetaData, b: TenantMetaData) => Number(b.Active) - Number(a.Active),
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
    sorter: (a: TenantMetaData, b: TenantMetaData) =>
      new Date(a.CreatedUtc).getTime() - new Date(b.CreatedUtc).getTime(),
    key: 'CreatedUtc',
    width: 200,
    render: (CreatedUtc: string) => <div>{formatDateTime(CreatedUtc)}</div>,
  },
  {
    title: 'Actions',
    key: 'actions',
    width: 100,
    render: (_: any, record: TenantMetaData) => {
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
            role="tenant-action-menu"
            icon={<MoreOutlined style={{ fontSize: '20px' }} />}
            style={{ fontSize: '16px' }}
          />
        </Dropdown>
      );
    },
  },
];
