import React from 'react';
import { LoadingOutlined, MoreOutlined, CodeOutlined } from '@ant-design/icons';
import CopyButton from '@/components/base/copy-button/CopyButton';
import { Button, Dropdown, TableProps } from 'antd';
import { TagType } from '@/types/types';
import { formatDateTime } from '@/utils/dateUtils';
import { FilterDropdownProps } from 'antd/es/table/interface';
import TableSearch from '@/components/table-search/TableSearch';
import { onGUIDFilter, onNameFilter } from '@/constants/table';
import { columnTooltip } from '@/utils/tooltipUtils';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';

export const tableColumns = (
  handleEdit: (record: TagType) => void,
  handleDelete: (record: TagType) => void,
  isNodesLoading: boolean,
  isEdgesLoading: boolean,
  handleViewJson?: (record: TagType) => void
): TableProps<TagType>['columns'] => [
  {
    title: columnTooltip('Key', 'Tag key identifier'),
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
    title: columnTooltip('Value', 'Tag value'),
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
    title: columnTooltip('GUID', 'Globally unique identifier'),
    dataIndex: 'GUID',
    key: 'GUID',
    width: 350,
    responsive: ['md'],
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder="Search GUID" />
    ),
    onFilter: (value, record) => onGUIDFilter(value, record.GUID),
    render: (GUID: string) => <span style={{ display: 'inline-flex', alignItems: 'center', gap: 4, fontFamily: 'monospace', fontSize: 12, whiteSpace: 'nowrap' }}>{GUID}<CopyButton text={GUID} tooltipTitle="Copy GUID" /></span>,
  },
  {
    title: columnTooltip('Node', 'Associated node name'),
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
    title: columnTooltip('Edge', 'Associated edge name'),
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
    title: columnTooltip('Created UTC', 'Date and time of creation in UTC'),
    dataIndex: 'CreatedUtc',
    sorter: (a: TagType, b: TagType) =>
      new Date(a.CreatedUtc).getTime() - new Date(b.CreatedUtc).getTime(),
    key: 'CreatedUtc',
    width: 200,
    responsive: ['md'],
    render: (CreatedUtc: string) => <div>{formatDateTime(CreatedUtc)}</div>,
  },
  {
    title: columnTooltip('Actions', 'Available operations'),
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
              role="tag-action-menu"
            />
          </LitegraphTooltip>
        </Dropdown>
      );
    },
  },
];
