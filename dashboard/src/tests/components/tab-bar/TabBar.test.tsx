import '@testing-library/jest-dom';
import React from 'react';
import { fireEvent, render, screen } from '@testing-library/react';
import TabBar, { TabBarItem } from '@/components/tab-bar/TabBar';

const mockPush = jest.fn();
jest.mock('next/navigation', () => ({
  useRouter: () => ({ push: mockPush, replace: jest.fn(), prefetch: jest.fn() }),
}));

const items: TabBarItem[] = [
  { key: 'graphs', label: 'Graphs', title: 'Manage graphs', href: '/dashboard/t/graphs/graphs' },
  { key: 'nodes', label: 'Nodes', href: '/dashboard/t/graphs/nodes?graph=g1' },
  { key: 'edges', label: 'Edges', href: '/dashboard/t/graphs/edges?graph=g1' },
];

describe('TabBar', () => {
  beforeEach(() => mockPush.mockClear());

  it('renders a labelled tab list with one tab per item and marks the active tab', () => {
    render(<TabBar items={items} activeKey="nodes" ariaLabel="Graphs tabs" />);
    expect(screen.getByRole('navigation', { name: 'Graphs tabs' })).toBeInTheDocument();
    const tabs = screen.getAllByRole('tab');
    expect(tabs).toHaveLength(3);
    expect(screen.getByRole('tab', { name: 'Nodes' })).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByRole('tab', { name: 'Graphs' })).toHaveAttribute('aria-selected', 'false');
    expect(screen.getByTestId('hub-tab-graphs')).toHaveAttribute('title', 'Manage graphs');
  });

  it('navigates to the tab href when a tab is clicked', () => {
    render(<TabBar items={items} activeKey="graphs" ariaLabel="Graphs tabs" />);
    fireEvent.click(screen.getByRole('tab', { name: 'Edges' }));
    expect(mockPush).toHaveBeenCalledWith('/dashboard/t/graphs/edges?graph=g1');
  });

  it('does not navigate when the active tab is clicked again', () => {
    render(<TabBar items={items} activeKey="graphs" ariaLabel="Graphs tabs" />);
    fireEvent.click(screen.getByRole('tab', { name: 'Graphs' }));
    expect(mockPush).not.toHaveBeenCalled();
  });

  it('is keyboard operable: arrow keys move between tabs and Enter activates one', () => {
    render(<TabBar items={items} activeKey="graphs" ariaLabel="Graphs tabs" />);
    const graphs = screen.getByRole('tab', { name: 'Graphs' });
    expect(graphs).toHaveAttribute('tabindex', '0');
    graphs.focus();
    fireEvent.keyDown(graphs, { key: 'ArrowRight', code: 'ArrowRight' });
    // antd activates the focused tab on Enter wherever the key event lands in the tab list.
    fireEvent.keyDown(graphs, { key: 'Enter', code: 'Enter' });
    expect(mockPush).toHaveBeenCalledWith('/dashboard/t/graphs/nodes?graph=g1');
  });

  it('renders no tab as active on a bare hub path', () => {
    render(<TabBar items={items} activeKey={null} ariaLabel="Graphs tabs" />);
    for (const tab of screen.getAllByRole('tab')) {
      expect(tab).toHaveAttribute('aria-selected', 'false');
    }
  });

  it('renders the optional right slot only when given', () => {
    const { rerender } = render(
      <TabBar items={items} activeKey="graphs" ariaLabel="Graphs tabs" />
    );
    expect(screen.queryByTestId('hub-tab-bar-right')).not.toBeInTheDocument();
    rerender(
      <TabBar
        items={items}
        activeKey="graphs"
        ariaLabel="Graphs tabs"
        rightSlot={<span data-testid="slot">Graph selector</span>}
      />
    );
    expect(screen.getByTestId('hub-tab-bar-right')).toContainElement(screen.getByTestId('slot'));
  });
});
