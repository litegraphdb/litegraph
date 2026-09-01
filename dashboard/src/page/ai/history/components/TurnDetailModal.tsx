'use client';
import React from 'react';
import { useTranslations } from 'next-intl';
import { Descriptions, Table, Tag } from 'antd';
import { CheckCircleFilled, CloseCircleFilled } from '@ant-design/icons';
import LitegraphModal from '@/components/base/modal/Modal';
import LitegraphFlex from '@/components/base/flex/Flex';
import LitegraphText from '@/components/base/typograpghy/Text';
import CopyButton from '@/components/base/copy-button/CopyButton';
import { ChatTurn } from '@/lib/sdk/chat';
import { parseToolTranscript } from '@/page/ai/chat/chatStream';

type Translator = (key: string, values?: Record<string, string | number>) => string;

type WaterfallStage = {
  key: string;
  label: string;
  start: number;
  duration: number;
  color: string;
};

/**
 * Computes the sequential stage layout for the turn waterfall: embedding →
 * retrieval → limiter wait → time-to-first-token → streaming. Offsets are
 * cumulative; the TTFT stage spans from the limiter end to the reported TTFT
 * when that is larger (TTFT is measured from turn start).
 */
export const buildWaterfallStages = (turn: ChatTurn, t: Translator): WaterfallStage[] => {
  const stages: WaterfallStage[] = [];
  let cursor = 0;
  const push = (key: string, label: string, duration: number | null | undefined, color: string) => {
    if (duration == null || duration <= 0) return;
    stages.push({ key, label, start: cursor, duration, color });
    cursor += duration;
  };
  push('embedding', t('turnDetail.stageEmbedding'), turn.EmbeddingDurationMs, '#7f5af0');
  push('retrieval', t('turnDetail.stageRetrieval'), turn.RetrievalDurationMs, '#2cb1bc');
  push('limiter', t('turnDetail.stageLimiter'), turn.LimiterWaitMs, '#f0a202');
  if (turn.TimeToFirstTokenMs != null && turn.TimeToFirstTokenMs > cursor) {
    stages.push({
      key: 'ttft',
      label: t('turnDetail.stageTtft'),
      start: cursor,
      duration: turn.TimeToFirstTokenMs - cursor,
      color: '#4361ee',
    });
    cursor = turn.TimeToFirstTokenMs;
  }
  if (turn.TimeToLastTokenMs != null && turn.TimeToLastTokenMs > cursor) {
    stages.push({
      key: 'streaming',
      label: t('turnDetail.stageStreaming'),
      start: cursor,
      duration: turn.TimeToLastTokenMs - cursor,
      color: '#06a77d',
    });
    cursor = turn.TimeToLastTokenMs;
  }
  return stages;
};

/** Hand-rolled SVG stage waterfall for one turn. */
const TurnWaterfall = ({ turn }: { turn: ChatTurn }) => {
  const t = useTranslations('ai.history');
  const stages = buildWaterfallStages(turn, t as unknown as Translator);
  if (stages.length === 0) {
    return (
      <LitegraphText style={{ fontSize: 12, color: 'var(--ant-color-text-tertiary)' }}>
        {t('turnDetail.noTimings')}
      </LitegraphText>
    );
  }
  const total = Math.max(
    turn.TotalDurationMs || 0,
    stages[stages.length - 1].start + stages[stages.length - 1].duration
  );
  const width = 560;
  const labelWidth = 130;
  const rowHeight = 26;
  const barAreaWidth = width - labelWidth - 70;
  const height = stages.length * rowHeight + 8;
  const scale = (ms: number) => (total > 0 ? (ms / total) * barAreaWidth : 0);

  return (
    <div style={{ overflowX: 'auto' }}>
      <svg
        viewBox={`0 0 ${width} ${height}`}
        role="img"
        aria-label={t('turnDetail.waterfallTitle')}
        style={{ width: '100%', minWidth: 420, height: 'auto', display: 'block' }}
        data-testid="turn-waterfall"
      >
        {stages.map((stage, index) => {
          const y = index * rowHeight + 4;
          const x = labelWidth + scale(stage.start);
          const barWidth = Math.max(2, scale(stage.duration));
          return (
            <g key={stage.key}>
              <text
                x={labelWidth - 8}
                y={y + 14}
                textAnchor="end"
                style={{ fontSize: 11, fill: 'var(--ant-color-text-secondary)' }}
              >
                {stage.label}
              </text>
              <rect
                x={x}
                y={y}
                width={barWidth}
                height={rowHeight - 10}
                rx={3}
                fill={stage.color}
                opacity={0.85}
              >
                <title>{`${stage.label}: ${stage.duration.toFixed(1)} ms`}</title>
              </rect>
              <text
                x={x + barWidth + 6}
                y={y + 12}
                style={{ fontSize: 10, fill: 'var(--ant-color-text-tertiary)' }}
              >
                {`${stage.duration.toFixed(0)} ms`}
              </text>
            </g>
          );
        })}
      </svg>
    </div>
  );
};

interface TurnDetailModalProps {
  turn: ChatTurn;
  onClose: () => void;
}

/** Full turn drill-down: waterfall, tokens, retries, tool transcript, and text. */
const TurnDetailModal = ({ turn, onClose }: TurnDetailModalProps) => {
  const t = useTranslations('ai.history');
  const tools = parseToolTranscript(turn.ToolTranscriptJson);

  return (
    <LitegraphModal
      title={t('turnDetail.title')}
      open
      onCancel={onClose}
      onOk={onClose}
      cancelButtonProps={{ style: { display: 'none' } }}
      okText={t('turnDetail.close')}
      width={680}
      data-testid="turn-detail-modal"
    >
      <LitegraphFlex vertical gap={16}>
        <div>
          <LitegraphText style={{ display: 'block', fontWeight: 600, marginBottom: 8, fontSize: 13 }}>
            {t('turnDetail.waterfallTitle')}
          </LitegraphText>
          <TurnWaterfall turn={turn} />
        </div>

        <Descriptions column={2} size="small" bordered>
          <Descriptions.Item label={t('turnDetail.promptTokens')}>
            {turn.PromptTokens ?? '—'}
          </Descriptions.Item>
          <Descriptions.Item label={t('turnDetail.completionTokens')}>
            {turn.CompletionTokens ?? '—'}
          </Descriptions.Item>
          <Descriptions.Item label={t('turnDetail.tokensPerSecond')}>
            {turn.TokensPerSecondOverall != null ? turn.TokensPerSecondOverall.toFixed(2) : '—'}
          </Descriptions.Item>
          <Descriptions.Item label={t('turnDetail.retries')}>{turn.RetryCount}</Descriptions.Item>
          <Descriptions.Item label={t('turnDetail.totalDuration')}>
            {`${turn.TotalDurationMs.toFixed(0)} ms`}
          </Descriptions.Item>
          <Descriptions.Item label={t('turnDetail.status')}>
            {turn.Success ? (
              <Tag color="green">{t('turnDetail.success')}</Tag>
            ) : (
              <Tag color="red">{t('turnDetail.failed')}</Tag>
            )}
          </Descriptions.Item>
          {turn.Error && (
            <Descriptions.Item label={t('turnDetail.error')} span={2}>
              <LitegraphText style={{ color: 'var(--ant-color-error)', fontSize: 12 }}>
                {turn.Error}
              </LitegraphText>
            </Descriptions.Item>
          )}
        </Descriptions>

        {tools.length > 0 && (
          <div>
            <LitegraphText
              style={{ display: 'block', fontWeight: 600, marginBottom: 8, fontSize: 13 }}
            >
              {t('turnDetail.toolsTitle')}
            </LitegraphText>
            <Table
              size="small"
              pagination={false}
              rowKey="key"
              dataSource={tools.map((tool, index) => ({ ...tool, key: `${tool.name}-${index}` }))}
              columns={[
                {
                  title: t('turnDetail.toolName'),
                  dataIndex: 'name',
                  key: 'name',
                  render: (name: string) => <code style={{ fontSize: 12 }}>{name}</code>,
                },
                {
                  title: t('turnDetail.toolIteration'),
                  dataIndex: 'iteration',
                  key: 'iteration',
                  width: 90,
                  align: 'center',
                },
                {
                  title: t('turnDetail.toolSuccess'),
                  dataIndex: 'success',
                  key: 'success',
                  width: 80,
                  align: 'center',
                  render: (success?: boolean) =>
                    success ? (
                      <CheckCircleFilled style={{ color: 'var(--ant-color-success)' }} />
                    ) : (
                      <CloseCircleFilled style={{ color: 'var(--ant-color-error)' }} />
                    ),
                },
                {
                  title: t('turnDetail.toolRuntime'),
                  dataIndex: 'runtimeMs',
                  key: 'runtimeMs',
                  width: 100,
                  render: (runtimeMs?: number | null) =>
                    runtimeMs != null ? `${runtimeMs.toFixed(1)} ms` : '—',
                },
              ]}
            />
          </div>
        )}

        <div>
          <LitegraphFlex align="center" gap={4} style={{ marginBottom: 4 }}>
            <LitegraphText style={{ fontWeight: 600, fontSize: 13 }}>
              {t('turnDetail.userMessage')}
            </LitegraphText>
            <CopyButton text={turn.UserMessage || ''} tooltipTitle={t('turnDetail.copy')} />
          </LitegraphFlex>
          <div
            style={{
              background: 'var(--ant-color-fill-quaternary)',
              borderRadius: 8,
              padding: 10,
              fontSize: 12.5,
              whiteSpace: 'pre-wrap',
              maxHeight: 180,
              overflowY: 'auto',
            }}
          >
            {turn.UserMessage}
          </div>
        </div>
        <div>
          <LitegraphFlex align="center" gap={4} style={{ marginBottom: 4 }}>
            <LitegraphText style={{ fontWeight: 600, fontSize: 13 }}>
              {t('turnDetail.assistantResponse')}
            </LitegraphText>
            <CopyButton text={turn.AssistantResponse || ''} tooltipTitle={t('turnDetail.copy')} />
          </LitegraphFlex>
          <div
            style={{
              background: 'var(--ant-color-fill-quaternary)',
              borderRadius: 8,
              padding: 10,
              fontSize: 12.5,
              whiteSpace: 'pre-wrap',
              maxHeight: 240,
              overflowY: 'auto',
            }}
          >
            {turn.AssistantResponse}
          </div>
        </div>
      </LitegraphFlex>
    </LitegraphModal>
  );
};

export default TurnDetailModal;
