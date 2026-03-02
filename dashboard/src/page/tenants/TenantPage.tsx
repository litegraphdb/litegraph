'use client';
import React, { useState } from 'react';
import { LoadingOutlined, PlusSquareOutlined, RedoOutlined } from '@ant-design/icons';
import PageContainer from '@/components/base/pageContainer/PageContainer';
import LitegraphButton from '@/components/base/button/Button';
import LitegraphTable from '@/components/base/table/Table';
import AddEditTenant from './components/AddEditTenant';
import DeleteTenant from './components/DeleteTenant';
import { tableColumns } from './constant';
import FallBack from '@/components/base/fallback/FallBack';
import { usePagination } from '@/hooks/appHooks';
import { useEnumerateTenantQuery } from '@/lib/store/slice/slice';
import { tablePaginationConfig } from '@/constants/pagination';
import { TenantMetaData } from 'litegraphdb/dist/types/types';
import LitegraphFlex from '@/components/base/flex/Flex';
import LitegraphText from '@/components/base/typograpghy/Text';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';
import ViewJsonModal from '@/components/base/view-json-modal/ViewJsonModal';

const TenantPage = () => {
  const [selectedTenant, setSelectedTenant] = useState<TenantMetaData | null>(null);
  const [isAddEditTenantVisible, setIsAddEditTenantVisible] = useState<boolean>(false);
  const [isDeleteModelVisible, setIsDeleteModelVisible] = useState<boolean>(false);
  const [jsonViewRecord, setJsonViewRecord] = useState<any>(null);
  const { page, pageSize, skip, handlePageChange } = usePagination();
  const {
    data,
    refetch: fetchTenantsList,
    isLoading,
    isFetching,
    error,
  } = useEnumerateTenantQuery({
    maxKeys: pageSize,
    skip: skip,
  });
  const tenantsList = data?.Objects || [];
  const isTenantsLoading = isLoading || isFetching;
  const handleCreateTenant = () => {
    setSelectedTenant(null);
    setIsAddEditTenantVisible(true);
  };

  const handleEditTenant = (data: TenantMetaData) => {
    setSelectedTenant(data);
    setIsAddEditTenantVisible(true);
  };

  const handleDeleteTenant = (data: TenantMetaData) => {
    setSelectedTenant(data);
    setIsDeleteModelVisible(true);
  };

  return (
    <PageContainer
      id="tenants"
      pageTitle={
        <LitegraphFlex align="center" gap={10}>
          <LitegraphText>Tenants</LitegraphText>
          {isTenantsLoading ? (
            <LoadingOutlined className="loading-icon" />
          ) : (
            <LitegraphTooltip title="Refresh Data" placement="right">
              <RedoOutlined className="cursor-pointer" onClick={fetchTenantsList} />
            </LitegraphTooltip>
          )}
        </LitegraphFlex>
      }
      pageTitleRightContent={
        <LitegraphTooltip title="Create a new tenant">
          <LitegraphButton
            type="link"
            icon={<PlusSquareOutlined />}
            onClick={handleCreateTenant}
            weight={500}
          >
            Create Tenant
          </LitegraphButton>
        </LitegraphTooltip>
      }
    >
      {error && !isTenantsLoading ? (
        <FallBack retry={fetchTenantsList}>Something went wrong.</FallBack>
      ) : (
        <LitegraphTable
          loading={isTenantsLoading}
          columns={tableColumns(handleEditTenant, handleDeleteTenant, setJsonViewRecord)}
          dataSource={tenantsList}
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

      {isAddEditTenantVisible && (
        <AddEditTenant
          isAddEditTenantVisible={isAddEditTenantVisible}
          setIsAddEditTenantVisible={setIsAddEditTenantVisible}
          tenant={selectedTenant || null}
        />
      )}

      {isDeleteModelVisible && selectedTenant && (
        <DeleteTenant
          title={`Are you sure you want to delete "${selectedTenant.Name}" tenant?`}
          paragraphText={'This action will delete tenant.'}
          isDeleteModelVisible={isDeleteModelVisible}
          setIsDeleteModelVisible={setIsDeleteModelVisible}
          selectedTenant={selectedTenant}
          setSelectedTenant={setSelectedTenant}
        />
      )}
      <ViewJsonModal
        open={!!jsonViewRecord}
        onClose={() => setJsonViewRecord(null)}
        data={jsonViewRecord}
        title="Tenant JSON"
      />
    </PageContainer>
  );
};

export default TenantPage;
