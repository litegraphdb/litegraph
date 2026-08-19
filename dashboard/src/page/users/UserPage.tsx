'use client';
import React, { useState } from 'react';
import { useTranslations } from 'next-intl';
import { PlusSquareOutlined } from '@ant-design/icons';
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
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';
import ViewJsonModal from '@/components/base/view-json-modal/ViewJsonModal';

const UserPage = () => {
  const t = useTranslations('users');
  const tCommon = useTranslations('common');
  const [selectedUser, setSelectedUser] = useState<UserMetadata | null>(null);
  const [isAddEditUserVisible, setIsAddEditUserVisible] = useState<boolean>(false);
  const [isDeleteModelVisible, setIsDeleteModelVisible] = useState<boolean>(false);
  const [jsonViewRecord, setJsonViewRecord] = useState<any>(null);
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
      pageTitle={t('title')}
      pageTitleRightContent={
        <LitegraphTooltip title={t('createTooltip')}>
          <LitegraphButton
            type="link"
            icon={<PlusSquareOutlined />}
            onClick={handleCreateUser}
            weight={500}
          >
            {t('createUser')}
          </LitegraphButton>
        </LitegraphTooltip>
      }
    >
      {error && !isUsersLoading ? (
        <FallBack retry={fetchUsersList}>{tCommon('states.somethingWentWrong')}</FallBack>
      ) : (
        <LitegraphTable
          hideHorizontalScroll
          loading={isUsersLoading}
          columns={tableColumns(t, handleEditUser, handleDeleteUser, setJsonViewRecord)}
          dataSource={usersList}
          rowKey={'GUID'}
          onRowClick={handleEditUser}
          onRefresh={fetchUsersList}
          isRefreshing={isUsersLoading}
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
          title={t('deleteTitle', { name: selectedUser.FirstName })}
          paragraphText={t('deleteBody')}
          isDeleteModelVisible={isDeleteModelVisible}
          setIsDeleteModelVisible={setIsDeleteModelVisible}
          selectedUser={selectedUser}
          setSelectedUser={setSelectedUser}
        />
      )}
      <ViewJsonModal
        open={!!jsonViewRecord}
        onClose={() => setJsonViewRecord(null)}
        data={jsonViewRecord}
        title={t('userJson')}
      />
    </PageContainer>
  );
};

export default UserPage;
