import React, { useState } from 'react';
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

const PasswordCell = ({ password }: { password: string }) => {
  const [isVisible, setIsVisible] = useState(false);

  const toggleVisibility = () => {
    setIsVisible(!isVisible);
  };

  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
      <span>{isVisible ? password : '*'.repeat(8)}</span>
      <LitegraphTooltip title={isVisible ? 'Hide password' : 'Show password'}>
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

export const tableColumns = (
  handleEdit: (user: UserMetadata) => void,
  handleDelete: (user: UserMetadata) => void,
  handleViewJson?: (record: UserMetadata) => void
): TableProps<UserMetadata>['columns'] => {
  return [
    {
      title: columnTooltip('GUID', 'Globally unique identifier'),
      dataIndex: 'GUID',
      key: 'GUID',
      width: 350,
      filterDropdown: (props: FilterDropdownProps) => (
        <TableSearch {...props} placeholder="Search GUID" />
      ),
      onFilter: (value, record) => onGUIDFilter(value, record.GUID),
      render: (GUID: string) => <span style={{ display: 'inline-flex', alignItems: 'center', gap: 4, fontFamily: 'monospace', fontSize: 12, whiteSpace: 'nowrap' }}>{GUID}<CopyButton text={GUID} tooltipTitle="Copy GUID" /></span>,
    },
    {
      title: columnTooltip('First Name', 'User first name'),
      dataIndex: 'FirstName',
      key: 'FirstName',
      width: 200,
      filterDropdown: (props: FilterDropdownProps) => (
        <TableSearch {...props} placeholder="Search First Name" />
      ),
      onFilter: (value, record) => onNameFilter(value, record.FirstName),
      sorter: (a: UserMetadata, b: UserMetadata) => a.FirstName.localeCompare(b.FirstName),
      render: (FirstName: string) => <div>{FirstName}</div>,
    },
    {
      title: columnTooltip('Last Name', 'User last name'),
      dataIndex: 'LastName',
      key: 'LastName',
      width: 200,
      filterDropdown: (props: FilterDropdownProps) => (
        <TableSearch {...props} placeholder="Search Last Name" />
      ),
      onFilter: (value, record) => onNameFilter(value, record.LastName),
      sorter: (a: UserMetadata, b: UserMetadata) => a.LastName.localeCompare(b.LastName),
      render: (LastName: string) => <div>{LastName}</div>,
    },
    {
      title: columnTooltip('Email', 'User email address'),
      dataIndex: 'Email',
      key: 'Email',
      width: 200,
      filterDropdown: (props: FilterDropdownProps) => (
        <TableSearch {...props} placeholder="Search Email" />
      ),
      onFilter: (value, record) => onNameFilter(value, record.Email),
      render: (Email: string) => <div>{Email}</div>,
    },
    {
      title: columnTooltip('Password', 'User password (click eye to reveal)'),
      dataIndex: 'Password',
      key: 'Password',
      width: 200,
      render: (Password: string, record: UserMetadata) => <PasswordCell password={Password} />,
    },
    {
      title: columnTooltip('Active', 'Whether the user account is active'),
      dataIndex: 'Active',
      key: 'Active',
      width: 100,
      sorter: (a: UserMetadata, b: UserMetadata) => Number(b.Active) - Number(a.Active),
      render: (active: boolean) =>
        active ? (
          <CheckCircleFilled style={{ color: 'green' }} />
        ) : (
          <CloseCircleFilled style={{ color: 'red' }} />
        ),
    },
    {
      title: columnTooltip('Created UTC', 'Date and time of creation in UTC'),
      dataIndex: 'CreatedUtc',
      key: 'CreatedUtc',
      width: 200,
      sorter: (a: UserMetadata, b: UserMetadata) =>
        new Date(a.CreatedUtc).getTime() - new Date(b.CreatedUtc).getTime(),
      render: (CreatedUtc: string) => <div>{formatDateTime(CreatedUtc)}</div>,
    },
    {
      title: columnTooltip('Actions', 'Available operations'),
      key: 'actions',
      width: 100,
      render: (_: any, record: UserMetadata) => {
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
