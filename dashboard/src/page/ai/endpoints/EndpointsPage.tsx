'use client';
import React, { useMemo, useState } from 'react';
import { useTranslations } from 'next-intl';
import { useParams } from 'next/navigation';
import { PlusSquareOutlined } from '@ant-design/icons';
import toast from 'react-hot-toast';
import PageContainer from '@/components/base/pageContainer/PageContainer';
import LitegraphButton from '@/components/base/button/Button';
import LitegraphTable from '@/components/base/table/Table';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';
import FallBack from '@/components/base/fallback/FallBack';
import ConfirmationModal from '@/components/confirmation-modal/ConfirmationModal';
import { ChatEndpoint, ChatEndpointHealth } from '@/lib/sdk/chat';
import {
  useDeleteChatEndpointMutation,
  useListChatEndpointHealthQuery,
  useListChatEndpointsQuery,
  useUpdateChatEndpointMutation,
} from '@/lib/store/slice/slice';
import { globalToastId } from '@/constants/config';
import { endpointTableColumns } from './constant';
import AddEditEndpoint from './components/AddEditEndpoint';
import TestEndpointModal from './components/TestEndpointModal';
import HealthDetailModal from './components/HealthDetailModal';

const HEALTH_POLL_INTERVAL_MS = 15000;

const EndpointsPage = () => {
  const t = useTranslations('ai.endpoints');
  const tCommon = useTranslations('common');
  const params = useParams();
  const tenantGuid = (params?.tenantId as string) || '';

  const {
    data: endpointsEnvelope,
    isLoading,
    isFetching,
    error,
    refetch,
  } = useListChatEndpointsQuery({ tenantGuid }, { skip: !tenantGuid });
  const endpoints = useMemo(() => endpointsEnvelope?.Objects ?? [], [endpointsEnvelope]);
  const { data: healthEnvelope } = useListChatEndpointHealthQuery(
    { tenantGuid },
    { skip: !tenantGuid, pollingInterval: HEALTH_POLL_INTERVAL_MS }
  );
  const healthList = useMemo(() => healthEnvelope?.Objects ?? [], [healthEnvelope]);
  const [updateEndpoint] = useUpdateChatEndpointMutation();
  const [deleteEndpoint, { isLoading: isDeleting }] = useDeleteChatEndpointMutation();

  const [isAddEditVisible, setIsAddEditVisible] = useState(false);
  const [selectedEndpoint, setSelectedEndpoint] = useState<ChatEndpoint | null>(null);
  const [testTarget, setTestTarget] = useState<ChatEndpoint | null>(null);
  const [healthTarget, setHealthTarget] = useState<ChatEndpoint | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<ChatEndpoint | null>(null);

  const healthByGuid = useMemo(() => {
    const map: Record<string, ChatEndpointHealth> = {};
    for (const health of healthList) map[health.EndpointGUID] = health;
    return map;
  }, [healthList]);

  const handleToggleActive = async (endpoint: ChatEndpoint) => {
    const { error: toggleError } = await updateEndpoint({
      tenantGuid,
      endpoint: { ...endpoint, Active: !endpoint.Active },
    });
    if (toggleError) {
      toast.error(t('toast.updateFailed'), { id: globalToastId });
      return;
    }
    toast.success(endpoint.Active ? t('toast.deactivated') : t('toast.activated'), {
      id: globalToastId,
    });
  };

  const handleDelete = async () => {
    if (!deleteTarget) return;
    const { error: deleteError } = await deleteEndpoint({
      tenantGuid,
      endpointGuid: deleteTarget.GUID,
    });
    if (deleteError) {
      toast.error(t('toast.deleteFailed'), { id: globalToastId });
      return;
    }
    toast.success(t('toast.deleted'), { id: globalToastId });
    setDeleteTarget(null);
  };

  const isTableLoading = isLoading || isFetching;

  return (
    <PageContainer
      id="ai-endpoints"
      pageTitle={t('title')}
      pageTitleRightContent={
        <LitegraphTooltip title={t('createTooltip')}>
          <LitegraphButton
            type="link"
            icon={<PlusSquareOutlined />}
            weight={500}
            onClick={() => {
              setSelectedEndpoint(null);
              setIsAddEditVisible(true);
            }}
            data-testid="endpoint-create"
          >
            {t('createEndpoint')}
          </LitegraphButton>
        </LitegraphTooltip>
      }
    >
      {error && !isTableLoading ? (
        <FallBack retry={refetch}>{tCommon('states.somethingWentWrong')}</FallBack>
      ) : (
        <LitegraphTable
          loading={isTableLoading}
          columns={endpointTableColumns(
            t,
            healthByGuid,
            (endpoint) => {
              setSelectedEndpoint(endpoint);
              setIsAddEditVisible(true);
            },
            (endpoint) => setTestTarget(endpoint),
            (endpoint) => setHealthTarget(endpoint),
            handleToggleActive,
            (endpoint) => setDeleteTarget(endpoint)
          )}
          dataSource={endpoints}
          rowKey="GUID"
          onRowClick={(endpoint: ChatEndpoint) => {
            setSelectedEndpoint(endpoint);
            setIsAddEditVisible(true);
          }}
          onRefresh={refetch}
          isRefreshing={isTableLoading}
        />
      )}

      {isAddEditVisible && (
        <AddEditEndpoint
          tenantGuid={tenantGuid}
          endpoint={selectedEndpoint}
          onClose={() => {
            setIsAddEditVisible(false);
            setSelectedEndpoint(null);
          }}
        />
      )}

      {testTarget && (
        <TestEndpointModal
          tenantGuid={tenantGuid}
          endpoint={testTarget}
          onClose={() => setTestTarget(null)}
        />
      )}

      {healthTarget && (
        <HealthDetailModal
          endpoint={healthTarget}
          health={healthByGuid[healthTarget.GUID]}
          onClose={() => setHealthTarget(null)}
        />
      )}

      <ConfirmationModal
        open={!!deleteTarget}
        title={t('deleteModal.title', { name: deleteTarget?.Name || '' })}
        content={t('deleteModal.body')}
        onCancel={() => setDeleteTarget(null)}
        onConfirm={handleDelete}
        loading={isDeleting}
      />
    </PageContainer>
  );
};

export default EndpointsPage;
