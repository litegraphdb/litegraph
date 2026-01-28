import React from 'react';
import { LoadingOutlined, MoreOutlined } from '@ant-design/icons';
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

export const tableColumns = (
  handleEdit: (record: EdgeType) => void,
  handleDelete: (record: EdgeType) => void,
  hasScoreOrDistance: boolean,
  isNodesLoading: boolean
): TableProps<EdgeType>['columns'] => [
  {
    title: 'Name',
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
    title: 'From',
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
    title: 'To',
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
    title: 'Cost',
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
    title: 'Labels',
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
    title: 'Tags',
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
    title: 'Vectors',
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
    title: 'Created UTC',
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
          title: 'Score',
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
          title: 'Distance',
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
    title: 'Actions',
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
      ];
      return (
        <Dropdown menu={{ items }} trigger={['click']} placement="bottomRight">
          <Button type="text" icon={<MoreOutlined style={{ fontSize: '20px' }} />} />
        </Dropdown>
      );
    },
  },
];
