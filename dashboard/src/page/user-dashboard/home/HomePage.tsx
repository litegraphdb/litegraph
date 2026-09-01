'use client';
import { useTranslations } from 'next-intl';
import FallBack from '@/components/base/fallback/FallBack';
import { defaultEdgeTooltip, defaultNodeTooltip } from '@/components/base/graph/constant';
import { GraphEdgeTooltip, GraphNodeTooltip } from '@/components/base/graph/types';
import PageLoading from '@/components/base/loading/PageLoading';
import PageContainer from '@/components/base/pageContainer/PageContainer';
import { useLayoutContext } from '@/components/layout/context';
import dynamic from 'next/dynamic';
import { useCallback, useRef, useState } from 'react';
import LitegraphButton from '@/components/base/button/Button';
import LitegraphFlex from '@/components/base/flex/Flex';
import { PlusSquareOutlined, ReloadOutlined } from '@ant-design/icons';
import { useAppSelector } from '@/lib/store/hooks';
import { RootState } from '@/lib/store/store';
import { useMemo } from 'react';
import LitegraphText from '@/components/base/typograpghy/Text';
import { useAppDynamicNavigation } from '@/hooks/hooks';
import { paths } from '@/constants/constant';
import HomeOverview from './HomeOverview';

const GraphViewer = dynamic(() => import('@/components/base/graph/GraphViewer'), {
  ssr: false,
});

const HomePage = () => {
  const t = useTranslations('home');
  const tCommon = useTranslations('common');
  const selectedGraphRedux = useAppSelector((state: RootState) => state.liteGraph.selectedGraph);
  const [nodeTooltip, setNodeTooltip] = useState<GraphNodeTooltip>(defaultNodeTooltip);
  const [edgeTooltip, setEdgeTooltip] = useState<GraphEdgeTooltip>(defaultEdgeTooltip);
  const [controlsPortalTarget, setControlsPortalTarget] = useState<HTMLDivElement | null>(null);

  // Modal state management
  const [isAddEditNodeVisible, setIsAddEditNodeVisible] = useState<boolean>(false);
  const [isAddEditEdgeVisible, setIsAddEditEdgeVisible] = useState<boolean>(false);
  const refetchFnRef = useRef<(() => void) | null>(null);
  const [refetchReady, setRefetchReady] = useState(false);

  const handleRefetchReady = useCallback((refetch: () => void) => {
    refetchFnRef.current = refetch;
    setRefetchReady(true);
  }, []);

  const { isGraphsLoading, graphError, refetchGraphs } = useLayoutContext();
  const { serializePath } = useAppDynamicNavigation();

  // Default observability locations for the bundled docker compose stack:
  // same host as the dashboard on each tool's default port.
  const observabilityHost = useMemo(() => {
    if (typeof window === 'undefined') return 'http://localhost';
    return `${window.location.protocol}//${window.location.hostname}`;
  }, []);

  if (isGraphsLoading) {
    return <PageLoading />;
  }

  if (graphError) {
    return (
      <FallBack retry={refetchGraphs}>
        {graphError ? tCommon('states.somethingWentWrong') : tCommon('states.cantViewDetails')}
      </FallBack>
    );
  }

  return (
    <PageContainer
      id="homepage"
      className="pb-0"
      pageTitle={t('pageTitle')}
      pageTitleRightContent={
        Boolean(selectedGraphRedux) ? (
          <LitegraphFlex gap={4}>
            <LitegraphButton
              type="link"
              icon={<ReloadOutlined />}
              onClick={() => refetchFnRef.current?.()}
              weight={600}
              disabled={!refetchReady}
            >
              {tCommon('actions.refresh')}
            </LitegraphButton>

            <div ref={setControlsPortalTarget} style={{ display: 'flex' }} />

            <LitegraphButton
              type="link"
              icon={<PlusSquareOutlined />}
              onClick={() => setIsAddEditNodeVisible(true)}
              weight={600}
            >
              {t('addNode')}
            </LitegraphButton>

            <LitegraphButton
              type="link"
              icon={<PlusSquareOutlined />}
              onClick={() => setIsAddEditEdgeVisible(true)}
              weight={600}
            >
              {t('addEdge')}
            </LitegraphButton>
          </LitegraphFlex>
        ) : undefined
      }
    >
      <HomeOverview />
      <div data-testid="graph-viewer">
        <GraphViewer
          isAddEditNodeVisible={isAddEditNodeVisible}
          setIsAddEditNodeVisible={setIsAddEditNodeVisible}
          nodeTooltip={nodeTooltip}
          edgeTooltip={edgeTooltip}
          setNodeTooltip={setNodeTooltip}
          setEdgeTooltip={setEdgeTooltip}
          isAddEditEdgeVisible={isAddEditEdgeVisible}
          setIsAddEditEdgeVisible={setIsAddEditEdgeVisible}
          onRefetchReady={handleRefetchReady}
          controlsPortalTarget={controlsPortalTarget}
        />
      </div>
      <LitegraphFlex
        align="center"
        gap={16}
        wrap="wrap"
        style={{ paddingBlock: 8 }}
        data-testid="home-observability-links"
      >
        <LitegraphText style={{ fontSize: 12, color: 'var(--ant-color-text-secondary)' }}>
          {t('observability.title')}
        </LitegraphText>
        <a
          href={`${observabilityHost}:3000`}
          target="_blank"
          rel="noreferrer"
          style={{ fontSize: 12 }}
          data-testid="home-link-grafana"
        >
          {t('observability.grafana')}
        </a>
        <a
          href={`${observabilityHost}:9090`}
          target="_blank"
          rel="noreferrer"
          style={{ fontSize: 12 }}
          data-testid="home-link-prometheus"
        >
          {t('observability.prometheus')}
        </a>
        <a
          href={serializePath(paths.requestHistory)}
          style={{ fontSize: 12 }}
          data-testid="home-link-requests"
        >
          {t('observability.requests')}
        </a>
      </LitegraphFlex>
    </PageContainer>
  );
};

export default HomePage;
