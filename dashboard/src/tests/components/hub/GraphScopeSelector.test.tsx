import '@testing-library/jest-dom';
import React from 'react';
import { fireEvent, render, screen } from '@testing-library/react';
import GraphScopeSelector from '@/components/hub/GraphScopeSelector';
import { storeSelectedGraph } from '@/lib/store/litegraph/actions';

let mockSearch = '';
let mockPathname = '/dashboard/t1/graphs/nodes';
const mockReplace = jest.fn();
jest.mock('next/navigation', () => ({
  usePathname: () => mockPathname,
  useSearchParams: () => new URLSearchParams(mockSearch),
  useRouter: () => ({ push: jest.fn(), replace: mockReplace, prefetch: jest.fn() }),
}));

let mockSelectedGraph = '';
jest.mock('@/hooks/entityHooks', () => ({
  useSelectedGraph: () => mockSelectedGraph,
}));

const mockDispatch = jest.fn();
jest.mock('@/lib/store/hooks', () => ({
  useAppDispatch: () => mockDispatch,
}));

// A native select stands in for the antd one so the test drives the selection directly.
jest.mock('@/components/base/select/Select', () => ({
  __esModule: true,
  default: ({ options, value, onChange, 'data-testid': testId, 'aria-label': ariaLabel }: any) => (
    <select
      data-testid={testId}
      aria-label={ariaLabel}
      value={value ?? ''}
      onChange={(e) => onChange(e.target.value)}
    >
      <option value="" />
      {options.map((o: any) => (
        <option key={o.value} value={o.value}>
          {o.label}
        </option>
      ))}
    </select>
  ),
}));

let mockGraphs: { GUID: string; Name: string }[] = [];
jest.mock('@/lib/store/slice/slice', () => ({
  useGetAllGraphsQuery: () => ({ data: { Objects: mockGraphs }, isLoading: false }),
}));

describe('GraphScopeSelector (shared graph selector)', () => {
  beforeEach(() => {
    mockReplace.mockClear();
    mockDispatch.mockClear();
    mockSearch = '';
    mockPathname = '/dashboard/t1/graphs/nodes';
    mockSelectedGraph = '';
    mockGraphs = [
      { GUID: 'g1', Name: 'First' },
      { GUID: 'g2', Name: 'Second' },
    ];
  });

  it('copies a graph in the URL into the store (deep link)', () => {
    mockSearch = 'graph=g2';
    mockSelectedGraph = 'g1';
    render(<GraphScopeSelector />);
    expect(mockDispatch).toHaveBeenCalledWith(storeSelectedGraph({ graph: 'g2' }));
    expect(mockReplace).not.toHaveBeenCalled();
  });

  it('writes the store graph to the URL when the URL has none, keeping other parameters', () => {
    mockSearch = 'thread=abc';
    mockSelectedGraph = 'g1';
    render(<GraphScopeSelector />);
    expect(mockReplace).toHaveBeenCalledWith('/dashboard/t1/graphs/nodes?thread=abc&graph=g1');
    expect(mockDispatch).not.toHaveBeenCalled();
  });

  it('selects the first graph when neither the URL nor the store has one', () => {
    render(<GraphScopeSelector />);
    expect(mockDispatch).toHaveBeenCalledWith(storeSelectedGraph({ graph: 'g1' }));
  });

  it('does nothing while the URL and the store agree', () => {
    mockSearch = 'graph=g1';
    mockSelectedGraph = 'g1';
    render(<GraphScopeSelector />);
    expect(mockDispatch).not.toHaveBeenCalled();
    expect(mockReplace).not.toHaveBeenCalled();
  });

  it('round-trips a user selection into the store and the URL', () => {
    mockSearch = 'graph=g1';
    mockSelectedGraph = 'g1';
    const { rerender } = render(<GraphScopeSelector />);
    fireEvent.change(screen.getByTestId('graph-scope-select'), { target: { value: 'g2' } });
    expect(mockDispatch).toHaveBeenCalledWith(storeSelectedGraph({ graph: 'g2' }));
    expect(mockReplace).toHaveBeenCalledWith('/dashboard/t1/graphs/nodes?graph=g2');

    // The store updates before the URL does: the stale URL value must not undo the selection.
    mockDispatch.mockClear();
    mockSelectedGraph = 'g2';
    rerender(<GraphScopeSelector />);
    expect(mockDispatch).not.toHaveBeenCalled();

    // Then the URL catches up; back navigation to the old URL restores the old graph.
    mockSearch = 'graph=g2';
    rerender(<GraphScopeSelector />);
    expect(mockDispatch).not.toHaveBeenCalled();
    mockSearch = 'graph=g1';
    rerender(<GraphScopeSelector />);
    expect(mockDispatch).toHaveBeenCalledWith(storeSelectedGraph({ graph: 'g1' }));
  });

  it('keeps the URL graph when switching to another graph-scoped tab', () => {
    // A tab link carries ?graph=; the selector on the new tab keeps it and changes nothing.
    mockPathname = '/dashboard/t1/graphs/vectors';
    mockSearch = 'graph=g2';
    mockSelectedGraph = 'g2';
    render(<GraphScopeSelector />);
    expect(mockDispatch).not.toHaveBeenCalled();
    expect(mockReplace).not.toHaveBeenCalled();
  });
});
