'use client';
import React, { useEffect, useMemo, useState } from 'react';
import { useTranslations } from 'next-intl';
import { useParams } from 'next/navigation';
import { Card, Input, InputNumber, Switch } from 'antd';
import toast from 'react-hot-toast';
import PageContainer from '@/components/base/pageContainer/PageContainer';
import PageLoading from '@/components/base/loading/PageLoading';
import FallBack from '@/components/base/fallback/FallBack';
import LitegraphButton from '@/components/base/button/Button';
import LitegraphFlex from '@/components/base/flex/Flex';
import LitegraphText from '@/components/base/typograpghy/Text';
import LitegraphSelect from '@/components/base/select/Select';
import { ChatSettings } from '@/lib/sdk/chat';
import {
  useGetChatSettingsQuery,
  useListChatEndpointsQuery,
  useUpdateChatSettingsMutation,
} from '@/lib/store/slice/slice';
import { globalToastId } from '@/constants/config';

/**
 * Shown (and saved on first edit) when the tenant has not set its own prompt;
 * the server always prepends its fixed tool/citation preamble regardless.
 */
export const DEFAULT_SYSTEM_PROMPT =
  'You are a helpful assistant for exploring this LiteGraph tenant. Use the available graph tools to look up real data before answering, cite node and edge identifiers when referencing specific objects, and say so plainly when the graph does not contain the answer.';

const ChatSettingsPage = () => {
  const t = useTranslations('ai.settings');
  const params = useParams();
  const tenantGuid = (params?.tenantId as string) || '';

  const {
    data: settings,
    isLoading,
    isFetching,
    error,
    refetch,
  } = useGetChatSettingsQuery({ tenantGuid }, { skip: !tenantGuid });
  const { data: endpoints = [], isLoading: isEndpointsLoading } = useListChatEndpointsQuery(
    { tenantGuid },
    { skip: !tenantGuid }
  );
  const [updateSettings, { isLoading: isSaving }] = useUpdateChatSettingsMutation();

  const [draft, setDraft] = useState<ChatSettings | null>(null);

  useEffect(() => {
    if (settings) setDraft(JSON.parse(JSON.stringify(settings)));
  }, [settings]);

  const isDirty = useMemo(() => {
    if (!settings || !draft) return false;
    return JSON.stringify(settings) !== JSON.stringify(draft);
  }, [settings, draft]);

  const completionOptions = endpoints
    .filter((endpoint) => endpoint.EndpointType === 'Completion')
    .map((endpoint) => ({ label: endpoint.Name, value: endpoint.GUID }));
  const embeddingOptions = endpoints
    .filter((endpoint) => endpoint.EndpointType === 'Embedding')
    .map((endpoint) => ({ label: endpoint.Name, value: endpoint.GUID }));

  const patchDraft = (patch: Partial<ChatSettings>) => {
    setDraft((prev) => (prev ? { ...prev, ...patch } : prev));
  };

  const handleSave = async () => {
    if (!draft) return;
    const { error: saveError } = await updateSettings({ tenantGuid, settings: draft });
    if (saveError) {
      toast.error(t('toast.saveFailed'), { id: globalToastId });
      return;
    }
    toast.success(t('toast.saved'), { id: globalToastId });
    refetch();
  };

  const handleDiscard = () => {
    if (settings) setDraft(JSON.parse(JSON.stringify(settings)));
  };

  const renderToggle = (
    label: string,
    tooltip: string,
    value: boolean,
    onChange: (checked: boolean) => void,
    testId: string
  ) => (
    <LitegraphFlex align="center" justify="space-between" gap={12} style={{ marginBottom: 12 }}>
      <LitegraphFlex vertical gap={0} style={{ minWidth: 0 }}>
        <LitegraphText fontSize={13}>{label}</LitegraphText>
        <LitegraphText fontSize={11.5} style={{ color: 'var(--ant-color-text-tertiary)' }}>
          {tooltip}
        </LitegraphText>
      </LitegraphFlex>
      <Switch checked={value} onChange={onChange} aria-label={label} data-testid={testId} />
    </LitegraphFlex>
  );

  const renderNumber = (
    label: string,
    hint: string,
    value: number,
    onChange: (val: number | null) => void,
    testId: string,
    min?: number,
    max?: number,
    step?: number
  ) => (
    <div style={{ marginBottom: 12 }}>
      <LitegraphText fontSize={13} style={{ display: 'block', marginBottom: 4 }}>
        {label}
      </LitegraphText>
      <InputNumber
        value={value}
        min={min}
        max={max}
        step={step}
        onChange={onChange}
        style={{ width: '100%' }}
        aria-label={label}
        data-testid={testId}
      />
      <LitegraphText
        fontSize={11.5}
        style={{ display: 'block', marginTop: 2, color: 'var(--ant-color-text-tertiary)' }}
      >
        {hint}
      </LitegraphText>
    </div>
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
    <PageContainer
      id="ai-settings"
      pageTitle={t('title')}
      pageTitleRightContent={
        <LitegraphFlex gap={8} align="center">
          {isDirty && (
            <LitegraphButton onClick={handleDiscard} disabled={isSaving} data-testid="chat-settings-discard">
              {t('actions.discard')}
            </LitegraphButton>
          )}
          <LitegraphButton
            type="primary"
            onClick={handleSave}
            disabled={!isDirty || isSaving}
            loading={isSaving}
            data-testid="chat-settings-save"
          >
            {t('actions.save')}
          </LitegraphButton>
        </LitegraphFlex>
      }
    >
      <LitegraphText
        fontSize={13}
        style={{ display: 'block', marginBottom: 16, color: 'var(--ant-color-text-secondary)' }}
      >
        {t('subtitle')}
      </LitegraphText>
      <LitegraphFlex vertical gap={16}>
        <Card size="small" title={t('sections.endpoints')} data-testid="chat-settings-endpoints">
          <div style={{ marginBottom: 12 }}>
            <LitegraphText fontSize={13} style={{ display: 'block', marginBottom: 4 }}>
              {t('fields.defaultCompletionEndpoint')}
            </LitegraphText>
            <LitegraphSelect
              allowClear
              loading={isEndpointsLoading}
              placeholder={t('fields.defaultCompletionEndpointPlaceholder')}
              value={draft.DefaultCompletionEndpointGUID || undefined}
              onChange={(value) =>
                patchDraft({ DefaultCompletionEndpointGUID: (value as string) || null })
              }
              options={completionOptions}
              style={{ width: '100%' }}
              data-testid="chat-settings-completion-endpoint"
            />
            <LitegraphText
              fontSize={11.5}
              style={{ display: 'block', marginTop: 2, color: 'var(--ant-color-text-tertiary)' }}
            >
              {t('fields.defaultCompletionEndpointHint')}
            </LitegraphText>
          </div>
          <div style={{ marginBottom: 4 }}>
            <LitegraphText fontSize={13} style={{ display: 'block', marginBottom: 4 }}>
              {t('fields.defaultEmbeddingEndpoint')}
            </LitegraphText>
            <LitegraphSelect
              allowClear
              loading={isEndpointsLoading}
              placeholder={t('fields.defaultEmbeddingEndpointPlaceholder')}
              value={draft.DefaultEmbeddingEndpointGUID || undefined}
              onChange={(value) =>
                patchDraft({ DefaultEmbeddingEndpointGUID: (value as string) || null })
              }
              options={embeddingOptions}
              style={{ width: '100%' }}
              data-testid="chat-settings-embedding-endpoint"
            />
            <LitegraphText
              fontSize={11.5}
              style={{ display: 'block', marginTop: 2, color: 'var(--ant-color-text-tertiary)' }}
            >
              {t('fields.defaultEmbeddingEndpointHint')}
            </LitegraphText>
          </div>
        </Card>

        <Card size="small" title={t('sections.prompt')} data-testid="chat-settings-prompt">
          <LitegraphText fontSize={13} style={{ display: 'block', marginBottom: 4 }}>
            {t('fields.systemPrompt')}
          </LitegraphText>
          <Input.TextArea
            value={draft.SystemPrompt ?? DEFAULT_SYSTEM_PROMPT}
            onChange={(e) => patchDraft({ SystemPrompt: e.target.value || null })}
            placeholder={t('fields.systemPromptPlaceholder')}
            autoSize={{ minRows: 4, maxRows: 12 }}
            data-testid="chat-settings-system-prompt"
          />
          <LitegraphText
            fontSize={11.5}
            style={{ display: 'block', marginTop: 2, color: 'var(--ant-color-text-tertiary)' }}
          >
            {t('fields.systemPromptHint')}
          </LitegraphText>
        </Card>

        <Card size="small" title={t('sections.features')} data-testid="chat-settings-features">
          {renderToggle(
            t('fields.enableChat'),
            t('fields.enableChatHint'),
            draft.EnableChat,
            (checked) => patchDraft({ EnableChat: checked }),
            'chat-settings-enable-chat'
          )}
          {renderToggle(
            t('fields.enableTools'),
            t('fields.enableToolsHint'),
            draft.EnableTools,
            (checked) => patchDraft({ EnableTools: checked }),
            'chat-settings-enable-tools'
          )}
          {renderToggle(
            t('fields.enableMutationTools'),
            t('fields.enableMutationToolsHint'),
            draft.EnableMutationTools,
            (checked) => patchDraft({ EnableMutationTools: checked }),
            'chat-settings-enable-mutation-tools'
          )}
          {renderToggle(
            t('fields.enableRag'),
            t('fields.enableRagHint'),
            draft.EnableRag,
            (checked) => patchDraft({ EnableRag: checked }),
            'chat-settings-enable-rag'
          )}
        </Card>

        <Card size="small" title={t('sections.limits')} data-testid="chat-settings-limits">
          {renderNumber(
            t('fields.maxToolIterations'),
            t('fields.maxToolIterationsHint'),
            draft.MaxToolIterations,
            (val) => patchDraft({ MaxToolIterations: val ?? 1 }),
            'chat-settings-max-tool-iterations',
            1
          )}
          {renderNumber(
            t('fields.ragTopK'),
            t('fields.ragTopKHint'),
            draft.RagTopK,
            (val) => patchDraft({ RagTopK: val ?? 1 }),
            'chat-settings-rag-topk',
            1,
            100
          )}
          {renderNumber(
            t('fields.ragScoreThreshold'),
            t('fields.ragScoreThresholdHint'),
            draft.RagScoreThreshold,
            (val) => patchDraft({ RagScoreThreshold: val ?? 0 }),
            'chat-settings-rag-score-threshold',
            -1,
            1,
            0.05
          )}
          {renderNumber(
            t('fields.historyRetentionDays'),
            t('fields.historyRetentionDaysHint'),
            draft.HistoryRetentionDays,
            (val) => patchDraft({ HistoryRetentionDays: val ?? 0 }),
            'chat-settings-history-retention-days',
            0
          )}
        </Card>
      </LitegraphFlex>
    </PageContainer>
  );
};

export default ChatSettingsPage;
