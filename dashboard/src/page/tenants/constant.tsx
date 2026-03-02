import React from 'react';
import { Button, Dropdown, TableProps } from 'antd';
import { CheckCircleFilled, CloseCircleFilled, MoreOutlined, CodeOutlined } from '@ant-design/icons';
import CopyButton from '@/components/base/copy-button/CopyButton';
import { formatDateTime } from '@/utils/dateUtils';
import { FilterDropdownProps } from 'antd/es/table/interface';
import TableSearch from '@/components/table-search/TableSearch';
import { onGUIDFilter, onNameFilter } from '@/constants/table';
import { TenantMetaData } from 'litegraphdb/dist/types/types';
import { columnTooltip } from '@/utils/tooltipUtils';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';

export const tableColumns = (
  handleEdit: (tenant: TenantMetaData) => void,
  handleDelete: (tenant: TenantMetaData) => void,
  handleViewJson?: (record: TenantMetaData) => void
): TableProps<TenantMetaData>['columns'] => [
  {
    title: columnTooltip('GUID', 'Globally unique identifier'),
    dataIndex: 'GUID',
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder="Search GUID" />
    ),
    onFilter: (value, record) => onGUIDFilter(value, record.GUID),
    key: 'GUID',
    width: 450,
    render: (GUID: string) => <span style={{ display: 'inline-flex', alignItems: 'center', gap: 4, fontFamily: 'monospace', fontSize: 12, whiteSpace: 'nowrap' }}>{GUID}<CopyButton text={GUID} tooltipTitle="Copy GUID" /></span>,
  },
  {
    title: columnTooltip('Name', 'Tenant display name'),
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
    title: columnTooltip('Active', 'Whether the tenant is active'),
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
    title: columnTooltip('Created UTC', 'Date and time of creation in UTC'),
    dataIndex: 'CreatedUtc',
    sorter: (a: TenantMetaData, b: TenantMetaData) =>
      new Date(a.CreatedUtc).getTime() - new Date(b.CreatedUtc).getTime(),
    key: 'CreatedUtc',
    width: 200,
    render: (CreatedUtc: string) => <div>{formatDateTime(CreatedUtc)}</div>,
  },
  {
    title: columnTooltip('Actions', 'Available operations'),
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
              role="tenant-action-menu"
              icon={<MoreOutlined style={{ fontSize: '20px' }} />}
              style={{ fontSize: '16px' }}
            />
          </LitegraphTooltip>
        </Dropdown>
      );
    },
  },
];
