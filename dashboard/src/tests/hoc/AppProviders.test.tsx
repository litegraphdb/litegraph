import '@testing-library/jest-dom';
import React from 'react';
import { render, act } from '@testing-library/react';
import AppProviders from '@/hoc/AppProviders';

// Mock all the dependencies
jest.mock('@ant-design/cssinjs', () => ({
  StyleProvider: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="style-provider">{children}</div>
  ),
}));

jest.mock('@ant-design/nextjs-registry', () => ({
  AntdRegistry: ({ children }: { children: React.ReactNode }) => (
    <div data-testid="antd-registry">{children}</div>
  ),
}));

jest.mock('@/hooks/appHooks', () => ({
  AppContext: {
    Provider: ({ children, value }: { children: React.ReactNode; value: any }) => (
      <div data-testid="app-context-provider" data-theme={value.theme}>
        {children}
      </div>
    ),
  },
}));

jest.mock('@/theme/theme', () => ({
  darkTheme: { token: { colorPrimary: '#000000' } },
  primaryTheme: { token: { colorPrimary: '#ffffff' } },
}));

jest.mock('antd', () => ({
  ConfigProvider: ({ children, theme }: { children: React.ReactNode; theme: any }) => (
    <div data-testid="config-provider" data-theme-config={JSON.stringify(theme)}>
      {children}
    </div>
  ),
}));

jest.mock('@/components/layout/AuthLayout', () => {
  return function MockAuthLayout({
    children,
    className,
  }: {
    children: React.ReactNode;
    className?: string;
  }) {
    return (
      <div data-testid="auth-layout" className={className}>
        {children}
      </div>
    );
  };
});

jest.mock('@/lib/store/StoreProvider', () => {
  return function MockStoreProvider({ children }: { children: React.ReactNode }) {
    return <div data-testid="store-provider">{children}</div>;
  };
});

jest.mock('react-hot-toast', () => ({
  Toaster: () => <div data-testid="toaster" />,
}));

jest.mock('@/constants/constant', () => ({
  localStorageKeys: {
    theme: 'theme',
  },
}));

// Mock localStorage
const mockLocalStorage = {
  getItem: jest.fn(),
  setItem: jest.fn(),
  removeItem: jest.fn(),
  clear: jest.fn(),
};

Object.defineProperty(window, 'localStorage', {
  value: mockLocalStorage,
  writable: true,
});

describe('AppProviders Component', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockLocalStorage.getItem.mockReturnValue(null);
  });

  it('renders without crashing', () => {
    const { getByTestId } = render(
      <AppProviders>
        <div>Test children</div>
      </AppProviders>
    );

    expect(getByTestId('store-provider')).toBeInTheDocument();
  });

  it('renders children correctly', () => {
    const { getByText } = render(
      <AppProviders>
        <div>Test children content</div>
      </AppProviders>
    );

    expect(getByText('Test children content')).toBeInTheDocument();
  });

  it('renders complete provider hierarchy', () => {
    const { getByTestId } = render(
      <AppProviders>
        <div>Test children</div>
      </AppProviders>
    );

    expect(getByTestId('store-provider')).toBeInTheDocument();
    expect(getByTestId('app-context-provider')).toBeInTheDocument();
    expect(getByTestId('style-provider')).toBeInTheDocument();
    expect(getByTestId('antd-registry')).toBeInTheDocument();
    expect(getByTestId('config-provider')).toBeInTheDocument();
    expect(getByTestId('auth-layout')).toBeInTheDocument();
    expect(getByTestId('toaster')).toBeInTheDocument();
  });

  it('maintains proper provider nesting order', () => {
    const { getByTestId } = render(
      <AppProviders>
        <div>Test children</div>
      </AppProviders>
    );

    const storeProvider = getByTestId('store-provider');
    const appContextProvider = getByTestId('app-context-provider');
    const styleProvider = getByTestId('style-provider');
    const antdRegistry = getByTestId('antd-registry');
    const configProvider = getByTestId('config-provider');
    const authLayout = getByTestId('auth-layout');

    // Check nesting hierarchy
    expect(storeProvider).toContainElement(appContextProvider);
    expect(appContextProvider).toContainElement(styleProvider);
    expect(styleProvider).toContainElement(antdRegistry);
    expect(antdRegistry).toContainElement(configProvider);
    expect(configProvider).toContainElement(authLayout);
  });

  it('initializes with theme from localStorage when available', () => {
    mockLocalStorage.getItem.mockReturnValue('DARK');

    const { getByTestId } = render(
      <AppProviders>
        <div>Test children</div>
      </AppProviders>
    );

    const appContextProvider = getByTestId('app-context-provider');
    expect(appContextProvider).toHaveAttribute('data-theme', 'DARK');
  });

  it('applies light theme class to AuthLayout when theme is LIGHT', () => {
    const { getByTestId } = render(
      <AppProviders>
        <div>Test children</div>
      </AppProviders>
    );

    const authLayout = getByTestId('auth-layout');
    expect(authLayout).not.toHaveClass('theme-dark-mode');
  });

  it('configures ConfigProvider with correct theme', () => {
    const { getByTestId } = render(
      <AppProviders>
        <div>Test children</div>
      </AppProviders>
    );

    const configProvider = getByTestId('config-provider');
    const themeConfig = JSON.parse(configProvider.getAttribute('data-theme-config') || '{}');
    expect(themeConfig).toEqual({ token: { colorPrimary: '#ffffff' } });
  });

  it('configures ConfigProvider with dark theme when theme is DARK', () => {
    mockLocalStorage.getItem.mockReturnValue('DARK');

    const { getByTestId } = render(
      <AppProviders>
        <div>Test children</div>
      </AppProviders>
    );

    const configProvider = getByTestId('config-provider');
    const themeConfig = JSON.parse(configProvider.getAttribute('data-theme-config') || '{}');
    expect(themeConfig).toEqual({ token: { colorPrimary: '#000000' } });
  });

  it('handles multiple children', () => {
    const { getByText } = render(
      <AppProviders>
        <div>First child</div>
        <div>Second child</div>
        <div>Third child</div>
      </AppProviders>
    );

    expect(getByText('First child')).toBeInTheDocument();
    expect(getByText('Second child')).toBeInTheDocument();
    expect(getByText('Third child')).toBeInTheDocument();
  });

  it('handles complex nested children', () => {
    const { getByText } = render(
      <AppProviders>
        <div>
          <h1>Title</h1>
          <p>
            Paragraph with <strong>bold text</strong>
          </p>
        </div>
      </AppProviders>
    );

    expect(getByText('Title')).toBeInTheDocument();
    expect(getByText('Paragraph with')).toBeInTheDocument();
    expect(getByText('bold text')).toBeInTheDocument();
  });

  it('handles empty children', () => {
    const { getByTestId } = render(<AppProviders>{null}</AppProviders>);

    expect(getByTestId('auth-layout')).toBeInTheDocument();
  });

  it('handles React fragments as children', () => {
    const { getByText } = render(
      <AppProviders>
        <>
          <div>Fragment child 1</div>
          <div>Fragment child 2</div>
        </>
      </AppProviders>
    );

    expect(getByText('Fragment child 1')).toBeInTheDocument();
    expect(getByText('Fragment child 2')).toBeInTheDocument();
  });

  it('renders children at the correct level in provider hierarchy', () => {
    const { getByText, getByTestId } = render(
      <AppProviders>
        <div>Test children</div>
      </AppProviders>
    );

    const authLayout = getByTestId('auth-layout');
    const childrenElement = getByText('Test children');

    expect(authLayout).toContainElement(childrenElement);
  });

  it('exports as default component', () => {
    const AppProvidersModule = require('@/hoc/AppProviders');

    expect(AppProvidersModule.default).toBeDefined();
    expect(typeof AppProvidersModule.default).toBe('function');
  });

  it('has correct component name', () => {
    const AppProvidersModule = require('@/hoc/AppProviders');

    expect(AppProvidersModule.default.name).toBe('AppProviders');
  });

  it('maintains component identity', () => {
    const AppProvidersComponent = require('@/hoc/AppProviders').default;

    expect(AppProvidersComponent).toBe(AppProviders);
  });

  it('has proper TypeScript types', () => {
    // This test ensures the component has proper typing
    const AppProvidersComponent: React.ComponentType = require('@/hoc/AppProviders').default;
    expect(typeof AppProvidersComponent).toBe('function');
  });

  it('handles different types of children content', () => {
    const { getByText } = render(
      <AppProviders>
        <div>Text content</div>
        <span>Inline content</span>
        <button>Button content</button>
      </AppProviders>
    );

    expect(getByText('Text content')).toBeInTheDocument();
    expect(getByText('Inline content')).toBeInTheDocument();
    expect(getByText('Button content')).toBeInTheDocument();
  });
});
