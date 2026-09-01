'use client';
import React, { useEffect, useRef } from 'react';
import { useTranslations } from 'next-intl';
import { Alert, Card, Collapse, Popover, Spin, Table, Tag } from 'antd';
import {
  CheckCircleFilled,
  CloseCircleFilled,
  DislikeOutlined,
  FileSearchOutlined,
  InfoCircleOutlined,
  LikeOutlined,
  ToolOutlined,
} from '@ant-design/icons';
import LitegraphButton from '@/components/base/button/Button';
import LitegraphFlex from '@/components/base/flex/Flex';
import LitegraphText from '@/components/base/typograpghy/Text';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';
import CopyButton from '@/components/base/copy-button/CopyButton';
import { ChatFeedbackRating } from '@/lib/sdk/chat';
import { ChatRetrievalChunk } from '@/lib/sdk/chatSse';
import { ToolActivityEntry } from '../chatStream';
import Markdown from './Markdown';

/** Per-turn statistics surfaced behind the (i) button. */
export type ChatTurnStats = {
  provider?: string | null;
  model?: string | null;
  promptTokens?: number | null;
  completionTokens?: number | null;
  ttftMs?: number | null;
  ttltMs?: number | null;
  totalDurationMs?: number | null;
  tpsOverall?: number | null;
  tpsGeneration?: number | null;
  toolCalls?: number | null;
  toolIterations?: number | null;
  ragChunks?: number | null;
  retries?: number | null;
};

/** Normalized display model for one exchange (server turn or live stream). */
export type ChatDisplayItem = {
  key: string;
  turnGuid: string | null;
  userMessage: string;
  assistant: string;
  thinking: string;
  tools: ToolActivityEntry[];
  retrieval: ChatRetrievalChunk[];
  error: string | null;
  streaming: boolean;
  stats?: ChatTurnStats | null;
  /** When set, the item renders as a centered system notice (slash-command output). */
  notice?: string;
};

interface MessageListProps {
  items: ChatDisplayItem[];
  onFeedback: (turnGuid: string, rating: ChatFeedbackRating) => void;
}

const bubbleBase: React.CSSProperties = {
  borderRadius: 12,
  padding: '10px 14px',
  maxWidth: 'min(720px, 92%)',
  overflowWrap: 'anywhere',
};

const ToolActivityView = ({ tools }: { tools: ToolActivityEntry[] }) => {
  const t = useTranslations('ai.chat');
  const allDone = tools.length > 0 && tools.every((tool) => tool.completed);

  if (tools.length === 0) return null;

  if (allDone) {
    return (
      <Card
        size="small"
        style={{ marginTop: 8, maxWidth: 'min(720px, 92%)' }}
        title={
          <LitegraphFlex align="center" gap={6}>
            <ToolOutlined />
            <span style={{ fontSize: 12.5 }}>{t('tools.summaryTitle', { count: tools.length })}</span>
          </LitegraphFlex>
        }
        data-testid="chat-tool-summary"
      >
        <Table
          size="small"
          pagination={false}
          rowKey="key"
          dataSource={tools.map((tool, index) => ({ ...tool, key: `${tool.name}-${index}` }))}
          columns={[
            {
              title: t('tools.name'),
              dataIndex: 'name',
              key: 'name',
              render: (name: string) => <code style={{ fontSize: 12 }}>{name}</code>,
            },
            {
              title: t('tools.success'),
              dataIndex: 'success',
              key: 'success',
              width: 90,
              render: (success?: boolean) =>
                success ? (
                  <CheckCircleFilled style={{ color: 'var(--ant-color-success)' }} />
                ) : (
                  <CloseCircleFilled style={{ color: 'var(--ant-color-error)' }} />
                ),
            },
            {
              title: t('tools.runtime'),
              dataIndex: 'runtimeMs',
              key: 'runtimeMs',
              width: 110,
              render: (runtimeMs?: number | null) =>
                runtimeMs != null ? `${runtimeMs.toFixed(1)} ms` : '—',
            },
          ]}
        />
      </Card>
    );
  }

  return (
    <LitegraphFlex vertical gap={6} style={{ marginTop: 8 }} data-testid="chat-tool-live">
      {tools.map((tool, index) => (
        <LitegraphFlex
          key={`${tool.name}-${index}`}
          align="center"
          gap={8}
          style={{
            ...bubbleBase,
            background: 'var(--ant-color-fill-quaternary)',
            border: '1px dashed var(--ant-color-border)',
            paddingBlock: 6,
          }}
        >
          {tool.completed ? (
            tool.success ? (
              <CheckCircleFilled style={{ color: 'var(--ant-color-success)' }} />
            ) : (
              <CloseCircleFilled style={{ color: 'var(--ant-color-error)' }} />
            )
          ) : (
            <Spin size="small" />
          )}
          <LitegraphText style={{ fontSize: 12.5 }}>
            {tool.completed
              ? t('tools.finished', { name: tool.name })
              : t('tools.running', { name: tool.name })}
          </LitegraphText>
          {tool.runtimeMs != null && (
            <LitegraphText style={{ fontSize: 11, color: 'var(--ant-color-text-tertiary)' }}>
              {`${tool.runtimeMs.toFixed(1)} ms`}
            </LitegraphText>
          )}
        </LitegraphFlex>
      ))}
    </LitegraphFlex>
  );
};

const TurnStatsButton = ({ stats }: { stats: ChatTurnStats }) => {
  const t = useTranslations('ai.chat');

  const streamingMs =
    stats.ttftMs != null && stats.ttltMs != null ? Math.max(0, stats.ttltMs - stats.ttftMs) : null;
  const totalTokens =
    stats.promptTokens != null || stats.completionTokens != null
      ? (stats.promptTokens ?? 0) + (stats.completionTokens ?? 0)
      : null;

  const rows: { label: string; value: string }[] = [];
  const push = (label: string, value: string | null | undefined) => {
    if (value != null && value !== '') rows.push({ label, value });
  };
  push(
    t('stats.model'),
    stats.model ? (stats.provider ? `${stats.provider} / ${stats.model}` : stats.model) : null
  );
  push(t('stats.promptTokens'), stats.promptTokens != null ? String(stats.promptTokens) : null);
  push(
    t('stats.completionTokens'),
    stats.completionTokens != null ? String(stats.completionTokens) : null
  );
  push(t('stats.totalTokens'), totalTokens != null ? String(totalTokens) : null);
  push(t('stats.ttft'), stats.ttftMs != null ? `${Math.round(stats.ttftMs)} ms` : null);
  push(t('stats.streamingTime'), streamingMs != null ? `${Math.round(streamingMs)} ms` : null);
  push(
    t('stats.totalDuration'),
    stats.totalDurationMs != null ? `${Math.round(stats.totalDurationMs)} ms` : null
  );
  push(t('stats.tpsOverall'), stats.tpsOverall != null ? stats.tpsOverall.toFixed(1) : null);
  push(
    t('stats.tpsGeneration'),
    stats.tpsGeneration != null ? stats.tpsGeneration.toFixed(1) : null
  );
  push(t('stats.toolCalls'), stats.toolCalls ? String(stats.toolCalls) : null);
  push(t('stats.toolIterations'), stats.toolIterations ? String(stats.toolIterations) : null);
  push(t('stats.ragChunks'), stats.ragChunks ? String(stats.ragChunks) : null);
  push(t('stats.retries'), stats.retries ? String(stats.retries) : null);

  if (rows.length === 0) return null;

  return (
    <Popover
      trigger="click"
      placement="topLeft"
      content={
        <table style={{ fontSize: 12, borderCollapse: 'collapse' }} data-testid="chat-turn-stats">
          <tbody>
            {rows.map((row) => (
              <tr key={row.label}>
                <td
                  style={{
                    padding: '2px 12px 2px 0',
                    color: 'var(--ant-color-text-secondary)',
                    whiteSpace: 'nowrap',
                  }}
                >
                  {row.label}
                </td>
                <td style={{ padding: '2px 0', fontVariantNumeric: 'tabular-nums' }}>{row.value}</td>
              </tr>
            ))}
          </tbody>
        </table>
      }
    >
      <LitegraphTooltip title={t('stats.tooltip')}>
        <LitegraphButton
          type="text"
          size="small"
          icon={<InfoCircleOutlined style={{ fontSize: 13 }} />}
          aria-label={t('stats.tooltip')}
          data-testid="chat-turn-stats-button"
        />
      </LitegraphTooltip>
    </Popover>
  );
};

const SourcesCard = ({ retrieval }: { retrieval: ChatRetrievalChunk[] }) => {
  const t = useTranslations('ai.chat');
  if (retrieval.length === 0) return null;
  return (
    <Card
      size="small"
      style={{ marginTop: 8, maxWidth: 'min(720px, 92%)' }}
      title={
        <LitegraphFlex align="center" gap={6}>
          <FileSearchOutlined />
          <span style={{ fontSize: 12.5 }}>{t('sources.title', { count: retrieval.length })}</span>
        </LitegraphFlex>
      }
      data-testid="chat-sources"
    >
      <LitegraphFlex vertical gap={4}>
        {retrieval.map((chunk, index) => (
          <LitegraphFlex key={`${chunk.nodeGuid}-${index}`} align="center" gap={8} wrap="wrap">
            <LitegraphText style={{ fontSize: 12.5, fontWeight: 500 }}>
              {chunk.name || t('sources.unnamedNode')}
            </LitegraphText>
            <LitegraphText
              style={{ fontSize: 11, fontFamily: 'monospace', color: 'var(--ant-color-text-tertiary)' }}
            >
              {chunk.nodeGuid}
            </LitegraphText>
            {chunk.score != null && (
              <Tag style={{ fontSize: 11, marginInlineEnd: 0 }}>
                {t('sources.score', { score: Number(chunk.score).toFixed(4) })}
              </Tag>
            )}
          </LitegraphFlex>
        ))}
      </LitegraphFlex>
    </Card>
  );
};

/** The scrolling message pane: user/assistant bubbles with streaming affordances. */
const MessageList = ({ items, onFeedback }: MessageListProps) => {
  const t = useTranslations('ai.chat');
  const bottomRef = useRef<HTMLDivElement | null>(null);
  const lastItem = items.length > 0 ? items[items.length - 1] : null;

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth', block: 'end' });
  }, [items.length, lastItem?.assistant, lastItem?.thinking]);

  return (
    <LitegraphFlex vertical gap={16} style={{ padding: '16px 16px 8px' }} data-testid="chat-messages">
      {items.map((item) => (
        <LitegraphFlex key={item.key} vertical gap={4}>
          {/* System notice (slash-command output) */}
          {item.notice != null ? (
            <LitegraphFlex justify="center">
              <div
                style={{
                  ...bubbleBase,
                  background: 'var(--ant-color-fill-quaternary)',
                  border: '1px dashed var(--ant-color-border)',
                  fontSize: 12.5,
                }}
                data-testid="chat-system-notice"
              >
                <Markdown content={item.notice} />
              </div>
            </LitegraphFlex>
          ) : null}
          {/* User bubble */}
          {item.notice == null && (
          <LitegraphFlex justify="flex-end">
            <div
              style={{
                ...bubbleBase,
                background: 'var(--ant-color-primary-bg)',
                whiteSpace: 'pre-wrap',
              }}
              data-testid="chat-user-bubble"
            >
              <LitegraphText style={{ fontSize: 13.5 }}>{item.userMessage}</LitegraphText>
            </div>
          </LitegraphFlex>
          )}

          {/* Thinking block */}
          {item.thinking && (
            <Collapse
              size="small"
              ghost
              style={{ maxWidth: 'min(720px, 92%)' }}
              items={[
                {
                  key: 'thinking',
                  label: (
                    <LitegraphText
                      style={{ fontSize: 12, color: 'var(--ant-color-text-tertiary)' }}
                    >
                      {t('thinking')}
                    </LitegraphText>
                  ),
                  children: (
                    <LitegraphText
                      style={{
                        fontSize: 12,
                        whiteSpace: 'pre-wrap',
                        color: 'var(--ant-color-text-secondary)',
                      }}
                    >
                      {item.thinking}
                    </LitegraphText>
                  ),
                },
              ]}
              data-testid="chat-thinking"
            />
          )}

          {/* Tool activity */}
          <ToolActivityView tools={item.tools} />

          {/* Sources */}
          <SourcesCard retrieval={item.retrieval} />

          {/* Assistant bubble */}
          {(item.assistant || item.streaming) && (
            <LitegraphFlex justify="flex-start">
              <div
                style={{
                  ...bubbleBase,
                  background: 'var(--ant-color-fill-quaternary)',
                  border: '1px solid var(--ant-color-border-secondary)',
                }}
                data-testid="chat-assistant-bubble"
              >
                {item.assistant ? (
                  <Markdown content={item.assistant} />
                ) : (
                  <Spin size="small" data-testid="chat-assistant-pending" />
                )}
                {!item.streaming && item.assistant && (
                  <LitegraphFlex align="center" gap={2} style={{ marginTop: 4 }}>
                    <CopyButton text={item.assistant} tooltipTitle={t('copyMessage')} />
                    {item.stats && <TurnStatsButton stats={item.stats} />}
                    {item.turnGuid && (
                      <>
                        <LitegraphTooltip title={t('feedback.up')}>
                          <LitegraphButton
                            type="text"
                            size="small"
                            icon={<LikeOutlined style={{ fontSize: 13 }} />}
                            aria-label={t('feedback.up')}
                            onClick={() => onFeedback(item.turnGuid as string, 'ThumbsUp')}
                            data-testid={`chat-thumbs-up-${item.turnGuid}`}
                          />
                        </LitegraphTooltip>
                        <LitegraphTooltip title={t('feedback.down')}>
                          <LitegraphButton
                            type="text"
                            size="small"
                            icon={<DislikeOutlined style={{ fontSize: 13 }} />}
                            aria-label={t('feedback.down')}
                            onClick={() => onFeedback(item.turnGuid as string, 'ThumbsDown')}
                            data-testid={`chat-thumbs-down-${item.turnGuid}`}
                          />
                        </LitegraphTooltip>
                      </>
                    )}
                  </LitegraphFlex>
                )}
              </div>
            </LitegraphFlex>
          )}

          {/* Error bubble */}
          {item.error && (
            <Alert
              type="error"
              showIcon
              message={t('errorTitle')}
              description={item.error}
              style={{ maxWidth: 'min(720px, 92%)' }}
              data-testid="chat-error-bubble"
            />
          )}
        </LitegraphFlex>
      ))}
      <div ref={bottomRef} />
    </LitegraphFlex>
  );
};

export default MessageList;
