import React, { useState } from 'react';
import { EditOutlined, MoreOutlined, CodeOutlined } from '@ant-design/icons';
import { DeleteOutlined } from '@ant-design/icons';
import { ExportOutlined } from '@ant-design/icons';
import { SettingOutlined } from '@ant-design/icons';
import CopyButton from '@/components/base/copy-button/CopyButton';
import { columnTooltip } from '@/utils/tooltipUtils';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';
import { GraphData } from '@/types/types';
import { TableProps, Dropdown, Button, Menu, Input } from 'antd';
import { formatDateTime } from '@/utils/dateUtils';
import { isNumber } from 'lodash';
import { NOT_AVAILABLE } from '@/constants/uiLabels';
import { FilterDropdownProps } from 'antd/es/table/interface';
import TableSearch from '@/components/table-search/TableSearch';
import { onGUIDFilter, onLabelFilter, onNameFilter, onTagFilter } from '@/constants/table';
import CountBadge from '@/components/base/count-badge/CountBadge';

type Translator = (key: string, values?: Record<string, string | number>) => string;

export const tableColumns = (
  t: Translator,
  handleEdit: (record: GraphData) => void,
  handleDelete: (record: GraphData) => void,
  handleExportGexf: (record: GraphData) => void,
  handleEnableVectorIndex: (record: GraphData) => void,
  handleReadVectorIndexConfig: (record: GraphData) => void,
  handleReadVectorIndexStats: (record: GraphData) => void,
  handleRebuildVectorIndex: (record: GraphData) => void,
  handleDeleteVectorIndex: (record: GraphData) => void,
  hasScoreOrDistance: boolean,
  handleViewJson?: (record: GraphData) => void
): TableProps<GraphData>['columns'] => [
  {
    title: columnTooltip(t('columns.name'), t('columns.nameDesc')),
    dataIndex: 'Name',
    key: 'name',
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder={t('search.name')} />
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
    title: columnTooltip(t('columns.guid'), t('columns.guidDesc')),
    dataIndex: 'GUID',
    key: 'GUID',
    width: 350,
    responsive: ['sm'],

    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder={t('search.guid')} />
    ),
    onFilter: (value, record) => onGUIDFilter(value, record.GUID),
    render: (GUID: string) => (
      <span
        style={{
          display: 'inline-flex',
          alignItems: 'center',
          gap: 4,
          fontFamily: 'monospace',
          fontSize: 12,
          whiteSpace: 'nowrap',
        }}
      >
        {GUID}
        <CopyButton text={GUID} tooltipTitle={t('copyGuid')} />
      </span>
    ),
  },
  {
    title: columnTooltip(t('columns.labels'), t('columns.labelsDesc')),
    dataIndex: 'Labels' as keyof GraphData,
    key: 'labels',
    width: 350,
    responsive: ['sm'],
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder={t('search.labels')} />
    ),
    onFilter: (value, record) => onLabelFilter(value, record.Labels),
    render: (labels: any) => {
      const count = Array.isArray(labels) ? labels.length : 0;
      return <CountBadge count={count} label={t('labelsCount', { count })} />;
    },
  },
  {
    title: columnTooltip(t('columns.tags'), t('columns.tagsDesc')),
    dataIndex: 'Tags',
    key: 'tags',
    width: 350,
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder={t('search.tags')} />
    ),
    onFilter: (val, record) => onTagFilter(val, record.Tags),
    responsive: ['sm'],
    render: (tags: any) => {
      const count = tags ? Object.keys(tags).length : 0;
      return <CountBadge count={count} label={t('tagsCount', { count })} />;
    },
  },
  {
    title: columnTooltip(t('columns.vectors'), t('columns.vectorsDesc')),
    dataIndex: 'Vectors',
    key: 'Vectors',
    width: 150,
    responsive: ['sm'],
    render: (_: any, record: GraphData) => {
      const count = record?.Vectors?.length || 0;
      return <div>{t('vectorsCount', { count })}</div>;
    },
  },
  {
    title: columnTooltip(t('columns.createdUtc'), t('columns.createdUtcDesc')),
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
          title: columnTooltip(t('columns.score'), t('columns.scoreDesc')),
          dataIndex: 'Score',
          key: 'Score',
          width: 150,
          render: (score: number) => (
            <div>
              <div>{isNumber(score) ? score : NOT_AVAILABLE}</div>
            </div>
          ),
        },
        {
          title: columnTooltip(t('columns.distance'), t('columns.distanceDesc')),
          dataIndex: 'Distance',
          key: 'Distance',
          width: 150,
          render: (distance: number) => (
            <div>
              <div>{isNumber(distance) ? distance : NOT_AVAILABLE}</div>
            </div>
          ),
        },
      ]
    : []),
  {
    title: columnTooltip(t('columns.actions'), t('columns.actionsDesc')),
    key: 'actions',
    responsive: ['sm'],
    render: (_: any, record: GraphData) => {
      const items = [
        {
          icon: <EditOutlined />,
          key: 'edit',
          label: t('rowActions.edit'),
          onClick: () => handleEdit(record),
        },
        {
          icon: <DeleteOutlined />,
          key: 'delete',
          label: t('rowActions.delete'),
          onClick: () => handleDelete(record),
        },
        {
          icon: <ExportOutlined />,
          key: 'export',
          label: t('rowActions.exportGexf'),
          onClick: () => handleExportGexf(record),
        },
        {
          icon: <SettingOutlined />,
          key: 'enable-vector-index',
          label: t('rowActions.enableVectorIndex'),
          onClick: () => handleEnableVectorIndex(record),
        },
        {
          icon: <SettingOutlined />,
          key: 'read-vector-index-configuration',
          label: t('rowActions.readVectorIndexConfig'),
          onClick: () => handleReadVectorIndexConfig(record),
        },
        {
          icon: <SettingOutlined />,
          key: 'read-vector-index-statistics',
          label: t('rowActions.readVectorIndexStats'),
          onClick: () => handleReadVectorIndexStats(record),
        },
        {
          icon: <SettingOutlined />,
          key: 'rebuild-vector-index',
          label: t('rowActions.rebuildVectorIndex'),
          onClick: () => handleRebuildVectorIndex(record),
        },
        {
          icon: <SettingOutlined />,
          key: 'delete-vector-index',
          label: t('rowActions.deleteVectorIndex'),
          onClick: () => handleDeleteVectorIndex(record),
        },
        {
          icon: <CodeOutlined />,
          key: 'view-json',
          label: t('rowActions.viewJson'),
          onClick: () => handleViewJson?.(record),
        },
      ];

      return (
        <Dropdown menu={{ items }} trigger={['click']} placement="bottomRight">
          <LitegraphTooltip title={t('rowActions.menu')}>
            <Button type="text" icon={<MoreOutlined style={{ fontSize: '20px' }} />} />
          </LitegraphTooltip>
        </Dropdown>
      );
    },
  },
];
