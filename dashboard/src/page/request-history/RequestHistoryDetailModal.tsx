'use client';
import React, { useEffect, useState } from 'react';
import { useTranslations } from 'next-intl';
import { Modal, Collapse, Typography, Tag, Space, Descriptions } from 'antd';
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
            children: (
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
