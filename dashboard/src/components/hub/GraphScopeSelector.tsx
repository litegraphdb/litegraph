'use client';
import React, { useCallback, useEffect, useMemo, useRef } from 'react';
import { useTranslations } from 'next-intl';
import { usePathname, useRouter, useSearchParams } from 'next/navigation';
import LitegraphSelect from '@/components/base/select/Select';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';
import { useSelectedGraph } from '@/hooks/entityHooks';
import { transformToOptions } from '@/lib/graph/utils';
import { useAppDispatch } from '@/lib/store/hooks';
import { storeSelectedGraph } from '@/lib/store/litegraph/actions';
import { useGetAllGraphsQuery } from '@/lib/store/slice/slice';

/** Query-string parameter that carries the graph scope shared by the graph-scoped tabs. */
export const GRAPH_SCOPE_PARAM = 'graph';

/**
 * The one graph selector for graph-scoped tabs, shown at the right of the hub tab bar. The selected graph lives in
 * the URL (`?graph=<guid>`) and is mirrored into the store, which is what the pages read (`useSelectedGraph`):
 * - a `graph` value in the URL wins and is copied into the store (deep links, back and forward);
 * - with no `graph` in the URL, the store's graph is written to the URL (replace, keeping other parameters);
 * - with neither, the first graph is selected;
 * - choosing a graph updates both.
 */
const GraphScopeSelector = () => {
  const t = useTranslations('header');
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const dispatch = useAppDispatch();
  const selectedGraph = useSelectedGraph();
  const { data: graphsEnvelope, isLoading } = useGetAllGraphsQuery(undefined);
  const graphOptions = useMemo(() => transformToOptions(graphsEnvelope?.Objects), [graphsEnvelope]);
  const urlGraph = searchParams?.get(GRAPH_SCOPE_PARAM) || null;
  // The URL value last copied into the store; a store change made here is not undone by the stale URL value.
  const appliedUrlGraph = useRef<string | null>(null);

  const writeUrl = useCallback(
    (graph: string) => {
      const next = new URLSearchParams(searchParams?.toString() ?? '');
      next.set(GRAPH_SCOPE_PARAM, graph);
      router.replace(`${pathname}?${next.toString()}`);
    },
    [router, pathname, searchParams]
  );

  useEffect(() => {
    if (urlGraph && urlGraph !== appliedUrlGraph.current) {
      appliedUrlGraph.current = urlGraph;
      if (urlGraph !== selectedGraph) dispatch(storeSelectedGraph({ graph: urlGraph }));
    }
  }, [urlGraph, selectedGraph, dispatch]);

  useEffect(() => {
    if (urlGraph) return;
    if (selectedGraph) {
      writeUrl(selectedGraph);
    } else if (graphOptions.length > 0) {
      dispatch(storeSelectedGraph({ graph: String(graphOptions[0].value) }));
    }
  }, [urlGraph, selectedGraph, graphOptions, writeUrl, dispatch]);

  const handleChange = (value: unknown) => {
    const graph = String(value);
    dispatch(storeSelectedGraph({ graph }));
    writeUrl(graph);
  };

  return (
    <>
      <LitegraphTooltip title={t('selectGraph')}>
        <span>{t('graphLabel')}</span>
      </LitegraphTooltip>
      <LitegraphSelect
        size="small"
        aria-label={t('selectGraph')}
        placeholder={t('selectAGraph')}
        options={graphOptions}
        value={selectedGraph || undefined}
        onChange={handleChange}
        style={{ width: 250, maxWidth: '100%' }}
        loading={isLoading}
        data-testid="graph-scope-select"
        tooltip={true}
      />
    </>
  );
};

export default GraphScopeSelector;
