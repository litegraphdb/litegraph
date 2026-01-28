'use client';
import React, { useState } from 'react';
import { LoadingOutlined, PlusSquareOutlined, RedoOutlined } from '@ant-design/icons';
import PageContainer from '@/components/base/pageContainer/PageContainer';
import LitegraphButton from '@/components/base/button/Button';
import LitegraphTable from '@/components/base/table/Table';
import { CredentialType } from '@/types/types';
import AddEditCredential from './components/AddEditCredential';
import DeleteCredential from './components/DeleteCredential';

import { tableColumns } from './constant';
import FallBack from '@/components/base/fallback/FallBack';
import { usePagination } from '@/hooks/appHooks';
import { useEnumerateCredentialQuery, useGetAllUsersQuery } from '@/lib/store/slice/slice';
import { tablePaginationConfig } from '@/constants/pagination';
import LitegraphFlex from '@/components/base/flex/Flex';
import LitegraphText from '@/components/base/typograpghy/Text';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';

const CredentialPage = () => {
  const [selectedCredential, setSelectedCredential] = useState<CredentialType | null>(null);
  const [isAddEditCredentialVisible, setIsAddEditCredentialVisible] = useState<boolean>(false);
  const [isDeleteModelVisible, setIsDeleteModelVisible] = useState<boolean>(false);
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
      pageTitle={
        <LitegraphFlex align="center" gap={10}>
          <LitegraphText>Credentials</LitegraphText>
          {isCredentialsLoading ? (
            <LoadingOutlined className="loading-icon" />
          ) : (
            <LitegraphTooltip title="Refresh Data" placement="right">
              <RedoOutlined className="cursor-pointer" onClick={fetchCredentialsList} />
            </LitegraphTooltip>
          )}
        </LitegraphFlex>
      }
      pageTitleRightContent={
        <LitegraphButton
          type="link"
          icon={<PlusSquareOutlined />}
          onClick={handleCreateCredential}
          weight={500}
        >
          Create Credential
        </LitegraphButton>
      }
    >
      {error && !isCredentialsLoading ? (
        <FallBack retry={fetchCredentialsList}>Something went wrong.</FallBack>
      ) : (
        <LitegraphTable
          loading={isCredentialsLoading || isUsersLoading}
          columns={tableColumns(handleEditCredential, handleDeleteCredential)}
          dataSource={credentialsListWithUsers}
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

      {isAddEditCredentialVisible && (
        <AddEditCredential
          isAddEditCredentialVisible={isAddEditCredentialVisible}
          setIsAddEditCredentialVisible={setIsAddEditCredentialVisible}
          credential={selectedCredential || null}
        />
      )}

      {isDeleteModelVisible && selectedCredential && (
        <DeleteCredential
          title={`Are you sure you want to delete "${selectedCredential.Name}" credential?`}
          paragraphText={'This action will delete credential.'}
          isDeleteModelVisible={isDeleteModelVisible}
          setIsDeleteModelVisible={setIsDeleteModelVisible}
          selectedCredential={selectedCredential}
          setSelectedCredential={setSelectedCredential}
        />
      )}
    </PageContainer>
  );
};

export default CredentialPage;
