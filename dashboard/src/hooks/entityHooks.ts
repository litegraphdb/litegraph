import { transformToOptions } from '@/lib/graph/utils';

import { useAppSelector } from '@/lib/store/hooks';
import { RootState } from '@/lib/store/store';
import {
  useGetAllNodesQuery,
  useGetAllEdgesQuery,
  useEnumerateAndSearchNodeQuery,
  useEnumerateAndSearchEdgeQuery,
  useGetSubGraphsMutation,
} from '@/lib/store/slice/slice';
import { useEffect, useRef, useState } from 'react';
import { Edge, EnumerateResponse, Node, ReadSubGraphResponse } from 'litegraphdb/dist/types/types';
import {
  buildAdjacencyList,
  topologicalSortKahn,
  parseEdge,
  parseNode,
  parseCircularNodeDeterministic,
  parseNodeGroupedByLabel,
} from '@/lib/graph/parser';
import { EdgeData, NodeData } from '@/lib/graph/types';
import {
  MAX_NODES_AND_EDGES_TO_FETCH_IN_SINGLE_REQUEST,
  MAX_NODES_TO_FETCH,
} from '@/constants/constant';

export const useCurrentTenant = () => {
  const tenantFromRedux = useAppSelector((state: RootState) => state.liteGraph.tenant);
  return tenantFromRedux;
};

export const useSelectedGraph = () => {
  const selectedGraphRedux = useAppSelector((state: RootState) => state.liteGraph.selectedGraph);
  return selectedGraphRedux;
};

export const useSelectedTenant = () => {
  const selectedTenantRedux = useAppSelector((state: RootState) => state.liteGraph.tenant);
  return selectedTenantRedux;
};

export const useNodeAndEdge = (graphId: string) => {
  const {
    data: nodesList,
    refetch: fetchNodesList,
    isLoading: isNodesLoading,
    error: nodesError,
  } = useGetAllNodesQuery({ graphId });
  const nodeOptions = transformToOptions(nodesList);
  const {
    data: edgesList,
    refetch: fetchEdgesList,
    isLoading: isEdgesLoading,
    error: edgesError,
  } = useGetAllEdgesQuery({ graphId });
  const edgeOptions = transformToOptions(edgesList);

  const fetchNodesAndEdges = async () => {
    await Promise.all([fetchNodesList(), fetchEdgesList()]);
  };

  return {
    nodesList,
    edgesList,
    fetchNodesAndEdges,
    isLoading: isNodesLoading || isEdgesLoading,
    error: nodesError || edgesError,
    edgesError,
    nodesError,
    nodeOptions,
    edgeOptions,
  };
};

export const useLazyLoadNodes = (
  graphId: string,
  maxNodesToFetch: number = MAX_NODES_TO_FETCH,
  onDataLoaded?: () => void
) => {
  const [hasMoreThanSupportedNodes, setHasMoreThanSupportedNodes] = useState<boolean>(false);
  const [loading, setLoading] = useState(false);
  const [firstResult, setFirstResult] = useState<EnumerateResponse<Node> | null>(null);
  const [nodes, setNodes] = useState<Node[]>([]);
  const [processedNodes, setProcessedNodes] = useState<NodeData[]>([]);
  const [continuationToken, setContinuationToken] = useState<string | undefined>(undefined);
  const isFirstRender = useRef(true);
  const {
    data: nodesList,
    refetch: fetchNodesList,
    isLoading,
    isFetching,
    isError: isNodesError,
  } = useEnumerateAndSearchNodeQuery(
    {
      graphId,
      request: {
        MaxResults: MAX_NODES_AND_EDGES_TO_FETCH_IN_SINGLE_REQUEST,
        ContinuationToken: continuationToken,
        IncludeSubordinates: true,
      },
    },
    { skip: !graphId }
  );
  const isLoadingOrFetching = isLoading || isFetching;

  useEffect(() => {
    setLoading(true);
    let updatedNodes: Node[] = [];
    if (nodesList?.Objects?.length) {
      if (nodes.length === nodesList.TotalRecords) {
        setNodes([]);
      }
      if (nodesList.TotalRecords > MAX_NODES_TO_FETCH) {
        setHasMoreThanSupportedNodes(true);
      }
      updatedNodes = [
        ...nodes,
        ...nodesList.Objects.filter((node) => !nodes.some((n) => n.GUID === node.GUID)),
      ];
      setNodes(updatedNodes);

      // Only process new nodes to avoid shuffling existing ones
      const newNodes = nodesList.Objects;
      const newProcessedNodes = parseCircularNodeDeterministic(newNodes);

      // Merge with existing processed nodes, preserving their positions
      setProcessedNodes((prevProcessedNodes) => {
        const existingNodeIds = new Set(prevProcessedNodes.map((node) => node.id));
        const newNodesToAdd = newProcessedNodes.filter((node) => !existingNodeIds.has(node.id));
        return [...prevProcessedNodes, ...newNodesToAdd];
      });
    } else {
      setLoading(false);
    }
    if (!firstResult && nodesList) {
      setLoading(false);
      setFirstResult(nodesList);
    }
    if (nodesList?.ContinuationToken && updatedNodes.length < maxNodesToFetch) {
      setContinuationToken(nodesList.ContinuationToken);
    }
    if (nodesList?.RecordsRemaining === 0 || updatedNodes.length >= maxNodesToFetch) {
      setLoading(false);
      onDataLoaded?.();
    }
  }, [nodesList]);

  useEffect(() => {
    if (isFirstRender.current) {
      isFirstRender.current = false; // skip the first run
      return;
    }

    setNodes([]);
    setProcessedNodes([]);
    setFirstResult(null);
    setContinuationToken(undefined);
    try {
      fetchNodesList();
    } catch (error) {
      console.error(error);
    }
  }, [graphId]);

  return {
    nodes,
    processedNodes,
    refetchNodes: fetchNodesList,
    firstResult,
    isNodesError,
    isNodesLoading: isLoadingOrFetching || loading,
    hasMoreThanSupportedNodes,
  };
};

export const useLazyLoadEdges = (
  graphId: string,
  onDataLoaded?: () => void,
  doNotFetchOnRender?: boolean
) => {
  const [loading, setLoading] = useState(false);
  const [firstResult, setFirstResult] = useState<EnumerateResponse<Edge> | null>(null);
  const [edges, setEdges] = useState<Edge[]>([]);
  const [continuationToken, setContinuationToken] = useState<string | undefined>(undefined);
  const {
    data: edgesList,
    refetch: fetchEdgesList,
    isLoading: isEdgesLoading,
    isFetching: isEdgesFetching,
    isError: isEdgesError,
  } = useEnumerateAndSearchEdgeQuery(
    {
      graphId,
      request: {
        MaxResults: MAX_NODES_AND_EDGES_TO_FETCH_IN_SINGLE_REQUEST,
        ContinuationToken: continuationToken,
      },
    },
    { skip: doNotFetchOnRender || !graphId }
  );
  const isEdgesLoadingOrFetching = isEdgesLoading || isEdgesFetching;
  useEffect(() => {
    setLoading(true);
    if (edgesList?.Objects?.length) {
      const updatedEdges = [
        ...edges,
        ...edgesList.Objects.filter((edge) => !edges.some((e) => e.GUID === edge.GUID)),
      ];
      setEdges(updatedEdges);
    } else {
      setLoading(false);
    }
    if (!firstResult && edgesList) {
      setFirstResult(edgesList);
    }
    if (edgesList?.ContinuationToken) {
      setContinuationToken(edgesList.ContinuationToken);
    }
    if (edgesList?.RecordsRemaining === 0) {
      onDataLoaded?.();
      setLoading(false);
    }
  }, [edgesList]);

  useEffect(() => {
    setEdges([]);
    setFirstResult(null);
    setContinuationToken(undefined);
    try {
      fetchEdgesList();
    } catch (error) {
      console.error(error);
    }
  }, [graphId]);

  return {
    edges,
    isEdgesLoading: isEdgesLoadingOrFetching || loading,
    refetchEdges: fetchEdgesList,
    firstResult,
    isEdgesError,
  };
};

export const useLazyLoadEdgesAndNodes = (
  graphId: string,
  showGraphHorizontal: boolean,
  topologicalSortNodes: boolean,
  maxNodesToFetch: number = MAX_NODES_TO_FETCH
) => {
  const [nodesForGraph, setNodesForGraph] = useState<NodeData[]>([]);
  const [edgesForGraph, setEdgesForGraph] = useState<EdgeData[]>([]);
  const [doNotFetchEdgesOnRender, setDoNotFetchEdgesOnRender] = useState(true);
  const [renderNodesRandomly, setRenderNodesRandomly] = useState<boolean>(false);
  const [isCyclic, setIsCyclic] = useState<boolean>(false);

  const {
    nodes,
    processedNodes,
    isNodesLoading,
    refetchNodes,
    firstResult: nodesFirstResult,
    isNodesError,
    hasMoreThanSupportedNodes,
  } = useLazyLoadNodes(graphId, maxNodesToFetch, () => {
    setDoNotFetchEdgesOnRender(false);
    setRenderNodesRandomly(false);
    // Keep circular layout until edges finish loading
  });

  const {
    edges,
    isEdgesLoading,
    refetchEdges,
    firstResult: edgesFirstResult,
    isEdgesError,
  } = useLazyLoadEdges(
    graphId,
    () => {
      // Edges finished loading; switch to topological layout
      setDoNotFetchEdgesOnRender(true);
    },
    doNotFetchEdgesOnRender
  );

  // Local state update functions for nodes and edges
  const updateLocalNode = (updatedNode: NodeData) => {
    setNodesForGraph((prevNodes) =>
      prevNodes.map((node) => (node.id === updatedNode.id ? updatedNode : node))
    );
  };

  const addLocalNode = (newNode: NodeData) => {
    setNodesForGraph((prevNodes) => [...prevNodes, newNode]);
  };

  const removeLocalNode = (nodeId: string) => {
    setNodesForGraph((prevNodes) => prevNodes.filter((node) => node.id !== nodeId));
    // Also remove edges connected to this node
    setEdgesForGraph((prevEdges) =>
      prevEdges.filter((edge) => edge.source !== nodeId && edge.target !== nodeId)
    );
  };

  const updateLocalEdge = (updatedEdge: EdgeData) => {
    // Don't update edges when in random rendering mode (edges not loaded yet)
    if (renderNodesRandomly) {
      console.warn(
        `Cannot update edge ${updatedEdge.id}: graph is in random rendering mode, edges not loaded yet`
      );
      return;
    }

    // Validate that the updated edge references existing nodes
    const nodeIds = nodesForGraph.map((node) => node.id);
    if (nodeIds.includes(updatedEdge.source) && nodeIds.includes(updatedEdge.target)) {
      setEdgesForGraph((prevEdges) =>
        prevEdges.map((edge) => (edge.id === updatedEdge.id ? updatedEdge : edge))
      );
    } else {
      console.warn(
        `Cannot update edge ${updatedEdge.id}: source node ${updatedEdge.source} or target node ${updatedEdge.target} not found in graph`
      );
    }
  };

  const addLocalEdge = (newEdge: EdgeData) => {
    // Don't add edges when in random rendering mode (edges not loaded yet)
    if (renderNodesRandomly) {
      console.warn(
        `Cannot add edge ${newEdge.id}: graph is in random rendering mode, edges not loaded yet`
      );
      return;
    }

    // Validate that the edge references existing nodes
    const nodeIds = nodesForGraph.map((node) => node.id);
    if (nodeIds.includes(newEdge.source) && nodeIds.includes(newEdge.target)) {
      setEdgesForGraph((prevEdges) => [...prevEdges, newEdge]);
    } else {
      console.warn(
        `Cannot add edge ${newEdge.id}: source node ${newEdge.source} or target node ${newEdge.target} not found in graph`
      );
    }
  };

  const removeLocalEdge = (edgeId: string) => {
    setEdgesForGraph((prevEdges) => prevEdges.filter((edge) => edge.id !== edgeId));
  };

  useEffect(() => {
    if (!nodes.length) return;

    if (renderNodesRandomly) {
      // Use processed circular nodes while edges are still loading
      setNodesForGraph(processedNodes);
      setEdgesForGraph([]); // No edges while loading
    } else {
      // Use topological layout once edges are fetched (or even if edges are not yet available)
      const adjList = buildAdjacencyList(
        nodes,
        edges.map((edge) => ({ from: edge.From, to: edge.To }))
      );
      const { topologicalOrder, isCyclic: isCyclicResult } = topologicalSortKahn(adjList);
      setIsCyclic(isCyclicResult);
      let uniqueNodes: NodeData[] = [];

      if (topologicalSortNodes) {
        uniqueNodes = parseNode(
          nodes,
          nodes.length,
          adjList,
          topologicalOrder,
          showGraphHorizontal
        );
      } else {
        uniqueNodes = parseNodeGroupedByLabel(nodes, showGraphHorizontal);
      }

      setNodesForGraph(uniqueNodes);
      const nodeIds = uniqueNodes.map((node) => node.id);

      // Parse API edges and preserve locally added edges
      const apiEdges = parseEdge(
        edges?.filter((edge) => nodeIds.includes(edge.From) && nodeIds.includes(edge.To)) || []
      );

      // Get existing locally added edges (edges with temporary UUIDs that don't exist in API)
      // Only include edges whose source and target nodes exist in the current graph
      const existingLocalEdges =
        edgesForGraph.length > 0
          ? edgesForGraph.filter((edge) => {
              // Check if this is a locally added edge (temporary UUID format)
              const isLocalEdge = edge.id.length === 36 && edge.id.includes('-');
              // Check if this edge is not in the API response
              const isNotInApi = !apiEdges.some((apiEdge) => apiEdge.id === edge.id);
              // Check if both source and target nodes exist in the current graph
              const hasValidNodes = nodeIds.includes(edge.source) && nodeIds.includes(edge.target);
              return isLocalEdge && isNotInApi && hasValidNodes;
            })
          : [];

      // Combine API edges with locally added edges
      setEdgesForGraph([...apiEdges, ...existingLocalEdges]);
    }
  }, [
    nodes,
    processedNodes,
    edges,
    showGraphHorizontal,
    renderNodesRandomly,
    topologicalSortNodes,
  ]);

  // On graph change, start with circular layout and fetch fresh edges
  useEffect(() => {
    setRenderNodesRandomly(false);
    setNodesForGraph([]);
    setEdgesForGraph([]);
  }, [graphId]);

  return {
    nodes: nodesForGraph,
    edges: edgesForGraph,
    rawEdges: edges, // Add raw edges for progress bar
    isNodesLoading,
    isEdgesLoading,
    isLoading: isNodesLoading || isEdgesLoading,
    refetch: () => {
      refetchNodes();
      refetchEdges();
    },
    nodesFirstResult,
    edgesFirstResult,
    isNodesError,
    isEdgesError,
    refetchNodes,
    refetchEdges,
    isError: isNodesError || isEdgesError,
    renderNodesRandomly,
    // Local state update functions
    updateLocalNode,
    addLocalNode,
    removeLocalNode,
    updateLocalEdge,
    addLocalEdge,
    removeLocalEdge,
    hasMoreThanSupportedNodes,
    isCyclic,
  };
};

export const useGetSubGraphs = (
  selectedNodeGuid: string | null,
  topologicalSortNodes: boolean,
  showGraphHorizontal: boolean
) => {
  const graphGuid = useSelectedGraph();
  const [getSubGraphs, { isLoading, isError }] = useGetSubGraphsMutation();
  const [subGraphNodes, setSubGraphNodes] = useState<NodeData[] | null>(null);
  const [subGraphEdges, setSubGraphEdges] = useState<EdgeData[] | null>(null);
  const [subGraphResponse, setSubGraphResponse] = useState<ReadSubGraphResponse | null>(null);

  const loadSubGraph = async () => {
    if (!selectedNodeGuid) return;
    const response = await getSubGraphs({
      graphGuid,
      nodeGuid: selectedNodeGuid,
      options: { maxDepth: 2, maxNodes: 100, maxEdges: 100, incldata: false, inclsub: true },
    }).unwrap();
    setSubGraphResponse(response);
  };
  useEffect(() => {
    if (selectedNodeGuid) {
      loadSubGraph();
    }
  }, [selectedNodeGuid]);

  useEffect(() => {
    const data = subGraphResponse;
    if (data) {
      const adjList = buildAdjacencyList(
        data.Nodes,
        data.Edges.map((edge: Edge) => ({ from: edge.From, to: edge.To }))
      );
      const { topologicalOrder, isCyclic: isCyclicResult } = topologicalSortKahn(adjList);
      //  setIsCyclic(isCyclicResult);
      let uniqueNodes: NodeData[] = [];

      if (topologicalSortNodes) {
        uniqueNodes = parseNode(
          data.Nodes,
          data.Nodes.length,
          adjList,
          topologicalOrder,
          showGraphHorizontal
        );
      } else {
        uniqueNodes = parseNodeGroupedByLabel(data.Nodes, showGraphHorizontal);
      }
      const nodeIds = uniqueNodes.map((node) => node.id);

      // Parse API edges and preserve locally added edges
      const edges = parseEdge(
        data.Edges?.filter(
          (edge: Edge) => nodeIds.includes(edge.From) && nodeIds.includes(edge.To)
        ) || []
      );
      setSubGraphNodes(uniqueNodes);
      setSubGraphEdges(edges);
    } else {
      setSubGraphNodes([]);
      setSubGraphEdges([]);
    }
  }, [subGraphResponse, topologicalSortNodes, showGraphHorizontal]);

  return {
    isSubGraphLoading: isLoading,
    isError,
    loadSubGraph,
    subGraphNodes,
    subGraphEdges,
    subGraphResponse,
  };
};
