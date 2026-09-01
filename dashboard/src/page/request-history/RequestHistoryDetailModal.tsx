'use client';
import React, { useEffect, useState } from 'react';
import { useTranslations } from 'next-intl';
import { Modal, Collapse, Typography, Tag, Space, Descriptions, Tooltip } from 'antd';
import { toast } from 'react-hot-toast';
import { globalToastId } from '@/constants/config';
import CopyButton from '@/components/base/copy-button/CopyButton';
import {
  getRequestHistoryDetail,
  RequestHistoryDetail,
  RequestHistoryEntry,
} from '@/lib/sdk/requestHistory';

const { Text } = Typography;

type Props = {
  entry: RequestHistoryEntry | null;
  open: boolean;
  onClose: () => void;
};

const statusColor = (code: number): string => {
  if (code < 300 && code >= 200) return 'green';
  if (code < 400 && code >= 300) return 'blue';
  if (code < 500 && code >= 400) return 'gold';
  return 'red';
};

const formatBytes = (n: number): string => {
  if (n < 1024) return `${n} B`;
  if (n < 1024 * 1024) return `${(n / 1024).toFixed(1)} KB`;
  return `${(n / (1024 * 1024)).toFixed(2)} MB`;
};

const prettyJson = (value: string | null | undefined): string => {
  if (!value) return '';
  try {
    return JSON.stringify(JSON.parse(value), null, 2);
  } catch {
    return value;
  }
};

const headersAsText = (headers: Record<string, string> | undefined | null): string => {
  if (!headers) return '';
  return JSON.stringify(headers, null, 2);
};

const noWrapValueStyle: React.CSSProperties = {
  whiteSpace: 'nowrap',
  overflowX: 'auto',
};

const CodeBlock: React.FC<{ text: string; empty?: string }> = ({ text, empty }) => {
  const t = useTranslations('requestHistory');
  const tCommon = useTranslations('common');
  return (
    <div style={{ position: 'relative' }}>
      <CopyButton
        text={text || ''}
        tooltipTitle={tCommon('copy.copy')}
        label={tCommon('copy.copy')}
        size="small"
        type="default"
        style={{ position: 'absolute', top: 8, right: 8, zIndex: 1 }}
      />
      <pre
        style={{
          margin: 0,
          padding: 12,
          paddingRight: 80,
          background: 'var(--ant-color-fill-quaternary)',
          border: '1px solid var(--ant-color-border)',
          borderRadius: 6,
          fontFamily: "'Monaco', 'Menlo', 'Consolas', monospace",
          fontSize: 12,
          lineHeight: 1.5,
          whiteSpace: 'pre-wrap',
          wordBreak: 'break-word',
          maxHeight: 400,
          overflow: 'auto',
          color: 'var(--ant-color-text)',
        }}
      >
        {text || empty || t('detail.empty')}
      </pre>
    </div>
  );
};

type SseFrame = { index: number; event: string; summary: string };

/** True when a stored response body is a text/event-stream payload. */
const isSseBody = (body: string | null | undefined, headers?: Record<string, string> | null): boolean => {
  if (headers) {
    const contentType = Object.entries(headers).find(([k]) => k.toLowerCase() === 'content-type');
    if (contentType && contentType[1].toLowerCase().includes('text/event-stream')) return true;
  }
  return !!body && /^(data:|event:)/m.test(body.trimStart().slice(0, 200));
};

/** Parse a stored SSE body into display frames plus the reconstructed assistant output. */
const parseSseBody = (body: string): { frames: SseFrame[]; reconstructed: string } => {
  const frames: SseFrame[] = [];
  let reconstructed = '';
  const rawFrames = body.replace(/\r\n/g, '\n').split(/\n\n+/);
  let index = 0;
  for (const rawFrame of rawFrames) {
    const dataLines = rawFrame
      .split('\n')
      .filter((line) => line.startsWith('data:'))
      .map((line) => line.slice(5).replace(/^\s/, ''));
    if (dataLines.length === 0) continue;
    const payload = dataLines.join('\n');
    index += 1;
    if (payload.trim() === '[DONE]') {
      frames.push({ index, event: 'done', summary: '[DONE]' });
      continue;
    }
    try {
      const parsed = JSON.parse(payload);
      const eventName = typeof parsed.event === 'string' ? parsed.event : '?';
      let summary = '';
      switch (eventName) {
        case 'delta':
        case 'thinking':
          summary = String(parsed.content ?? '');
          if (eventName === 'delta') reconstructed += String(parsed.content ?? '');
          break;
        case 'started':
          summary = `thread ${parsed.threadGuid ?? '?'} · turn ${parsed.turnGuid ?? '?'}`;
          break;
        case 'tool_call':
          summary = String(parsed.name ?? '');
          break;
        case 'tool_result':
          summary = `${parsed.name ?? ''} · ${parsed.success ? 'ok' : 'failed'}${parsed.runtimeMs != null ? ` · ${Number(parsed.runtimeMs).toFixed(1)} ms` : ''}`;
          break;
        case 'usage':
          summary = `prompt ${parsed.usage?.PromptTokens ?? '?'} · completion ${parsed.usage?.CompletionTokens ?? '?'} · ${parsed.usage?.TotalDurationMs != null ? `${Math.round(parsed.usage.TotalDurationMs)} ms` : ''}`;
          break;
        case 'retrieval':
          summary = `${Array.isArray(parsed.chunks) ? parsed.chunks.length : '?'} chunk(s)`;
          break;
        case 'error':
          summary = String(parsed.message ?? '');
          break;
        default:
          summary = payload.length > 120 ? `${payload.slice(0, 120)}…` : payload;
      }
      frames.push({ index, event: eventName, summary });
    } catch {
      frames.push({ index, event: 'raw', summary: payload.length > 120 ? `${payload.slice(0, 120)}…` : payload });
    }
  }
  return { frames, reconstructed };
};

const SseEventsView: React.FC<{ body: string }> = ({ body }) => {
  const t = useTranslations('requestHistory');
  const { frames, reconstructed } = parseSseBody(body);
  return (
    <div data-testid="request-detail-sse">
      {reconstructed && (
        <>
          <Tooltip title={t('detail.sse.reconstructedTooltip')}>
            <Text strong style={{ fontSize: 12.5, display: 'inline-block', marginBottom: 4 }}>
              {t('detail.sse.reconstructed')}
            </Text>
          </Tooltip>
          <CodeBlock text={reconstructed} />
        </>
      )}
      <Tooltip title={t('detail.sse.eventsTooltip')}>
        <Text strong style={{ fontSize: 12.5, display: 'inline-block', margin: '12px 0 4px' }}>
          {t('detail.sse.events', { count: frames.length })}
        </Text>
      </Tooltip>
      <div
        style={{
          border: '1px solid var(--ant-color-border)',
          borderRadius: 6,
          maxHeight: 320,
          overflowY: 'auto',
          fontFamily: "'Monaco', 'Menlo', 'Consolas', monospace",
          fontSize: 12,
        }}
      >
        {frames.map((frame) => (
          <div
            key={frame.index}
            style={{
              display: 'flex',
              gap: 8,
              padding: '3px 10px',
              borderBottom: '1px solid var(--ant-color-border-secondary)',
              alignItems: 'baseline',
            }}
          >
            <span style={{ color: 'var(--ant-color-text-tertiary)', minWidth: 32, textAlign: 'right' }}>
              {frame.index}
            </span>
            <Tag style={{ marginInlineEnd: 0, fontSize: 11 }}>{frame.event}</Tag>
            <span style={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word', flex: 1 }}>
              {frame.summary}
            </span>
          </div>
        ))}
      </div>
      <Collapse
        size="small"
        ghost
        style={{ marginTop: 8 }}
        items={[
          {
            key: 'raw-sse',
            label: (
              <Tooltip title={t('detail.sse.rawTooltip')}>
                <span>{t('detail.sse.raw')}</span>
              </Tooltip>
            ),
            children: <CodeBlock text={body} />,
          },
        ]}
      />
    </div>
  );
};

const RequestHistoryDetailModal: React.FC<Props> = ({ entry, open, onClose }) => {
  const t = useTranslations('requestHistory');
  const tCommon = useTranslations('common');
  const [detail, setDetail] = useState<RequestHistoryDetail | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!open || !entry) {
      setDetail(null);
      return;
    }
    let cancelled = false;
    setLoading(true);
    getRequestHistoryDetail(entry.GUID)
      .then((d) => {
        if (!cancelled) {
          setDetail(d);
          setLoading(false);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setDetail(null);
          setLoading(false);
          toast.error(t('detail.loadFailed'), { id: globalToastId });
        }
      });
    return () => {
      cancelled = true;
    };
  }, [open, entry]);

  if (!entry) return null;

  return (
    <Modal
      title={t('detail.title')}
      open={open}
      onCancel={onClose}
      footer={null}
      width="min(1688px, calc(100vw - 32px))"
      style={{ top: 16, paddingBottom: 0 }}
      styles={{ body: { maxHeight: 'calc(100vh - 130px)', overflowY: 'auto' } }}
      destroyOnHidden
      maskClosable
    >
      <Descriptions
        size="small"
        column={2}
        bordered
        style={{ marginBottom: 16 }}
        styles={{ label: { whiteSpace: 'nowrap' } }}
      >
        <Descriptions.Item label={t('detail.fields.id')} span={2}>
          <Space size={4}>
            <Text code>{entry.GUID}</Text>
            <CopyButton text={entry.GUID} tooltipTitle={t('detail.copyId')} />
          </Space>
        </Descriptions.Item>
        <Descriptions.Item label={t('detail.fields.method')}>{entry.Method}</Descriptions.Item>
        <Descriptions.Item label={t('detail.fields.status')}>
          <Tag color={statusColor(entry.StatusCode)}>{entry.StatusCode}</Tag>
        </Descriptions.Item>
        <Descriptions.Item label={t('detail.fields.duration')}>
          {entry.ProcessingTimeMs.toFixed(2)} ms
        </Descriptions.Item>
        <Descriptions.Item label={t('detail.fields.time')} contentStyle={noWrapValueStyle}>
          <span data-testid="request-detail-time" style={noWrapValueStyle}>
            {new Date(entry.CreatedUtc).toLocaleString()}
          </span>
        </Descriptions.Item>
        <Descriptions.Item label={t('detail.fields.sourceIp')} contentStyle={noWrapValueStyle}>
          <span data-testid="request-detail-source-ip" style={noWrapValueStyle}>
            {entry.SourceIp || '-'}
          </span>
        </Descriptions.Item>
        <Descriptions.Item label={t('detail.fields.tenant')}>
          {entry.TenantGUID ? <Text code>{entry.TenantGUID}</Text> : '-'}
        </Descriptions.Item>
        <Descriptions.Item label={t('detail.fields.requestSize')}>
          {formatBytes(entry.RequestBodyLength)}
        </Descriptions.Item>
        <Descriptions.Item label={t('detail.fields.responseSize')}>
          {formatBytes(entry.ResponseBodyLength)}
        </Descriptions.Item>
        <Descriptions.Item label={t('detail.fields.url')} span={2}>
          <Text code style={{ wordBreak: 'break-all' }}>
            {entry.Url}
          </Text>
        </Descriptions.Item>
      </Descriptions>

      <Collapse
        items={[
          {
            key: 'req-headers',
            label: (
              <Space>
                <span>{t('detail.sections.requestHeaders')}</span>
                {detail?.RequestHeaders && (
                  <Text type="secondary" style={{ fontSize: 12 }}>
                    {t('detail.entries', { count: Object.keys(detail.RequestHeaders).length })}
                  </Text>
                )}
              </Space>
            ),
            children: (
              <CodeBlock
                text={headersAsText(detail?.RequestHeaders)}
                empty={loading ? tCommon('states.loading') : t('detail.empty')}
              />
            ),
          },
          {
            key: 'req-body',
            label: (
              <Space>
                <span>{t('detail.sections.requestBody')}</span>
                {entry.RequestBodyLength > 0 && (
                  <Text type="secondary" style={{ fontSize: 12 }}>
                    {formatBytes(entry.RequestBodyLength)}
                    {entry.RequestBodyTruncated ? ` ${t('detail.truncated')}` : ''}
                  </Text>
                )}
              </Space>
            ),
            children: (
              <CodeBlock
                text={prettyJson(detail?.RequestBody)}
                empty={loading ? tCommon('states.loading') : t('detail.empty')}
              />
            ),
          },
          {
            key: 'resp-headers',
            label: (
              <Space>
                <span>{t('detail.sections.responseHeaders')}</span>
                {detail?.ResponseHeaders && (
                  <Text type="secondary" style={{ fontSize: 12 }}>
                    {t('detail.entries', { count: Object.keys(detail.ResponseHeaders).length })}
                  </Text>
                )}
              </Space>
            ),
            children: (
              <CodeBlock
                text={headersAsText(detail?.ResponseHeaders)}
                empty={loading ? tCommon('states.loading') : t('detail.empty')}
              />
            ),
          },
          {
            key: 'resp-body',
            label: (
              <Space>
                <span>{t('detail.sections.responseBody')}</span>
                {entry.ResponseBodyLength > 0 && (
                  <Text type="secondary" style={{ fontSize: 12 }}>
                    {formatBytes(entry.ResponseBodyLength)}
                    {entry.ResponseBodyTruncated ? ` ${t('detail.truncated')}` : ''}
                  </Text>
                )}
              </Space>
            ),
            children: isSseBody(detail?.ResponseBody, detail?.ResponseHeaders) ? (
              <SseEventsView body={detail?.ResponseBody || ''} />
            ) : (
              <CodeBlock
                text={prettyJson(detail?.ResponseBody)}
                empty={loading ? tCommon('states.loading') : t('detail.empty')}
              />
            ),
          },
          ...(entry.TransactionDiagnosticsJson || detail?.TransactionDiagnosticsJson
            ? [
                {
                  key: 'transaction-diagnostics',
                  label: t('detail.sections.transactionDiagnostics'),
                  children: (
                    <CodeBlock
                      text={prettyJson(
                        detail?.TransactionDiagnosticsJson || entry.TransactionDiagnosticsJson
                      )}
                      empty={loading ? tCommon('states.loading') : t('detail.empty')}
                    />
                  ),
                },
              ]
            : []),
        ]}
      />
    </Modal>
  );
};

export default RequestHistoryDetailModal;
