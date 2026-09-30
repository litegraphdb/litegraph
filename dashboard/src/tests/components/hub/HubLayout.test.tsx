import '@testing-library/jest-dom';
import React from 'react';
import { fireEvent, render, screen, within } from '@testing-library/react';
import HubLayout from '@/components/hub/HubLayout';
import { Principal } from '@/lib/authz/capabilities';

let mockPathname = '';
const mockPush = jest.fn();
jest.mock('next/navigation', () => ({
  usePathname: () => mockPathname,
  useRouter: () => ({ push: mockPush, replace: jest.fn(), prefetch: jest.fn() }),
}));

let mockPrincipal: Principal;
jest.mock('@/hooks/permissionHooks', () => {
  const actual = jest.requireActual('@/lib/authz/capabilities');
  return {
    usePrincipal: () => mockPrincipal,
    useCan: () => ({
      principal: mockPrincipal,
      can: (action: any, resource: any, scope: any) =>
        actual.can(mockPrincipal, action, resource, scope),
    }),
  };
});

jest.mock('@/hooks/hooks', () => ({
  useAppDynamicNavigation: () => ({ navigate: jest.fn(), serializePath: (p: string) => p }),
}));

let mockSelectedGraph = '';
jest.mock('@/hooks/entityHooks', () => ({
  useCurrentTenant: () => ({ GUID: 'redux-tenant' }),
  useSelectedGraph: () => mockSelectedGraph,
}));

const mockGraphsQuery = jest.fn();
jest.mock('@/lib/store/slice/slice', () => ({
  useGetAllGraphsQuery: (...args: any[]) => mockGraphsQuery(...args),
}));

jest.mock('@/components/hub/GraphScopeSelector', () => ({
  __esModule: true,
  GRAPH_SCOPE_PARAM: 'graph',
  default: () => <div data-testid="graph-scope-stub" />,
}));

const principal = (overrides: Partial<Principal>): Principal => ({
  isSystemAdmin: false,
  isTenantAdmin: false,
  isBreakGlass: false,
  userGuid: 'u',
  tenantGuid: 't1',
  ...overrides,
});
const systemAdmin = principal({ isSystemAdmin: true });
const tenantAdmin = principal({ isTenantAdmin: true });
const regular = principal({});

const tabNames = () => screen.getAllByRole('tab').map((tab: HTMLElement) => tab.textContent);

const renderHub = (hubId: any, pathname: string) => {
  mockPathname = pathname;
  return render(
    <HubLayout hubId={hubId}>
      <div data-testid="tab-content">content</div>
    </HubLayout>
  );
};

describe('HubLayout', () => {
  beforeEach(() => {
    mockSelectedGraph = '';
    mockPush.mockClear();
    mockGraphsQuery.mockReturnValue({ isLoading: false, error: undefined, refetch: jest.fn() });
  });

  describe('visible tabs per role', () => {
    it('Graphs hub shows all seven tabs to every role', () => {
      for (const p of [systemAdmin, tenantAdmin, regular]) {
        mockPrincipal = p;
        const { unmount } = renderHub('graphs', '/dashboard/t1/graphs/nodes');
        expect(tabNames()).toEqual([
          'Graphs',
          'Nodes',
          'Edges',
          'Labels',
          'Tags',
          'Vectors',
          'Algorithms',
        ]);
        unmount();
      }
    });

    it('Chat hub shows every tab to admins and only Chat to regular users', () => {
      mockPrincipal = tenantAdmin;
      const { unmount } = renderHub('chat', '/dashboard/t1/chat/chat');
      expect(tabNames()).toEqual(['Chat', 'History', 'Feedback', 'Endpoints', 'Settings']);
      unmount();
      mockPrincipal = regular;
      renderHub('chat', '/dashboard/t1/chat/chat');
      expect(tabNames()).toEqual(['Chat']);
    });

    it('Access hub hides Authorization from regular users', () => {
      mockPrincipal = systemAdmin;
      const { unmount } = renderHub('access', '/dashboard/access/tenants');
      expect(tabNames()).toEqual(['Tenants', 'Users', 'Credentials', 'Authorization']);
      unmount();
      mockPrincipal = regular;
      renderHub('access', '/dashboard/access/tenants');
      expect(tabNames()).toEqual(['Tenants', 'Users', 'Credentials']);
    });

    it('System hub shows Settings, Cluster and Backups to a system administrator', () => {
      mockPrincipal = systemAdmin;
      renderHub('system', '/dashboard/system/cluster');
      expect(tabNames()).toEqual(['Settings', 'Cluster', 'Backups']);
      expect(screen.getByRole('tab', { name: 'Cluster' })).toHaveAttribute('aria-selected', 'true');
    });

    it('Developer hub shows API Requests and API Explorer', () => {
      mockPrincipal = regular;
      renderHub('developer', '/dashboard/t1/developer/api-explorer');
      expect(tabNames()).toEqual(['API Requests', 'API Explorer']);
    });
  });

  it('labels the tab list with the hub name', () => {
    mockPrincipal = systemAdmin;
    renderHub('graphs', '/dashboard/t1/graphs/graphs');
    expect(screen.getByRole('navigation', { name: 'Graphs tabs' })).toBeInTheDocument();
  });

  it('renders the active tab content', () => {
    mockPrincipal = systemAdmin;
    renderHub('graphs', '/dashboard/t1/graphs/edges');
    expect(screen.getByRole('tab', { name: 'Edges' })).toHaveAttribute('aria-selected', 'true');
    expect(screen.getByTestId('tab-content')).toBeInTheDocument();
  });

  it('denies a forbidden tab URL like the forbidden page, keeping the tab bar', () => {
    mockPrincipal = regular;
    renderHub('system', '/dashboard/system/settings');
    expect(screen.queryByTestId('tab-content')).not.toBeInTheDocument();
    expect(screen.getByTestId('capability-denied')).toBeInTheDocument();

    mockPrincipal = regular;
    const { container } = renderHub('chat', '/dashboard/t1/chat/endpoints');
    expect(within(container).getByTestId('capability-denied')).toBeInTheDocument();
  });

  it('builds tab links from the tenant in the URL', () => {
    mockPrincipal = systemAdmin;
    renderHub('graphs', '/dashboard/url-tenant/graphs/graphs');
    fireEvent.click(screen.getByRole('tab', { name: 'Nodes' }));
    expect(mockPush).toHaveBeenCalledWith('/dashboard/url-tenant/graphs/nodes');
  });

  it('carries the selected graph into graph-scoped tab links', () => {
    mockPrincipal = systemAdmin;
    mockSelectedGraph = 'graph-7';
    renderHub('graphs', '/dashboard/t1/graphs/graphs');
    fireEvent.click(screen.getByRole('tab', { name: 'Vectors' }));
    expect(mockPush).toHaveBeenCalledWith('/dashboard/t1/graphs/vectors?graph=graph-7');
  });

  it('shows the graph selector only on graph-scoped tabs', () => {
    mockPrincipal = systemAdmin;
    const { unmount } = renderHub('graphs', '/dashboard/t1/graphs/tags');
    expect(screen.getByTestId('graph-scope-stub')).toBeInTheDocument();
    unmount();

    renderHub('chat', '/dashboard/t1/chat/history');
    expect(screen.queryByTestId('graph-scope-stub')).not.toBeInTheDocument();
  });

  it('queries graphs only for hubs with graph-scoped tabs', () => {
    mockPrincipal = systemAdmin;
    renderHub('system', '/dashboard/system/settings');
    expect(mockGraphsQuery).toHaveBeenLastCalledWith(undefined, { skip: true });
    renderHub('graphs', '/dashboard/t1/graphs/nodes');
    expect(mockGraphsQuery).toHaveBeenLastCalledWith(undefined, { skip: false });
  });
});
