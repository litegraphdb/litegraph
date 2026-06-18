'use client';
import React, { useEffect, useState } from 'react';
import { Modal, Collapse, Button, Typography, Tag, Space, Descriptions } from 'antd';
import { CopyOutlined } from '@ant-design/icons';
import { toast } from 'react-hot-toast';
import { globalToastId } from '@/constants/config';
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
  if (code >= 200 && code < 300) return 'green';
  if (code >= 300 && code < 400) return 'blue';
  if (code >= 400 && code < 500) return 'gold';
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

const CodeBlock: React.FC<{ text: string; empty?: string }> = ({ text, empty = '(empty)' }) => {
  const onCopy = async () => {
    try {
      await navigator.clipboard.writeText(text || '');
      toast.success('Copied to clipboard', { id: globalToastId });
    } catch {
      toast.error('Unable to copy', { id: globalToastId });
    }
  };
  return (
    <div style={{ position: 'relative' }}>
      <Button
        size="small"
        icon={<CopyOutlined />}
        onClick={onCopy}
        style={{ position: 'absolute', top: 8, right: 8, zIndex: 1 }}
      >
        Copy
      </Button>
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
        {text || empty}
      </pre>
    </div>
  );
};

const RequestHistoryDetailModal: React.FC<Props> = ({ entry, open, onClose }) => {
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
          toast.error('Unable to load request detail', { id: globalToastId });
        }
      });
    return () => {
      cancelled = true;
    };
  }, [open, entry]);

  if (!entry) return null;

  return (
    <Modal
      title="Request Detail"
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
        <Descriptions.Item label="ID" span={2}>
          <Text code copyable>
            {entry.GUID}
          </Text>
        </Descriptions.Item>
        <Descriptions.Item label="Method">{entry.Method}</Descriptions.Item>
        <Descriptions.Item label="Status">
          <Tag color={statusColor(entry.StatusCode)}>{entry.StatusCode}</Tag>
        </Descriptions.Item>
        <Descriptions.Item label="Duration">
          {entry.ProcessingTimeMs.toFixed(2)} ms
        </Descriptions.Item>
        <Descriptions.Item label="Time" contentStyle={noWrapValueStyle}>
          <span data-testid="request-detail-time" style={noWrapValueStyle}>
            {new Date(entry.CreatedUtc).toLocaleString()}
          </span>
        </Descriptions.Item>
        <Descriptions.Item label="Source IP" contentStyle={noWrapValueStyle}>
          <span data-testid="request-detail-source-ip" style={noWrapValueStyle}>
            {entry.SourceIp || '-'}
          </span>
        </Descriptions.Item>
        <Descriptions.Item label="Tenant">
          {entry.TenantGUID ? <Text code>{entry.TenantGUID}</Text> : '-'}
        </Descriptions.Item>
        <Descriptions.Item label="Request Size">{formatBytes(entry.RequestBodyLength)}</Descriptions.Item>
        <Descriptions.Item label="Response Size">{formatBytes(entry.ResponseBodyLength)}</Descriptions.Item>
        <Descriptions.Item label="URL" span={2}>
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
                <span>Request Headers</span>
                {detail?.RequestHeaders && (
                  <Text type="secondary" style={{ fontSize: 12 }}>
                    {Object.keys(detail.RequestHeaders).length} entries
                  </Text>
                )}
              </Space>
            ),
            children: (
              <CodeBlock
                text={headersAsText(detail?.RequestHeaders)}
                empty={loading ? 'Loading...' : '(empty)'}
              />
            ),
          },
          {
            key: 'req-body',
            label: (
              <Space>
                <span>Request Body</span>
                {entry.RequestBodyLength > 0 && (
                  <Text type="secondary" style={{ fontSize: 12 }}>
                    {formatBytes(entry.RequestBodyLength)}
                    {entry.RequestBodyTruncated ? ' [TRUNCATED]' : ''}
                  </Text>
                )}
              </Space>
            ),
            children: (
              <CodeBlock
                text={prettyJson(detail?.RequestBody)}
                empty={loading ? 'Loading...' : '(empty)'}
              />
            ),
          },
          {
            key: 'resp-headers',
            label: (
              <Space>
                <span>Response Headers</span>
                {detail?.ResponseHeaders && (
                  <Text type="secondary" style={{ fontSize: 12 }}>
                    {Object.keys(detail.ResponseHeaders).length} entries
                  </Text>
                )}
              </Space>
            ),
            children: (
              <CodeBlock
                text={headersAsText(detail?.ResponseHeaders)}
                empty={loading ? 'Loading...' : '(empty)'}
              />
            ),
          },
          {
            key: 'resp-body',
            label: (
              <Space>
                <span>Response Body</span>
                {entry.ResponseBodyLength > 0 && (
                  <Text type="secondary" style={{ fontSize: 12 }}>
                    {formatBytes(entry.ResponseBodyLength)}
                    {entry.ResponseBodyTruncated ? ' [TRUNCATED]' : ''}
                  </Text>
                )}
              </Space>
            ),
            children: (
              <CodeBlock
                text={prettyJson(detail?.ResponseBody)}
                empty={loading ? 'Loading...' : '(empty)'}
              />
            ),
          },
          ...(entry.TransactionDiagnosticsJson || detail?.TransactionDiagnosticsJson
            ? [
                {
                  key: 'transaction-diagnostics',
                  label: 'Transaction Diagnostics',
                  children: (
                    <CodeBlock
                      text={prettyJson(detail?.TransactionDiagnosticsJson || entry.TransactionDiagnosticsJson)}
                      empty={loading ? 'Loading...' : '(empty)'}
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
