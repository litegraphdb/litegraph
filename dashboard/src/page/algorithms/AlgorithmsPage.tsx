'use client';
import { useState } from 'react';
import { useTranslations } from 'next-intl';
import { Card, Col, Input, InputNumber, Row, Select, Space, Switch, Table, Tag } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import toast from 'react-hot-toast';
import PageContainer from '@/components/base/pageContainer/PageContainer';
import LitegraphButton from '@/components/base/button/Button';
import LitegraphText from '@/components/base/typograpghy/Text';
import { useSelectedGraph, useSelectedTenant } from '@/hooks/entityHooks';
import {
  exportGraphProjection,
  generateEmbeddings,
  importAlgorithmResults,
  runAlgorithm,
  type GraphAlgorithmNodeResult,
  type GraphAlgorithmRequest,
  type GraphAlgorithmResult,
  type GraphAlgorithmType,
  type GraphExportAttributeLevel,
  type GraphExportFormat,
} from '@/lib/sdk/algorithms';

const ALGORITHM_TYPES: GraphAlgorithmType[] = [
  'DegreeCentrality',
  'PageRank',
  'ClosenessCentrality',
  'EigenvectorCentrality',
  'BetweennessCentrality',
  'WeaklyConnectedComponents',
  'StronglyConnectedComponents',
  'LabelPropagation',
  'Louvain',
  'ClusteringCoefficient',
  'KCore',
];

const EXPORT_FORMATS: GraphExportFormat[] = ['NodeLinkJson', 'EdgeList', 'Graphml'];
const ATTRIBUTE_LEVELS: GraphExportAttributeLevel[] = ['None', 'Meta', 'Full'];

const isCommunityAlgorithm = (type: GraphAlgorithmType): boolean =>
  type === 'WeaklyConnectedComponents' ||
  type === 'StronglyConnectedComponents' ||
  type === 'LabelPropagation' ||
  type === 'Louvain';

// A fixed, colorblind-friendly palette for community tags; cycles for large community counts.
const COMMUNITY_COLORS = [
  '#4E79A7',
  '#F28E2B',
  '#59A14F',
  '#E15759',
  '#B07AA1',
  '#76B7B2',
  '#EDC948',
  '#FF9DA7',
  '#9C755F',
  '#BAB0AC',
];

const communityColor = (community: number): string =>
  COMMUNITY_COLORS[((community % COMMUNITY_COLORS.length) + COMMUNITY_COLORS.length) % COMMUNITY_COLORS.length];

const AlgorithmsPage = () => {
  const t = useTranslations('algorithms');
  const graphGuid = useSelectedGraph();
  const tenant = useSelectedTenant();
  const tenantGuid = tenant?.GUID || '';

  const [algorithmType, setAlgorithmType] = useState<GraphAlgorithmType>('PageRank');
  const [dampingFactor, setDampingFactor] = useState<number>(0.85);
  const [maxIterations, setMaxIterations] = useState<number>(100);
  const [tolerance, setTolerance] = useState<number>(0.000001);
  const [maxResults, setMaxResults] = useState<number | null>(null);
  const [writeBack, setWriteBack] = useState<boolean>(false);
  const [writeBackProperty, setWriteBackProperty] = useState<string>('');
  const [running, setRunning] = useState<boolean>(false);
  const [result, setResult] = useState<GraphAlgorithmResult | null>(null);

  const [exportFormat, setExportFormat] = useState<GraphExportFormat>('NodeLinkJson');
  const [attributes, setAttributes] = useState<GraphExportAttributeLevel>('Meta');
  const [exporting, setExporting] = useState<boolean>(false);

  const [importText, setImportText] = useState<string>('');
  const [importing, setImporting] = useState<boolean>(false);

  const [embedding, setEmbedding] = useState<boolean>(false);

  const ready = Boolean(tenantGuid && graphGuid);

  const describeError = (error: unknown): string =>
    error instanceof Error ? error.message : String(error);

  const handleRun = async () => {
    if (!ready) {
      toast.error(t('selectGraphFirst'));
      return;
    }
    const request: GraphAlgorithmRequest = {
      AlgorithmType: algorithmType,
      DampingFactor: dampingFactor,
      MaxIterations: maxIterations,
      Tolerance: tolerance,
      MaxResults: maxResults,
      WriteBack: writeBack,
      WriteBackProperty: writeBackProperty ? writeBackProperty : null,
    };
    setRunning(true);
    try {
      const response = await runAlgorithm(tenantGuid, graphGuid, request);
      setResult(response);
      toast.success(t('runSuccess'));
    } catch (error) {
      toast.error(`${t('error')}: ${describeError(error)}`);
    } finally {
      setRunning(false);
    }
  };

  const handleExport = async () => {
    if (!ready) {
      toast.error(t('selectGraphFirst'));
      return;
    }
    setExporting(true);
    try {
      const text = await exportGraphProjection(tenantGuid, graphGuid, exportFormat, attributes);
      const blob = new Blob([text], { type: 'application/octet-stream' });
      const link = document.createElement('a');
      link.href = URL.createObjectURL(blob);
      link.download = `graph-${graphGuid}-${exportFormat}.txt`;
      link.click();
      URL.revokeObjectURL(link.href);
      toast.success(t('exportSuccess'));
    } catch (error) {
      toast.error(`${t('error')}: ${describeError(error)}`);
    } finally {
      setExporting(false);
    }
  };

  const handleImport = async () => {
    if (!ready) {
      toast.error(t('selectGraphFirst'));
      return;
    }
    let values: Record<string, Record<string, number>>;
    try {
      const parsed = JSON.parse(importText);
      if (parsed === null || typeof parsed !== 'object' || Array.isArray(parsed)) {
        throw new Error('not-an-object');
      }
      values = parsed as Record<string, Record<string, number>>;
    } catch {
      toast.error(t('importInvalid'));
      return;
    }
    setImporting(true);
    try {
      const response = await importAlgorithmResults(tenantGuid, graphGuid, { Values: values });
      toast.success(`${t('importSuccess')} (${response.NodesUpdated})`);
    } catch (error) {
      toast.error(`${t('error')}: ${describeError(error)}`);
    } finally {
      setImporting(false);
    }
  };

  const community = Boolean(result && isCommunityAlgorithm(result.AlgorithmType));
  let maxScore = 0;
  if (result) {
    for (const node of result.Nodes) if (node.Score > maxScore) maxScore = node.Score;
  }

  const handleGenerateEmbeddings = async () => {
    if (!ready) {
      toast.error(t('selectGraphFirst'));
      return;
    }
    setEmbedding(true);
    try {
      const response = await generateEmbeddings(tenantGuid, graphGuid, {});
      toast.success(`${t('embeddingsSuccess')} (${response.NodesEmbedded})`);
    } catch (error) {
      toast.error(`${t('error')}: ${describeError(error)}`);
    } finally {
      setEmbedding(false);
    }
  };

  const columns: ColumnsType<GraphAlgorithmNodeResult> = [
    { title: t('node'), dataIndex: 'Name', key: 'name', render: (name: string | null, row) => name || row.NodeGUID },
  ];
  if (community) {
    columns.push({
      title: t('community'),
      dataIndex: 'Community',
      key: 'community',
      render: (value: number | null) =>
        value == null ? null : <Tag color={communityColor(value)}>{value}</Tag>,
    });
  } else {
    columns.push({
      title: t('score'),
      dataIndex: 'Score',
      key: 'score',
      render: (score: number) => (
        <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
          <div
            style={{
              width: maxScore > 0 ? `${Math.max(2, (score / maxScore) * 100)}px` : '0px',
              maxWidth: 100,
              height: 8,
              backgroundColor: '#4E79A7',
              borderRadius: 2,
            }}
          />
          <span>{score.toFixed(6)}</span>
        </div>
      ),
    });
  }
  if (result && result.AlgorithmType === 'DegreeCentrality') {
    columns.push({ title: t('edgesIn'), dataIndex: 'EdgesIn', key: 'edgesIn' });
    columns.push({ title: t('edgesOut'), dataIndex: 'EdgesOut', key: 'edgesOut' });
  }

  return (
    <PageContainer id="algorithms-page">
      <Space direction="vertical" size="middle" style={{ width: '100%' }}>
        <LitegraphText fontSize={16} weight={600}>
          {t('title')}
        </LitegraphText>
        <LitegraphText>{t('subtitle')}</LitegraphText>
        {!ready && <LitegraphText>{t('selectGraphFirst')}</LitegraphText>}

        <Card title={t('parameters')}>
          <Row gutter={[16, 16]}>
            <Col xs={24} md={8}>
              <LitegraphText>{t('algorithm')}</LitegraphText>
              <Select<GraphAlgorithmType>
                value={algorithmType}
                onChange={setAlgorithmType}
                style={{ width: '100%' }}
                options={ALGORITHM_TYPES.map((value) => ({ value, label: value }))}
                data-testid="algorithm-select"
              />
            </Col>
            <Col xs={24} md={8}>
              <LitegraphText>{t('dampingFactor')}</LitegraphText>
              <InputNumber
                value={dampingFactor}
                min={0}
                max={1}
                step={0.05}
                onChange={(value) => setDampingFactor(value ?? 0.85)}
                style={{ width: '100%' }}
              />
            </Col>
            <Col xs={24} md={8}>
              <LitegraphText>{t('maxIterations')}</LitegraphText>
              <InputNumber
                value={maxIterations}
                min={1}
                onChange={(value) => setMaxIterations(value ?? 100)}
                style={{ width: '100%' }}
              />
            </Col>
            <Col xs={24} md={8}>
              <LitegraphText>{t('tolerance')}</LitegraphText>
              <InputNumber
                value={tolerance}
                min={0}
                step={0.000001}
                onChange={(value) => setTolerance(value ?? 0.000001)}
                style={{ width: '100%' }}
              />
            </Col>
            <Col xs={24} md={8}>
              <LitegraphText>{t('maxResults')}</LitegraphText>
              <InputNumber
                value={maxResults ?? undefined}
                min={1}
                onChange={(value) => setMaxResults(value ?? null)}
                style={{ width: '100%' }}
              />
            </Col>
            <Col xs={24} md={8}>
              <LitegraphText>{t('writeBack')}</LitegraphText>
              <div>
                <Switch checked={writeBack} onChange={setWriteBack} data-testid="writeback-switch" />
              </div>
            </Col>
            {writeBack && (
              <Col xs={24} md={8}>
                <LitegraphText>{t('writeBackProperty')}</LitegraphText>
                <Input
                  value={writeBackProperty}
                  onChange={(event) => setWriteBackProperty(event.target.value)}
                />
              </Col>
            )}
          </Row>
          <div style={{ marginTop: 16 }}>
            <LitegraphButton type="primary" loading={running} disabled={!ready} onClick={handleRun}>
              {running ? t('running') : t('run')}
            </LitegraphButton>
          </div>
        </Card>

        {result && (
          <Card title={t('results')}>
            <Space wrap size="large" style={{ marginBottom: 16 }}>
              <LitegraphText>{`${t('nodeCount')}: ${result.NodeCount}`}</LitegraphText>
              <LitegraphText>{`${t('edgeCount')}: ${result.EdgeCount}`}</LitegraphText>
              <LitegraphText>{`${t('iterations')}: ${result.Iterations}`}</LitegraphText>
              <LitegraphText>{`${t('converged')}: ${String(result.Converged)}`}</LitegraphText>
              {result.CommunityCount != null && (
                <LitegraphText>{`${t('communityCount')}: ${result.CommunityCount}`}</LitegraphText>
              )}
              <LitegraphText>{`${t('computeMs')}: ${result.ComputeMs.toFixed(2)}`}</LitegraphText>
            </Space>
            <Table<GraphAlgorithmNodeResult>
              rowKey="NodeGUID"
              columns={columns}
              dataSource={result.Nodes}
              pagination={{ pageSize: 25 }}
              size="small"
            />
          </Card>
        )}

        <Card title={t('exportSection')}>
          <Space wrap size="middle">
            <Select<GraphExportFormat>
              value={exportFormat}
              onChange={setExportFormat}
              style={{ width: 180 }}
              options={EXPORT_FORMATS.map((value) => ({ value, label: value }))}
            />
            <Select<GraphExportAttributeLevel>
              value={attributes}
              onChange={setAttributes}
              style={{ width: 140 }}
              options={ATTRIBUTE_LEVELS.map((value) => ({ value, label: value }))}
            />
            <LitegraphButton loading={exporting} disabled={!ready} onClick={handleExport}>
              {t('export')}
            </LitegraphButton>
          </Space>
        </Card>

        <Card title={t('embeddingsSection')}>
          <Space direction="vertical" style={{ width: '100%' }}>
            <LitegraphText>{t('embeddingsHint')}</LitegraphText>
            <LitegraphButton loading={embedding} disabled={!ready} onClick={handleGenerateEmbeddings}>
              {t('generateEmbeddings')}
            </LitegraphButton>
          </Space>
        </Card>

        <Card title={t('importSection')}>
          <Space direction="vertical" style={{ width: '100%' }}>
            <Input.TextArea
              rows={4}
              value={importText}
              placeholder={t('importPlaceholder')}
              onChange={(event) => setImportText(event.target.value)}
            />
            <LitegraphButton
              loading={importing}
              disabled={!ready || !importText}
              onClick={handleImport}
            >
              {t('import')}
            </LitegraphButton>
          </Space>
        </Card>
      </Space>
    </PageContainer>
  );
};

export default AlgorithmsPage;
