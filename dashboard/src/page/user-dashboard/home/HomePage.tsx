'use client';
import FallBack from '@/components/base/fallback/FallBack';
import { defaultEdgeTooltip, defaultNodeTooltip } from '@/components/base/graph/constant';
import { GraphEdgeTooltip, GraphNodeTooltip } from '@/components/base/graph/types';
import PageLoading from '@/components/base/loading/PageLoading';
import PageContainer from '@/components/base/pageContainer/PageContainer';
import { useLayoutContext } from '@/components/layout/context';
import dynamic from 'next/dynamic';
import { useState } from 'react';
import LitegraphButton from '@/components/base/button/Button';
import LitegraphFlex from '@/components/base/flex/Flex';
import { PlusSquareOutlined } from '@ant-design/icons';
import { useAppSelector } from '@/lib/store/hooks';
import { RootState } from '@/lib/store/store';

const GraphViewer = dynamic(() => import('@/components/base/graph/GraphViewer'), {
  ssr: false,
});

const HomePage = () => {
  const selectedGraphRedux = useAppSelector((state: RootState) => state.liteGraph.selectedGraph);
  const [nodeTooltip, setNodeTooltip] = useState<GraphNodeTooltip>(defaultNodeTooltip);
  const [edgeTooltip, setEdgeTooltip] = useState<GraphEdgeTooltip>(defaultEdgeTooltip);

  // Modal state management
  const [isAddEditNodeVisible, setIsAddEditNodeVisible] = useState<boolean>(false);
  const [isAddEditEdgeVisible, setIsAddEditEdgeVisible] = useState<boolean>(false);

  const { isGraphsLoading, graphError, refetchGraphs } = useLayoutContext();

  if (isGraphsLoading) {
    return <PageLoading />;
  }

  if (graphError) {
    return (
      <FallBack retry={refetchGraphs}>
        {graphError ? 'Something went wrong.' : "Can't view details at the moment."}
      </FallBack>
    );
  }

  return (
    <PageContainer
      id="homepage"
      className="pb-0"
      pageTitle={'Home'}
      pageTitleRightContent={
        Boolean(selectedGraphRedux) ? (
          <LitegraphFlex>
            <LitegraphButton
              type="link"
              icon={<PlusSquareOutlined />}
              onClick={() => setIsAddEditNodeVisible(true)}
              weight={600}
            >
              Add Node
            </LitegraphButton>

            <LitegraphButton
              type="link"
              icon={<PlusSquareOutlined />}
              onClick={() => setIsAddEditEdgeVisible(true)}
              weight={600}
            >
              Add Edge
            </LitegraphButton>
          </LitegraphFlex>
        ) : undefined
      }
    >
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
        />
      </div>
    </PageContainer>
  );
};

export default HomePage;
