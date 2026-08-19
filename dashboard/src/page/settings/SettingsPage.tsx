'use client';
import React, { useEffect, useMemo, useState } from 'react';
import { useTranslations } from 'next-intl';
import { Card, Input, InputNumber, Switch, Tag } from 'antd';
import { ReloadOutlined } from '@ant-design/icons';
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
import {
  useGetServerSettingsQuery,
  useRestartServerMutation,
  useUpdateServerSettingsMutation,
} from '@/lib/store/slice/slice';
import { useValidateConnectivity } from '@/lib/sdk/litegraph.service';
import { SettingsUpdateResult } from '@/lib/sdk/settings';
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
  };

  const handleReset = () => {
    if (settings) setDraft(JSON.parse(JSON.stringify(settings)));
  };

  const handleConfirmRestart = async () => {
    setIsRestartModalOpen(false);
    const { error: restartError } = await restart();
    if (restartError) {
      toast.error(t('toast.restartFailed'), { id: globalToastId });
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
        {t('actions.restart')}
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
      <LitegraphFlex vertical gap={16}>
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
      </LitegraphFlex>

      <ConfirmationModal
        open={isRestartModalOpen}
        title={t('restartModal.title')}
        content={t('restartModal.body')}
        onCancel={() => setIsRestartModalOpen(false)}
        onConfirm={handleConfirmRestart}
        loading={isRestarting}
      />
    </PageContainer>
  );
};

export default SettingsPage;
