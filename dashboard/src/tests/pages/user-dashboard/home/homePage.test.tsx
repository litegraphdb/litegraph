import { screen, fireEvent, waitFor } from '@testing-library/react';
import { createMockInitialState } from '@/tests/store/mockStore';
import HomePage from '@/page/user-dashboard/home/HomePage';
import { renderWithRedux } from '@/tests/store/utils';
import { commonHandlers } from '@/tests/handler'; // Import common handlers
import { graphHandlers } from './handler'; // Import graph-related handlers
import { setTenant } from '@/lib/sdk/litegraph.service';
import { getServer } from '@/tests/server';
import { mockTenantData } from '../../mockData';
import PageLoading from '@/components/base/loading/PageLoading';
import FallBack from '@/components/base/fallback/FallBack';
import GraphViewer from '@/components/base/graph/GraphViewer';

const server = getServer([...commonHandlers, ...graphHandlers]);
setTenant(mockTenantData[0].GUID);

describe('HomePage', () => {
  beforeAll(() => {
    server.listen();
  });
  afterEach(() => server.resetHandlers());
  afterAll(() => server.close());

  it('should render HomePage component', async () => {
    const initialState = createMockInitialState();
    renderWithRedux(<HomePage />, initialState, undefined, true);
    expect(screen.getByText('Home')).toBeInTheDocument();
  });

  it('should display loading state when graphs are loading', async () => {
    const initialState = createMockInitialState();
    renderWithRedux(<PageLoading message="Loading..." />, initialState, undefined, true);

    await waitFor(() => {
      const loadingElement = screen.getByTestId('loading-message');
      expect(loadingElement).toBeInTheDocument();
    });
  });

  it('should display error message if graph fails to load', async () => {
    const initialState = createMockInitialState();
    renderWithRedux(
      <FallBack retry={() => {}}>Something went wrong.</FallBack>,
      initialState,
      undefined,
      true
    );

    await waitFor(() => {
      expect(screen.getByText('Something went wrong.')).toBeInTheDocument();
    });
  });

  it('should display "Add Node" and "Add Edge" buttons if graph is selected', async () => {
    const initialState = createMockInitialState();
    renderWithRedux(<HomePage />, initialState, undefined, true);

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /add node/i })).toBeInTheDocument();
      expect(screen.getByRole('button', { name: /add edge/i })).toBeInTheDocument();
    });
  });

  it('should open "Add Node" modal when "Add Node" button is clicked', async () => {
    const initialState = createMockInitialState();
    renderWithRedux(<HomePage />, initialState, undefined, true);

    fireEvent.click(screen.getByRole('button', { name: /add node/i }));

    await waitFor(() => {
      expect(screen.getByText('Add Node')).toBeInTheDocument();
    });
  });

  it('should open "Add Edge" modal when "Add Edge" button is clicked', async () => {
    const initialState = createMockInitialState();
    renderWithRedux(<HomePage />, initialState, undefined, true);

    fireEvent.click(screen.getByRole('button', { name: /add edge/i }));

    await waitFor(() => {
      expect(screen.getByText('Add Edge')).toBeInTheDocument();
    });
  });

  it('should render GraphViewer component correctly', async () => {
    const initialState = createMockInitialState();
    renderWithRedux(<HomePage />, initialState, undefined, true);

    await waitFor(() => {
      expect(screen.getByTestId('graph-viewer')).toBeInTheDocument();
    });
  });
});
