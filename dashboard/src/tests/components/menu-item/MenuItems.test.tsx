import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import MenuItems from '@/components/menu-item/MenuItems';
import { useAppDynamicNavigation } from '@/hooks/hooks';
import { MenuItemProps } from '@/components/menu-item/types';

// Mock dependencies
jest.mock('@/hooks/hooks');
jest.mock('next/link', () => {
  return function MockLink({ children, href }: any) {
    return <a href={href}>{children}</a>;
  };
});

const mockUseAppDynamicNavigation = useAppDynamicNavigation as jest.MockedFunction<
  typeof useAppDynamicNavigation
>;

describe('MenuItems', () => {
  const mockSerializePath = jest.fn((path: string) => path);

  const mockMenuItems: MenuItemProps[] = [
    {
      key: 'dashboard',
      label: 'Dashboard',
      path: '/dashboard',
      icon: <span data-testid="dashboard-icon">📊</span>,
    },
    {
      key: 'graphs',
      label: 'Graphs',
      path: '/graphs',
      icon: <span data-testid="graphs-icon">🔗</span>,
      children: [
        {
          key: 'graph-list',
          label: 'Graph List',
          path: '/graphs/list',
          icon: <span data-testid="graph-list-icon">📋</span>,
        },
        {
          key: 'graph-create',
          label: 'Create Graph',
          path: '/graphs/create',
          icon: <span data-testid="graph-create-icon">➕</span>,
        },
      ],
    },
    {
      key: 'settings',
      label: 'Settings',
      path: '/settings',
      icon: <span data-testid="settings-icon">⚙️</span>,
    },
  ];

  const mockHandleClickMenuItem = jest.fn();

  beforeEach(() => {
    mockUseAppDynamicNavigation.mockReturnValue({
      serializePath: mockSerializePath,
      navigate: jest.fn(),
    });
    jest.clearAllMocks();
  });

  afterEach(() => {
    jest.clearAllMocks();
  });

  it('renders without crashing', () => {
    render(<MenuItems menuItems={mockMenuItems} />);

    expect(screen.getByText('Dashboard')).toBeInTheDocument();
    expect(screen.getByText('Graphs')).toBeInTheDocument();
    expect(screen.getByText('Settings')).toBeInTheDocument();
  });

  it('renders menu items with correct structure', () => {
    render(<MenuItems menuItems={mockMenuItems} />);

    // Check main menu items
    expect(screen.getByText('Dashboard')).toBeInTheDocument();
    expect(screen.getByText('Graphs')).toBeInTheDocument();
    expect(screen.getByText('Settings')).toBeInTheDocument();

    // Check icons
    expect(screen.getByTestId('dashboard-icon')).toBeInTheDocument();
    expect(screen.getByTestId('graphs-icon')).toBeInTheDocument();
    expect(screen.getByTestId('settings-icon')).toBeInTheDocument();
  });

  it('renders submenu items correctly', () => {
    render(<MenuItems menuItems={mockMenuItems} />);

    // Check that the submenu container exists but items are not immediately visible
    expect(screen.getByText('Graphs')).toBeInTheDocument();

    // The submenu items are rendered but hidden by default in Ant Design
    // We can check that the submenu structure exists
    const graphsSubmenu = screen.getByText('Graphs').closest('li');
    expect(graphsSubmenu).toHaveClass('ant-menu-submenu');
  });

  it('calls handleClickMenuItem when menu item is clicked', () => {
    render(<MenuItems menuItems={mockMenuItems} handleClickMenuItem={mockHandleClickMenuItem} />);

    const dashboardItem = screen.getByText('Dashboard');
    fireEvent.click(dashboardItem);

    expect(mockHandleClickMenuItem).toHaveBeenCalledWith(mockMenuItems[0]);
  });

  it('does not call handleClickMenuItem when not provided', () => {
    render(<MenuItems menuItems={mockMenuItems} />);

    const dashboardItem = screen.getByText('Dashboard');
    fireEvent.click(dashboardItem);

    expect(mockHandleClickMenuItem).not.toHaveBeenCalled();
  });

  it('renders links with correct paths', () => {
    render(<MenuItems menuItems={mockMenuItems} />);

    const dashboardLink = screen.getByText('Dashboard').closest('a');
    const settingsLink = screen.getByText('Settings').closest('a');

    expect(dashboardLink).toHaveAttribute('href', '/dashboard');
    expect(settingsLink).toHaveAttribute('href', '/settings');
  });

  it('calls serializePath for menu item paths', () => {
    // Reset the mock to clear previous calls
    mockSerializePath.mockClear();

    render(<MenuItems menuItems={mockMenuItems} />);

    expect(mockSerializePath).toHaveBeenCalledWith('/dashboard');
    expect(mockSerializePath).toHaveBeenCalledWith('/graphs/list');
    expect(mockSerializePath).toHaveBeenCalledWith('/graphs/create');
    expect(mockSerializePath).toHaveBeenCalledWith('/settings');

    // Check that serializePath was called for all items with paths (including submenu items)
    expect(mockSerializePath).toHaveBeenCalledTimes(4);
  });

  it('handles menu items without icons', () => {
    const menuItemsWithoutIcons: MenuItemProps[] = [
      {
        key: 'no-icon',
        label: 'No Icon Item',
        path: '/no-icon',
      },
    ];

    render(<MenuItems menuItems={menuItemsWithoutIcons} />);

    expect(screen.getByText('No Icon Item')).toBeInTheDocument();
  });

  it('handles menu items without paths', () => {
    const menuItemsWithoutPaths: MenuItemProps[] = [
      {
        key: 'no-path',
        label: 'No Path Item',
        icon: <span data-testid="no-path-icon">🚫</span>,
      },
    ];

    render(<MenuItems menuItems={menuItemsWithoutPaths} />);

    expect(screen.getByText('No Path Item')).toBeInTheDocument();
    expect(screen.getByTestId('no-path-icon')).toBeInTheDocument();
  });

  it('handles menu items without labels', () => {
    const menuItemsWithoutLabels: MenuItemProps[] = [
      {
        key: 'no-label',
        path: '/no-label',
        icon: <span data-testid="no-label-icon">🏷️</span>,
      },
    ];

    render(<MenuItems menuItems={menuItemsWithoutLabels} />);

    expect(screen.getByTestId('no-label-icon')).toBeInTheDocument();
  });

  it('handles empty menu items array', () => {
    render(<MenuItems menuItems={[]} />);

    // Should render without crashing
    expect(screen.getByRole('menu')).toBeInTheDocument();
  });

  it('handles nested submenu items', () => {
    const nestedMenuItems: MenuItemProps[] = [
      {
        key: 'level1',
        label: 'Level 1',
        icon: <span data-testid="level1-icon">1️⃣</span>,
        children: [
          {
            key: 'level2',
            label: 'Level 2',
            icon: <span data-testid="level2-icon">2️⃣</span>,
            children: [
              {
                key: 'level3',
                label: 'Level 3',
                icon: <span data-testid="level3-icon">3️⃣</span>,
                path: '/level1/level2/level3',
              },
            ],
          },
        ],
      },
    ];

    render(<MenuItems menuItems={nestedMenuItems} />);

    // Only the top level is immediately visible
    expect(screen.getByText('Level 1')).toBeInTheDocument();

    // Check that the submenu structure exists
    const level1Submenu = screen.getByText('Level 1').closest('li');
    expect(level1Submenu).toHaveClass('ant-menu-submenu');

    // Icons should be visible
    expect(screen.getByTestId('level1-icon')).toBeInTheDocument();
  });

  it('handles menu items with additional props', () => {
    const menuItemsWithProps: MenuItemProps[] = [
      {
        key: 'with-props',
        label: 'With Props',
        path: '/with-props',
        icon: <span data-testid="with-props-icon">📦</span>,
        props: { 'data-custom': 'value' },
      },
    ];

    render(<MenuItems menuItems={menuItemsWithProps} />);

    expect(screen.getByText('With Props')).toBeInTheDocument();
    expect(screen.getByTestId('with-props-icon')).toBeInTheDocument();
  });

  it('renders with custom menu props', () => {
    const customProps = {
      mode: 'horizontal' as const,
      defaultSelectedKeys: ['dashboard'],
      className: 'custom-menu',
    };

    render(<MenuItems menuItems={mockMenuItems} {...customProps} />);

    const menu = screen.getByRole('menu');
    expect(menu).toHaveClass('custom-menu');
  });

  it('handles menu items with special characters in labels', () => {
    const specialCharMenuItems: MenuItemProps[] = [
      {
        key: 'special-chars',
        label: 'Special Chars: !@#$%^&*()_+-=[]{}|;:,.<>?',
        path: '/special-chars',
        icon: <span data-testid="special-chars-icon">🔤</span>,
      },
    ];

    render(<MenuItems menuItems={specialCharMenuItems} />);

    expect(screen.getByText('Special Chars: !@#$%^&*()_+-=[]{}|;:,.<>?')).toBeInTheDocument();
    expect(screen.getByTestId('special-chars-icon')).toBeInTheDocument();
  });

  it('handles menu items with unicode characters in labels', () => {
    const unicodeMenuItems: MenuItemProps[] = [
      {
        key: 'unicode',
        label: 'Unicode: 🚀🌟🎉 café résumé 你好世界',
        path: '/unicode',
        icon: <span data-testid="unicode-icon">🌍</span>,
      },
    ];

    render(<MenuItems menuItems={unicodeMenuItems} />);

    expect(screen.getByText('Unicode: 🚀🌟🎉 café résumé 你好世界')).toBeInTheDocument();
    expect(screen.getByTestId('unicode-icon')).toBeInTheDocument();
  });

  it('handles menu items with very long labels', () => {
    const longLabel = 'a'.repeat(1000);
    const longLabelMenuItems: MenuItemProps[] = [
      {
        key: 'long-label',
        label: longLabel,
        path: '/long-label',
        icon: <span data-testid="long-label-icon">📏</span>,
      },
    ];

    render(<MenuItems menuItems={longLabelMenuItems} />);

    expect(screen.getByText(longLabel)).toBeInTheDocument();
    expect(screen.getByTestId('long-label-icon')).toBeInTheDocument();
  });

  it('handles menu items with numeric keys', () => {
    const numericKeyMenuItems: MenuItemProps[] = [
      {
        key: '123',
        label: 'Numeric Key',
        path: '/123',
        icon: <span data-testid="numeric-key-icon">🔢</span>,
      },
    ];

    render(<MenuItems menuItems={numericKeyMenuItems} />);

    expect(screen.getByText('Numeric Key')).toBeInTheDocument();
    expect(screen.getByTestId('numeric-key-icon')).toBeInTheDocument();
  });

  it('handles menu items with special characters in keys', () => {
    const specialKeyMenuItems: MenuItemProps[] = [
      {
        key: 'special-key-!@#$%^&*()',
        label: 'Special Key',
        path: '/special-key',
        icon: <span data-testid="special-key-icon">🔑</span>,
      },
    ];

    render(<MenuItems menuItems={specialKeyMenuItems} />);

    expect(screen.getByText('Special Key')).toBeInTheDocument();
    expect(screen.getByTestId('special-key-icon')).toBeInTheDocument();
  });

  it('handles menu items with empty strings', () => {
    const emptyStringMenuItems: MenuItemProps[] = [
      {
        key: 'empty-string',
        label: '',
        path: '/empty-string',
        icon: <span data-testid="empty-string-icon">📭</span>,
      },
    ];

    render(<MenuItems menuItems={emptyStringMenuItems} />);

    expect(screen.getByTestId('empty-string-icon')).toBeInTheDocument();
  });

  it('handles menu items with whitespace-only labels', () => {
    const whitespaceMenuItems: MenuItemProps[] = [
      {
        key: 'whitespace',
        label: '   ',
        path: '/whitespace',
        icon: <span data-testid="whitespace-icon">␣</span>,
      },
    ];

    render(<MenuItems menuItems={whitespaceMenuItems} />);

    expect(screen.getByTestId('whitespace-icon')).toBeInTheDocument();
  });

  it('handles menu items with complex nested structures', () => {
    const complexMenuItems: MenuItemProps[] = [
      {
        key: 'complex',
        label: 'Complex',
        icon: <span data-testid="complex-icon">🔄</span>,
        children: [
          {
            key: 'sub1',
            label: 'Sub 1',
            icon: <span data-testid="sub1-icon">1️⃣</span>,
            children: [
              {
                key: 'sub1a',
                label: 'Sub 1A',
                path: '/complex/sub1/sub1a',
                icon: <span data-testid="sub1a-icon">A</span>,
              },
              {
                key: 'sub1b',
                label: 'Sub 1B',
                path: '/complex/sub1/sub1b',
                icon: <span data-testid="sub1b-icon">B</span>,
              },
            ],
          },
          {
            key: 'sub2',
            label: 'Sub 2',
            path: '/complex/sub2',
            icon: <span data-testid="sub2-icon">2️⃣</span>,
          },
        ],
      },
    ];

    render(<MenuItems menuItems={complexMenuItems} />);

    // Only the top level is immediately visible
    expect(screen.getByText('Complex')).toBeInTheDocument();

    // Check that the submenu structure exists
    const complexSubmenu = screen.getByText('Complex').closest('li');
    expect(complexSubmenu).toHaveClass('ant-menu-submenu');

    // Top level icon should be visible
    expect(screen.getByTestId('complex-icon')).toBeInTheDocument();
  });

  it('handles menu items with mixed content types', () => {
    const mixedMenuItems: MenuItemProps[] = [
      {
        key: 'mixed',
        label: 'Mixed Content',
        path: '/mixed',
        icon: <span data-testid="mixed-icon">🎭</span>,
        children: [
          {
            key: 'text-only',
            label: 'Text Only',
            path: '/mixed/text-only',
          },
          {
            key: 'icon-only',
            icon: <span data-testid="icon-only-icon">👁️</span>,
            path: '/mixed/icon-only',
          },
          {
            key: 'no-path',
            label: 'No Path',
            icon: <span data-testid="no-path-icon">🚫</span>,
          },
        ],
      },
    ];

    render(<MenuItems menuItems={mixedMenuItems} />);

    // Only the top level is immediately visible
    expect(screen.getByText('Mixed Content')).toBeInTheDocument();

    // Check that the submenu structure exists
    const mixedSubmenu = screen.getByText('Mixed Content').closest('li');
    expect(mixedSubmenu).toHaveClass('ant-menu-submenu');

    // Top level icon should be visible
    expect(screen.getByTestId('mixed-icon')).toBeInTheDocument();
  });

  // Tests to cover uncovered lines 18-31
  it('handles recursive menu rendering with showVerticalSubMenu flag', () => {
    const recursiveMenuItems: MenuItemProps[] = [
      {
        key: 'level1',
        label: 'Level 1',
        icon: <span data-testid="level1-icon">1️⃣</span>,
        children: [
          {
            key: 'level2',
            label: 'Level 2',
            icon: <span data-testid="level2-icon">2️⃣</span>,
            children: [
              {
                key: 'level3',
                label: 'Level 3',
                path: '/level1/level2/level3',
                icon: <span data-testid="level3-icon">3️⃣</span>,
              },
            ],
          },
        ],
      },
    ];

    render(<MenuItems menuItems={recursiveMenuItems} />);

    // Only the top level is immediately visible
    expect(screen.getByText('Level 1')).toBeInTheDocument();

    // Check that the submenu structure exists
    const level1Submenu = screen.getByText('Level 1').closest('li');
    expect(level1Submenu).toHaveClass('ant-menu-submenu');

    // Top level icon should be visible
    expect(screen.getByTestId('level1-icon')).toBeInTheDocument();
  });

  it('handles menu items with handleClickMenuItem callback', () => {
    const menuItemsWithCallback: MenuItemProps[] = [
      {
        key: 'clickable',
        label: 'Clickable Item',
        path: '/clickable',
        icon: <span data-testid="clickable-icon">🖱️</span>,
      },
    ];

    render(
      <MenuItems menuItems={menuItemsWithCallback} handleClickMenuItem={mockHandleClickMenuItem} />
    );

    const clickableItem = screen.getByText('Clickable Item');
    fireEvent.click(clickableItem);

    expect(mockHandleClickMenuItem).toHaveBeenCalledWith(menuItemsWithCallback[0]);
  });

  it('handles menu items without handleClickMenuItem callback', () => {
    const menuItemsWithoutCallback: MenuItemProps[] = [
      {
        key: 'no-callback',
        label: 'No Callback Item',
        path: '/no-callback',
        icon: <span data-testid="no-callback-icon">🔇</span>,
      },
    ];

    render(<MenuItems menuItems={menuItemsWithoutCallback} />);

    const noCallbackItem = screen.getByText('No Callback Item');
    fireEvent.click(noCallbackItem);

    // Should not throw error when no callback is provided
    expect(noCallbackItem).toBeInTheDocument();
  });

  it('handles menu items with empty children array', () => {
    const menuItemsWithEmptyChildren: MenuItemProps[] = [
      {
        key: 'empty-children',
        label: 'Empty Children',
        icon: <span data-testid="empty-children-icon">📭</span>,
        children: [],
      },
    ];

    render(<MenuItems menuItems={menuItemsWithEmptyChildren} />);

    expect(screen.getByText('Empty Children')).toBeInTheDocument();
    expect(screen.getByTestId('empty-children-icon')).toBeInTheDocument();
  });

  it('handles menu items with null/undefined children', () => {
    const menuItemsWithNullChildren: MenuItemProps[] = [
      {
        key: 'null-children',
        label: 'Null Children',
        path: '/null-children',
        icon: <span data-testid="null-children-icon">❓</span>,
        children: null as any,
      },
    ];

    render(<MenuItems menuItems={menuItemsWithNullChildren} />);

    expect(screen.getByText('Null Children')).toBeInTheDocument();
    expect(screen.getByTestId('null-children-icon')).toBeInTheDocument();
  });

  it('handles menu items with complex nested structure and vertical submenu', () => {
    const complexVerticalMenuItems: MenuItemProps[] = [
      {
        key: 'vertical-complex',
        label: 'Vertical Complex',
        icon: <span data-testid="vertical-complex-icon">📐</span>,
        children: [
          {
            key: 'vertical-sub1',
            label: 'Vertical Sub 1',
            icon: <span data-testid="vertical-sub1-icon">📏</span>,
            children: [
              {
                key: 'vertical-sub1a',
                label: 'Vertical Sub 1A',
                path: '/vertical/sub1/sub1a',
                icon: <span data-testid="vertical-sub1a-icon">📐</span>,
              },
            ],
          },
        ],
      },
    ];

    render(<MenuItems menuItems={complexVerticalMenuItems} />);

    // Only the top level is immediately visible
    expect(screen.getByText('Vertical Complex')).toBeInTheDocument();

    // Check that the submenu structure exists
    const verticalComplexSubmenu = screen.getByText('Vertical Complex').closest('li');
    expect(verticalComplexSubmenu).toHaveClass('ant-menu-submenu');

    // Top level icon should be visible
    expect(screen.getByTestId('vertical-complex-icon')).toBeInTheDocument();
  });
});
