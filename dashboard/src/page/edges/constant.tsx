import React from 'react';
import { LoadingOutlined, MoreOutlined, CodeOutlined } from '@ant-design/icons';
import CopyButton from '@/components/base/copy-button/CopyButton';
import { Button, Dropdown, TableProps } from 'antd';
import { EdgeType } from '@/types/types';
import { formatDateTime } from '@/utils/dateUtils';
import { isNumber } from 'lodash';
import TableSearch from '@/components/table-search/TableSearch';
import { FilterDropdownProps } from 'antd/es/table/interface';
import { onGUIDFilter, onLabelFilter, onNameFilter, onTagFilter } from '@/constants/table';
import { columnTooltip } from '@/utils/tooltipUtils';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';
import CountBadge from '@/components/base/count-badge/CountBadge';

type Translator = (key: string, values?: Record<string, string | number>) => string;

export const tableColumns = (
  t: Translator,
  tCommon: Translator,
  handleEdit: (record: EdgeType) => void,
  handleDelete: (record: EdgeType) => void,
  hasScoreOrDistance: boolean,
  isNodesLoading: boolean,
  handleViewJson?: (record: EdgeType) => void
): TableProps<EdgeType>['columns'] => [
  {
    title: columnTooltip(t('columns.name'), t('columns.nameDesc')),
    dataIndex: 'Name' as keyof EdgeType,
    key: 'Name',
    width: 250,
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder={t('search.name')} />
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
    title: columnTooltip(t('columns.guid'), t('columns.guidDesc')),
    dataIndex: 'GUID',
    key: 'GUID',
    width: 350,
    responsive: ['md'],
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
        <CopyButton text={GUID} tooltipTitle={tCommon('copy.copyGuid')} />
      </span>
    ),
  },
  {
    title: columnTooltip(t('columns.from'), t('columns.fromDesc')),
    dataIndex: 'FromName' as keyof EdgeType,
    key: 'FromName',
    width: 250,
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder={t('search.from')} />
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
    title: columnTooltip(t('columns.to'), t('columns.toDesc')),
    dataIndex: 'ToName' as keyof EdgeType,
    key: 'ToName',
    width: 250,
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder={t('search.to')} />
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
    title: columnTooltip(t('columns.cost'), t('columns.costDesc')),
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
    title: columnTooltip(t('columns.labels'), t('columns.labelsDesc')),
    dataIndex: 'Labels' as keyof EdgeType,
    key: 'Labels',
    width: 150,
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder={t('search.labels')} />
    ),
    onFilter: (value, record) => onLabelFilter(value, record.Labels),
    render: (Labels: string[]) => {
      const count = Labels?.length ?? 0;
      return <CountBadge count={count} label={t('labelsCount', { count })} />;
    },
  },
  {
    title: columnTooltip(t('columns.tags'), t('columns.tagsDesc')),
    dataIndex: 'Tags' as keyof EdgeType,
    key: 'Tags',
    width: 150,
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder={t('search.tags')} />
    ),
    onFilter: (val, record) => onTagFilter(val, record.Tags),
    render: (Tags: any) => {
      const count = Object.keys(Tags || {}).length;
      return <CountBadge count={count} label={t('tagsCount', { count })} />;
    },
  },
  {
    title: columnTooltip(t('columns.vectors'), t('columns.vectorsDesc')),
    dataIndex: 'Vectors' as keyof EdgeType,
    key: 'Vectors',
    width: 150,
    responsive: ['md'],
    render: (_: any, record: EdgeType) => {
      const count = record?.Vectors?.length || 0;
      return <div>{t('vectorsCount', { count })}</div>;
    },
  },
  {
    title: columnTooltip(t('columns.createdUtc'), t('columns.createdUtcDesc')),
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
          title: columnTooltip(t('columns.score'), t('columns.scoreDesc')),
          dataIndex: 'Score' as keyof EdgeType,
          key: 'Score',
          width: 150,
          render: (score: number) => (
            <div>
              <div>{isNumber(score) ? score : tCommon('states.notAvailable')}</div>
            </div>
          ),
        },
        {
          title: columnTooltip(t('columns.distance'), t('columns.distanceDesc')),
          dataIndex: 'Distance' as keyof EdgeType,
          key: 'Distance',
          width: 150,
          render: (Distance: number) => (
            <div>
              <div>{isNumber(Distance) ? Distance : tCommon('states.notAvailable')}</div>
            </div>
          ),
        },
      ]
    : []),
  {
    title: columnTooltip(t('columns.actions'), t('columns.actionsDesc')),
    key: 'actions',
    render: (_: any, record: EdgeType) => {
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
            <Button type="text" icon={<MoreOutlined style={{ fontSize: '20px' }} />} />
          </LitegraphTooltip>
        </Dropdown>
      );
    },
  },
];
