import React from 'react';
import { LoadingOutlined, MoreOutlined } from '@ant-design/icons';
import { Button, Dropdown, TableProps } from 'antd';
import { VectorType } from '@/types/types';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';
import { formatDateTime } from '@/utils/dateUtils';
import { FilterDropdownProps } from 'antd/es/table/interface';
import { onGUIDFilter, onNameFilter } from '@/constants/table';
import TableSearch from '@/components/table-search/TableSearch';

export const tableColumns = (
  handleEdit: (record: VectorType) => void,
  handleDelete: (record: VectorType) => void,
  isNodesLoading: boolean,
  isEdgesLoading: boolean
): TableProps<VectorType>['columns'] => [
  {
    title: 'Model',
    dataIndex: 'Model',
    sorter: (a: VectorType, b: VectorType) => a.Model.localeCompare(b.Model),
    key: 'Model',
    width: 250,
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder="Search Model" />
    ),
    onFilter: (value, record) => onNameFilter(value, record.Model),
    responsive: ['md'],
    render: (Model: string) => (
      <div>
        <div>{Model}</div>
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
    sorter: (a: VectorType, b: VectorType) => a.NodeName?.localeCompare(b.NodeName || '') || 0,
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
    sorter: (a: VectorType, b: VectorType) => a.EdgeName?.localeCompare(b.EdgeName || '') || 0,
    key: 'EdgeName',
    width: 200,
    responsive: ['md'],
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder="Search Edge" />
    ),
    onFilter: (value, record) => onNameFilter(value, record.EdgeName || ''),
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
    title: 'Dimensionality',
    dataIndex: 'Dimensionality',
    sorter: (a: VectorType, b: VectorType) => a.Dimensionality - b.Dimensionality,
    key: 'Dimensionality',
    width: 200,
    responsive: ['md'],
    render: (Dimensionality: number) => (
      <div>
        <div>{Dimensionality}</div>
      </div>
    ),
  },
  {
    title: 'Content',
    dataIndex: 'Content',
    sorter: (a: VectorType, b: VectorType) => a.Content.localeCompare(b.Content),
    key: 'Content',
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder="Search Content" />
    ),
    onFilter: (value, record) => onNameFilter(value, record.Content),
    width: 200,
    responsive: ['md'],
    render: (Content: string) => (
      <div>
        <div>{Content}</div>
      </div>
    ),
  },
  {
    title: 'Vectors',
    dataIndex: 'Vectors',
    sorter: (a: VectorType, b: VectorType) => a.Vectors.length - b.Vectors.length,
    key: 'Vectors',
    width: 200,
    responsive: ['md'],
    render: (_: any, record: VectorType) => (
      <LitegraphTooltip title={record.Vectors.join(', ')}>
        <div>{record.Vectors.length} vectors</div>
      </LitegraphTooltip>
    ),
  },
  {
    title: 'Created UTC',
    dataIndex: 'CreatedUtc',
    sorter: (a: VectorType, b: VectorType) =>
      new Date(a.CreatedUtc).getTime() - new Date(b.CreatedUtc).getTime(),
    key: 'CreatedUtc',
    width: 200,
    responsive: ['md'],
    render: (CreatedUtc: string) => <div>{formatDateTime(CreatedUtc)}</div>,
  },
  {
    title: 'Actions',
    key: 'actions',
    render: (_: any, record: VectorType) => {
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
            role="vector-action-menu"
            style={{ fontSize: '16px' }}
          />
        </Dropdown>
      );
    },
  },
];
