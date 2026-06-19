'use client';
import React, { useCallback, useEffect, useMemo, useState } from 'react';
import { Alert, Button, Dropdown, Input, Modal, Select, Space, Tag, Typography } from 'antd';
import { CodeOutlined, DeleteOutlined, EyeOutlined, MoreOutlined } from '@ant-design/icons';
import type { ColumnsType } from 'antd/es/table';
import toast from 'react-hot-toast';
import PageContainer from '@/components/base/pageContainer/PageContainer';
import LitegraphTable from '@/components/base/table/Table';
import ViewJsonModal from '@/components/base/view-json-modal/ViewJsonModal';
import { globalToastId } from '@/constants/config';
import RequestHistoryChart from './RequestHistoryChart';
import RequestHistoryDetailModal from './RequestHistoryDetailModal';
import {
  deleteRequestHistory,
  getMetricsEndpointUrl,
  listRequestHistory,
  RequestHistoryEntry,
  RequestHistorySearchResult,
} from '@/lib/sdk/requestHistory';

const { Text } = Typography;

const noWrapStyle: React.CSSProperties = { whiteSpace: 'nowrap' };

const summaryToolbarStyle: React.CSSProperties = {
  display: 'flex',
  alignItems: 'flex-start',
  justifyContent: 'space-between',
  gap: 12,
  marginBottom: 12,
  flexWrap: 'wrap',
};

const summaryStatsStyle: React.CSSProperties = {
  display: 'flex',
  alignItems: 'center',
  gap: 12,
  flex: '1 1 520px',
  flexWrap: 'wrap',
};

const filterControlsStyle: React.CSSProperties = {
  display: 'flex',
  justifyContent: 'flex-end',
  gap: 8,
  flex: '1 1 440px',
  flexWrap: 'wrap',
};

const statBubbleStyle: React.CSSProperties = {
  display: 'inline-flex',
  alignItems: 'center',
  borderRadius: 999,
  background: 'var(--ant-color-fill-quaternary)',
  border: '1px solid var(--ant-color-border-secondary)',
  padding: '1px 8px',
  fontFamily: 'monospace',
  fontSize: 12,
  lineHeight: 1.6,
  marginLeft: 4,
};

const STATUS_OPTIONS = [
  { value: '', label: 'All statuses' },
  { value: '200', label: '200' },
  { value: '201', label: '201' },
  { value: '204', label: '204' },
  { value: '400', label: '400' },
  { value: '401', label: '401' },
  { value: '403', label: '403' },
  { value: '404', label: '404' },
  { value: '500', label: '500' },
];

const OUTCOME_OPTIONS = [
  { value: '', label: 'All outcomes' },
  { value: 'true', label: 'Successful' },
  { value: 'false', label: 'Errors' },
];

const TRANSACTION_OPTIONS = [
  { value: '', label: 'All requests' },
  { value: 'true', label: 'Transactions' },
  { value: 'false', label: 'Non-transactions' },
];

const METHOD_OPTIONS = [
  { value: '', label: 'All methods' },
  'GET',
  'POST',
  'PUT',
  'DELETE',
  'HEAD',
  'PATCH',
].map((m) => (typeof m === 'string' ? { value: m, label: m } : m));

type TransactionDiagnostics = {
  TransactionId?: string;
  State?: string;
  Success?: boolean;
  RolledBack?: boolean;
  ValidationFailure?: boolean;
  IsolationLevel?: string;
  Provider?: string;
  QueueWaitDurationMs?: number;
  CommitDurationMs?: number;
  RollbackDurationMs?: number;
  Retryable?: boolean;
  ConcurrencyConflict?: boolean;
  ProviderErrorCode?: string;
};

const parseTransactionDiagnostics = (
  value?: string | null
): TransactionDiagnostics | null => {
  if (!value) return null;
  try {
    const parsed = JSON.parse(value);
    if (!parsed || typeof parsed !== 'object') return null;
    return parsed as TransactionDiagnostics;
  } catch {
    return null;
  }
};

const transactionState = (
  diagnostics: TransactionDiagnostics
): { label: string; color: string } => {
  if (diagnostics.ValidationFailure) return { label: 'validation', color: 'orange' };
  if (diagnostics.State === 'Committed') return { label: 'committed', color: 'green' };
  if (diagnostics.State === 'RolledBack') return { label: 'rolled back', color: 'red' };
  if (diagnostics.State === 'Faulted') return { label: 'failed', color: 'red' };
  if (diagnostics.RolledBack) return { label: 'rolled back', color: 'red' };
  if (diagnostics.Success === true) return { label: 'committed', color: 'green' };
  if (diagnostics.Success === false) return { label: 'failed', color: 'red' };
  return { label: 'transaction', color: 'blue' };
};

const statusColor = (code: number): string => {
  if (code >= 200 && code < 300) return 'green';
  if (code >= 300 && code < 400) return 'blue';
  if (code >= 400 && code < 500) return 'gold';
  return 'red';
};

const methodColor = (method: string): string => {
  switch (method.toUpperCase()) {
    case 'GET':
      return 'green';
    case 'POST':
      return 'blue';
    case 'PUT':
    case 'PATCH':
      return 'gold';
    case 'DELETE':
      return 'red';
    default:
      return 'default';
  }
};

const percentile = (values: number[], pct: number): number => {
  if (values.length === 0) return 0;
  const sorted = [...values].sort((a, b) => a - b);
  const index = Math.min(sorted.length - 1, Math.ceil((pct / 100) * sorted.length) - 1);
  return sorted[index];
};

const StatValue = ({ children }: { children: React.ReactNode }) => (
  <span data-testid="request-history-stat-value" style={statBubbleStyle}>
    {children}
  </span>
);

type Props = {
  tenantScope?: string;
  mode: 'admin' | 'tenant';
};

const RequestHistoryPage: React.FC<Props> = ({ tenantScope, mode }) => {
  const [page, setPage] = useState(0);
  const [pageSize, setPageSize] = useState(25);
  const [method, setMethod] = useState<string>('');
  const [statusCode, setStatusCode] = useState<string>('');
  const [outcome, setOutcome] = useState<string>('');
  const [transactionFilter, setTransactionFilter] = useState<string>('');
  const [transactionId, setTransactionId] = useState<string>('');
  const [path, setPath] = useState<string>('');
  const [result, setResult] = useState<RequestHistorySearchResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [refreshKey, setRefreshKey] = useState(0);
  const [selected, setSelected] = useState<RequestHistoryEntry | null>(null);
  const [jsonViewRecord, setJsonViewRecord] = useState<RequestHistoryEntry | null>(null);
  const [detailOpen, setDetailOpen] = useState(false);
  const metricsEndpointUrl = useMemo(() => getMetricsEndpointUrl(), []);

  const fetchList = useCallback(() => {
    setLoading(true);
    listRequestHistory({
      page,
      pageSize,
      method: method || undefined,
      statusCode: statusCode ? Number(statusCode) : undefined,
      success: outcome ? outcome === 'true' : undefined,
      hasTransactionDiagnostics: transactionFilter
        ? transactionFilter === 'true'
        : undefined,
      transactionId: transactionId || undefined,
      path: path || undefined,
      tenantGuid: mode === 'tenant' ? tenantScope : undefined,
    })
      .then((data) => setResult(data))
      .catch(() => {
        setResult(null);
        toast.error('Unable to load request history', { id: globalToastId });
      })
      .finally(() => setLoading(false));
  }, [
    page,
    pageSize,
    method,
    statusCode,
    outcome,
    transactionFilter,
    transactionId,
    path,
    mode,
    tenantScope,
    refreshKey,
  ]);

  useEffect(() => {
    fetchList();
  }, [fetchList]);

  const onDelete = async (id: string) => {
    try {
      await deleteRequestHistory(id);
      toast.success('Entry deleted', { id: globalToastId });
      setRefreshKey((n) => n + 1);
    } catch {
      toast.error('Unable to delete', { id: globalToastId });
    }
  };

  const onView = (entry: RequestHistoryEntry) => {
    setSelected(entry);
    setDetailOpen(true);
  };

  const confirmDelete = (entry: RequestHistoryEntry) => {
    Modal.confirm({
      title: 'Delete request history entry?',
      content: entry.Path,
      okText: 'Delete',
      cancelText: 'Cancel',
      okButtonProps: { danger: true },
      maskClosable: true,
      onOk: () => onDelete(entry.GUID),
    });
  };

  const columns: ColumnsType<RequestHistoryEntry> = useMemo(
    () => [
      {
        title: 'Time',
        dataIndex: 'CreatedUtc',
        width: 170,
        onHeaderCell: () => ({ style: noWrapStyle }),
        render: (v: string) => (
          <span data-testid="request-history-time" style={noWrapStyle}>
            {new Date(v).toLocaleString()}
          </span>
        ),
      },
      {
        title: 'Method',
        dataIndex: 'Method',
        width: 80,
        onHeaderCell: () => ({ style: noWrapStyle }),
        render: (v: string) => (
          <Tag color={methodColor(v)} data-testid="request-history-method" style={noWrapStyle}>
            {v}
          </Tag>
        ),
      },
      {
        title: 'Path',
        dataIndex: 'Path',
        ellipsis: true,
        render: (v: string) => <Text code>{v}</Text>,
      },
      {
        title: 'Status',
        dataIndex: 'StatusCode',
        width: 80,
        render: (v: number) => <Tag color={statusColor(v)}>{v}</Tag>,
      },
      {
        title: 'Transaction',
        dataIndex: 'TransactionDiagnosticsJson',
        width: 190,
        render: (v?: string | null) => {
          const diagnostics = parseTransactionDiagnostics(v);
          if (!diagnostics) return <Text type="secondary">-</Text>;

          const state = transactionState(diagnostics);
          const shortId = diagnostics.TransactionId
            ? diagnostics.TransactionId.substring(0, 8)
            : 'transaction';

          return (
            <Space direction="vertical" size={2}>
              <Text code style={{ fontSize: 11 }} data-testid="request-history-transaction-id">
                {shortId}
              </Text>
              <Space size={4} wrap>
                <Tag
                  color={state.color}
                  style={noWrapStyle}
                  data-testid="request-history-transaction-state"
                >
                  {state.label}
                </Tag>
                {diagnostics.IsolationLevel && (
                  <Tag style={noWrapStyle}>{diagnostics.IsolationLevel}</Tag>
                )}
                {diagnostics.Provider && <Tag style={noWrapStyle}>{diagnostics.Provider}</Tag>}
                {diagnostics.ProviderErrorCode && (
                  <Tag style={noWrapStyle}>{diagnostics.ProviderErrorCode}</Tag>
                )}
                {diagnostics.Retryable && <Tag color="purple">retryable</Tag>}
                {diagnostics.ConcurrencyConflict && <Tag color="red">conflict</Tag>}
              </Space>
            </Space>
          );
        },
      },
      {
        title: 'Duration',
        dataIndex: 'ProcessingTimeMs',
        width: 100,
        render: (v: number) => `${v.toFixed(1)} ms`,
      },
      {
        title: 'Size (req/resp)',
        width: 140,
        render: (_: unknown, r: RequestHistoryEntry) =>
          `${r.RequestBodyLength} / ${r.ResponseBodyLength} B`,
      },
      ...(mode === 'admin'
        ? [
            {
              title: 'Tenant',
              dataIndex: 'TenantGUID',
              width: 160,
              render: (v?: string | null) =>
                v ? (
                  <Text code style={{ fontSize: 11 }}>
                    {v.substring(0, 8)}
                  </Text>
                ) : (
                  '-'
                ),
            },
          ]
        : []),
      {
        title: 'Actions',
        width: 90,
        fixed: 'right' as const,
        render: (_: unknown, r: RequestHistoryEntry) => {
          const items = [
            {
              key: 'view',
              label: 'View',
              icon: <EyeOutlined />,
              onClick: () => onView(r),
            },
            {
              key: 'view-json',
              label: 'View JSON',
              icon: <CodeOutlined />,
              onClick: () => setJsonViewRecord(r),
            },
            {
              key: 'delete',
              label: 'Delete',
              icon: <DeleteOutlined />,
              danger: true,
              onClick: () => confirmDelete(r),
            },
          ];

          return (
            <span data-row-click-ignore="true" onClick={(event) => event.stopPropagation()}>
              <Dropdown menu={{ items }} trigger={['click']} placement="bottomRight">
                <Button
                  size="small"
                  type="text"
                  aria-label="Request actions"
                  icon={<MoreOutlined style={{ fontSize: 18 }} />}
                />
              </Dropdown>
            </span>
          );
        },
      },
    ],
    [mode]
  );

  const visibleStats = useMemo(() => {
    const rows = result?.Objects || [];
    const durations = rows.map((entry) => entry.ProcessingTimeMs).filter((value) => value >= 0);
    const failures = rows.filter((entry) => !entry.Success).length;
    const averageDuration =
      durations.length === 0
        ? 0
        : durations.reduce((sum, value) => sum + value, 0) / durations.length;

    return {
      total: rows.length,
      failures,
      errorRate: rows.length === 0 ? 0 : (failures / rows.length) * 100,
      averageDuration,
      p95Duration: percentile(durations, 95),
    };
  }, [result]);

  return (
    <PageContainer id="request-history" pageTitle="Request History">
      <RequestHistoryChart
        tenantGuid={mode === 'tenant' ? tenantScope : undefined}
        refreshKey={refreshKey}
      />

      <Alert
        type="info"
        showIcon
        style={{ marginBottom: 12 }}
        message="Operational telemetry"
        description={
          <Space size="middle" wrap>
            <Typography.Link href={metricsEndpointUrl} target="_blank" rel="noreferrer">
              Prometheus metrics
            </Typography.Link>
            <Typography.Link
              href="https://opentelemetry.io/docs/"
              target="_blank"
              rel="noreferrer"
            >
              OpenTelemetry setup
            </Typography.Link>
            <Text type="secondary">
              Request history is for recent request inspection; metrics and traces are for
              aggregate monitoring.
            </Text>
          </Space>
        }
      />

      <div style={summaryToolbarStyle}>
        <div
          data-testid="request-history-observability-summary"
          style={summaryStatsStyle}
        >
          <Text>
            Visible requests: <StatValue>{visibleStats.total.toLocaleString()}</StatValue>
          </Text>
          <Text>
            Visible errors: <StatValue>{visibleStats.failures.toLocaleString()}</StatValue>
          </Text>
          <Text>
            Error rate: <StatValue>{visibleStats.errorRate.toFixed(1)}%</StatValue>
          </Text>
          <Text>
            Average duration:{' '}
            <StatValue>{visibleStats.averageDuration.toFixed(1)} ms</StatValue>
          </Text>
          <Text>
            P95 duration: <StatValue>{visibleStats.p95Duration.toFixed(1)} ms</StatValue>
          </Text>
        </div>

        <div data-testid="request-history-filter-controls" style={filterControlsStyle}>
          <Select
            style={{ width: 140 }}
            value={method}
            onChange={(v) => {
              setMethod(v);
              setPage(0);
            }}
            options={METHOD_OPTIONS}
          />
          <Select
            style={{ width: 140 }}
            value={statusCode}
            onChange={(v) => {
              setStatusCode(v);
              setPage(0);
            }}
            options={STATUS_OPTIONS}
          />
          <Select
            style={{ width: 150 }}
            value={outcome}
            onChange={(v) => {
              setOutcome(v);
              setPage(0);
            }}
            options={OUTCOME_OPTIONS}
          />
          <Select
            style={{ width: 170 }}
            value={transactionFilter}
            onChange={(v) => {
              setTransactionFilter(v);
              setPage(0);
            }}
            options={TRANSACTION_OPTIONS}
          />
          <Input
            placeholder="Transaction ID..."
            value={transactionId}
            onChange={(e) => setTransactionId(e.target.value)}
            onPressEnter={() => setPage(0)}
            style={{ width: 220, maxWidth: '100%' }}
            allowClear
          />
          <Input
            placeholder="Path contains..."
            value={path}
            onChange={(e) => setPath(e.target.value)}
            onPressEnter={() => setPage(0)}
            style={{ width: 280, maxWidth: '100%' }}
            allowClear
          />
        </div>
      </div>

      <LitegraphTable<RequestHistoryEntry>
        loading={loading}
        columns={columns}
        dataSource={result?.Objects || []}
        rowKey="GUID"
        size="small"
        scroll={{ x: 1000 }}
        onRow={(record) => ({
          onClick: (e) => {
            const target = e.target;
            if (
              target instanceof Element &&
              target.closest('button, .ant-popover, [data-row-click-ignore="true"]')
            ) {
              return;
            }
            onView(record);
          },
          style: { cursor: 'pointer' },
        })}
        onRefresh={() => setRefreshKey((n) => n + 1)}
        isRefreshing={loading}
        pagination={{
          current: page + 1,
          pageSize,
          total: result?.TotalCount || 0,
          showSizeChanger: true,
          pageSizeOptions: ['10', '25', '50', '100'],
          onChange: (p, ps) => {
            setPage(p - 1);
            setPageSize(ps);
          },
        }}
      />

      <RequestHistoryDetailModal
        entry={selected}
        open={detailOpen}
        onClose={() => setDetailOpen(false)}
      />
      <ViewJsonModal
        open={!!jsonViewRecord}
        onClose={() => setJsonViewRecord(null)}
        data={jsonViewRecord}
        title="Request History Entry JSON"
      />
    </PageContainer>
  );
};

export default RequestHistoryPage;
