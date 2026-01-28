import React, { useState } from 'react';
import { Button, Dropdown, TableProps } from 'antd';
import {
  MoreOutlined,
  CheckCircleFilled,
  CloseCircleFilled,
  EyeOutlined,
  EyeInvisibleOutlined,
} from '@ant-design/icons';
import { formatDateTime } from '@/utils/dateUtils';
import { FilterDropdownProps } from 'antd/es/table/interface';
import TableSearch from '@/components/table-search/TableSearch';
import { onGUIDFilter, onNameFilter } from '@/constants/table';
import { UserMetadata } from 'litegraphdb/dist/types/types';

const PasswordCell = ({ password }: { password: string }) => {
  const [isVisible, setIsVisible] = useState(false);

  const toggleVisibility = () => {
    setIsVisible(!isVisible);
  };

  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
      <span>{isVisible ? password : '*'.repeat(8)}</span>
      <Button
        type="text"
        size="small"
        icon={isVisible ? <EyeInvisibleOutlined /> : <EyeOutlined />}
        onClick={toggleVisibility}
        style={{ padding: '0 4px', minWidth: 'auto' }}
      />
    </div>
  );
};

export const tableColumns = (
  handleEdit: (user: UserMetadata) => void,
  handleDelete: (user: UserMetadata) => void
): TableProps<UserMetadata>['columns'] => {
  return [
    {
      title: 'GUID',
      dataIndex: 'GUID',
      key: 'GUID',
      width: 350,
      filterDropdown: (props: FilterDropdownProps) => (
        <TableSearch {...props} placeholder="Search GUID" />
      ),
      onFilter: (value, record) => onGUIDFilter(value, record.GUID),
      render: (GUID: string) => <div>{GUID}</div>,
    },
    {
      title: 'First Name',
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
      title: 'Last Name',
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
      title: 'Email',
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
      title: 'Password',
      dataIndex: 'Password',
      key: 'Password',
      width: 200,
      render: (Password: string, record: UserMetadata) => <PasswordCell password={Password} />,
    },
    {
      title: 'Active',
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
      title: 'Created UTC',
      dataIndex: 'CreatedUtc',
      key: 'CreatedUtc',
      width: 200,
      sorter: (a: UserMetadata, b: UserMetadata) =>
        new Date(a.CreatedUtc).getTime() - new Date(b.CreatedUtc).getTime(),
      render: (CreatedUtc: string) => <div>{formatDateTime(CreatedUtc)}</div>,
    },
    {
      title: 'Actions',
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
        ];
        return (
          <Dropdown menu={{ items }} trigger={['click']} placement="bottomRight">
            <Button
              role="user-action-menu"
              type="text"
              icon={<MoreOutlined style={{ fontSize: '20px' }} />}
              style={{ fontSize: '16px' }}
            />
          </Dropdown>
        );
      },
    },
  ];
};
