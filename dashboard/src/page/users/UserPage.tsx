'use client';
import React, { useState } from 'react';
import { LoadingOutlined, PlusSquareOutlined, RedoOutlined } from '@ant-design/icons';
import PageContainer from '@/components/base/pageContainer/PageContainer';
import LitegraphButton from '@/components/base/button/Button';
import LitegraphTable from '@/components/base/table/Table';
import AddEditUser from './components/AddEditUser';
import DeleteUser from './components/DeleteUser';
import { tableColumns } from './constant';
import FallBack from '@/components/base/fallback/FallBack';
import { usePagination } from '@/hooks/appHooks';
import { useEnumerateUserQuery } from '@/lib/store/slice/slice';
import { tablePaginationConfig } from '@/constants/pagination';
import { useSelectedTenant } from '@/hooks/entityHooks';
import { UserMetadata } from 'litegraphdb/dist/types/types';
import LitegraphFlex from '@/components/base/flex/Flex';
import LitegraphText from '@/components/base/typograpghy/Text';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';

const UserPage = () => {
  const [selectedUser, setSelectedUser] = useState<UserMetadata | null>(null);
  const [isAddEditUserVisible, setIsAddEditUserVisible] = useState<boolean>(false);
  const [isDeleteModelVisible, setIsDeleteModelVisible] = useState<boolean>(false);
  const { page, pageSize, skip, handlePageChange } = usePagination();
  const selectedTenantRedux = useSelectedTenant();
  const {
    data,
    refetch: fetchUsersList,
    isLoading,
    isFetching,
    error,
  } = useEnumerateUserQuery(
    {
      maxKeys: pageSize,
      skip: skip,
    },
    {
      skip: !selectedTenantRedux,
    }
  );
  const isUsersLoading = isLoading || isFetching;
  const usersList = data?.Objects || [];

  const handleCreateUser = () => {
    setSelectedUser(null);
    setIsAddEditUserVisible(true);
  };

  const handleEditUser = (data: UserMetadata) => {
    setSelectedUser(data);
    setIsAddEditUserVisible(true);
  };

  const handleDeleteUser = (data: UserMetadata) => {
    setSelectedUser(data);
    setIsDeleteModelVisible(true);
  };

  return (
    <PageContainer
      id="users"
      pageTitle={
        <LitegraphFlex align="center" gap={10}>
          <LitegraphText>Users</LitegraphText>
          {isUsersLoading ? (
            <LoadingOutlined className="loading-icon" />
          ) : (
            <LitegraphTooltip title="Refresh Data" placement="right">
              <RedoOutlined className="cursor-pointer" onClick={fetchUsersList} />
            </LitegraphTooltip>
          )}
        </LitegraphFlex>
      }
      pageTitleRightContent={
        <LitegraphButton
          type="link"
          icon={<PlusSquareOutlined />}
          onClick={handleCreateUser}
          weight={500}
        >
          Create User
        </LitegraphButton>
      }
    >
      {error && !isUsersLoading ? (
        <FallBack retry={fetchUsersList}>Something went wrong.</FallBack>
      ) : (
        <LitegraphTable
          loading={isUsersLoading}
          columns={tableColumns(handleEditUser, handleDeleteUser)}
          dataSource={usersList}
          rowKey={'GUID'}
          pagination={{
            ...tablePaginationConfig,
            total: data?.TotalRecords,
            pageSize: pageSize,
            current: page,
            onChange: handlePageChange,
          }}
        />
      )}

      {isAddEditUserVisible && (
        <AddEditUser
          isAddEditUserVisible={isAddEditUserVisible}
          setIsAddEditUserVisible={setIsAddEditUserVisible}
          user={selectedUser || null}
        />
      )}

      {isDeleteModelVisible && selectedUser && (
        <DeleteUser
          title={`Are you sure you want to delete "${selectedUser.FirstName}" user?`}
          paragraphText={'This action will delete user.'}
          isDeleteModelVisible={isDeleteModelVisible}
          setIsDeleteModelVisible={setIsDeleteModelVisible}
          selectedUser={selectedUser}
          setSelectedUser={setSelectedUser}
        />
      )}
    </PageContainer>
  );
};

export default UserPage;
