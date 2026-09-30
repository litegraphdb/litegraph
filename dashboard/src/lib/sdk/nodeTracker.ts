import { useSyncExternalStore } from 'react';

/** Response header naming the LiteGraph node that answered (v10.0). */
export const NODE_HEADER = 'x-litegraph-node';

type Listener = () => void;

let lastNodeId: string | null = null;
let seenNodeCount = 0;
const seenNodes = new Set<string>();
const listeners = new Set<Listener>();

const remember = (value: string): void => {
  if (seenNodes.has(value)) return;
  seenNodes.add(value);
  seenNodeCount = seenNodes.size;
};

/** Node that answered the most recent request made through the dashboard's fetch helpers, or null. */
export const getLastNodeId = (): string | null => lastNodeId;

/** Record the node named by a response's x-litegraph-node header; responses without the header are ignored. */
export const recordNodeFromResponse = (response: { headers?: { get?: (name: string) => string | null } } | null | undefined): void => {
  const value = response?.headers?.get?.(NODE_HEADER);
  if (!value || value === lastNodeId) return;
  remember(value);
  lastNodeId = value;
  listeners.forEach((listener) => listener());
};

/** Replace the recorded node directly (tests and non-fetch clients). */
export const setLastNodeId = (value: string | null): void => {
  if (value === lastNodeId) return;
  if (value) remember(value);
  lastNodeId = value;
  listeners.forEach((listener) => listener());
};

/** Number of distinct nodes seen answering requests; more than one means the dashboard is talking to a cluster. */
export const getSeenNodeCount = (): number => seenNodeCount;

/** Forget every node seen so far (tests and signing out). */
export const resetNodeTracker = (): void => {
  seenNodes.clear();
  seenNodeCount = 0;
  lastNodeId = null;
  listeners.forEach((listener) => listener());
};

/** Subscribe to changes of the last answering node; returns an unsubscribe function. */
export const subscribeLastNodeId = (listener: Listener): (() => void) => {
  listeners.add(listener);
  return () => {
    listeners.delete(listener);
  };
};

/** React hook returning the node that answered the most recent request, or null. */
export const useLastNodeId = (): string | null =>
  useSyncExternalStore(subscribeLastNodeId, getLastNodeId, () => null);

/** React hook returning how many distinct nodes have answered requests so far. */
export const useSeenNodeCount = (): number =>
  useSyncExternalStore(subscribeLastNodeId, getSeenNodeCount, () => 0);
