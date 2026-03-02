import React from 'react';
import { LoadingOutlined, MoreOutlined, CodeOutlined } from '@ant-design/icons';
import CopyButton from '@/components/base/copy-button/CopyButton';
import { Button, Dropdown, Skeleton, TableProps } from 'antd';
import { EdgeType } from '@/types/types';
import { formatDateTime } from '@/utils/dateUtils';
import { pluralize } from '@/utils/stringUtils';
import { isNumber } from 'lodash';
import { NONE, NOT_AVAILABLE } from '@/constants/uiLabels';
import TableSearch from '@/components/table-search/TableSearch';
import { FilterDropdownProps } from 'antd/es/table/interface';
import { onGUIDFilter, onLabelFilter, onNameFilter, onTagFilter } from '@/constants/table';
import LitegraphTag from '@/components/base/tag/Tag';
import { columnTooltip } from '@/utils/tooltipUtils';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';

export const tableColumns = (
  handleEdit: (record: EdgeType) => void,
  handleDelete: (record: EdgeType) => void,
  hasScoreOrDistance: boolean,
  isNodesLoading: boolean,
  handleViewJson?: (record: EdgeType) => void
): TableProps<EdgeType>['columns'] => [
  {
    title: columnTooltip('Name', 'Edge display name'),
    dataIndex: 'Name' as keyof EdgeType,
    key: 'Name',
    width: 250,
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder="Search Name" />
    ),
    onFilter: (value, record) => onNameFilter(value, record.Name),
    sorter: (a: EdgeType, b: EdgeType) => a.Name.localeCompare(b.Name),
    render: (Name: string) => (
      <div>
        <div>{Name}</div>
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
    title: columnTooltip('From', 'Source node of this edge'),
    dataIndex: 'FromName' as keyof EdgeType,
    key: 'FromName',
    width: 250,
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder="Search From" />
    ),
    onFilter: (value, record) => onNameFilter(value, record.FromName || ''),
    responsive: ['md'],
    render: (FromName: string) =>
      isNodesLoading ? (
        <LoadingOutlined />
      ) : (
        <div>
          <div>{FromName}</div>
        </div>
      ),
  },
  {
    title: columnTooltip('To', 'Target node of this edge'),
    dataIndex: 'ToName' as keyof EdgeType,
    key: 'ToName',
    width: 250,
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder="Search To" />
    ),
    onFilter: (value, record) => onNameFilter(value, record.ToName || ''),
    responsive: ['md'],
    render: (ToName: string) =>
      isNodesLoading ? (
        <LoadingOutlined />
      ) : (
        <div>
          <div>{ToName}</div>
        </div>
      ),
  },
  {
    title: columnTooltip('Cost', 'Edge traversal cost'),
    dataIndex: 'Cost' as keyof EdgeType,
    key: 'Cost',
    width: 150,
    sorter: (a: EdgeType, b: EdgeType) => (a.Cost ?? 0) - (b.Cost ?? 0),
    render: (cost: number) => (
      <div>
        <div>{cost}</div>
      </div>
    ),
  },
  {
    title: columnTooltip('Labels', 'Classification labels assigned to this edge'),
    dataIndex: 'Labels' as keyof EdgeType,
    key: 'Labels',
    width: 150,
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder="Search Labels" />
    ),
    onFilter: (value, record) => onLabelFilter(value, record.Labels),
    render: (Labels: string[]) => (
      <div>
        {Labels?.length ? Labels?.map((label) => <LitegraphTag key={label} label={label} />) : NONE}
      </div>
    ),
  },
  {
    title: columnTooltip('Tags', 'Key-value metadata tags'),
    dataIndex: 'Tags' as keyof EdgeType,
    key: 'Tags',
    width: 150,
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder="Search Tags" />
    ),
    onFilter: (val, record) => onTagFilter(val, record.Tags),
    render: (Tags: any) => (
      <div>
        <div>{Object.keys(Tags || {}).length > 0 ? JSON.stringify(Tags) : NONE}</div>
      </div>
    ),
  },
  {
    title: columnTooltip('Vectors', 'Vector embeddings associated with this edge'),
    dataIndex: 'Vectors' as keyof EdgeType,
    key: 'Vectors',
    width: 150,
    responsive: ['md'],
    render: (_: any, record: EdgeType) => (
      <div>
        {record?.Vectors?.length > 0 ? pluralize(record?.Vectors?.length ?? 0, 'vector') : NONE}
      </div>
    ),
  },
  {
    title: columnTooltip('Created UTC', 'Date and time of creation in UTC'),
    dataIndex: 'CreatedUtc' as keyof EdgeType,
    key: 'CreatedUtc',
    width: 250,
    responsive: ['md'],
    sorter: (a: EdgeType, b: EdgeType) =>
      new Date(a.CreatedUtc).getTime() - new Date(b.CreatedUtc).getTime(),
    render: (CreatedUtc: string) => <div>{formatDateTime(CreatedUtc)}</div>,
  },
  ...(hasScoreOrDistance
    ? [
        {
          title: columnTooltip('Score', 'Relevance score from search'),
          dataIndex: 'Score' as keyof EdgeType,
          key: 'Score',
          width: 150,
          render: (score: number) => (
            <div>
              <div>{isNumber(score) ? score : 'N/A'}</div>
            </div>
          ),
        },
        {
          title: columnTooltip('Distance', 'Vector distance from search query'),
          dataIndex: 'Distance' as keyof EdgeType,
          key: 'Distance',
          width: 150,
          render: (Distance: number) => (
            <div>
              <div>{isNumber(Distance) ? Distance : 'N/A'}</div>
            </div>
          ),
        },
      ]
    : []),
  {
    title: columnTooltip('Actions', 'Available operations'),
    key: 'actions',
    render: (_: any, record: EdgeType) => {
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
          <LitegraphTooltip title="Actions"><Button type="text" icon={<MoreOutlined style={{ fontSize: '20px' }} />} /></LitegraphTooltip>
        </Dropdown>
      );
    },
  },
];
