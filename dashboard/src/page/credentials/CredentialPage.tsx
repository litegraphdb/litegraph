'use client';
import React, { useState } from 'react';
import { useTranslations } from 'next-intl';
import { PlusSquareOutlined } from '@ant-design/icons';
import PageContainer from '@/components/base/pageContainer/PageContainer';
import LitegraphButton from '@/components/base/button/Button';
import LitegraphTable from '@/components/base/table/Table';
import { CredentialType } from '@/types/types';
import AddEditCredential from './components/AddEditCredential';
import DeleteCredential from './components/DeleteCredential';

import { tableColumns } from './constant';
import FallBack from '@/components/base/fallback/FallBack';
import ViewJsonModal from '@/components/base/view-json-modal/ViewJsonModal';
import { usePagination } from '@/hooks/appHooks';
import { useEnumerateCredentialQuery, useGetAllUsersQuery } from '@/lib/store/slice/slice';
import { tablePaginationConfig } from '@/constants/pagination';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';

const CredentialPage = () => {
  const t = useTranslations('credentials');
  const tCommon = useTranslations('common');
  const [selectedCredential, setSelectedCredential] = useState<CredentialType | null>(null);
  const [isAddEditCredentialVisible, setIsAddEditCredentialVisible] = useState<boolean>(false);
  const [isDeleteModelVisible, setIsDeleteModelVisible] = useState<boolean>(false);
  const [jsonViewRecord, setJsonViewRecord] = useState<any>(null);
  const { data: usersList = [], isLoading: isUsersLoading } = useGetAllUsersQuery();
  const { skip, page, pageSize, handlePageChange } = usePagination();

  const {
    data,
    refetch: fetchCredentialsList,
    isLoading,
    isFetching,
    error,
  } = useEnumerateCredentialQuery({
    skip: skip,
    maxKeys: pageSize,
  });
  const credentialsList = data?.Objects || [];
  const isCredentialsLoading = isLoading || isFetching;
  const credentialsListWithUsers = credentialsList.map((credential) => {
    const user = usersList.find((user) => user.GUID === credential.UserGUID);
    return {
      ...credential,
      userName: user ? `${user.FirstName} ${user.LastName}` : 'N/A',
    };
  });

  const handleCreateCredential = () => {
    setSelectedCredential(null);
    setIsAddEditCredentialVisible(true);
  };

  const handleEditCredential = (data: CredentialType) => {
    setSelectedCredential(data);
    setIsAddEditCredentialVisible(true);
  };

  const handleDeleteCredential = (data: CredentialType) => {
    setSelectedCredential(data);
    setIsDeleteModelVisible(true);
  };

  return (
    <PageContainer
      id="credentials"
      pageTitle={t('title')}
      pageTitleRightContent={
        <LitegraphTooltip title={t('createTooltip')}>
          <LitegraphButton
            type="link"
            icon={<PlusSquareOutlined />}
            onClick={handleCreateCredential}
            weight={500}
          >
            {t('createCredential')}
          </LitegraphButton>
        </LitegraphTooltip>
      }
    >
      {error && !isCredentialsLoading ? (
        <FallBack retry={fetchCredentialsList}>{tCommon('states.somethingWentWrong')}</FallBack>
      ) : (
        <LitegraphTable
          hideHorizontalScroll
          loading={isCredentialsLoading || isUsersLoading}
          columns={tableColumns(t, handleEditCredential, handleDeleteCredential, setJsonViewRecord)}
          dataSource={credentialsListWithUsers}
          rowKey={'GUID'}
          onRowClick={handleEditCredential}
          onRefresh={fetchCredentialsList}
          isRefreshing={isCredentialsLoading || isUsersLoading}
          pagination={{
            ...tablePaginationConfig,
            total: data?.TotalRecords,
            pageSize: pageSize,
            current: page,
            onChange: handlePageChange,
          }}
        />
      )}

      {isAddEditCredentialVisible && (
        <AddEditCredential
          isAddEditCredentialVisible={isAddEditCredentialVisible}
          setIsAddEditCredentialVisible={setIsAddEditCredentialVisible}
          credential={selectedCredential || null}
        />
      )}

      {isDeleteModelVisible && selectedCredential && (
        <DeleteCredential
          title={t('deleteTitle', { name: selectedCredential.Name })}
          paragraphText={t('deleteBody')}
          isDeleteModelVisible={isDeleteModelVisible}
          setIsDeleteModelVisible={setIsDeleteModelVisible}
          selectedCredential={selectedCredential}
          setSelectedCredential={setSelectedCredential}
        />
      )}
      <ViewJsonModal
        open={!!jsonViewRecord}
        onClose={() => setJsonViewRecord(null)}
        data={jsonViewRecord}
        title={t('credentialJson')}
      />
    </PageContainer>
  );
};

export default CredentialPage;
