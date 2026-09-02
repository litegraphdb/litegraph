'use client';
import React, { useMemo } from 'react';
import Link from 'next/link';
import { Card, Statistic } from 'antd';
import {
  ApiOutlined,
  BranchesOutlined,
  DeploymentUnitOutlined,
  NodeIndexOutlined,
  ShareAltOutlined,
  TagsOutlined,
} from '@ant-design/icons';
import { useTranslations } from 'next-intl';
import LitegraphFlex from '@/components/base/flex/Flex';
import LitegraphText from '@/components/base/typograpghy/Text';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';
import { useAppSelector } from '@/lib/store/hooks';
import { RootState } from '@/lib/store/store';
import { useAppDynamicNavigation } from '@/hooks/hooks';
import { paths } from '@/constants/constant';
import {
  useEnumerateAndSearchEdgeQuery,
  useEnumerateAndSearchLabelQuery,
  useEnumerateAndSearchNodeQuery,
  useEnumerateAndSearchTagQuery,
  useEnumerateAndSearchVectorQuery,
  useGetAllGraphsQuery,
} from '@/lib/store/slice/slice';

const COUNT_REQUEST = { MaxResults: 1 } as any;

/** A single compact KPI card. */
const KpiCard = ({
  label,
  scope,
  scopeTooltip,
  value,
  loading,
}: {
  label: string;
  scope: string;
  scopeTooltip: string;
  value?: number;
  loading?: boolean;
}) => (
  <LitegraphTooltip title={scopeTooltip}>
    <Card
      size="small"
      style={{ flex: '1 1 150px', minWidth: 140 }}
      styles={{ body: { padding: '4px 10px' } }}
      data-testid="home-kpi"
    >
      <Statistic
        title={
          <span style={{ fontSize: 11, lineHeight: 1.1 }}>
            {label}{' '}
            <span style={{ color: 'var(--ant-color-text-tertiary)' }}>· {scope}</span>
          </span>
        }
        value={value ?? 0}
        loading={loading}
        valueStyle={{ fontSize: 15, fontWeight: 600, lineHeight: 1.1 }}
      />
    </Card>
  </LitegraphTooltip>
);

/** A single navigational CTA card. */
const CtaCard = ({
  href,
  icon,
  title,
  description,
}: {
  href: string;
  icon: React.ReactNode;
  title: string;
  description: string;
}) => (
  <Link href={href} style={{ flex: '1 1 200px', minWidth: 190, textDecoration: 'none' }}>
    <Card
      size="small"
      hoverable
      style={{ height: '100%' }}
      styles={{ body: { padding: '6px 10px' } }}
      data-testid="home-cta"
    >
      <LitegraphFlex align="center" gap={10}>
        <span style={{ fontSize: 18, color: 'var(--ant-color-primary)' }}>{icon}</span>
        <LitegraphFlex vertical gap={0}>
          <LitegraphText fontSize={13} weight={600} style={{ lineHeight: 1.25 }}>
            {title}
          </LitegraphText>
          <LitegraphText
            fontSize={11}
            style={{ color: 'var(--ant-color-text-tertiary)', lineHeight: 1.25 }}
          >
            {description}
          </LitegraphText>
        </LitegraphFlex>
      </LitegraphFlex>
    </Card>
  </Link>
);

/**
 * At-a-glance overview shown at the top of the tenant Home page: a row of small
 * KPI counters (graphs/nodes/edges/labels/tags/vectors) and a row of CTA cards
 * that jump to the most common workflows. Counts use the enumerate endpoints
 * with a minimal page size so only the totals are fetched.
 */
const HomeOverview = () => {
  const t = useTranslations('home.overview');
  const { serializePath } = useAppDynamicNavigation();
  const selectedGraph = useAppSelector((state: RootState) => state.liteGraph.selectedGraph);
  const graphScopeReady = Boolean(selectedGraph);

  const { data: graphsList, isFetching: graphsFetching } = useGetAllGraphsQuery();
  const { data: nodes, isFetching: nodesFetching } = useEnumerateAndSearchNodeQuery(
    { graphId: selectedGraph, request: COUNT_REQUEST },
    { skip: !graphScopeReady }
  );
  const { data: edges, isFetching: edgesFetching } = useEnumerateAndSearchEdgeQuery(
    { graphId: selectedGraph, request: COUNT_REQUEST },
    { skip: !graphScopeReady }
  );
  const { data: labels, isFetching: labelsFetching } = useEnumerateAndSearchLabelQuery(COUNT_REQUEST);
  const { data: tags, isFetching: tagsFetching } = useEnumerateAndSearchTagQuery(COUNT_REQUEST);
  const { data: vectors, isFetching: vectorsFetching } = useEnumerateAndSearchVectorQuery(COUNT_REQUEST);

  const inGraph = t('kpis.inSelectedGraph');
  const inTenant = t('kpis.inTenant');
  const graphScopeTooltip = t('kpis.graphScopeTooltip');
  const tenantScopeTooltip = t('kpis.tenantScopeTooltip');

  const kpis = useMemo(
    () => [
      { label: t('kpis.graphs'), scope: inTenant, scopeTooltip: tenantScopeTooltip, value: graphsList?.TotalRecords, loading: graphsFetching },
      { label: t('kpis.nodes'), scope: inGraph, scopeTooltip: graphScopeTooltip, value: nodes?.TotalRecords, loading: nodesFetching },
      { label: t('kpis.edges'), scope: inGraph, scopeTooltip: graphScopeTooltip, value: edges?.TotalRecords, loading: edgesFetching },
      { label: t('kpis.labels'), scope: inTenant, scopeTooltip: tenantScopeTooltip, value: labels?.TotalRecords, loading: labelsFetching },
      { label: t('kpis.tags'), scope: inTenant, scopeTooltip: tenantScopeTooltip, value: tags?.TotalRecords, loading: tagsFetching },
      { label: t('kpis.vectors'), scope: inTenant, scopeTooltip: tenantScopeTooltip, value: vectors?.TotalRecords, loading: vectorsFetching },
    ],
    [
      t, inGraph, inTenant, graphScopeTooltip, tenantScopeTooltip, graphsList, graphsFetching,
      nodes, nodesFetching, edges, edgesFetching,
      labels, labelsFetching, tags, tagsFetching, vectors, vectorsFetching,
    ]
  );

  const ctas = useMemo(
    () => [
      { href: serializePath(paths.graphs), icon: <DeploymentUnitOutlined />, title: t('cta.browseGraphs'), description: t('cta.browseGraphsDesc') },
      { href: serializePath(paths.nodes), icon: <NodeIndexOutlined />, title: t('cta.browseNodes'), description: t('cta.browseNodesDesc') },
      { href: serializePath(paths.edges), icon: <ShareAltOutlined />, title: t('cta.browseEdges'), description: t('cta.browseEdgesDesc') },
      { href: serializePath(paths.labels), icon: <TagsOutlined />, title: t('cta.metadata'), description: t('cta.metadataDesc') },
      { href: serializePath(paths.vectors), icon: <BranchesOutlined />, title: t('cta.vectorSearch'), description: t('cta.vectorSearchDesc') },
      { href: serializePath(paths.apiExplorer), icon: <ApiOutlined />, title: t('cta.apiExplorer'), description: t('cta.apiExplorerDesc') },
    ],
    [serializePath, t]
  );

  return (
    <div style={{ marginBottom: 16 }} data-testid="home-overview">
      <LitegraphFlex gap={12} wrap="wrap" style={{ marginBottom: 16 }}>
        {kpis.map((kpi) => (
          <KpiCard
            key={kpi.label}
            label={kpi.label}
            scope={kpi.scope}
            scopeTooltip={kpi.scopeTooltip}
            value={kpi.value}
            loading={kpi.loading}
          />
        ))}
      </LitegraphFlex>
      <LitegraphFlex gap={12} wrap="wrap">
        {ctas.map((cta) => (
          <CtaCard key={cta.title} href={cta.href || '#'} icon={cta.icon} title={cta.title} description={cta.description} />
        ))}
      </LitegraphFlex>
    </div>
  );
};

export default HomeOverview;
