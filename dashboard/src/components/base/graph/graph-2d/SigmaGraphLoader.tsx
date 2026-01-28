'use client';

import { Dispatch, SetStateAction, useEffect, useRef, useState } from 'react';
import Graph from 'graphology';
import { useLoadGraph, useRegisterEvents, useSigma } from '@react-sigma/core';
import { GraphEdgeTooltip, GraphNodeTooltip } from '../types';
import { calculateTooltipPosition } from '@/utils/appUtils';
import { EdgeData, NodeData } from '@/lib/graph/types';
import { LightGraphTheme } from '@/theme/theme';
import { useAppContext } from '@/hooks/appHooks';
import { ThemeEnum } from '@/types/types';
import { defaultNodeColor } from '../constant';

interface GraphLoaderProps {
  gexfContent: string;
  setTooltip: Dispatch<SetStateAction<GraphNodeTooltip>>;
  setEdgeTooltip: Dispatch<SetStateAction<GraphEdgeTooltip>>;
  nodeTooltip: GraphNodeTooltip;
  edgeTooltip: GraphEdgeTooltip;
  nodes: NodeData[];
  edges: EdgeData[];
  groupDragging: boolean;
  legends: Record<string, { legend: string; color: string }>;
}

const GraphLoader = ({
  gexfContent,
  setTooltip,
  setEdgeTooltip,
  nodeTooltip,
  edgeTooltip,
  nodes,
  edges,
  groupDragging,
  legends,
}: GraphLoaderProps) => {
  const { theme } = useAppContext();
  const loadGraph = useLoadGraph();
  const sigma = useSigma();
  const graph = new Graph({ multi: true, allowSelfLoops: true });
  const animationFrameRef = useRef<number>();
  const registerEvents = useRegisterEvents();
  const [draggedNode, setDraggedNode] = useState<string | null>(null);
  const [draggedEdge, setDraggedEdge] = useState<string | null>(null);
  // NEW refs for group dragging
  const draggedNodeRef = useRef<string | null>(null);
  const draggedSetRef = useRef<Set<string>>(new Set()); // node + siblings
  const startPosRef = useRef<Map<string, { x: number; y: number }>>(new Map());
  const dragStartGraphPosRef = useRef<{ x: number; y: number } | null>(null);

  const isDraggingRef = useRef(false);

  // Reset the tooltips while zoom in-zoom out
  useEffect(() => {
    const sigmaInstance = sigma.getCamera(); // Access the camera
    const handleCameraUpdate = () => {
      // Clear all tooltips when zoom or pan occurs
      setTooltip({ visible: false, nodeId: '', x: 0, y: 0 });
      setEdgeTooltip({ visible: false, edgeId: '', x: 0, y: 0 });
    };

    // Attach the event listener for camera updates
    sigmaInstance.on('updated', handleCameraUpdate);

    // Cleanup the event listener on unmount
    return () => {
      sigmaInstance.removeListener('updated', handleCameraUpdate);
    };
  }, [sigma]);

  useEffect(() => {
    // Add nodes with circle shape
    nodes.forEach((node) => {
      graph.addNode(node.id, {
        x: node.x,
        y: node.y,
        size: 15,
        label: node.label,
        color:
          theme === ThemeEnum.LIGHT
            ? legends[node.type]?.color || defaultNodeColor
            : legends[node.type]?.color || defaultNodeColor,
        type: 'circle',
        vx: 0,
        vy: 0,
        isDragging: false,
      });
    });

    // Add edges with unique IDs
    edges.forEach((edge) => {
      // Check if both source and target nodes exist in the graph
      if (graph.hasNode(edge.source) && graph.hasNode(edge.target)) {
        graph.addEdgeWithKey(
          edge.id,
          edge.source,
          edge.target,
          {
            size: 3,
            label: `${edge.id}${edge.cost}`,
            color: theme === ThemeEnum.LIGHT ? '#aaa' : '#555',
            type: 'arrow',
          }
          // { generateId: () => edge.id }
        );
      } else {
        console.warn(
          `Skipping edge ${edge.id}: source node ${edge.source} or target node ${edge.target} not found in graph`
        );
      }
    });

    loadGraph(graph);

    return () => {
      if (animationFrameRef.current) {
        cancelAnimationFrame(animationFrameRef.current);
      }
      sigma.removeAllListeners();
      graph.clear();
    };
  }, [gexfContent, loadGraph, sigma, nodes?.length, edges?.length]);

  useEffect(() => {
    const graph = sigma.getGraph();

    registerEvents({
      downNode: (e) => {
        // highlight as you already do
        setDraggedNode(e.node);
        isDraggingRef.current = false;
        graph.setNodeAttribute(e.node, 'highlighted', true);
        // ---- NEW: prepare group-drag state
        e.preventSigmaDefault?.();
        const startPointer = sigma.viewportToGraph({ x: e.event.x, y: e.event.y });
        dragStartGraphPosRef.current = startPointer;
        const selectedNode = nodes.find((node) => node.id === e.node);
        // Define "siblings": here we use graph neighbors; adjust if your domain differs
        const siblings =
          groupDragging && selectedNode?.type
            ? nodes.filter((node) => node.type === selectedNode?.type)
            : [];
        const draggedSet = new Set<string>([e.node, ...siblings.map((node) => node.id)]);

        draggedNodeRef.current = e.node;
        draggedSetRef.current = draggedSet;

        // Snapshot starting positions
        const starts = new Map<string, { x: number; y: number }>();
        draggedSet.forEach((id) => {
          const attrs = graph.getNodeAttributes(id);
          starts.set(id, { x: attrs.x, y: attrs.y });
        });
        startPosRef.current = starts;
      },

      mousemovebody: (e) => {
        // your tooltip hiding logic
        const isDraggingSomething = !!draggedNodeRef.current || !!draggedEdge;
        if (isDraggingSomething) {
          if (nodeTooltip?.visible) {
            setTooltip({ visible: false, nodeId: '', x: 0, y: 0 });
          }
          if (edgeTooltip?.visible) {
            setEdgeTooltip({ visible: false, edgeId: '', x: 0, y: 0 });
          }
        }

        // ---- NEW: apply delta to the dragged set
        if (draggedNodeRef.current && dragStartGraphPosRef.current) {
          isDraggingRef.current = true;
          const nowGraph = sigma.viewportToGraph({ x: e.x, y: e.y });
          const dx = nowGraph.x - dragStartGraphPosRef.current.x;
          const dy = nowGraph.y - dragStartGraphPosRef.current.y;

          const graph = sigma.getGraph();
          const starts = startPosRef.current;
          const draggedSet = draggedSetRef.current;

          // Update all nodes in one paint cycle
          draggedSet.forEach((id) => {
            const s = starts.get(id);
            if (!s) return;
            graph.setNodeAttribute(id, 'x', s.x + dx);
            graph.setNodeAttribute(id, 'y', s.y + dy);
          });

          // stop camera panning
          e.preventSigmaDefault();
          e.original?.preventDefault?.();
          e.original?.stopPropagation?.();
        }

        // If you later add edge dragging, keep your existing branch:
        if (draggedEdge) {
          e.preventSigmaDefault();
          e.original?.preventDefault?.();
          e.original?.stopPropagation?.();
        }
      },

      mouseup: () => {
        // clear old highlighting
        if (draggedNode) {
          sigma.getGraph().removeNodeAttribute(draggedNode, 'highlighted');
        }

        // ---- NEW: clear group-drag state
        draggedNodeRef.current = null;
        draggedSetRef.current.clear();
        startPosRef.current.clear();
        dragStartGraphPosRef.current = null;

        // your existing state clears
        if (draggedNode) setDraggedNode(null);
        if (draggedEdge) {
          sigma.getGraph().removeEdgeAttribute(draggedEdge, 'highlighted');
          setDraggedEdge(null);
        }
      },

      mousedown: () => {
        if (!sigma.getCustomBBox()) sigma.setCustomBBox(sigma.getBBox());
      },

      // clickNode / enterEdge / clickEdge / leaveEdge unchanged...
      clickNode: (event) => {
        if (isDraggingRef.current) {
          isDraggingRef.current = false;
          return;
        }
        const { clientX: x, clientY: y } = event.event?.original || { clientX: 0, clientY: 0 };
        const node = event.node;
        const { x: tooltipX, y: tooltipY } = calculateTooltipPosition(x, y);
        setTooltip({ visible: true, nodeId: node, x: tooltipX, y: tooltipY });
        setEdgeTooltip({ visible: false, edgeId: '', x: 0, y: 0 });
      },

      // ...rest of your handlers unchanged
      enterEdge: (event) => {
        const { edge } = event;
        sigma.getGraph().updateEdgeAttributes(edge, (attrs) => ({
          ...attrs,
          color: '#ff9900',
          size: attrs.size * 2,
        }));
        sigma.refresh();
      },
      clickEdge: (event) => {
        const { clientX: x, clientY: y } = event.event?.original || { clientX: 0, clientY: 0 };
        const edgeId = event.edge;
        const { x: tooltipX, y: tooltipY } = calculateTooltipPosition(x, y);
        setEdgeTooltip({ visible: true, edgeId, x: tooltipX, y: tooltipY });
        setTooltip({ visible: false, nodeId: '', x: 0, y: 0 });
      },
      leaveEdge: (event) => {
        const { edge } = event;
        sigma.getGraph().updateEdgeAttributes(edge, (attrs) => ({
          ...attrs,
          color: theme === ThemeEnum.LIGHT ? '#aaa' : '#555',
          size: 5,
        }));
        sigma.refresh();
      },
    });
  }, [
    registerEvents,
    sigma,
    nodeTooltip?.visible,
    edgeTooltip?.visible,
    draggedNode,
    draggedEdge,
    theme,
    groupDragging,
  ]);
  return null;
};

export default GraphLoader;
