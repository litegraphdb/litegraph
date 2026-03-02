import React from 'react';
import { MoreOutlined, CodeOutlined } from '@ant-design/icons';
import CopyButton from '@/components/base/copy-button/CopyButton';
import { Button, Dropdown, TableProps } from 'antd';
import { NodeType } from '@/types/types';
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
  handleEdit: (record: NodeType) => void,
  handleDelete: (record: NodeType) => void,
  hasScoreOrDistance: boolean,
  handleViewJson?: (record: NodeType) => void
): TableProps<NodeType>['columns'] => [
  {
    title: columnTooltip('Name', 'Node display name'),
    dataIndex: 'Name' as keyof NodeType,
    key: 'Name',
    width: 250,
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder="Search Name" />
    ),
    onFilter: (value, record) => onNameFilter(value, record.Name),
    sorter: (a: NodeType, b: NodeType) => a.Name.localeCompare(b.Name),
    render: (name: string) => (
      <div>
        <div>{name}</div>
      </div>
    ),
  },
  {
    title: columnTooltip('GUID', 'Globally unique identifier'),
    dataIndex: 'GUID' as keyof NodeType,
    key: 'GUID',
    width: 350,
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder="Search GUID" />
    ),
    onFilter: (value, record) => onGUIDFilter(value, record.GUID),
    render: (GUID: string) => <span style={{ display: 'inline-flex', alignItems: 'center', gap: 4, fontFamily: 'monospace', fontSize: 12, whiteSpace: 'nowrap' }}>{GUID}<CopyButton text={GUID} tooltipTitle="Copy GUID" /></span>,
  },
  {
    title: columnTooltip('Labels', 'Classification labels assigned to this node'),
    dataIndex: 'Labels' as keyof NodeType,
    key: 'Labels',
    width: 150,
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder="Search Labels" />
    ),
    onFilter: (value, record) => onLabelFilter(value, record.Labels),
    render: (label: string[]) => (
      <div>
        {label?.length ? label?.map((label) => <LitegraphTag key={label} label={label} />) : NONE}
      </div>
    ),
  },
  {
    title: columnTooltip('Tags', 'Key-value metadata tags'),
    dataIndex: 'Tags' as keyof NodeType,
    key: 'Tags',
    width: 250,
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder="Search Tags" />
    ),
    onFilter: (val, record) => onTagFilter(val, record.Tags),
    render: (tags: any) => (
      <div>
        <div>{Object.keys(tags || {}).length > 0 ? JSON.stringify(tags) : NONE}</div>
      </div>
    ),
  },
  {
    title: columnTooltip('Vectors', 'Vector embeddings associated with this node'),
    dataIndex: 'Vectors',
    key: 'Vectors',
    width: 250,
    render: (_: any, record: NodeType) => (
      <div>{record?.Vectors?.length > 0 ? pluralize(record?.Vectors?.length, 'vector') : NONE}</div>
    ),
  },
  {
    title: columnTooltip('Created UTC', 'Date and time of creation in UTC'),
    dataIndex: 'CreatedUtc',
    key: 'CreatedUtc',
    width: 250,
    sorter: (a: NodeType, b: NodeType) =>
      new Date(a.CreatedUtc).getTime() - new Date(b.CreatedUtc).getTime(),
    render: (CreatedUtc: string) => (
      <div>
        <div>{formatDateTime(CreatedUtc)}</div>
      </div>
    ),
  },
  ...(hasScoreOrDistance
    ? [
        {
          title: columnTooltip('Score', 'Relevance score from search'),
          dataIndex: 'Score' as keyof NodeType,
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
          dataIndex: 'Distance' as keyof NodeType,
          key: 'Distance',
          width: 150,
          render: (distance: number) => (
            <div>
              <div>{isNumber(distance) ? distance : 'N/A'}</div>
            </div>
          ),
        },
      ]
    : []),
  {
    title: columnTooltip('Actions', 'Available operations'),
    key: 'actions',
    render: (_: any, record: NodeType) => {
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
              role="node-action-menu"
            />
          </LitegraphTooltip>
        </Dropdown>
      );
    },
  },
];
