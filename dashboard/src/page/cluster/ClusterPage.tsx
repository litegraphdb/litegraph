'use client';
import React, { useMemo, useState } from 'react';
import { useTranslations } from 'next-intl';
import { Alert, Card, Select, Table, Tag } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import {
  ClusterOutlined,
  DeleteOutlined,
  PoweroffOutlined,
  ReloadOutlined,
  SyncOutlined,
} from '@ant-design/icons';
import toast from 'react-hot-toast';
import PageContainer from '@/components/base/pageContainer/PageContainer';
import PageLoading from '@/components/base/loading/PageLoading';
import FallBack from '@/components/base/fallback/FallBack';
import LitegraphButton from '@/components/base/button/Button';
import LitegraphFlex from '@/components/base/flex/Flex';
import LitegraphText from '@/components/base/typograpghy/Text';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';
import CopyButton from '@/components/base/copy-button/CopyButton';
import ConfirmationModal from '@/components/confirmation-modal/ConfirmationModal';
import { globalToastId } from '@/constants/config';
import {
  useDeleteClusterNodeMutation,
  useGetClusterNodesQuery,
  useRestartClusterMutation,
  useRestartClusterNodeMutation,
} from '@/lib/store/slice/slice';
import { ClusterNode, ClusterStatus } from '@/lib/sdk/cluster';

/** Auto-refresh interval for the node list. */
export const CLUSTER_REFRESH_MS = 5000;

/** How long after a restart request the progress panel reappears on its own, for example after a page reload. */
const PROGRESS_WINDOW_MS = 30 * 60 * 1000;

type NodeState = ClusterNode['State'];
type ProgressState = 'pending' | 'restarting' | 'restarted';
type PendingAction =
  | { kind: 'rolling' }
  | { kind: 'restart'; nodeId: string }
  | { kind: 'remove'; nodeId: string };

const STATES: NodeState[] = ['Healthy', 'Degraded', 'Unavailable', 'Draining', 'Restarting', 'Stopped', 'Offline'];

/** Tag color for a node state; matches the Settings page node card. */
export const stateColor = (state: NodeState): string => {
  switch (state) {
    case 'Healthy':
      return 'green';
    case 'Degraded':
    case 'Draining':
    case 'Restarting':
      return 'orange';
    case 'Stopped':
      return 'default';
    default:
      return 'red';
  }
};

const isDown = (state: NodeState): boolean => state === 'Offline' || state === 'Stopped';

/** Where a node is in a restart requested at `requestedUtc`. */
export const progressFor = (node: ClusterNode, requestedUtc: string): ProgressState => {
  const requested = new Date(requestedUtc).getTime();
  if (new Date(node.StartedUtc).getTime() > requested && node.State === 'Healthy') return 'restarted';
  if (node.State === 'Restarting' || node.State === 'Draining' || node.State === 'Offline') return 'restarting';
  return 'pending';
};

const errorMessage = (error: unknown): string => {
  if (error && typeof error === 'object' && 'message' in error) return String((error as Error).message);
  return String(error ?? '');
};

const formatTime = (value?: string | null): string => (value ? new Date(value).toLocaleString() : '');

const ClusterPage = () => {
  const t = useTranslations('cluster');
  const [stateFilter, setStateFilter] = useState<NodeState | 'all'>('all');
  const [pendingAction, setPendingAction] = useState<PendingAction | null>(null);
  const [requestedUtc, setRequestedUtc] = useState<string | null>(null);
  const [dismissedUtc, setDismissedUtc] = useState<string | null>(null);

  // Auto-refresh pauses while a confirmation is open so the table does not shift under the user.
  const {
    data: cluster,
    error,
    isLoading,
    isFetching,
    refetch,
    fulfilledTimeStamp,
  } = useGetClusterNodesQuery(undefined, {
    pollingInterval: pendingAction ? 0 : CLUSTER_REFRESH_MS,
  });
  const [restartCluster, { isLoading: isRollingLoading }] = useRestartClusterMutation();
  const [restartNode, { isLoading: isRestartLoading }] = useRestartClusterNodeMutation();
  const [removeNode, { isLoading: isRemoveLoading }] = useDeleteClusterNodeMutation();

  const isCluster = Boolean(cluster?.ClusterEnabled);
  const nodes = useMemo(() => cluster?.Nodes ?? [], [cluster]);
  const filteredNodes = useMemo(
    () => (stateFilter === 'all' ? nodes : nodes.filter((n) => n.State === stateFilter)),
    [nodes, stateFilter]
  );

  // A restart requested from this page, or a recent one requested elsewhere that has not finished, drives the
  // progress panel.  It is derived from polling, so it survives this page losing its connection or being reloaded.
  const progressSince = useMemo(() => {
    if (requestedUtc) return requestedUtc;
    const recent = cluster?.RestartRequestedUtc;
    if (!recent || recent === dismissedUtc) return null;
    if (Date.now() - new Date(recent).getTime() > PROGRESS_WINDOW_MS) return null;
    return nodes.some((n) => progressFor(n, recent) !== 'restarted') ? recent : null;
  }, [requestedUtc, dismissedUtc, cluster, nodes]);

  const summary = useMemo(() => summarize(cluster), [cluster]);

  const confirmAction = async () => {
    const action = pendingAction;
    if (!action) return;
    if (action.kind === 'rolling') {
      const { data, error: actionError } = await restartCluster();
      setPendingAction(null);
      if (actionError) {
        toast.error(t('toast.actionFailed', { message: errorMessage(actionError) }), { id: globalToastId });
        return;
      }
      setRequestedUtc(data?.RequestedUtc ?? new Date().toISOString());
      toast.success(t('toast.rollingRequested'), { id: globalToastId });
    } else if (action.kind === 'restart') {
      const { error: actionError } = await restartNode(action.nodeId);
      setPendingAction(null);
      if (actionError) {
        toast.error(t('toast.actionFailed', { message: errorMessage(actionError) }), { id: globalToastId });
        return;
      }
      toast.success(t('toast.restartRequested', { node: action.nodeId }), { id: globalToastId });
    } else {
      const { error: actionError } = await removeNode(action.nodeId);
      setPendingAction(null);
      if (actionError) {
        toast.error(t('toast.actionFailed', { message: errorMessage(actionError) }), { id: globalToastId });
        return;
      }
      toast.success(t('toast.removed', { node: action.nodeId }), { id: globalToastId });
    }
    refetch();
  };

  const dismissProgress = () => {
    setDismissedUtc(progressSince);
    setRequestedUtc(null);
  };

  const columns: ColumnsType<ClusterNode> = [
    {
      title: t('columns.node'),
      dataIndex: 'NodeId',
      key: 'NodeId',
      sorter: (a, b) => a.NodeId.localeCompare(b.NodeId),
      defaultSortOrder: 'ascend',
      render: (nodeId: string) => (
        <LitegraphFlex align="center" gap={4}>
          <span>{nodeId}</span>
          <CopyButton text={nodeId} tooltipTitle={t('actions.copyNodeId')} />
        </LitegraphFlex>
      ),
    },
    { title: t('columns.host'), dataIndex: 'Hostname', key: 'Hostname' },
    { title: t('columns.version'), dataIndex: 'Version', key: 'Version' },
    {
      title: t('columns.state'),
      dataIndex: 'State',
      key: 'State',
      sorter: (a, b) => STATES.indexOf(a.State) - STATES.indexOf(b.State),
      render: (state: NodeState) => <Tag color={stateColor(state)}>{state}</Tag>,
    },
    {
      title: t('columns.started'),
      dataIndex: 'StartedUtc',
      key: 'StartedUtc',
      sorter: (a, b) => new Date(a.StartedUtc).getTime() - new Date(b.StartedUtc).getTime(),
      render: (value: string) => formatTime(value),
    },
    {
      title: t('columns.heartbeat'),
      dataIndex: 'HeartbeatAgeMs',
      key: 'HeartbeatAgeMs',
      sorter: (a, b) => (a.HeartbeatAgeMs ?? 0) - (b.HeartbeatAgeMs ?? 0),
      render: (value?: number | null) =>
        value === undefined || value === null ? '' : t('secondsAgo', { seconds: Math.round(value / 1000) }),
    },
    {
      title: t('columns.checks'),
      key: 'Checks',
      render: (_: unknown, node: ClusterNode) => (
        <LitegraphFlex gap={4} wrap="wrap">
          <Tag color={node.Checks?.Database ? 'green' : 'red'}>{t('checks.database')}</Tag>
          {node.Checks?.Clutch !== null && node.Checks?.Clutch !== undefined && (
            <Tag color={node.Checks.Clutch ? 'green' : 'red'}>{t('checks.clutch')}</Tag>
          )}
          {node.Checks?.Redis !== null && node.Checks?.Redis !== undefined && (
            <Tag color={node.Checks.Redis ? 'green' : 'red'}>{t('checks.redis')}</Tag>
          )}
        </LitegraphFlex>
      ),
    },
    {
      title: t('columns.settingsVersion'),
      dataIndex: 'SettingsVersion',
      key: 'SettingsVersion',
      sorter: (a, b) => a.SettingsVersion - b.SettingsVersion,
    },
    {
      title: t('columns.restartPending'),
      dataIndex: 'RestartPending',
      key: 'RestartPending',
      render: (value: boolean) =>
        value ? <Tag color="orange">{t('yes')}</Tag> : <Tag>{t('no')}</Tag>,
    },
    {
      title: t('columns.actions'),
      key: 'actions',
      render: (_: unknown, node: ClusterNode) => (
        <LitegraphFlex gap={4}>
          <LitegraphTooltip title={t('actions.restartNode')}>
            <LitegraphButton
              size="small"
              icon={<PoweroffOutlined />}
              disabled={isDown(node.State) || (isCluster && cluster?.RegistryAvailable === false)}
              onClick={() => setPendingAction({ kind: 'restart', nodeId: node.NodeId })}
              aria-label={t('actions.restartNode')}
              data-testid={`cluster-restart-${node.NodeId}`}
            />
          </LitegraphTooltip>
          {isCluster && (
            <LitegraphTooltip title={t('actions.remove')}>
              <LitegraphButton
                size="small"
                danger
                icon={<DeleteOutlined />}
                disabled={!isDown(node.State)}
                onClick={() => setPendingAction({ kind: 'remove', nodeId: node.NodeId })}
                aria-label={t('actions.remove')}
                data-testid={`cluster-remove-${node.NodeId}`}
              />
            </LitegraphTooltip>
          )}
        </LitegraphFlex>
      ),
    },
  ];

  const headerActions = (
    <LitegraphFlex gap={8} align="center" wrap="wrap">
      {error && cluster ? (
        <Tag icon={<SyncOutlined spin />} color="warning" data-testid="cluster-reconnecting">
          {t('reconnecting')}
        </Tag>
      ) : (
        <LitegraphText fontSize={12} style={{ color: 'var(--ant-color-text-tertiary)' }} data-testid="cluster-refresh-interval">
          {pendingAction ? t('autoRefreshPaused') : t('autoRefresh', { seconds: CLUSTER_REFRESH_MS / 1000 })}
          {fulfilledTimeStamp ? ` · ${t('lastUpdated', { time: new Date(fulfilledTimeStamp).toLocaleTimeString() })}` : ''}
        </LitegraphText>
      )}
      <LitegraphButton icon={<ReloadOutlined spin={isFetching} />} onClick={() => refetch()} data-testid="cluster-refresh">
        {t('refresh')}
      </LitegraphButton>
      {isCluster && (
        <LitegraphButton
          type="primary"
          danger
          icon={<ReloadOutlined />}
          disabled={cluster?.RegistryAvailable === false}
          loading={isRollingLoading}
          onClick={() => setPendingAction({ kind: 'rolling' })}
          data-testid="cluster-rolling-restart"
        >
          {t('actions.rollingRestart')}
        </LitegraphButton>
      )}
    </LitegraphFlex>
  );

  if (isLoading && !cluster) return <PageLoading message={t('loading')} />;

  if (!cluster) {
    return (
      <PageContainer pageTitle={t('title')}>
        <FallBack retry={refetch}>{t('loadError')}</FallBack>
      </PageContainer>
    );
  }

  const modalTitle =
    pendingAction?.kind === 'rolling'
      ? t('rollingModal.title')
      : pendingAction?.kind === 'restart'
        ? t('restartNodeModal.title', { node: pendingAction.nodeId })
        : pendingAction?.kind === 'remove'
          ? t('removeModal.title', { node: pendingAction.nodeId })
          : '';
  const modalBody =
    pendingAction?.kind === 'rolling'
      ? t('rollingModal.body')
      : pendingAction?.kind === 'restart'
        ? isCluster
          ? t('restartNodeModal.body')
          : t('restartNodeModal.bodySingle')
        : pendingAction?.kind === 'remove'
          ? t('removeModal.body')
          : '';

  return (
    <PageContainer pageTitle={t('title')} pageTitleRightContent={headerActions}>
      <LitegraphText fontSize={13} style={{ display: 'block', marginBottom: 16, color: 'var(--ant-color-text-secondary)' }}>
        {t('subtitle')}
      </LitegraphText>

      {!isCluster && (
        <Alert type="info" showIcon style={{ marginBottom: 16 }} message={t('singleNodeDescription')} data-testid="cluster-single-node" />
      )}
      {isCluster && cluster.RegistryAvailable === false && (
        <Alert type="warning" showIcon style={{ marginBottom: 16 }} message={t('registryUnavailable')} data-testid="cluster-registry-unavailable" />
      )}

      <LitegraphFlex gap={16} wrap="wrap" style={{ marginBottom: 16 }}>
        <Card size="small" style={{ minWidth: 180, flex: '1 1 180px' }} data-testid="cluster-card-nodes">
          <LitegraphText fontSize={12} style={{ display: 'block', color: 'var(--ant-color-text-secondary)' }}>
            {t('cards.nodes')}
          </LitegraphText>
          <LitegraphText fontSize={22} weight={600}>
            {summary.healthy} / {summary.total}
          </LitegraphText>
        </Card>
        <Card size="small" style={{ minWidth: 180, flex: '1 1 180px' }} data-testid="cluster-card-name">
          <LitegraphText fontSize={12} style={{ display: 'block', color: 'var(--ant-color-text-secondary)' }}>
            {t('cards.cluster')}
          </LitegraphText>
          {isCluster ? (
            <LitegraphFlex align="center" gap={8}>
              <ClusterOutlined />
              <LitegraphText fontSize={18} weight={600}>
                {cluster.ClusterName}
              </LitegraphText>
            </LitegraphFlex>
          ) : (
            <Tag>{t('singleNode')}</Tag>
          )}
        </Card>
        <Card size="small" style={{ minWidth: 180, flex: '1 1 180px' }} data-testid="cluster-card-registry">
          <LitegraphText fontSize={12} style={{ display: 'block', color: 'var(--ant-color-text-secondary)' }}>
            {t('cards.registry')}
          </LitegraphText>
          {!isCluster ? (
            <Tag>{t('cards.registryNa')}</Tag>
          ) : cluster.RegistryAvailable ? (
            <Tag color="green">{t('cards.registryUp')}</Tag>
          ) : (
            <Tag color="red">{t('cards.registryDown')}</Tag>
          )}
        </Card>
        <Card size="small" style={{ minWidth: 180, flex: '1 1 180px' }} data-testid="cluster-card-settings">
          <LitegraphText fontSize={12} style={{ display: 'block', color: 'var(--ant-color-text-secondary)' }}>
            {t('cards.settings')}
          </LitegraphText>
          <LitegraphText fontSize={22} weight={600} style={{ display: 'block' }}>
            {cluster.SettingsVersion}
          </LitegraphText>
          <LitegraphText fontSize={12} style={{ color: 'var(--ant-color-text-tertiary)' }}>
            {t('cards.settingsLagging', { count: summary.lagging, pending: summary.pending })}
          </LitegraphText>
        </Card>
        <Card size="small" style={{ minWidth: 180, flex: '1 1 180px' }} data-testid="cluster-card-restart">
          <LitegraphText fontSize={12} style={{ display: 'block', color: 'var(--ant-color-text-secondary)' }}>
            {t('cards.lastRestart')}
          </LitegraphText>
          <LitegraphText fontSize={14}>
            {cluster.RestartRequestedUtc ? formatTime(cluster.RestartRequestedUtc) : t('cards.never')}
          </LitegraphText>
        </Card>
      </LitegraphFlex>

      {progressSince && (
        <Card
          size="small"
          style={{ marginBottom: 16 }}
          title={t('progress.title')}
          extra={
            <LitegraphButton size="small" onClick={dismissProgress} data-testid="cluster-progress-dismiss">
              {t('progress.dismiss')}
            </LitegraphButton>
          }
          data-testid="cluster-progress"
        >
          <LitegraphText fontSize={12} style={{ display: 'block', marginBottom: 8, color: 'var(--ant-color-text-tertiary)' }}>
            {t('progress.requested', { time: formatTime(progressSince) })}
          </LitegraphText>
          <LitegraphFlex gap={8} wrap="wrap">
            {nodes.map((node) => {
              const progress = progressFor(node, progressSince);
              const color = progress === 'restarted' ? 'green' : progress === 'restarting' ? 'orange' : 'default';
              return (
                <Tag key={node.NodeId} color={color} data-testid={`cluster-progress-${node.NodeId}`}>
                  {node.NodeId}: {t(`progress.${progress}`)}
                </Tag>
              );
            })}
          </LitegraphFlex>
          {nodes.length > 0 && nodes.every((n) => progressFor(n, progressSince) === 'restarted') && (
            <LitegraphText fontSize={13} style={{ display: 'block', marginTop: 8 }} data-testid="cluster-progress-complete">
              {t('progress.complete')}
            </LitegraphText>
          )}
        </Card>
      )}

      <LitegraphFlex align="center" gap={8} style={{ marginBottom: 8 }} wrap="wrap">
        <LitegraphText fontSize={13}>{t('filter.state')}</LitegraphText>
        <Select
          value={stateFilter}
          onChange={(value) => setStateFilter(value)}
          style={{ minWidth: 160 }}
          aria-label={t('filter.state')}
          data-testid="cluster-state-filter"
          options={[
            { value: 'all', label: t('filter.all') },
            ...STATES.map((state) => ({ value: state, label: state })),
          ]}
        />
        <LitegraphText fontSize={12} style={{ color: 'var(--ant-color-text-tertiary)' }}>
          {t('filter.localNote', { shown: filteredNodes.length, total: nodes.length })}
        </LitegraphText>
      </LitegraphFlex>

      <Table<ClusterNode>
        size="small"
        rowKey="NodeId"
        columns={columns}
        dataSource={filteredNodes}
        pagination={false}
        scroll={{ x: 'max-content' }}
        locale={{ emptyText: t('empty') }}
        data-testid="cluster-nodes-table"
      />

      <ConfirmationModal
        open={pendingAction !== null}
        title={modalTitle}
        content={modalBody}
        onCancel={() => setPendingAction(null)}
        onConfirm={confirmAction}
        loading={isRollingLoading || isRestartLoading || isRemoveLoading}
      />
    </PageContainer>
  );
};

/** Counts for the summary cards. */
export const summarize = (cluster?: ClusterStatus) => {
  const nodes = cluster?.Nodes ?? [];
  return {
    total: nodes.length,
    healthy: nodes.filter((n) => n.State === 'Healthy').length,
    lagging: nodes.filter((n) => !isDown(n.State) && n.SettingsVersion < (cluster?.SettingsVersion ?? 0)).length,
    pending: nodes.filter((n) => n.RestartPending).length,
  };
};

export default ClusterPage;
