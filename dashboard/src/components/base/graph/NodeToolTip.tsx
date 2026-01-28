'use client';
import LiteGraphCard from '@/components/base/card/Card';
import LiteGraphSpace from '@/components/base/space/Space';
import React, { Dispatch, SetStateAction, useEffect, useState } from 'react';
import { GraphNodeTooltip } from './types';
import { CloseCircleFilled, CopyOutlined, ExpandOutlined } from '@ant-design/icons';
import { defaultNodeTooltip } from './constant';
import LitegraphText from '@/components/base/typograpghy/Text';
import LitegraphFlex from '@/components/base/flex/Flex';
import { JsonEditor } from 'jsoneditor-react';
import FallBack from '@/components/base/fallback/FallBack';
import PageLoading from '@/components/base/loading/PageLoading';
import LitegraphButton from '@/components/base/button/Button';
import AddEditNode from '@/page/nodes/components/AddEditNode';
import { NodeType } from '@/types/types';
import DeleteNode from '@/page/nodes/components/DeleteNode';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';
import AddEditEdge from '@/page/edges/components/AddEditEdge';
import classNames from 'classnames';
import styles from './tooltip.module.scss';
import { copyTextToClipboard } from '@/utils/jsonCopyUtils';
import { useGetNodeByIdQuery } from '@/lib/store/slice/slice';
import { useEnumerateAndSearchTagQuery } from '@/lib/store/slice/slice';

type NodeTooltipProps = {
  tooltip: GraphNodeTooltip;
  setTooltip: Dispatch<SetStateAction<GraphNodeTooltip>>;
  graphId: string;
  // Local state update functions for graph viewer
  updateLocalNode?: (node: any) => void;
  addLocalNode?: (node: any) => void;
  removeLocalNode?: (nodeId: string) => void;
  updateLocalEdge?: (edge: any) => void;
  addLocalEdge?: (edge: any) => void;
  removeLocalEdge?: (edgeId: string) => void;
  // Current graph data for immediate updates
  currentNodes?: any[];
  currentEdges?: any[];
};

const NodeToolTip = ({
  tooltip,
  setTooltip,
  graphId,
  updateLocalNode,
  addLocalNode,
  removeLocalNode,
  updateLocalEdge,
  addLocalEdge,
  removeLocalEdge,
  currentNodes,
  currentEdges,
}: NodeTooltipProps) => {
  const [isExpanded, setIsExpanded] = useState<boolean>(false);

  // Find the current node data from local state first, fallback to API if not found
  const localNodeData = currentNodes?.find((node: any) => node.id === tooltip.nodeId);

  // Check if this is for adding a new node
  const isAddingNewNode = tooltip.nodeId === 'new';

  const {
    data: node,
    isLoading,
    isFetching,
    error,
    refetch,
  } = useGetNodeByIdQuery(
    {
      graphId,
      nodeId: tooltip.nodeId,
      request: { includeSubordinates: true },
    },
    {
      // Fetch details for existing nodes; skip only when adding new or missing ids
      skip: !graphId || !tooltip.nodeId || isAddingNewNode,
    }
  );

  // Display API node (shows loader until it arrives)
  const displayNode = node;

  // Fetch tags separately since they are not included in the node response
  const {
    data: tagsData,
    isLoading: isTagsLoading,
    refetch: refetchTags,
  } = useEnumerateAndSearchTagQuery(
    {
      MaxResults: 1000, // Fetch more tags to ensure we get all relevant ones
      GraphGUID: graphId,
    },
    {
      skip: !graphId || tooltip.nodeId === 'new',
    }
  );

  // Filter tags by NodeGUID
  const nodeTags = tagsData?.Objects?.filter((tag) => tag.NodeGUID === tooltip.nodeId) || [];

  // Merge tags data with displayNode
  const nodeWithTags = displayNode
    ? {
        ...displayNode,
        Tags: nodeTags.reduce(
          (acc, tag) => {
            acc[tag.Key] = tag.Value;
            return acc;
          },
          {} as Record<string, string>
        ),
      }
    : displayNode;

  // const { refetch: fetchGexfByGraphId } = useGetGraphGexfContentQuery(graphId);

  // State for AddEditDeleteNode visibility and selected node
  const [isAddEditNodeVisible, setIsAddEditNodeVisible] = useState<boolean>(false);
  const [isDeleteModelVisible, setIsDeleteModelVisible] = useState<boolean>(false);
  const [isAddEditEdgeVisible, setIsAddEditEdgeVisible] = useState<boolean>(false);
  const [selectedNode, setSelectedNode] = useState<NodeType | null | undefined>(undefined);

  // Callback for handling node update
  const handleNodeUpdate = async () => {
    if (graphId && tooltip.nodeId) {
      try {
        const refetchPromises = [];

        // Only refetch node if the query is not skipped
        if (!isAddingNewNode) {
          refetchPromises.push(refetch());
        }

        // Only refetch tags if the query is not skipped
        if (!isAddingNewNode) {
          refetchPromises.push(refetchTags());
        }

        if (refetchPromises.length > 0) {
          await Promise.all(refetchPromises);
        }

        // Don't clear the tooltip immediately - let the user see the updated information
        // The tooltip will be updated with new data from the refetch
      } catch (error) {
        console.warn('Some queries could not be refetched:', error);
        // Continue with the update process even if refetch fails
      }
    }
  };

  // Callback for handling node deletion
  const handleNodeDelete = async () => {
    // await fetchGexfByGraphId();

    // Clear the tooltip after deletion
    setTooltip(defaultNodeTooltip);
  };

  // Callback for handling edge update
  const handleEdgeUpdate = async () => {
    // await fetchGexfByGraphId();

    // Clear node tooltip
    setTooltip({ visible: false, nodeId: '', x: 0, y: 0 });
  };

  // Refresh tags when tooltip becomes visible for an existing node
  useEffect(() => {
    if (tooltip.visible && tooltip.nodeId && tooltip.nodeId !== 'new' && graphId) {
      // Only refetch if the query is not skipped
      if (!isAddingNewNode) {
        refetchTags();
      }
    }
  }, [tooltip.visible, tooltip.nodeId, graphId, refetchTags, isAddingNewNode]);
  return (
    <>
      <LiteGraphSpace
        key={`node-tooltip-${tooltip.nodeId}-${nodeWithTags?.LastUpdateUtc || 'new'}`}
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
              Node Information
            </LitegraphText>
          }
          extra={
            <LitegraphFlex gap={10}>
              <LitegraphTooltip title="Expand" placement="bottom">
                <ExpandOutlined
                  className="cursor-pointer"
                  onClick={() => {
                    setSelectedNode(nodeWithTags as any);
                    setIsExpanded(true);
                    setIsAddEditNodeVisible(true);
                  }}
                />
              </LitegraphTooltip>
              <CloseCircleFilled
                className="cursor-pointer"
                onClick={() => setTooltip(defaultNodeTooltip)}
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
          ) : isAddingNewNode ? (
            // Show Add Node form
            <LitegraphFlex vertical>
              <LitegraphText>
                <strong>Add New Node</strong>
              </LitegraphText>
              <LitegraphButton
                type="primary"
                onClick={() => {
                  setSelectedNode(null); // null means new node
                  setIsAddEditNodeVisible(true);
                }}
                className="mt-2"
              >
                Open Add Node Form
              </LitegraphButton>
            </LitegraphFlex>
          ) : (
            // Ready to show data after API is ready
            <LitegraphFlex vertical>
              <LitegraphFlex vertical className="card-details">
                <LitegraphText data-testid="node-guid">
                  <strong>GUID: </strong>
                  {nodeWithTags?.GUID}{' '}
                  <CopyOutlined
                    style={{ cursor: 'pointer' }}
                    onClick={() => {
                      copyTextToClipboard(nodeWithTags?.GUID || '', 'GUID');
                    }}
                  />
                </LitegraphText>
                <LitegraphText>
                  <strong>Name: </strong>
                  {nodeWithTags?.Name}
                </LitegraphText>

                <LitegraphText>
                  <strong>Labels: </strong>
                  {`${nodeWithTags?.Labels?.length ? nodeWithTags?.Labels?.join(', ') : 'None'}`}
                </LitegraphText>

                {/* <LitegraphText>
                  <strong>Vectors: </strong>
                  {pluralize(node?.Vectors?.length || 0, 'vector')}
                </LitegraphText> */}

                <LitegraphText>
                  <strong>Tags: </strong>
                  {(() => {
                    const tags = nodeWithTags?.Tags || {};
                    const tagKeys = Object.keys(tags);

                    return tagKeys.length > 0 ? (
                      <JsonEditor
                        key={JSON.stringify(tags)}
                        value={tags}
                        mode="view" // Use 'view' mode to make it read-only
                        mainMenuBar={false} // Hide the menu bar
                        statusBar={false} // Hide the status bar
                        navigationBar={false} // Hide the navigation bar
                        enableSort={false}
                        enableTransform={false}
                      />
                    ) : (
                      <LitegraphText>None</LitegraphText>
                    );
                  })()}
                </LitegraphText>

                {/* <LitegraphFlex align="center" gap={6}>
                  <LitegraphText>
                    <strong>Data:</strong>
                  </LitegraphText>
                  <LitegraphTooltip title="Copy JSON">
                    <CopyOutlined
                      className="cursor-pointer"
                      onClick={() => {
                        copyJsonToClipboard(node?.Data || {}, 'Data');
                      }}
                    />
                  </LitegraphTooltip>
                </LitegraphFlex>
                <JsonEditor
                  key={JSON.stringify(node?.Data && JSON.parse(JSON.stringify(node.Data)))}
                  value={(node?.Data && JSON.parse(JSON.stringify(node.Data))) || {}}
                  mode="view"
                  mainMenuBar={false}
                  statusBar={false}
                  navigationBar={false}
                  enableSort={false}
                  enableTransform={false}
                /> */}
              </LitegraphFlex>
              {/* Buttons */}
              <LitegraphFlex className="pt-3" gap={10} justify="space-between">
                <LitegraphTooltip title={'Update Node'} placement="bottom">
                  <LitegraphButton
                    type="link"
                    onClick={() => {
                      setSelectedNode(nodeWithTags);
                      setIsAddEditNodeVisible(true);
                    }}
                  >
                    Update
                  </LitegraphButton>
                </LitegraphTooltip>

                <LitegraphTooltip title={'Delete Node'} placement="bottom">
                  <LitegraphButton
                    type="link"
                    onClick={() => {
                      setSelectedNode(nodeWithTags);
                      setIsDeleteModelVisible(true);
                    }}
                  >
                    Delete
                  </LitegraphButton>
                </LitegraphTooltip>

                <LitegraphTooltip title={'Add Edge'} placement="bottom">
                  <LitegraphButton
                    type="link"
                    onClick={() => {
                      setIsAddEditEdgeVisible(true);
                    }}
                  >
                    Add Edge
                  </LitegraphButton>
                </LitegraphTooltip>
              </LitegraphFlex>
            </LitegraphFlex>
          )}
        </LiteGraphCard>
      </LiteGraphSpace>

      {/* AddEditNode Component On Update*/}
      {selectedNode !== undefined && (
        <AddEditNode
          isAddEditNodeVisible={isAddEditNodeVisible}
          setIsAddEditNodeVisible={setIsAddEditNodeVisible}
          node={selectedNode as any}
          selectedGraph={graphId}
          readonly={isExpanded}
          onNodeUpdated={handleNodeUpdate} // Pass callback to handle updates
          onClose={() => {
            setIsExpanded(false);
            setSelectedNode(undefined);
            // Close the tooltip if it was for adding a new node
            if (tooltip.nodeId === 'new') {
              setTooltip({ visible: false, nodeId: '', x: 0, y: 0 });
            }
          }}
          updateLocalNode={updateLocalNode}
          addLocalNode={addLocalNode}
          removeLocalNode={removeLocalNode}
          currentNodes={currentNodes}
          currentEdges={currentEdges}
        />
      )}

      {/* DeleteNode Component On Delete*/}
      <DeleteNode
        title={`Are you sure you want to delete "${selectedNode?.Name}" node?`}
        paragraphText={'This action will delete node.'}
        isDeleteModelVisible={isDeleteModelVisible}
        setIsDeleteModelVisible={setIsDeleteModelVisible}
        selectedNode={selectedNode}
        setSelectedNode={setSelectedNode}
        onNodeDeleted={handleNodeDelete}
        removeLocalNode={removeLocalNode}
      />

      {/* AddEditEdge Component On Add*/}
      <AddEditEdge
        isAddEditEdgeVisible={isAddEditEdgeVisible}
        setIsAddEditEdgeVisible={setIsAddEditEdgeVisible}
        edge={null}
        selectedGraph={graphId}
        onEdgeUpdated={handleEdgeUpdate} // Pass callback to handle updates
        fromNodeGUID={tooltip?.nodeId}
        updateLocalEdge={updateLocalEdge}
        addLocalEdge={addLocalEdge}
        removeLocalEdge={removeLocalEdge}
        currentNodes={currentNodes}
        currentEdges={currentEdges}
      />
    </>
  );
};

export default NodeToolTip;
