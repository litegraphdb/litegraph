'use client';
import { Dispatch, SetStateAction, useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { createPortal } from 'react-dom';
import Graph2DViewer from './graph-2d/Graph2DViewer';
import '@react-sigma/core/lib/react-sigma.min.css';
import { useAppSelector } from '@/lib/store/hooks';
import { GraphEdgeTooltip, GraphNodeTooltip } from './types';
import NodeToolTip from './NodeToolTip';
import { RootState } from '@/lib/store/store';
import PageLoading from '../loading/PageLoading';
import EdgeToolTip from './EdgeTooltip';
import AddEditNode from '@/page/nodes/components/AddEditNode';
import AddEditEdge from '@/page/edges/components/AddEditEdge';
import FallBack, { FallBackEnums } from '../fallback/FallBack';
import styles from './graph.module.scss';
import { useGetSubGraphs, useLazyLoadEdgesAndNodes } from '@/hooks/entityHooks';
import GraphLoader3d from './GraphLoader3d';
import LitegraphFlex from '../flex/Flex';
import { Alert, Switch } from 'antd';
import ProgressBar from './ProgressBar';
import LitegraphTooltip from '../tooltip/Tooltip';
import ErrorBoundary from '@/hoc/ErrorBoundary';
import { getLegendsForNodes } from './utils';
import { MAX_NODES_TO_FETCH } from '@/constants/constant';
import { ControlOutlined, RedoOutlined, SearchOutlined } from '@ant-design/icons';
import LitegraphButton from '../button/Button';
import LitegraphDropdown from '../dropdown/Dropdown';
import NodeSearchModal from './NodeSearchModal';
import { Node } from 'litegraphdb/dist/types/types';
import { EdgeData } from '@/lib/graph/types';
import { NodeData } from '@/lib/graph/types';
import { defaultEdgeTooltip, defaultNodeTooltip } from './constant';
import { buildCollapsibleDisplayGraph, DEFAULT_COLLAPSE_THRESHOLD } from '@/lib/graph/smartLayout';

const GraphViewer = ({
  nodeTooltip,
  edgeTooltip,
  setNodeTooltip,
  setEdgeTooltip,
  isAddEditNodeVisible,
  setIsAddEditNodeVisible,
  isAddEditEdgeVisible,
  setIsAddEditEdgeVisible,
  onRefetchReady,
  controlsPortalTarget,
}: {
  nodeTooltip: GraphNodeTooltip;
  edgeTooltip: GraphEdgeTooltip;
  setNodeTooltip: Dispatch<SetStateAction<GraphNodeTooltip>>;
  setEdgeTooltip: Dispatch<SetStateAction<GraphEdgeTooltip>>;
  isAddEditNodeVisible: boolean;
  setIsAddEditNodeVisible: Dispatch<SetStateAction<boolean>>;
  isAddEditEdgeVisible: boolean;
  setIsAddEditEdgeVisible: Dispatch<SetStateAction<boolean>>;
  onRefetchReady?: (refetch: () => void) => void;
  controlsPortalTarget?: HTMLElement | null;
}) => {
  // Redux state for the list of graphs
  const [containerDivHeightAndWidth, setContainerDivHeightAndWidth] = useState<{
    height?: number;
    width?: number;
  }>({
    height: undefined,
    width: undefined,
  });
  const [show3d, setShow3d] = useState(false);
  const [selectedNodeGuid, setSelectedNodeGuid] = useState<string | null>(null);
  const [topologicalSortNodes, setTopologicalSortNodes] = useState(false);
  const [showGraphHorizontal, setShowGraphHorizontal] = useState(false);
  const [showGraphLegend, setShowGraphLegend] = useState(true);
  const [showMoreThanSupportedNodesWarning, setShowMoreThanSupportedNodesWarning] = useState(true);
  const [showLabel, setShowLabel] = useState(false);
  const [groupDragging, setGroupDragging] = useState(false);
  const [collapseRelatedNodes, setCollapseRelatedNodes] = useState(true);
  const [expandedClusterIds, setExpandedClusterIds] = useState<Set<string>>(new Set());
  const [isControlsOpen, setIsControlsOpen] = useState(false);
  const [isNodeSearchModalVisible, setIsNodeSearchModalVisible] = useState(false);
  const selectedGraphRedux = useAppSelector((state: RootState) => state.liteGraph.selectedGraph);
  const ref = useRef<HTMLDivElement>(null);
  const {
    nodes,
    edges,
    rawEdges,
    refetch,
    isError,
    nodesFirstResult,
    edgesFirstResult,
    isLoading: isGraphLoading,
    isNodesLoading,
    isEdgesLoading,
    updateLocalNode,
    addLocalNode,
    removeLocalNode,
    updateLocalEdge,
    addLocalEdge,
    removeLocalEdge,
    hasMoreThanSupportedNodes,
    isCyclic,
  } = useLazyLoadEdgesAndNodes(
    selectedGraphRedux,
    showGraphHorizontal,
    topologicalSortNodes,
    MAX_NODES_TO_FETCH
  );
  const { loadSubGraph, isSubGraphLoading, subGraphNodes, subGraphEdges } = useGetSubGraphs(
    selectedNodeGuid,
    topologicalSortNodes,
    showGraphHorizontal
  );
  const isLoading = isGraphLoading || isSubGraphLoading;
  const activeNodes = selectedNodeGuid ? subGraphNodes || [] : nodes;
  const activeEdges = selectedNodeGuid ? subGraphEdges || [] : edges;
  const legends = getLegendsForNodes(activeNodes);

  const displayGraph = useMemo(() => {
    if (!topologicalSortNodes) {
      return {
        nodes: activeNodes,
        edges: activeEdges,
        collapsibleClusterIds: [] as string[],
        collapsedClusterIds: [] as string[],
        expandedClusterIds: [] as string[],
      };
    }

    return buildCollapsibleDisplayGraph(activeNodes, activeEdges, {
      collapseThreshold: DEFAULT_COLLAPSE_THRESHOLD,
      collapseRelatedNodes,
      expandedClusterIds,
    });
  }, [activeNodes, activeEdges, topologicalSortNodes, collapseRelatedNodes, expandedClusterIds]);

  useEffect(() => {
    setShow3d(false);
  }, [selectedGraphRedux]);

  useEffect(() => {
    setExpandedClusterIds(new Set());
  }, [selectedGraphRedux, selectedNodeGuid, topologicalSortNodes, showGraphHorizontal]);

  useEffect(() => {
    if (!topologicalSortNodes || !collapseRelatedNodes) {
      setExpandedClusterIds(new Set());
    }
  }, [topologicalSortNodes, collapseRelatedNodes]);

  useEffect(() => {
    const handleResize = () => {
      setContainerDivHeightAndWidth({
        height: ref.current?.clientHeight,
        width: ref.current?.clientWidth,
      });
    };

    window.addEventListener('resize', handleResize);

    handleResize();

    return () => {
      window.removeEventListener('resize', handleResize);
    };
  }, []);

  const onRefetchReadyRef = useRef(onRefetchReady);
  onRefetchReadyRef.current = onRefetchReady;

  useEffect(() => {
    if (refetch && onRefetchReadyRef.current) {
      onRefetchReadyRef.current(refetch);
    }
  }, [refetch]);

  const handleNodeSelect = async (node: Node) => {
    // Handle node selection - you can add custom logic here
    // For example, focus on the node in the graph, show tooltip, etc.
    setSelectedNodeGuid(node.GUID);
  };

  const clearGraphTooltips = useCallback(() => {
    setNodeTooltip(defaultNodeTooltip);
    setEdgeTooltip(defaultEdgeTooltip);
  }, [setNodeTooltip, setEdgeTooltip]);

  const handleDisplayNodeClick = useCallback(
    (node: NodeData, position: { x: number; y: number }) => {
      if (node.nodeKind === 'group' && node.groupTargetId) {
        clearGraphTooltips();
        setExpandedClusterIds((currentExpandedClusterIds) => {
          const nextExpandedClusterIds = new Set(currentExpandedClusterIds);
          nextExpandedClusterIds.add(node.groupTargetId || '');
          return nextExpandedClusterIds;
        });
        return;
      }

      if (node.nodeKind === 'group-anchor' && node.groupTargetId) {
        clearGraphTooltips();
        setExpandedClusterIds((currentExpandedClusterIds) => {
          const nextExpandedClusterIds = new Set(currentExpandedClusterIds);
          nextExpandedClusterIds.delete(node.groupTargetId || '');
          return nextExpandedClusterIds;
        });
        return;
      }

      setEdgeTooltip(defaultEdgeTooltip);
      setNodeTooltip({
        visible: true,
        nodeId: node.id,
        x: position.x,
        y: position.y,
      });
    },
    [clearGraphTooltips, setNodeTooltip, setEdgeTooltip]
  );

  const handleDisplayEdgeClick = useCallback(
    (edge: EdgeData, position: { x: number; y: number }) => {
      if (edge.isSynthetic) {
        clearGraphTooltips();
        return;
      }

      setNodeTooltip(defaultNodeTooltip);
      setEdgeTooltip({
        visible: true,
        edgeId: edge.id,
        x: position.x,
        y: position.y,
      });
    },
    [clearGraphTooltips, setNodeTooltip, setEdgeTooltip]
  );

  const handleToggle3d = useCallback(
    (checked: boolean) => {
      setShow3d(checked);
      setNodeTooltip({ visible: false, nodeId: '', x: 0, y: 0 });
      setEdgeTooltip({ visible: false, edgeId: '', x: 0, y: 0 });
    },
    [setNodeTooltip, setEdgeTooltip]
  );

  const loadingIndicator = isNodesLoading ? (
    <div className={styles.progressContainer}>
      <ProgressBar
        loaded={nodes.length}
        total={Math.min(nodesFirstResult?.TotalRecords || 0, MAX_NODES_TO_FETCH)}
        label="Loading nodes..."
      />
    </div>
  ) : isEdgesLoading ? (
    <div className={styles.progressContainer}>
      <ProgressBar
        loaded={rawEdges.length}
        total={edgesFirstResult?.TotalRecords || 0}
        label="Loading edges..."
      />
    </div>
  ) : null;

  const controlsDropdown = selectedGraphRedux ? (
    <LitegraphDropdown
      open={isControlsOpen}
      onOpenChange={setIsControlsOpen}
      trigger={['click']}
      placement="bottomLeft"
      popupRender={() => (
        <div
          className={styles.controlsDropdown}
          onClick={(event) => {
            event.stopPropagation();
          }}
        >
          <LitegraphFlex vertical gap={10}>
            {selectedNodeGuid ? (
              <LitegraphButton
                type="text"
                icon={<RedoOutlined />}
                className={styles.controlsActionButton}
                onClick={() => {
                  setSelectedNodeGuid(null);
                  setIsControlsOpen(false);
                }}
              >
                Clear Sub Graph
              </LitegraphButton>
            ) : (
              <LitegraphButton
                type="text"
                icon={<SearchOutlined />}
                className={styles.controlsActionButton}
                onClick={() => {
                  setIsNodeSearchModalVisible(true);
                  setIsControlsOpen(false);
                }}
              >
                Search Sub Graph
              </LitegraphButton>
            )}
            <div className={styles.controlsDivider} />
            <LitegraphFlex align="center" justify="space-between" className={styles.controlRow}>
              <span>3D</span>
              <Switch
                disabled={isNodesLoading || isEdgesLoading}
                checked={show3d}
                onChange={handleToggle3d}
                size="small"
                data-testid="3d-switch"
              />
            </LitegraphFlex>
            {!show3d && (
              <>
                <LitegraphFlex align="center" justify="space-between" className={styles.controlRow}>
                  <span>Horizontal layout</span>
                  <Switch
                    size="small"
                    checked={showGraphHorizontal}
                    onChange={(checked) => setShowGraphHorizontal(checked)}
                  />
                </LitegraphFlex>
                <LitegraphFlex align="center" justify="space-between" className={styles.controlRow}>
                  <LitegraphTooltip
                    title={
                      isCyclic
                        ? 'Cycles are condensed automatically so the depth layout stays readable.'
                        : ''
                    }
                  >
                    <span>Depth-aware layout</span>
                  </LitegraphTooltip>
                  <Switch
                    size="small"
                    checked={topologicalSortNodes}
                    onChange={(checked) => setTopologicalSortNodes(checked)}
                  />
                </LitegraphFlex>
                {topologicalSortNodes && (
                  <LitegraphFlex
                    align="center"
                    justify="space-between"
                    className={styles.controlRow}
                  >
                    <LitegraphTooltip title="Click grouped nodes to expand. Groups are inferred from labels, tag keys, and connection similarity.">
                      <span>Collapse related nodes</span>
                    </LitegraphTooltip>
                    <Switch
                      size="small"
                      checked={collapseRelatedNodes}
                      onChange={(checked) => setCollapseRelatedNodes(checked)}
                    />
                  </LitegraphFlex>
                )}
                <LitegraphFlex align="center" justify="space-between" className={styles.controlRow}>
                  <span>Drag by label</span>
                  <Switch
                    size="small"
                    checked={groupDragging}
                    onChange={(checked) => setGroupDragging(checked)}
                  />
                </LitegraphFlex>
              </>
            )}
            <LitegraphFlex align="center" justify="space-between" className={styles.controlRow}>
              <span>Show graph legend</span>
              <Switch
                size="small"
                checked={showGraphLegend}
                onChange={(checked) => setShowGraphLegend(checked)}
              />
            </LitegraphFlex>
            <LitegraphFlex align="center" justify="space-between" className={styles.controlRow}>
              <span>Show node name</span>
              <Switch
                size="small"
                checked={showLabel}
                onChange={(checked) => setShowLabel(checked)}
              />
            </LitegraphFlex>
            {topologicalSortNodes &&
              displayGraph.collapsibleClusterIds.length > 0 &&
              expandedClusterIds.size > 0 && (
                <>
                  <div className={styles.controlsDivider} />
                  <LitegraphButton
                    type="text"
                    icon={<RedoOutlined />}
                    className={styles.controlsActionButton}
                    onClick={() => {
                      setExpandedClusterIds(new Set());
                      setIsControlsOpen(false);
                    }}
                  >
                    Collapse Expanded
                  </LitegraphButton>
                </>
              )}
          </LitegraphFlex>
        </div>
      )}
    >
      <LitegraphButton type="link" icon={<ControlOutlined />} weight={600}>
        Controls
      </LitegraphButton>
    </LitegraphDropdown>
  ) : null;

  const inlineControls = !controlsPortalTarget ? controlsDropdown : null;
  const shouldRenderToolbar = Boolean(loadingIndicator || inlineControls);
  const toolbarJustify =
    loadingIndicator && inlineControls
      ? 'space-between'
      : loadingIndicator
        ? 'flex-start'
        : 'flex-end';

  return (
    <div className="space-y-2">
      {controlsPortalTarget && controlsDropdown
        ? createPortal(controlsDropdown, controlsPortalTarget)
        : null}
      {shouldRenderToolbar && (
        <LitegraphFlex
          justify={toolbarJustify}
          align="center"
          style={{ marginTop: '-10px' }}
          className="mb-sm"
        >
          {loadingIndicator}
          {inlineControls}
        </LitegraphFlex>
      )}
      <ErrorBoundary>
        <div className={styles.graphContainer} ref={ref}>
          <>
            {isError ? (
              <FallBack className={styles.emptyState} type={FallBackEnums.ERROR} retry={refetch}>
                Error loading graph
              </FallBack>
            ) : (isLoading && nodes.length === 0) || isSubGraphLoading ? (
              <PageLoading />
            ) : !nodes.length && !isLoading ? (
              <FallBack className={styles.emptyState} type={FallBackEnums.EMPTY}>
                This graph has no nodes.
              </FallBack>
            ) : (
              <>
                {showGraphLegend && (
                  <LitegraphFlex className={styles.legendContainer} gap={15}>
                    {Object.values(legends).map((legend) => (
                      <LitegraphFlex key={legend.legend} align="center" gap={5}>
                        <div
                          className={styles.legendColor}
                          style={{ backgroundColor: legend.color }}
                        />
                        <span>{legend.legend}</span>
                      </LitegraphFlex>
                    ))}
                  </LitegraphFlex>
                )}
                {showMoreThanSupportedNodesWarning && hasMoreThanSupportedNodes && (
                  <Alert
                    type="warning"
                    closable
                    onClose={() => {
                      setShowMoreThanSupportedNodesWarning(false);
                    }}
                    className={styles.moreThanSupportedNodes}
                    description={
                      <>
                        Too many nodes exist to properly render the graph. Showing the first{' '}
                        {MAX_NODES_TO_FETCH} nodes.
                      </>
                    }
                  />
                )}
                {show3d ? (
                  <GraphLoader3d
                    legends={legends}
                    nodes={activeNodes}
                    edges={activeEdges}
                    setTooltip={setNodeTooltip}
                    setEdgeTooltip={setEdgeTooltip}
                    ref={ref}
                    showLabels={showLabel}
                    containerDivHeightAndWidth={containerDivHeightAndWidth}
                  />
                ) : (
                  <Graph2DViewer
                    legends={legends}
                    show3d={show3d}
                    selectedGraphRedux={selectedGraphRedux}
                    nodes={displayGraph.nodes}
                    edges={displayGraph.edges}
                    gexfContent={''}
                    showGraphHorizontal={showGraphHorizontal}
                    topologicalSortNodes={topologicalSortNodes}
                    setTooltip={setNodeTooltip}
                    setEdgeTooltip={setEdgeTooltip}
                    nodeTooltip={nodeTooltip}
                    edgeTooltip={edgeTooltip}
                    showLabel={showLabel}
                    groupDragging={groupDragging}
                    onNodeClick={handleDisplayNodeClick}
                    onEdgeClick={handleDisplayEdgeClick}
                  />
                )}
              </>
            )}
            {nodeTooltip.visible && (
              <NodeToolTip
                tooltip={nodeTooltip}
                setTooltip={setNodeTooltip}
                graphId={selectedGraphRedux}
                data-testid="node-tooltip"
                updateLocalNode={updateLocalNode}
                addLocalNode={addLocalNode}
                removeLocalNode={removeLocalNode}
                updateLocalEdge={updateLocalEdge}
                addLocalEdge={addLocalEdge}
                removeLocalEdge={removeLocalEdge}
                currentNodes={activeNodes}
                currentEdges={activeEdges}
              />
            )}
            {edgeTooltip.visible && (
              <EdgeToolTip
                tooltip={edgeTooltip}
                setTooltip={setEdgeTooltip}
                graphId={selectedGraphRedux}
                data-testid="edge-tooltip"
                updateLocalEdge={updateLocalEdge}
                addLocalEdge={addLocalEdge}
                removeLocalEdge={removeLocalEdge}
                currentNodes={activeNodes}
                currentEdges={activeEdges}
              />
            )}
          </>
        </div>
      </ErrorBoundary>

      {/* Add Node Modal */}
      {isAddEditNodeVisible && (
        <AddEditNode
          isAddEditNodeVisible={isAddEditNodeVisible}
          setIsAddEditNodeVisible={setIsAddEditNodeVisible}
          node={null}
          selectedGraph={selectedGraphRedux || ''}
          onNodeUpdated={async () => {
            // Refresh the graph data after node creation
            // The GraphViewer will automatically update through its local state
          }}
          updateLocalNode={updateLocalNode}
          addLocalNode={addLocalNode}
          removeLocalNode={removeLocalNode}
          currentNodes={activeNodes}
          currentEdges={activeEdges}
        />
      )}

      {/* Add Edge Modal */}
      {isAddEditEdgeVisible && (
        <AddEditEdge
          isAddEditEdgeVisible={isAddEditEdgeVisible}
          setIsAddEditEdgeVisible={setIsAddEditEdgeVisible}
          edge={null}
          selectedGraph={selectedGraphRedux || ''}
          onEdgeUpdated={async () => {
            // Refresh the graph data after edge creation
            // The GraphViewer will automatically update through its local state
          }}
          updateLocalEdge={updateLocalEdge}
          addLocalEdge={addLocalEdge}
          removeLocalEdge={removeLocalEdge}
          currentNodes={activeNodes}
          currentEdges={activeEdges}
        />
      )}

      {/* Node Search Modal */}
      {selectedGraphRedux && (
        <NodeSearchModal
          isVisible={isNodeSearchModalVisible}
          setIsVisible={setIsNodeSearchModalVisible}
          graphId={selectedGraphRedux}
          onNodeSelect={handleNodeSelect}
        />
      )}
    </div>
  );
};

export default GraphViewer;
