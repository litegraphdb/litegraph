import '@testing-library/jest-dom';
import React from 'react';
import { render } from '@testing-library/react';
import StoreProvider from '@/lib/store/StoreProvider';

// Mock the store
jest.mock('@/lib/store/store', () => ({
  makeStore: jest.fn(() => ({
    dispatch: jest.fn(),
    getState: jest.fn(),
    subscribe: jest.fn(),
  })),
}));

// Mock react-redux Provider
jest.mock('react-redux', () => ({
  Provider: jest.fn(({ children, store }) => (
    <div data-testid="redux-provider" data-store={store ? 'store-exists' : 'no-store'}>
      {children}
    </div>
  )),
}));

describe('StoreProvider', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('should render without crashing', () => {
    const { getByTestId } = render(
      <StoreProvider>
        <div>Test Child</div>
      </StoreProvider>
    );

    expect(getByTestId('redux-provider')).toBeInTheDocument();
  });

  it('should render children', () => {
    const { getByText } = render(
      <StoreProvider>
        <div>Test Child Content</div>
      </StoreProvider>
    );

    expect(getByText('Test Child Content')).toBeInTheDocument();
  });

  it('should provide store to Provider', () => {
    const { getByTestId } = render(
      <StoreProvider>
        <div>Test Child</div>
      </StoreProvider>
    );

    const provider = getByTestId('redux-provider');
    expect(provider).toHaveAttribute('data-store', 'store-exists');
  });

  it('should create store only once', () => {
    const { makeStore } = require('@/lib/store/store');

    render(
      <StoreProvider>
        <div>Test Child</div>
      </StoreProvider>
    );

    expect(makeStore).toHaveBeenCalledTimes(1);
  });

  it('should not recreate store on re-render', () => {
    const { makeStore } = require('@/lib/store/store');
    const { rerender } = render(
      <StoreProvider>
        <div>Test Child</div>
      </StoreProvider>
    );

    // Clear the mock to check if it's called again
    makeStore.mockClear();

    rerender(
      <StoreProvider>
        <div>Different Child</div>
      </StoreProvider>
    );

    expect(makeStore).not.toHaveBeenCalled();
  });

  it('should handle multiple children', () => {
    const { getByText } = render(
      <StoreProvider>
        <div>Child 1</div>
        <div>Child 2</div>
        <div>Child 3</div>
      </StoreProvider>
    );

    expect(getByText('Child 1')).toBeInTheDocument();
    expect(getByText('Child 2')).toBeInTheDocument();
    expect(getByText('Child 3')).toBeInTheDocument();
  });

  it('should handle complex nested children', () => {
    const { getByText } = render(
      <StoreProvider>
        <div>
          <span>Nested Child</span>
          <div>
            <p>Deeply Nested</p>
          </div>
        </div>
      </StoreProvider>
    );

    expect(getByText('Nested Child')).toBeInTheDocument();
    expect(getByText('Deeply Nested')).toBeInTheDocument();
  });

  it('should maintain store reference across renders', () => {
    const { makeStore } = require('@/lib/store/store');
    const mockStore = { dispatch: jest.fn(), getState: jest.fn(), subscribe: jest.fn() };
    makeStore.mockReturnValue(mockStore);

    const { rerender } = render(
      <StoreProvider>
        <div>Test Child</div>
      </StoreProvider>
    );

    rerender(
      <StoreProvider>
        <div>Different Child</div>
      </StoreProvider>
    );

    // The same store instance should be used
    expect(makeStore).toHaveBeenCalledTimes(1);
  });

  it('should handle null children', () => {
    const { container } = render(<StoreProvider>{null}</StoreProvider>);

    expect(container.firstChild).toBeInTheDocument();
    expect(container.firstChild).toHaveAttribute('data-testid', 'redux-provider');
  });
});
