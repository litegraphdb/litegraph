'use client';
import React, { useEffect, useMemo, useState } from 'react';
import { Alert, Button, Input, Select, Space, Tag, Typography, Tooltip, message } from 'antd';
import { SendOutlined, ReloadOutlined, ClearOutlined } from '@ant-design/icons';
import PageContainer from '@/components/base/pageContainer/PageContainer';
import CopyButton from '@/components/base/copy-button/CopyButton';
import { sdk } from '@/lib/sdk/litegraph.service';
import { flattenOpenApi, buildRequestUrl } from './openApi';
import { generateSnippet, CodeLanguage } from './codeSnippets';
import { getQueryErrorSummary, getTransactionFailureSummary } from './responseSummaries';
import { ApiOperation, ApiResponseState, RecentRequest } from './types';
import styles from './ApiExplorerPage.module.scss';

const { Text } = Typography;

const RESPONSE_TABS: Array<'preview' | 'body' | 'headers' | 'status' | 'code'> = [
  'preview',
  'body',
  'headers',
  'status',
  'code',
];

const CODE_LANGS: CodeLanguage[] = ['curl', 'javascript', 'csharp'];

const HISTORY_KEY = 'litegraph_api_explorer_history';
const MAX_HISTORY = 12;

const methodClass = (method: string): string => {
  switch (method.toUpperCase()) {
    case 'GET':
      return styles.methodGet;
    case 'POST':
      return styles.methodPost;
    case 'PUT':
      return styles.methodPut;
    case 'PATCH':
      return styles.methodPatch;
    case 'DELETE':
      return styles.methodDelete;
    case 'HEAD':
      return styles.methodHead;
    default:
      return '';
  }
};

const loadHistory = (): RecentRequest[] => {
  if (typeof window === 'undefined') return [];
  try {
    const raw = localStorage.getItem(HISTORY_KEY);
    if (!raw) return [];
    return JSON.parse(raw);
  } catch {
    return [];
  }
};

const saveHistory = (items: RecentRequest[]) => {
  try {
    localStorage.setItem(HISTORY_KEY, JSON.stringify(items));
  } catch {
    // ignore
  }
};

const ApiExplorerPage: React.FC = () => {
  const [operations, setOperations] = useState<ApiOperation[]>([]);
  const [selectedId, setSelectedId] = useState<string | undefined>(undefined);
  const [pathValues, setPathValues] = useState<Record<string, string>>({});
  const [queryValues, setQueryValues] = useState<Record<string, string>>({});
  const [headerValues, setHeaderValues] = useState<Record<string, string>>({});
  const [body, setBody] = useState<string>('');
  const [sending, setSending] = useState(false);
  const [response, setResponse] = useState<ApiResponseState | null>(null);
  const [activeTab, setActiveTab] = useState<(typeof RESPONSE_TABS)[number]>('preview');
  const [codeLang, setCodeLang] = useState<CodeLanguage>('curl');
  const [history, setHistory] = useState<RecentRequest[]>(() => loadHistory());

  useEffect(() => {
    const endpoint = sdk.config.endpoint || '';
    const base = endpoint.endsWith('/') ? endpoint.slice(0, -1) : endpoint;
    fetch(`${base}/openapi.json`)
      .then((r) => (r.ok ? r.json() : Promise.reject(r.status)))
      .then((data) => {
        setOperations(flattenOpenApi(data));
      })
      .catch(() => {
        message.error('Unable to load OpenAPI spec');
      });
  }, []);

  const selectedOperation = useMemo(
    () => operations.find((o) => o.id === selectedId),
    [operations, selectedId]
  );

  useEffect(() => {
    if (!selectedOperation) return;
    const newPath: Record<string, string> = {};
    const newQuery: Record<string, string> = {};
    for (const p of selectedOperation.parameters) {
      if (p.in === 'path') newPath[p.name] = '';
      if (p.in === 'query') newQuery[p.name] = '';
    }
    setPathValues(newPath);
    setQueryValues(newQuery);
    setHeaderValues({});
    setBody(selectedOperation.requestBodyExample || '');
    setResponse(null);
    setActiveTab('preview');
  }, [selectedOperation]);

  const baseUrl = useMemo(() => {
    const endpoint = sdk.config.endpoint || '';
    return endpoint.endsWith('/') ? endpoint.slice(0, -1) : endpoint;
  }, []);

  const computedUrl = useMemo(() => {
    if (!selectedOperation) return '';
    return buildRequestUrl(baseUrl, selectedOperation, pathValues, queryValues);
  }, [selectedOperation, pathValues, queryValues, baseUrl]);

  const buildHeaders = (): Record<string, string> => {
    const headers: Record<string, string> = { ...headerValues };
    const defaults = (sdk.config as unknown as { defaultHeaders?: Record<string, string> })
      .defaultHeaders;
    if (defaults) {
      for (const k of Object.keys(defaults)) {
        if (!(k in headers)) headers[k] = defaults[k];
      }
    }
    if (body && !('Content-Type' in headers) && !('content-type' in headers)) {
      headers['Content-Type'] = 'application/json';
    }
    return headers;
  };

  const onSend = async () => {
    if (!selectedOperation) return;
    setSending(true);
    setResponse(null);
    const url = computedUrl;
    const headers = buildHeaders();
    const t0 = performance.now();
    try {
      const resp = await fetch(url, {
        method: selectedOperation.method,
        headers,
        body: selectedOperation.hasRequestBody && body ? body : undefined,
      });
      const duration = performance.now() - t0;
      const text = await resp.text();
      let json: unknown = null;
      try {
        json = text ? JSON.parse(text) : null;
      } catch {
        json = null;
      }
      const respHeaders: Record<string, string> = {};
      resp.headers.forEach((v, k) => {
        respHeaders[k] = v;
      });
      setResponse({
        status: resp.status,
        statusText: resp.statusText,
        headers: respHeaders,
        contentType: resp.headers.get('content-type') || '',
        text,
        json,
        durationMs: duration,
        url,
      });

      const historyEntry: RecentRequest = {
        id: `${Date.now()}-${Math.random().toString(36).slice(2, 8)}`,
        method: selectedOperation.method,
        path: selectedOperation.path,
        url,
        timestamp: new Date().toISOString(),
        status: resp.status,
      };
      const next = [historyEntry, ...history].slice(0, MAX_HISTORY);
      setHistory(next);
      saveHistory(next);
    } catch (e) {
      message.error('Request failed: ' + (e instanceof Error ? e.message : String(e)));
    } finally {
      setSending(false);
    }
  };

  const codeSnippet = useMemo(() => {
    if (!selectedOperation) return '';
    return generateSnippet(codeLang, {
      method: selectedOperation.method,
      url: computedUrl,
      headers: buildHeaders(),
      body: selectedOperation.hasRequestBody && body ? body : undefined,
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [codeLang, selectedOperation, computedUrl, body, headerValues]);

  const previewText = useMemo(() => {
    if (!response) return '';
    if (response.json !== null && response.json !== undefined) {
      return JSON.stringify(response.json, null, 2);
    }
    return response.text;
  }, [response]);

  const transactionFailure = useMemo(() => {
    return response?.json ? getTransactionFailureSummary(response.json) : null;
  }, [response]);

  const queryError = useMemo(() => {
    return response?.json ? getQueryErrorSummary(response.json) : null;
  }, [response]);

  const grouped = useMemo(() => {
    const groups: Record<string, ApiOperation[]> = {};
    for (const op of operations) {
      if (!groups[op.tag]) groups[op.tag] = [];
      groups[op.tag].push(op);
    }
    return groups;
  }, [operations]);

  return (
    <PageContainer id="api-explorer" pageTitle="API Explorer">
      <div className={styles.grid}>
        <div className={styles.card}>
          <div className={styles.cardHeader}>
            <span className={styles.cardTitle}>Request</span>
            <Space>
              {selectedOperation && (
                <Tooltip title="Reset body to template">
                  <Button
                    size="small"
                    icon={<ReloadOutlined />}
                    onClick={() => setBody(selectedOperation.requestBodyExample || '')}
                  />
                </Tooltip>
              )}
              <Button
                size="small"
                type="primary"
                icon={<SendOutlined />}
                onClick={onSend}
                loading={sending}
                disabled={!selectedOperation}
              >
                Send
              </Button>
            </Space>
          </div>
          <div className={styles.cardBody}>
            <Select
              showSearch
              placeholder="Select an operation..."
              value={selectedId}
              onChange={(v) => setSelectedId(v)}
              style={{ width: '100%' }}
              options={Object.entries(grouped).map(([tag, ops]) => ({
                label: tag,
                options: ops.map((o) => ({
                  value: o.id,
                  label: `${o.method} ${o.path}`,
                })),
              }))}
              filterOption={(input, option) =>
                (option?.label as string).toLowerCase().includes(input.toLowerCase())
              }
            />

            {selectedOperation && (
              <>
                <div className={styles.endpointBox}>
                  <span
                    className={`${styles.methodBadge} ${methodClass(selectedOperation.method)}`}
                  >
                    {selectedOperation.method}
                  </span>
                  <Text code style={{ flex: 1, wordBreak: 'break-all' }}>
                    {computedUrl || selectedOperation.path}
                  </Text>
                  <CopyButton
                    text={computedUrl || selectedOperation.path}
                    tooltipTitle="Copy URL"
                  />
                </div>

                {selectedOperation.description && (
                  <Text type="secondary" style={{ fontSize: 12 }}>
                    {selectedOperation.description}
                  </Text>
                )}

                {selectedOperation.parameters.some((p) => p.in === 'path') && (
                  <>
                    <Text
                      strong
                      style={{ fontSize: 12, textTransform: 'uppercase', letterSpacing: 0.5 }}
                    >
                      Path parameters
                    </Text>
                    <div className={styles.paramGrid}>
                      {selectedOperation.parameters
                        .filter((p) => p.in === 'path')
                        .map((p) => (
                          <Input
                            key={p.name}
                            placeholder={p.name + (p.required ? ' *' : '')}
                            value={pathValues[p.name] || ''}
                            onChange={(e) =>
                              setPathValues((prev) => ({ ...prev, [p.name]: e.target.value }))
                            }
                          />
                        ))}
                    </div>
                  </>
                )}

                {selectedOperation.parameters.some((p) => p.in === 'query') && (
                  <>
                    <Text
                      strong
                      style={{ fontSize: 12, textTransform: 'uppercase', letterSpacing: 0.5 }}
                    >
                      Query parameters
                    </Text>
                    <div className={styles.paramGrid}>
                      {selectedOperation.parameters
                        .filter((p) => p.in === 'query')
                        .map((p) => (
                          <Input
                            key={p.name}
                            placeholder={p.name + (p.required ? ' *' : '')}
                            value={queryValues[p.name] || ''}
                            onChange={(e) =>
                              setQueryValues((prev) => ({ ...prev, [p.name]: e.target.value }))
                            }
                          />
                        ))}
                    </div>
                  </>
                )}

                {selectedOperation.hasRequestBody && (
                  <>
                    <Text
                      strong
                      style={{ fontSize: 12, textTransform: 'uppercase', letterSpacing: 0.5 }}
                    >
                      Request body
                    </Text>
                    <Input.TextArea
                      value={body}
                      onChange={(e) => setBody(e.target.value)}
                      rows={12}
                      style={{
                        fontFamily: "'Monaco', 'Menlo', 'Consolas', monospace",
                        fontSize: 12,
                      }}
                    />
                  </>
                )}
              </>
            )}

            {history.length > 0 && (
              <>
                <Space>
                  <Text
                    strong
                    style={{ fontSize: 12, textTransform: 'uppercase', letterSpacing: 0.5 }}
                  >
                    Recent Requests
                  </Text>
                  <Button
                    size="small"
                    type="text"
                    icon={<ClearOutlined />}
                    onClick={() => {
                      setHistory([]);
                      saveHistory([]);
                    }}
                  >
                    Clear
                  </Button>
                </Space>
                <div className={styles.recentList}>
                  {history.map((h) => (
                    <div
                      key={h.id}
                      className={styles.recentItem}
                      onClick={() => {
                        const op = operations.find(
                          (o) => o.method === h.method && o.path === h.path
                        );
                        if (op) setSelectedId(op.id);
                      }}
                    >
                      <span className={`${styles.methodBadge} ${methodClass(h.method)}`}>
                        {h.method}
                      </span>
                      <Text
                        style={{
                          flex: 1,
                          fontSize: 12,
                          overflow: 'hidden',
                          textOverflow: 'ellipsis',
                        }}
                      >
                        {h.path}
                      </Text>
                      {h.status !== undefined && (
                        <Tag color={h.status < 400 ? 'green' : 'red'}>{h.status}</Tag>
                      )}
                    </div>
                  ))}
                </div>
              </>
            )}
          </div>
        </div>

        <div className={styles.card}>
          <div className={styles.cardHeader}>
            <span className={styles.cardTitle}>Response</span>
            {response && (
              <Tag color={response.status < 400 ? 'green' : 'red'}>
                {response.status} {response.statusText}
              </Tag>
            )}
          </div>
          <div className={styles.tabBar}>
            {RESPONSE_TABS.map((t) => (
              <button
                key={t}
                type="button"
                className={`${styles.tab} ${activeTab === t ? styles.tabActive : ''}`}
                onClick={() => setActiveTab(t)}
              >
                {t.charAt(0).toUpperCase() + t.slice(1)}
              </button>
            ))}
          </div>
          <div className={styles.cardBody}>
            {transactionFailure && (
              <Alert
                type="error"
                showIcon
                message="Transaction failed"
                description={transactionFailure}
              />
            )}
            {queryError && (
              <Alert type="error" showIcon message="Query failed" description={queryError} />
            )}
            {activeTab === 'preview' && (
              <pre className={styles.codeBlock}>{previewText || '(no response yet)'}</pre>
            )}
            {activeTab === 'body' && (
              <pre className={styles.codeBlock}>{response?.text || '(no response yet)'}</pre>
            )}
            {activeTab === 'headers' && (
              <pre className={styles.codeBlock}>
                {response ? JSON.stringify(response.headers, null, 2) : '(no response yet)'}
              </pre>
            )}
            {activeTab === 'status' && response && (
              <div className={styles.statusGrid}>
                <div className={styles.statusCard}>
                  <div className={styles.statusCardLabel}>Status</div>
                  <div className={styles.statusCardValue}>
                    {response.status} {response.statusText}
                  </div>
                </div>
                <div className={styles.statusCard}>
                  <div className={styles.statusCardLabel}>Content-Type</div>
                  <div className={styles.statusCardValue}>{response.contentType || '-'}</div>
                </div>
                <div className={styles.statusCard}>
                  <div className={styles.statusCardLabel}>Duration</div>
                  <div className={styles.statusCardValue}>{response.durationMs.toFixed(1)} ms</div>
                </div>
                <div className={styles.statusCard}>
                  <div className={styles.statusCardLabel}>Size</div>
                  <div className={styles.statusCardValue}>{response.text.length} B</div>
                </div>
              </div>
            )}
            {activeTab === 'status' && !response && <Text type="secondary">(no response yet)</Text>}
            {activeTab === 'code' && (
              <>
                <Space style={{ marginBottom: 8 }}>
                  {CODE_LANGS.map((l) => (
                    <Button
                      key={l}
                      size="small"
                      type={codeLang === l ? 'primary' : 'default'}
                      onClick={() => setCodeLang(l)}
                    >
                      {l === 'curl' ? 'curl' : l === 'javascript' ? 'JavaScript' : 'C#'}
                    </Button>
                  ))}
                  <CopyButton
                    text={codeSnippet}
                    tooltipTitle="Copy code"
                    label="Copy"
                    size="small"
                    type="default"
                  />
                </Space>
                <pre className={styles.codeBlock}>{codeSnippet || '(select an operation)'}</pre>
              </>
            )}
          </div>
        </div>
      </div>
    </PageContainer>
  );
};

export default ApiExplorerPage;
