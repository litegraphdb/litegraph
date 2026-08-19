import React, { useState } from 'react';
import { useTranslations } from 'next-intl';
import { Button, Dropdown, TableProps } from 'antd';
import {
  MoreOutlined,
  CheckCircleFilled,
  CloseCircleFilled,
  EyeOutlined,
  EyeInvisibleOutlined,
  CodeOutlined,
} from '@ant-design/icons';
import CopyButton from '@/components/base/copy-button/CopyButton';
import { formatDateTime } from '@/utils/dateUtils';
import { FilterDropdownProps } from 'antd/es/table/interface';
import TableSearch from '@/components/table-search/TableSearch';
import { onGUIDFilter, onNameFilter } from '@/constants/table';
import { UserMetadata } from 'litegraphdb/dist/types/types';
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

const PasswordCell = ({ password }: { password: string }) => {
  const t = useTranslations('users');
  const [isVisible, setIsVisible] = useState(false);

  const toggleVisibility = () => {
    setIsVisible(!isVisible);
  };

  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
      <span>{isVisible ? password : '*'.repeat(8)}</span>
      <LitegraphTooltip title={isVisible ? t('password.hide') : t('password.show')}>
        <Button
          type="text"
          size="small"
          icon={isVisible ? <EyeInvisibleOutlined /> : <EyeOutlined />}
          onClick={toggleVisibility}
          style={{ padding: '0 4px', minWidth: 'auto' }}
        />
      </LitegraphTooltip>
    </div>
  );
};

type Translator = (key: string, values?: Record<string, string | number>) => string;

export const tableColumns = (
  t: Translator,
  handleEdit: (user: UserMetadata) => void,
  handleDelete: (user: UserMetadata) => void,
  handleViewJson?: (record: UserMetadata) => void
): TableProps<UserMetadata>['columns'] => {
  return [
    {
      title: columnTooltip(t('columns.guid'), t('columns.guidDesc')),
      dataIndex: 'GUID',
      key: 'GUID',
      width: 220,
      ellipsis: true,
      filterDropdown: (props: FilterDropdownProps) => (
        <TableSearch {...props} placeholder={t('search.guid')} />
      ),
      onFilter: (value, record) => onGUIDFilter(value, record.GUID),
      render: (GUID: string) => (
        <span style={monoCellStyle} title={GUID}>
          <span style={monoValueStyle}>{GUID}</span>
          <CopyButton text={GUID} tooltipTitle={t('copyGuid')} />
        </span>
      ),
    },
    {
      title: columnTooltip(t('columns.firstName'), t('columns.firstNameDesc')),
      dataIndex: 'FirstName',
      key: 'FirstName',
      width: 120,
      ellipsis: true,
      filterDropdown: (props: FilterDropdownProps) => (
        <TableSearch {...props} placeholder={t('search.firstName')} />
      ),
      onFilter: (value, record) => onNameFilter(value, record.FirstName),
      sorter: (a: UserMetadata, b: UserMetadata) => a.FirstName.localeCompare(b.FirstName),
      render: (FirstName: string) => <div>{FirstName}</div>,
    },
    {
      title: columnTooltip(t('columns.lastName'), t('columns.lastNameDesc')),
      dataIndex: 'LastName',
      key: 'LastName',
      width: 120,
      ellipsis: true,
      filterDropdown: (props: FilterDropdownProps) => (
        <TableSearch {...props} placeholder={t('search.lastName')} />
      ),
      onFilter: (value, record) => onNameFilter(value, record.LastName),
      sorter: (a: UserMetadata, b: UserMetadata) => a.LastName.localeCompare(b.LastName),
      render: (LastName: string) => <div>{LastName}</div>,
    },
    {
      title: columnTooltip(t('columns.email'), t('columns.emailDesc')),
      dataIndex: 'Email',
      key: 'Email',
      width: 170,
      ellipsis: true,
      filterDropdown: (props: FilterDropdownProps) => (
        <TableSearch {...props} placeholder={t('search.email')} />
      ),
      onFilter: (value, record) => onNameFilter(value, record.Email),
      render: (Email: string) => <div>{Email}</div>,
    },
    {
      title: columnTooltip(t('columns.password'), t('columns.passwordDesc')),
      dataIndex: 'Password',
      key: 'Password',
      width: 100,
      render: (Password: string, record: UserMetadata) => <PasswordCell password={Password} />,
    },
    {
      title: columnTooltip(t('columns.active'), t('columns.activeDesc')),
      dataIndex: 'Active',
      key: 'Active',
      width: 70,
      sorter: (a: UserMetadata, b: UserMetadata) => Number(b.Active) - Number(a.Active),
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
      key: 'CreatedUtc',
      width: 150,
      ellipsis: true,
      sorter: (a: UserMetadata, b: UserMetadata) =>
        new Date(a.CreatedUtc).getTime() - new Date(b.CreatedUtc).getTime(),
      render: (CreatedUtc: string) => <div>{formatDateTime(CreatedUtc)}</div>,
    },
    {
      title: columnTooltip(t('columns.actions'), t('columns.actionsDesc')),
      key: 'actions',
      width: 70,
      render: (_: any, record: UserMetadata) => {
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
                role="user-action-menu"
                type="text"
                icon={<MoreOutlined style={{ fontSize: '20px' }} />}
                style={{ fontSize: '16px' }}
              />
            </LitegraphTooltip>
          </Dropdown>
        );
      },
    },
  ];
};
