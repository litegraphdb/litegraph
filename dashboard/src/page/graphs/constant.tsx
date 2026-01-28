import React, { useState } from 'react';
import { EditOutlined, MoreOutlined } from '@ant-design/icons';
import { DeleteOutlined } from '@ant-design/icons';
import { ExportOutlined } from '@ant-design/icons';
import { SettingOutlined } from '@ant-design/icons';
import { GraphData } from '@/types/types';
import { TableProps, Dropdown, Button, Menu, Input } from 'antd';
import { formatDateTime } from '@/utils/dateUtils';
import { pluralize } from '@/utils/stringUtils';
import { isNumber } from 'lodash';
import { NONE, NOT_AVAILABLE } from '@/constants/uiLabels';
import { FilterDropdownProps } from 'antd/es/table/interface';
import TableSearch from '@/components/table-search/TableSearch';
import { onGUIDFilter, onLabelFilter, onNameFilter, onTagFilter } from '@/constants/table';

export const tableColumns = (
  handleEdit: (record: GraphData) => void,
  handleDelete: (record: GraphData) => void,
  handleExportGexf: (record: GraphData) => void,
  handleEnableVectorIndex: (record: GraphData) => void,
  handleReadVectorIndexConfig: (record: GraphData) => void,
  handleReadVectorIndexStats: (record: GraphData) => void,
  handleRebuildVectorIndex: (record: GraphData) => void,
  handleDeleteVectorIndex: (record: GraphData) => void,
  hasScoreOrDistance: boolean
): TableProps<GraphData>['columns'] => [
  {
    title: 'Name',
    dataIndex: 'Name',
    key: 'name',
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder="Search Name" />
    ),
    onFilter: (value, record) => onNameFilter(value, record.Name),
    width: 350,
    responsive: ['sm'],
    sorter: (a: GraphData, b: GraphData) => a.Name.localeCompare(b.Name),
    render: (name: string) => (
      <div>
        <div>{name}</div>
      </div>
    ),
  },
  {
    title: 'GUID',
    dataIndex: 'GUID',
    key: 'GUID',
    width: 350,
    responsive: ['sm'],

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
    dataIndex: 'Labels' as keyof GraphData,
    key: 'labels',
    width: 350,
    responsive: ['sm'],
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder="Search Labels" />
    ),
    onFilter: (value, record) => onLabelFilter(value, record.Labels),
    render: (labels: any) => (
      <div>
        <div>{Array.isArray(labels) && labels.length > 0 ? labels.join(', ') : NOT_AVAILABLE}</div>
      </div>
    ),
  },
  {
    title: 'Tags',
    dataIndex: 'Tags',
    key: 'tags',
    width: 350,
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder="Search Tags" />
    ),
    onFilter: (val, record) => onTagFilter(val, record.Tags),
    responsive: ['sm'],
    render: (tags: any) => {
      return (
        <div>
          <div>{tags && Object.keys(tags).length > 0 ? JSON.stringify(tags) : NONE}</div>
        </div>
      );
    },
  },
  {
    title: 'Vectors',
    dataIndex: 'Vectors',
    key: 'Vectors',
    width: 150,
    responsive: ['sm'],
    render: (_: any, record: GraphData) => (
      <div>{record?.Vectors?.length > 0 ? pluralize(record?.Vectors?.length, 'vector') : NONE}</div>
    ),
  },
  {
    title: 'Created UTC',
    dataIndex: 'CreatedUtc',
    key: 'CreatedUtc',
    width: 350,
    responsive: ['sm'],
    sorter: (a: GraphData, b: GraphData) =>
      new Date(a.CreatedUtc).getTime() - new Date(b.CreatedUtc).getTime(),
    render: (CreatedUtc: string) => <div>{formatDateTime(CreatedUtc)}</div>,
  },
  ...(hasScoreOrDistance
    ? [
        {
          title: 'Score',
          dataIndex: 'Score',
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
          dataIndex: 'Distance',
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
    responsive: ['sm'],
    render: (_: any, record: GraphData) => {
      const items = [
        {
          icon: <EditOutlined />,
          key: 'edit',
          label: 'Edit',
          onClick: () => handleEdit(record),
        },
        {
          icon: <DeleteOutlined />,
          key: 'delete',
          label: 'Delete',
          onClick: () => handleDelete(record),
        },
        {
          icon: <ExportOutlined />,
          key: 'export',
          label: 'Export to GEXF',
          onClick: () => handleExportGexf(record),
        },
        {
          icon: <SettingOutlined />,
          key: 'enable-vector-index',
          label: 'Enable Vector Index',
          onClick: () => handleEnableVectorIndex(record),
        },
        {
          icon: <SettingOutlined />,
          key: 'read-vector-index-configuration',
          label: 'Read Vector Index Configuration',
          onClick: () => handleReadVectorIndexConfig(record),
        },
        {
          icon: <SettingOutlined />,
          key: 'read-vector-index-statistics',
          label: 'Read Vector Index Statistics',
          onClick: () => handleReadVectorIndexStats(record),
        },
        {
          icon: <SettingOutlined />,
          key: 'rebuild-vector-index',
          label: 'Rebuild Vector Index',
          onClick: () => handleRebuildVectorIndex(record),
        },
        {
          icon: <SettingOutlined />,
          key: 'delete-vector-index',
          label: 'Delete Vector Index',
          onClick: () => handleDeleteVectorIndex(record),
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
