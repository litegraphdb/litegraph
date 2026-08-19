import React from 'react';
import { MoreOutlined, CodeOutlined } from '@ant-design/icons';
import CopyButton from '@/components/base/copy-button/CopyButton';
import { Button, Dropdown, TableProps } from 'antd';
import { NodeType } from '@/types/types';
import { formatDateTime } from '@/utils/dateUtils';
import { isNumber } from 'lodash';
import { NOT_AVAILABLE } from '@/constants/uiLabels';
import TableSearch from '@/components/table-search/TableSearch';
import { FilterDropdownProps } from 'antd/es/table/interface';
import { onGUIDFilter, onLabelFilter, onNameFilter, onTagFilter } from '@/constants/table';
import { columnTooltip } from '@/utils/tooltipUtils';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';
import CountBadge from '@/components/base/count-badge/CountBadge';

type Translator = (key: string, values?: Record<string, string | number>) => string;

export const tableColumns = (
  t: Translator,
  handleEdit: (record: NodeType) => void,
  handleDelete: (record: NodeType) => void,
  hasScoreOrDistance: boolean,
  handleViewJson?: (record: NodeType) => void
): TableProps<NodeType>['columns'] => [
  {
    title: columnTooltip(t('columns.name'), t('columns.nameDesc')),
    dataIndex: 'Name' as keyof NodeType,
    key: 'Name',
    width: 250,
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder={t('search.name')} />
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
    title: columnTooltip(t('columns.guid'), t('columns.guidDesc')),
    dataIndex: 'GUID' as keyof NodeType,
    key: 'GUID',
    width: 350,
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
    dataIndex: 'Labels' as keyof NodeType,
    key: 'Labels',
    width: 150,
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder={t('search.labels')} />
    ),
    onFilter: (value, record) => onLabelFilter(value, record.Labels),
    render: (label: string[]) => {
      const count = label?.length ?? 0;
      return <CountBadge count={count} label={t('labelsCount', { count })} />;
    },
  },
  {
    title: columnTooltip(t('columns.tags'), t('columns.tagsDesc')),
    dataIndex: 'Tags' as keyof NodeType,
    key: 'Tags',
    width: 250,
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder={t('search.tags')} />
    ),
    onFilter: (val, record) => onTagFilter(val, record.Tags),
    render: (tags: any) => {
      const count = Object.keys(tags || {}).length;
      return <CountBadge count={count} label={t('tagsCount', { count })} />;
    },
  },
  {
    title: columnTooltip(t('columns.vectors'), t('columns.vectorsDesc')),
    dataIndex: 'Vectors',
    key: 'Vectors',
    width: 250,
    render: (_: any, record: NodeType) => {
      const count = record?.Vectors?.length || 0;
      return <div>{t('vectorsCount', { count })}</div>;
    },
  },
  {
    title: columnTooltip(t('columns.createdUtc'), t('columns.createdUtcDesc')),
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
          title: columnTooltip(t('columns.score'), t('columns.scoreDesc')),
          dataIndex: 'Score' as keyof NodeType,
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
          dataIndex: 'Distance' as keyof NodeType,
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
    render: (_: any, record: NodeType) => {
      const items = [
        {
          key: 'edit',
          label: t('rowActions.edit'),
          onClick: () => handleEdit(record),
        },
        {
          key: 'delete',
          label: t('rowActions.delete'),
          onClick: () => handleDelete(record),
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
