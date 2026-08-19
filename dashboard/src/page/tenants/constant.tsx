import React from 'react';
import { Button, Dropdown, TableProps } from 'antd';
import {
  CheckCircleFilled,
  CloseCircleFilled,
  MoreOutlined,
  CodeOutlined,
} from '@ant-design/icons';
import CopyButton from '@/components/base/copy-button/CopyButton';
import { formatDateTime } from '@/utils/dateUtils';
import { FilterDropdownProps } from 'antd/es/table/interface';
import TableSearch from '@/components/table-search/TableSearch';
import { onGUIDFilter, onNameFilter } from '@/constants/table';
import { TenantMetaData } from 'litegraphdb/dist/types/types';
import { columnTooltip } from '@/utils/tooltipUtils';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';

const monoCellStyle = {
  display: 'inline-flex',
  alignItems: 'center',
  gap: 4,
  fontFamily: 'monospace',
  fontSize: 12,
  maxWidth: '100%',
  minWidth: 0,
  width: '100%',
} as const;

const monoValueStyle = {
  display: 'block',
  flex: '1 1 auto',
  minWidth: 0,
  overflow: 'hidden',
  textOverflow: 'ellipsis',
  whiteSpace: 'nowrap',
} as const;

type Translator = (key: string, values?: Record<string, string | number>) => string;

export const tableColumns = (
  t: Translator,
  handleEdit: (tenant: TenantMetaData) => void,
  handleDelete: (tenant: TenantMetaData) => void,
  handleViewJson?: (record: TenantMetaData) => void
): TableProps<TenantMetaData>['columns'] => [
  {
    title: columnTooltip(t('columns.guid'), t('columns.guidDesc')),
    dataIndex: 'GUID',
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder={t('search.guid')} />
    ),
    onFilter: (value, record) => onGUIDFilter(value, record.GUID),
    key: 'GUID',
    width: 240,
    ellipsis: true,
    render: (GUID: string) => (
      <span style={monoCellStyle} title={GUID}>
        <span style={monoValueStyle}>{GUID}</span>
        <CopyButton text={GUID} tooltipTitle={t('copyGuid')} />
      </span>
    ),
  },
  {
    title: columnTooltip(t('columns.name'), t('columns.nameDesc')),
    dataIndex: 'Name',
    key: 'Name',
    width: 220,
    ellipsis: true,
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder={t('search.name')} />
    ),
    onFilter: (value, record) => onNameFilter(value, record.Name),
    sorter: (a: TenantMetaData, b: TenantMetaData) => a.Name.localeCompare(b.Name),
    render: (Name: string) => <div>{Name}</div>,
  },

  {
    title: columnTooltip(t('columns.active'), t('columns.activeDesc')),
    dataIndex: 'Active',
    key: 'Active',
    width: 80,
    sorter: (a: TenantMetaData, b: TenantMetaData) => Number(b.Active) - Number(a.Active),
    render: (active: boolean) =>
      active ? (
        <CheckCircleFilled style={{ color: 'green' }} />
      ) : (
        <CloseCircleFilled style={{ color: 'red' }} />
      ),
  },
  {
    title: columnTooltip(t('columns.createdUtc'), t('columns.createdUtcDesc')),
    dataIndex: 'CreatedUtc',
    sorter: (a: TenantMetaData, b: TenantMetaData) =>
      new Date(a.CreatedUtc).getTime() - new Date(b.CreatedUtc).getTime(),
    key: 'CreatedUtc',
    width: 150,
    ellipsis: true,
    render: (CreatedUtc: string) => <div>{formatDateTime(CreatedUtc)}</div>,
  },
  {
    title: columnTooltip(t('columns.actions'), t('columns.actionsDesc')),
    key: 'actions',
    width: 70,
    render: (_: any, record: TenantMetaData) => {
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
              role="tenant-action-menu"
              icon={<MoreOutlined style={{ fontSize: '20px' }} />}
              style={{ fontSize: '16px' }}
            />
          </LitegraphTooltip>
        </Dropdown>
      );
    },
  },
];
