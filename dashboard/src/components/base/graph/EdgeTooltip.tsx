import { defaultEdgeTooltip } from './constant';
import { GraphEdgeTooltip } from './types';
import { Dispatch, SetStateAction, useEffect, useState } from 'react';
import LiteGraphSpace from '@/components/base/space/Space';
import LiteGraphCard from '@/components/base/card/Card';
import {
  CloseCircleFilled,
  CopyOutlined,
  ExpandOutlined,
  LoadingOutlined,
} from '@ant-design/icons';
import FallBack from '@/components/base/fallback/FallBack';
import PageLoading from '@/components/base/loading/PageLoading';
import LitegraphFlex from '@/components/base/flex/Flex';
import LitegraphText from '@/components/base/typograpghy/Text';
import LitegraphButton from '@/components/base/button/Button';
import AddEditEdge from '@/page/edges/components/AddEditEdge';
import { EdgeType } from '@/types/types';
import DeleteEdge from '@/page/edges/components/DeleteEdge';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';
import { JsonEditor } from 'jsoneditor-react';
import styles from './tooltip.module.scss';
import classNames from 'classnames';
import {
  useGetEdgeByIdQuery,
  useGetGraphGexfContentQuery,
  useGetManyNodesQuery,
} from '@/lib/store/slice/slice';
import { copyTextToClipboard } from '@/utils/jsonCopyUtils';

type EdgeTooltipProps = {
  tooltip: GraphEdgeTooltip;
  setTooltip: Dispatch<SetStateAction<GraphEdgeTooltip>>;
  graphId: string;
  // Local state update functions for graph viewer
  updateLocalEdge?: (edge: any) => void;
  addLocalEdge?: (edge: any) => void;
  removeLocalEdge?: (edgeId: string) => void;
  // Current graph data for immediate updates
  currentNodes?: any[];
  currentEdges?: any[];
};

const EdgeToolTip = ({
  tooltip,
  setTooltip,
  graphId,
  updateLocalEdge,
  addLocalEdge,
  removeLocalEdge,
  currentNodes,
  currentEdges,
}: EdgeTooltipProps) => {
  const [isExpanded, setIsExpanded] = useState<boolean>(false);

  // State for AddEditDeleteNode visibility and selected node
  const [isAddEditEdgeVisible, setIsAddEditEdgeVisible] = useState<boolean>(false);
  const [isDeleteModelVisisble, setIsDeleteModelVisisble] = useState<boolean>(false);
  const [selectedEdge, setSelectedEdge] = useState<EdgeType | null | undefined>(undefined);

  // Find the current edge data from local state first, fallback to API if not found
  const localEdgeData = currentEdges?.find((edge: any) => edge.id === tooltip.edgeId);

  // Check if this is for adding a new edge
  const isAddingNewEdge = tooltip.edgeId === 'new';

  const {
    data: edge,
    isLoading,
    isFetching,
    error,
    refetch,
  } = useGetEdgeByIdQuery(
    {
      graphId,
      edgeId: tooltip.edgeId,
      request: { includeSubordinates: true },
    },
    {
      // Fetch details for existing edges; skip only when adding new or missing ids
      skip: !graphId || !tooltip.edgeId || isAddingNewEdge,
    }
  );

  // Display API edge (shows loader until it arrives)
  const displayEdge = edge;
  // const { refetch: fetchGexfByGraphId } = useGetGraphGexfContentQuery(graphId);
  const nodeIds = [displayEdge?.From, displayEdge?.To].filter(Boolean) as string[];
  const {
    data: nodesList,
    isLoading: isNodesLoading,
    isFetching: isNodesFetching,
    refetch: refetchNodes,
  } = useGetManyNodesQuery({ graphId, nodeIds }, { skip: !nodeIds.length });
  const isNodesLoadingOrFetching = isNodesLoading || isNodesFetching;
  const fromNode = nodesList?.find((node) => node.GUID === displayEdge?.From);
  const toNode = nodesList?.find((node) => node.GUID === displayEdge?.To);

  // Callback for handling edge update
  const handleEdgeUpdate = async () => {
    if (graphId && tooltip.edgeId) {
      try {
        const refetchPromises = [];
        if (!isAddingNewEdge) {
          refetchPromises.push(refetch());
        }

        if (!isAddingNewEdge && nodeIds.length > 0) {
          refetchPromises.push(refetchNodes());
        }

        if (refetchPromises.length > 0) {
          await Promise.all(refetchPromises);
        }
      } catch (error) {
        console.warn('Some queries could not be refetched:', error);
      }
    }
  };

  // Callback for handling edge deletion
  const handleEdgeDelete = async () => {
    // await fetchGexfByGraphId();

    // Clear the tooltip after deletion
    setTooltip(defaultEdgeTooltip);
  };

  // Refresh edge and related data when tooltip becomes visible
  useEffect(() => {
    if (tooltip.visible && tooltip.edgeId && tooltip.edgeId !== 'new' && graphId) {
      try {
        if (!isAddingNewEdge) {
          refetch();
        }

        if (!isAddingNewEdge && nodeIds.length > 0) {
          refetchNodes();
        }
      } catch (error) {
        console.warn('Some queries could not be refetched:', error);
      }
    }
  }, [
    tooltip.visible,
    tooltip.edgeId,
    graphId,
    refetch,
    refetchNodes,
    nodeIds.length,
    isAddingNewEdge,
  ]);

  return (
    <>
      <LiteGraphSpace
        key={`edge-tooltip-${tooltip.edgeId}-${displayEdge?.LastUpdateUtc || 'new'}`}
        direction="vertical"
        size={16}
        className={classNames(styles.tooltipContainer)}
        style={{
          top: tooltip.y,
          left: tooltip.x,
        }}
      >
        <LiteGraphCard
          title={
            <LitegraphText weight={600} fontSize={18}>
              Edge Information
            </LitegraphText>
          }
          extra={
            <LitegraphFlex gap={10}>
              <LitegraphTooltip title="Expand" placement="bottom">
                <ExpandOutlined
                  className="cursor-pointer"
                  onClick={() => {
                    setSelectedEdge(displayEdge as any);
                    setIsExpanded(true);
                    setIsAddEditEdgeVisible(true);
                  }}
                />
              </LitegraphTooltip>
              <CloseCircleFilled
                className="cursor-pointer"
                onClick={() => setTooltip(defaultEdgeTooltip)}
              />
            </LitegraphFlex>
          }
          style={{ width: 300 }}
        >
          {/* If error then fallback displays */}
          {isLoading || isFetching ? (
            // If not error but API is in loading state then dispalys loader
            <PageLoading withoutWhiteBG />
          ) : error ? (
            <FallBack retry={refetch}>
              {error ? 'Something went wrong.' : "Can't view details at the moment."}
            </FallBack>
          ) : isAddingNewEdge ? (
            // Show Add Edge form
            <LitegraphFlex vertical>
              <LitegraphText>
                <strong>Add New Edge</strong>
              </LitegraphText>
              <LitegraphButton
                type="primary"
                onClick={() => {
                  setSelectedEdge(null); // null means new edge
                  setIsAddEditEdgeVisible(true);
                }}
                className="mt-2"
              >
                Open Add Edge Form
              </LitegraphButton>
            </LitegraphFlex>
          ) : (
            // Ready to show data after API is ready
            <LitegraphFlex vertical>
              <LitegraphFlex vertical className="card-details">
                <LitegraphText data-testid="edge-guid">
                  <strong>GUID: </strong>
                  {displayEdge?.GUID}{' '}
                  <CopyOutlined
                    style={{ cursor: 'pointer' }}
                    onClick={() => {
                      copyTextToClipboard(displayEdge?.GUID || '', 'GUID');
                    }}
                  />
                </LitegraphText>
                <LitegraphText>
                  <strong>Name: </strong>
                  {displayEdge?.Name}
                </LitegraphText>

                <LitegraphText>
                  <strong>From: </strong>
                  {isNodesLoadingOrFetching ? <LoadingOutlined /> : fromNode?.Name}
                </LitegraphText>

                <LitegraphText>
                  <strong>To: </strong>
                  {isNodesLoadingOrFetching ? <LoadingOutlined /> : toNode?.Name}
                </LitegraphText>

                <LitegraphText>
                  <strong>Cost: </strong>
                  {displayEdge?.Cost}
                </LitegraphText>

                <LitegraphText>
                  <strong>Labels: </strong>
                  {`${displayEdge?.Labels?.length ? displayEdge?.Labels?.join(', ') : 'None'}`}
                </LitegraphText>

                {/* <LitegraphText>
                  <strong>Vectors: </strong>
                  {pluralize(displayEdge?.Vectors?.length || 0, 'vector')}
                </LitegraphText> */}

                <LitegraphText>
                  <strong>Tags: </strong>
                  {Object.keys(displayEdge?.Tags || {}).length > 0 ? (
                    <JsonEditor
                      key={JSON.stringify(
                        displayEdge?.Tags && JSON.parse(JSON.stringify(displayEdge.Tags))
                      )}
                      value={
                        (displayEdge?.Tags && JSON.parse(JSON.stringify(displayEdge.Tags))) || {}
                      }
                      mode="view" // Use 'view' mode to make it read-only
                      mainMenuBar={false} // Hide the menu bar
                      statusBar={false} // Hide the status bar
                      navigationBar={false} // Hide the navigation bar
                      enableSort={false}
                      enableTransform={false}
                    />
                  ) : (
                    <LitegraphText>None</LitegraphText>
                  )}
                </LitegraphText>
                {/* 
                <LitegraphFlex align="center" gap={6}>
                  <LitegraphText>
                    <strong>Data:</strong>
                  </LitegraphText>
                  <LitegraphTooltip title="Copy JSON">
                    <CopyOutlined
                      className="cursor-pointer"
                      onClick={() => {
                        copyJsonToClipboard(displayEdge?.Data || {}, 'Data');
                      }}
                    />
                  </LitegraphTooltip>
                </LitegraphFlex>
                <JsonEditor
                  key={JSON.stringify(displayEdge?.Data && JSON.parse(JSON.stringify(displayEdge?.Data)))}
                  value={(displayEdge?.Data && JSON.parse(JSON.stringify(displayEdge.Data))) || {}}
                  mode="view" // Use 'view' mode to make it read-only
                  mainMenuBar={false} // Hide the menu bar
                  statusBar={false} // Hide the status bar
                  navigationBar={false} // Hide the navigation bar
                  enableSort={false}
                  enableTransform={false}
                /> */}
              </LitegraphFlex>

              {/* Buttons */}
              <LitegraphFlex justify="space-between" gap={10} className="pt-3">
                <LitegraphTooltip title={'Update Edge'} placement="bottom">
                  <LitegraphButton
                    type="link"
                    onClick={() => {
                      setSelectedEdge(displayEdge);
                      setIsAddEditEdgeVisible(true);
                    }}
                  >
                    Update
                  </LitegraphButton>
                </LitegraphTooltip>
                <LitegraphTooltip title={'Delete Edge'} placement="bottom">
                  <LitegraphButton
                    type="link"
                    onClick={() => {
                      setSelectedEdge(displayEdge);
                      setIsDeleteModelVisisble(true);
                    }}
                  >
                    Delete
                  </LitegraphButton>
                </LitegraphTooltip>
              </LitegraphFlex>
            </LitegraphFlex>
          )}
        </LiteGraphCard>
      </LiteGraphSpace>

      {/* AddEditEdge Component On Update*/}
      {selectedEdge !== undefined && (
        <AddEditEdge
          isAddEditEdgeVisible={isAddEditEdgeVisible}
          setIsAddEditEdgeVisible={setIsAddEditEdgeVisible}
          edge={selectedEdge as any}
          selectedGraph={graphId}
          onEdgeUpdated={handleEdgeUpdate} // Pass callback to handle updates
          readonly={isExpanded}
          onClose={() => {
            setIsAddEditEdgeVisible(false);
            setSelectedEdge(undefined);
            setIsExpanded(false);
            // Close the tooltip if it was for adding a new edge
            if (tooltip.edgeId === 'new') {
              setTooltip({ visible: false, edgeId: '', x: 0, y: 0 });
            }
          }}
          updateLocalEdge={updateLocalEdge}
          addLocalEdge={addLocalEdge}
          removeLocalEdge={removeLocalEdge}
          currentNodes={currentNodes}
          currentEdges={currentEdges}
        />
      )}

      {/* DeleteEdge Component On Delete*/}
      <DeleteEdge
        title={`Are you sure you want to delete "${selectedEdge?.Name}|| ''" edge?`}
        paragraphText={'This action will delete edge.'}
        isDeleteModelVisisble={isDeleteModelVisisble}
        setIsDeleteModelVisisble={setIsDeleteModelVisisble}
        selectedEdge={selectedEdge}
        setSelectedEdge={setSelectedEdge}
        onEdgeDeleted={handleEdgeDelete}
        removeLocalEdge={removeLocalEdge}
      />
    </>
  );
};

export default EdgeToolTip;
