import React from 'react';
import { LoadingOutlined, MoreOutlined } from '@ant-design/icons';
import { Button, Dropdown, TableProps } from 'antd';
import { TagType } from '@/types/types';
import { formatDateTime } from '@/utils/dateUtils';
import { FilterDropdownProps } from 'antd/es/table/interface';
import TableSearch from '@/components/table-search/TableSearch';
import { onGUIDFilter, onNameFilter } from '@/constants/table';

export const tableColumns = (
  handleEdit: (record: TagType) => void,
  handleDelete: (record: TagType) => void,
  isNodesLoading: boolean,
  isEdgesLoading: boolean
): TableProps<TagType>['columns'] => [
  {
    title: 'Key',
    dataIndex: 'Key',
    key: 'Key',
    width: 200,
    responsive: ['md'],
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder="Search Key" />
    ),
    onFilter: (value, record) => onNameFilter(value, record.Key),
    sorter: (a: TagType, b: TagType) => a.Key.localeCompare(b.Key),
    render: (key: string) => (
      <div>
        <div>{key}</div>
      </div>
    ),
  },
  {
    title: 'Value',
    dataIndex: 'Value',
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder="Search Value" />
    ),
    onFilter: (value, record) => onNameFilter(value, record.Value),
    sorter: (a: TagType, b: TagType) => a.Value.localeCompare(b.Value),
    key: 'Value',
    width: 200,
    responsive: ['md'],
    render: (value: string) => (
      <div>
        <div>{value}</div>
      </div>
    ),
  },
  {
    title: 'GUID',
    dataIndex: 'GUID',
    key: 'GUID',
    width: 350,
    responsive: ['md'],
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder="Search GUID" />
    ),
    onFilter: (value, record) => onGUIDFilter(value, record.GUID),
    render: (GUID: string) => (
      <div>
        <div>{GUID}</div>
      </div>
    ),
  },
  {
    title: 'Node',
    dataIndex: 'NodeName',
    sorter: (a: TagType, b: TagType) => a.NodeName?.localeCompare(b.NodeName || '') || 0,
    key: 'NodeName',
    width: 200,
    responsive: ['md'],
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder="Search Node" />
    ),
    onFilter: (value, record) => onNameFilter(value, record.NodeName || ''),
    render: (NodeGUID: string) =>
      isNodesLoading ? (
        <LoadingOutlined />
      ) : (
        <div>
          <div>{NodeGUID}</div>
        </div>
      ),
  },
  {
    title: 'Edge',
    dataIndex: 'EdgeName',
    sorter: (a: TagType, b: TagType) => a.EdgeName?.localeCompare(b.EdgeName || '') || 0,
    key: 'EdgeName',
    width: 200,
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder="Search Edge" />
    ),
    onFilter: (value, record) => onNameFilter(value, record.EdgeName || ''),
    responsive: ['md'],
    render: (EdgeName: string) =>
      isEdgesLoading ? (
        <LoadingOutlined />
      ) : (
        <div>
          <div>{EdgeName}</div>
        </div>
      ),
  },
  {
    title: 'Created UTC',
    dataIndex: 'CreatedUtc',
    sorter: (a: TagType, b: TagType) =>
      new Date(a.CreatedUtc).getTime() - new Date(b.CreatedUtc).getTime(),
    key: 'CreatedUtc',
    width: 200,
    responsive: ['md'],
    render: (CreatedUtc: string) => <div>{formatDateTime(CreatedUtc)}</div>,
  },
  {
    title: 'Actions',
    key: 'actions',
    render: (_: any, record: TagType) => {
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
            role="tag-action-menu"
          />
        </Dropdown>
      );
    },
  },
];
