'use client';
import React, { useState } from 'react';
import { useTranslations } from 'next-intl';
import { PlusSquareOutlined } from '@ant-design/icons';
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
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';
import ViewJsonModal from '@/components/base/view-json-modal/ViewJsonModal';

const TenantPage = () => {
  const t = useTranslations('tenants');
  const tCommon = useTranslations('common');
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
      pageTitle={t('title')}
      pageTitleRightContent={
        <LitegraphTooltip title={t('createTooltip')}>
          <LitegraphButton
            type="link"
            icon={<PlusSquareOutlined />}
            onClick={handleCreateTenant}
            weight={500}
          >
            {t('createTenant')}
          </LitegraphButton>
        </LitegraphTooltip>
      }
    >
      {error && !isTenantsLoading ? (
        <FallBack retry={fetchTenantsList}>{tCommon('states.somethingWentWrong')}</FallBack>
      ) : (
        <LitegraphTable
          hideHorizontalScroll
          loading={isTenantsLoading}
          columns={tableColumns(t, handleEditTenant, handleDeleteTenant, setJsonViewRecord)}
          dataSource={tenantsList}
          rowKey={'GUID'}
          onRowClick={handleEditTenant}
          onRefresh={fetchTenantsList}
          isRefreshing={isTenantsLoading}
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
          title={t('deleteTitle', { name: selectedTenant.Name })}
          paragraphText={t('deleteBody')}
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
        title={t('tenantJson')}
      />
    </PageContainer>
  );
};

export default TenantPage;
