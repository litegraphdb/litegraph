import React from 'react';
import { MoreOutlined } from '@ant-design/icons';
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

export const tableColumns = (
  handleEdit: (record: NodeType) => void,
  handleDelete: (record: NodeType) => void,
  hasScoreOrDistance: boolean
): TableProps<NodeType>['columns'] => [
  {
    title: 'Name',
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
    title: 'GUID',
    dataIndex: 'GUID' as keyof NodeType,
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
    title: 'Labels',
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
    title: 'Tags',
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
    title: 'Vectors',
    dataIndex: 'Vectors',
    key: 'Vectors',
    width: 250,
    render: (_: any, record: NodeType) => (
      <div>{record?.Vectors?.length > 0 ? pluralize(record?.Vectors?.length, 'vector') : NONE}</div>
    ),
  },
  {
    title: 'Created UTC',
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
          title: 'Score',
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
          title: 'Distance',
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
    title: 'Actions',
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
      ];
      return (
        <Dropdown menu={{ items }} trigger={['click']} placement="bottomRight">
          <Button
            type="text"
            icon={<MoreOutlined style={{ fontSize: '20px' }} />}
            role="node-action-menu"
          />
        </Dropdown>
      );
    },
  },
];
