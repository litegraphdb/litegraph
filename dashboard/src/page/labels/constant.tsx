import React from 'react';
import { LoadingOutlined, MoreOutlined } from '@ant-design/icons';
import { Button, Dropdown, TableProps } from 'antd';
import { formatDateTime } from '@/utils/dateUtils';
import { onGUIDFilter, onNameFilter } from '@/constants/table';
import TableSearch from '@/components/table-search/TableSearch';
import { FilterDropdownProps } from 'antd/es/table/interface';
import { LabelMetadataForTable } from './types';

export const tableColumns = (
  handleEdit: (record: LabelMetadataForTable) => void,
  handleDelete: (record: LabelMetadataForTable) => void,
  isNodesLoading: boolean,
  isEdgesLoading: boolean
): TableProps<LabelMetadataForTable>['columns'] => [
  {
    title: 'Label',
    dataIndex: 'Label',
    key: 'Label',
    width: 250,
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder="Search Label" />
    ),
    onFilter: (value, record) => onNameFilter(value, record.Label),
    sorter: (a: LabelMetadataForTable, b: LabelMetadataForTable) => a.Label.localeCompare(b.Label),
    render: (key: string) => (
      <div>
        <div>{key}</div>
      </div>
    ),
  },

  {
    title: 'GUID',
    dataIndex: 'GUID',
    key: 'GUID',
    width: 350,
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
    key: 'NodeName',
    width: 200,
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder="Search Node" />
    ),
    onFilter: (value, record) => onNameFilter(value, record.NodeName || ''),
    sorter: (a: LabelMetadataForTable, b: LabelMetadataForTable) =>
      a.NodeName.localeCompare(b.NodeName),
    render: (NodeName: string) =>
      isNodesLoading ? (
        <LoadingOutlined />
      ) : (
        <div>
          <div>{NodeName}</div>
        </div>
      ),
  },
  {
    title: 'Edge',
    dataIndex: 'EdgeName',
    key: 'EdgeName',
    width: 200,
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder="Search Edge" />
    ),
    onFilter: (value, record) => onNameFilter(value, record.EdgeName || ''),
    sorter: (a: LabelMetadataForTable, b: LabelMetadataForTable) =>
      a.EdgeName.localeCompare(b.EdgeName),
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
    key: 'CreatedUtc',
    width: 200,
    sorter: (a: LabelMetadataForTable, b: LabelMetadataForTable) =>
      new Date(a.CreatedUtc).getTime() - new Date(b.CreatedUtc).getTime(),
    render: (CreatedUtc: string) => <div>{formatDateTime(CreatedUtc)}</div>,
  },
  {
    title: 'Actions',
    key: 'actions',
    render: (_: any, record: LabelMetadataForTable) => {
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
            role="label-action-menu"
            type="text"
            icon={<MoreOutlined style={{ fontSize: '20px' }} />}
            style={{ fontSize: '16px' }}
          />
        </Dropdown>
      );
    },
  },
];
