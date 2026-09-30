'use client';
import React, { useEffect, useMemo, useState } from 'react';
import { useTranslations } from 'next-intl';
import Link from 'next/link';
import { Card, Input, InputNumber, Switch, Table, Tag } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { AreaChartOutlined, ClusterOutlined, ExportOutlined, ReloadOutlined } from '@ant-design/icons';
import toast from 'react-hot-toast';
import PageContainer from '@/components/base/pageContainer/PageContainer';
import PageLoading from '@/components/base/loading/PageLoading';
import FallBack from '@/components/base/fallback/FallBack';
import LitegraphButton from '@/components/base/button/Button';
import LitegraphFlex from '@/components/base/flex/Flex';
import LitegraphText from '@/components/base/typograpghy/Text';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';
import ConfirmationModal from '@/components/confirmation-modal/ConfirmationModal';
import { globalToastId } from '@/constants/config';
import { paths } from '@/constants/constant';
import {
  useGetClusterNodesQuery,
  useGetServerSettingsQuery,
  useRestartServerMutation,
  useUpdateServerSettingsMutation,
} from '@/lib/store/slice/slice';
import { useValidateConnectivity } from '@/lib/sdk/litegraph.service';
import { ClusterNode, SettingsUpdateResult } from '@/lib/sdk/settings';
import { SETTINGS_SCHEMA, SettingField, getPath, setPath } from './schema';

const SettingsPage = () => {
  const t = useTranslations('settings');
  const {
    data: settings,
    isLoading,
    isFetching,
    error,
    refetch,
  } = useGetServerSettingsQuery();
  const [updateSettings, { isLoading: isSaving }] = useUpdateServerSettingsMutation();
  const [restart, { isLoading: isRestarting }] = useRestartServerMutation();
  const { validateConnectivity } = useValidateConnectivity();
  // Node list refreshes every 5 seconds so a rolling restart can be followed node by node.
  const { data: cluster, refetch: refetchCluster } = useGetClusterNodesQuery(undefined, {
    pollingInterval: 5000,
  });
  const isCluster = Boolean(cluster?.ClusterEnabled);

  const [draft, setDraft] = useState<Record<string, any> | null>(null);
  const [lastResult, setLastResult] = useState<SettingsUpdateResult | null>(null);
  const [isRestartModalOpen, setIsRestartModalOpen] = useState(false);
  const [isReconnecting, setIsReconnecting] = useState(false);

  useEffect(() => {
    if (settings) {
      setDraft(JSON.parse(JSON.stringify(settings)));
      setLastResult(null);
    }
  }, [settings]);

  const isDirty = useMemo(() => {
    if (!settings || !draft) return false;
    return JSON.stringify(settings) !== JSON.stringify(draft);
  }, [settings, draft]);

  // Default Grafana location for the bundled docker compose stack: same host as
  // the dashboard, on Grafana's default port. Falls back to localhost during SSR.
  const grafanaUrl = useMemo(() => {
    if (typeof window === 'undefined') return 'http://localhost:3000';
    return `${window.location.protocol}//${window.location.hostname}:3000`;
  }, []);

  const handleFieldChange = (field: SettingField, value: any) => {
    setDraft((prev) => (prev ? setPath(prev, field.path, value) : prev));
  };

  const handleSave = async () => {
    if (!draft) return;
    const { data, error: saveError } = await updateSettings(draft);
    if (saveError || !data) {
      toast.error(t('toast.saveFailed'), { id: globalToastId });
      return;
    }
    setLastResult(data);
    toast.success(t('toast.saved'), { id: globalToastId });
    refetch();
    refetchCluster();
  };

  const handleReset = () => {
    if (settings) setDraft(JSON.parse(JSON.stringify(settings)));
  };

  const handleConfirmRestart = async () => {
    setIsRestartModalOpen(false);
    const { data: restartResult, error: restartError } = await restart();
    if (restartError) {
      toast.error(t('toast.restartFailed'), { id: globalToastId });
      return;
    }
    if (restartResult?.Rolling) {
      // The node serving this page keeps serving until its own turn, and the load balancer routes around it,
      // so there is nothing to reconnect to: follow progress in the node list instead.
      toast.success(t('toast.rollingRestart'), { id: globalToastId });
      refetchCluster();
      return;
    }
    setIsReconnecting(true);
    // Poll until the server comes back up, then recover.
    const start = Date.now();
    const poll = async () => {
      const ok = await validateConnectivity();
      if (ok) {
        setIsReconnecting(false);
        toast.success(t('toast.reconnected'), { id: globalToastId });
        refetch();
        return;
      }
      if (Date.now() - start > 120000) {
        setIsReconnecting(false);
        toast.error(t('toast.reconnectTimeout'), { id: globalToastId });
        return;
      }
      setTimeout(poll, 3000);
    };
    setTimeout(poll, 3000);
  };

  const stateColor = (state: ClusterNode['State']): string => {
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

  const nodeColumns: ColumnsType<ClusterNode> = [
    { title: t('cluster.columns.node'), dataIndex: 'NodeId', key: 'NodeId' },
    {
      title: t('cluster.columns.state'),
      dataIndex: 'State',
      key: 'State',
      render: (state: ClusterNode['State']) => <Tag color={stateColor(state)}>{state}</Tag>,
    },
    { title: t('cluster.columns.version'), dataIndex: 'Version', key: 'Version' },
    {
      title: t('cluster.columns.started'),
      dataIndex: 'StartedUtc',
      key: 'StartedUtc',
      render: (value: string) => (value ? new Date(value).toLocaleString() : ''),
    },
    {
      title: t('cluster.columns.heartbeat'),
      dataIndex: 'HeartbeatAgeMs',
      key: 'HeartbeatAgeMs',
      render: (value?: number | null) =>
        value === undefined || value === null
          ? ''
          : t('cluster.secondsAgo', { seconds: Math.round(value / 1000) }),
    },
    { title: t('cluster.columns.settingsVersion'), dataIndex: 'SettingsVersion', key: 'SettingsVersion' },
    {
      title: t('cluster.columns.restartPending'),
      dataIndex: 'RestartPending',
      key: 'RestartPending',
      render: (value: boolean) =>
        value ? <Tag color="orange">{t('cluster.yes')}</Tag> : <Tag>{t('cluster.no')}</Tag>,
    },
  ];

  const renderSectionStatus = (sectionId: string, serverSection: string, applies: string) => {
    if (lastResult) {
      if (lastResult.AppliedLive?.includes(serverSection)) {
        return <Tag color="green">{t('status.appliedLive')}</Tag>;
      }
      if (lastResult.RestartRequired?.includes(serverSection)) {
        return <Tag color="orange">{t('status.restartRequired')}</Tag>;
      }
    }
    return applies === 'restart' ? (
      <LitegraphTooltip title={t('status.restartHintTooltip')}>
        <Tag color="orange">{t('status.restartHint')}</Tag>
      </LitegraphTooltip>
    ) : (
      <LitegraphTooltip title={t('status.liveHintTooltip')}>
        <Tag color="blue">{t('status.liveHint')}</Tag>
      </LitegraphTooltip>
    );
  };

  const renderField = (field: SettingField) => {
    const value = draft ? getPath(draft, field.path) : undefined;
    const label = t(`fields.${field.labelKey}` as any);
    if (field.type === 'boolean') {
      return (
        <LitegraphFlex key={field.path} align="center" justify="space-between" gap={12} style={{ marginBottom: 12 }}>
          <LitegraphText fontSize={13}>{label}</LitegraphText>
          <Switch
            checked={Boolean(value)}
            onChange={(checked) => handleFieldChange(field, checked)}
            aria-label={label}
          />
        </LitegraphFlex>
      );
    }
    return (
      <div key={field.path} style={{ marginBottom: 12 }}>
        <LitegraphText fontSize={13} className="mb-xs" style={{ display: 'block', marginBottom: 4 }}>
          {label}
        </LitegraphText>
        {field.type === 'number' ? (
          <InputNumber
            value={value ?? undefined}
            onChange={(val) => handleFieldChange(field, val)}
            style={{ width: '100%' }}
            aria-label={label}
          />
        ) : field.type === 'password' ? (
          <Input.Password
            value={value ?? ''}
            onChange={(e) => handleFieldChange(field, e.target.value)}
            autoComplete="new-password"
            aria-label={label}
          />
        ) : (
          <Input
            value={value ?? ''}
            onChange={(e) => handleFieldChange(field, e.target.value)}
            aria-label={label}
          />
        )}
      </div>
    );
  };

  const headerActions = (
    <LitegraphFlex gap={8} align="center">
      {isDirty && (
        <LitegraphButton onClick={handleReset} disabled={isSaving} data-testid="settings-reset">
          {t('actions.discard')}
        </LitegraphButton>
      )}
      <LitegraphButton
        type="primary"
        onClick={handleSave}
        disabled={!isDirty || isSaving}
        loading={isSaving}
        data-testid="settings-save"
      >
        {t('actions.save')}
      </LitegraphButton>
      <LitegraphButton
        danger
        icon={<ReloadOutlined />}
        onClick={() => setIsRestartModalOpen(true)}
        loading={isRestarting}
        data-testid="settings-restart"
      >
        {isCluster ? t('actions.restartCluster') : t('actions.restart')}
      </LitegraphButton>
    </LitegraphFlex>
  );

  if (isLoading || (isFetching && !settings)) {
    return <PageLoading message={t('loading')} />;
  }

  if (error || !draft) {
    return (
      <PageContainer pageTitle={t('title')}>
        <FallBack retry={refetch}>{t('loadError')}</FallBack>
      </PageContainer>
    );
  }

  return (
    <PageContainer pageTitle={t('title')} pageTitleRightContent={headerActions}>
      {isReconnecting && (
        <Card
          size="small"
          style={{ marginBottom: 16, borderColor: 'var(--ant-color-warning)' }}
          data-testid="settings-reconnecting"
        >
          <LitegraphFlex align="center" gap={10}>
            <ReloadOutlined spin />
            <LitegraphText>{t('reconnecting')}</LitegraphText>
          </LitegraphFlex>
        </Card>
      )}
      <LitegraphText fontSize={13} className="ant-color-text-secondary" style={{ display: 'block', marginBottom: 16 }}>
        {t('subtitle')}
      </LitegraphText>
      {lastResult?.EnvironmentOverrides && lastResult.EnvironmentOverrides.length > 0 && (
        <LitegraphTooltip title={lastResult.EnvironmentOverrides.join(', ')}>
          <LitegraphText
            fontSize={12}
            style={{ display: 'block', marginBottom: 16, color: 'var(--ant-color-text-tertiary)' }}
            data-testid="settings-environment-overrides"
          >
            {t('environmentOverrides', { count: lastResult.EnvironmentOverrides.length })}
          </LitegraphText>
        </LitegraphTooltip>
      )}
      <LitegraphFlex vertical gap={16}>
        {cluster && (
          <Card
            size="small"
            title={
              <LitegraphFlex align="center" gap={8}>
                <ClusterOutlined />
                <span>{t('cluster.title')}</span>
              </LitegraphFlex>
            }
            data-testid="settings-cluster-card"
          >
            <LitegraphText fontSize={13} style={{ display: 'block', marginBottom: 12 }}>
              {isCluster
                ? t('cluster.descriptionCluster', { name: cluster.ClusterName ?? '' })
                : t('cluster.descriptionSingle')}
            </LitegraphText>
            {isCluster && cluster.RegistryAvailable === false && (
              <LitegraphText
                fontSize={12}
                style={{ display: 'block', marginBottom: 12, color: 'var(--ant-color-warning)' }}
                data-testid="settings-cluster-registry-unavailable"
              >
                {t('cluster.registryUnavailable')}
              </LitegraphText>
            )}
            <Table<ClusterNode>
              size="small"
              rowKey="NodeId"
              columns={nodeColumns}
              dataSource={cluster.Nodes}
              pagination={false}
              data-testid="settings-cluster-nodes"
            />
            <Link
              href={paths.cluster}
              style={{ display: 'inline-block', marginTop: 12 }}
              data-testid="settings-cluster-link"
            >
              {t('cluster.openPage')}
            </Link>
          </Card>
        )}

        {SETTINGS_SCHEMA.map((section) => (
          <Card
            key={section.id}
            size="small"
            title={
              <LitegraphFlex align="center" justify="space-between" gap={8}>
                <span>{t(`sections.${section.titleKey}` as any)}</span>
                {renderSectionStatus(section.id, section.serverSection, section.applies)}
              </LitegraphFlex>
            }
            data-testid={`settings-section-${section.id}`}
          >
            {section.fields.map((field) => renderField(field))}
          </Card>
        ))}

        <Card
          size="small"
          title={
            <LitegraphFlex align="center" gap={8}>
              <AreaChartOutlined />
              <span>{t('grafana.title')}</span>
            </LitegraphFlex>
          }
          data-testid="settings-grafana-card"
        >
          <LitegraphText fontSize={13} style={{ display: 'block', marginBottom: 12 }}>
            {t('grafana.description')}
          </LitegraphText>
          <LitegraphFlex vertical gap={8} style={{ marginBottom: 16 }}>
            <LitegraphFlex align="center" gap={8} wrap="wrap">
              <LitegraphText fontSize={13} style={{ minWidth: 140, color: 'var(--ant-color-text-secondary)' }}>
                {t('grafana.urlLabel')}
              </LitegraphText>
              <a href={grafanaUrl} target="_blank" rel="noreferrer" data-testid="settings-grafana-url">
                {grafanaUrl}
              </a>
            </LitegraphFlex>
            <LitegraphFlex align="center" gap={8} wrap="wrap">
              <LitegraphText fontSize={13} style={{ minWidth: 140, color: 'var(--ant-color-text-secondary)' }}>
                {t('grafana.credentialsLabel')}
              </LitegraphText>
              <LitegraphText fontSize={13}>
                <code>{t('grafana.credentialsValue')}</code>
              </LitegraphText>
            </LitegraphFlex>
          </LitegraphFlex>
          <LitegraphText fontSize={12} style={{ display: 'block', marginBottom: 4, color: 'var(--ant-color-text-tertiary)' }}>
            {t('grafana.credentialsHint')}
          </LitegraphText>
          <LitegraphText fontSize={12} style={{ display: 'block', marginBottom: 16, color: 'var(--ant-color-text-tertiary)' }}>
            {t('grafana.urlHint')}
          </LitegraphText>
          <a href={grafanaUrl} target="_blank" rel="noreferrer">
            <LitegraphButton type="primary" icon={<ExportOutlined />} data-testid="settings-grafana-open">
              {t('grafana.open')}
            </LitegraphButton>
          </a>
        </Card>
      </LitegraphFlex>

      <ConfirmationModal
        open={isRestartModalOpen}
        title={isCluster ? t('restartModal.titleCluster') : t('restartModal.title')}
        content={isCluster ? t('restartModal.bodyCluster') : t('restartModal.body')}
        onCancel={() => setIsRestartModalOpen(false)}
        onConfirm={handleConfirmRestart}
        loading={isRestarting}
      />
    </PageContainer>
  );
};

export default SettingsPage;
