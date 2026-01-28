import { createContext, useContext } from 'react';

export const LayoutContext = createContext<{
  isGraphsLoading: boolean;
  graphError: any;
  refetchGraphs: () => void;
}>({
  isGraphsLoading: false,
  graphError: null,
  refetchGraphs: () => {},
});

export const useLayoutContext = () => useContext(LayoutContext);
