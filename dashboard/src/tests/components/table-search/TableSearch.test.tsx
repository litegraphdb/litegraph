import '@testing-library/jest-dom';
import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import TableSearch from '@/components/table-search/TableSearch';
import { FilterDropdownProps } from 'antd/es/table/interface';

describe('TableSearch Component', () => {
  const mockSetSelectedKeys = jest.fn();
  const mockConfirm = jest.fn();
  const mockClearFilters = jest.fn();

  const defaultProps: FilterDropdownProps = {
    setSelectedKeys: mockSetSelectedKeys,
    selectedKeys: [],
    confirm: mockConfirm,
    clearFilters: mockClearFilters,
    filters: [] as any,
    visible: true,
    prefixCls: 'ant-table-filter',
    close: jest.fn(),
  };

  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders with default placeholder', () => {
    render(<TableSearch {...defaultProps} />);

    const searchInput = screen.getByPlaceholderText('Search');
    expect(searchInput).toBeInTheDocument();
  });

  it('renders with custom placeholder', () => {
    render(<TableSearch {...defaultProps} placeholder="Search users..." />);

    const searchInput = screen.getByPlaceholderText('Search users...');
    expect(searchInput).toBeInTheDocument();
  });

  it('displays selectedKeys value in input', () => {
    const propsWithValue = {
      ...defaultProps,
      selectedKeys: ['test value'],
    };

    render(<TableSearch {...propsWithValue} />);

    const searchInput = screen.getByDisplayValue('test value');
    expect(searchInput).toBeInTheDocument();
  });

  it('calls setSelectedKeys when input value changes', () => {
    render(<TableSearch {...defaultProps} />);

    const searchInput = screen.getByPlaceholderText('Search');
    fireEvent.change(searchInput, { target: { value: 'new search term' } });

    expect(mockSetSelectedKeys).toHaveBeenCalledWith(['new search term']);
  });

  it('calls setSelectedKeys with empty array when input is cleared', () => {
    render(<TableSearch {...defaultProps} />);

    const searchInput = screen.getByPlaceholderText('Search');

    // First set a value, then clear it
    fireEvent.change(searchInput, { target: { value: 'test' } });
    expect(mockSetSelectedKeys).toHaveBeenCalledWith(['test']);

    fireEvent.change(searchInput, { target: { value: '' } });
    expect(mockSetSelectedKeys).toHaveBeenCalledWith([]);
    expect(mockConfirm).toHaveBeenCalledTimes(1);
  });

  it('calls confirm when search button is clicked', () => {
    render(<TableSearch {...defaultProps} />);

    const searchButton = screen.getByRole('button', { name: /search/i });
    fireEvent.click(searchButton);

    expect(mockConfirm).toHaveBeenCalledTimes(1);
  });

  it('calls confirm when Enter key is pressed', () => {
    render(<TableSearch {...defaultProps} />);

    const searchInput = screen.getByPlaceholderText('Search');
    fireEvent.keyDown(searchInput, { key: 'Enter', code: 'Enter' });

    expect(mockConfirm).toHaveBeenCalledTimes(1);
  });

  it('handles input with special characters', () => {
    render(<TableSearch {...defaultProps} />);

    const searchInput = screen.getByPlaceholderText('Search');
    const specialValue = 'test@example.com +123-456-7890';

    fireEvent.change(searchInput, { target: { value: specialValue } });

    expect(mockSetSelectedKeys).toHaveBeenCalledWith([specialValue]);
  });

  it('handles input with numbers', () => {
    render(<TableSearch {...defaultProps} />);

    const searchInput = screen.getByPlaceholderText('Search');
    fireEvent.change(searchInput, { target: { value: '12345' } });

    expect(mockSetSelectedKeys).toHaveBeenCalledWith(['12345']);
  });

  it('handles input with spaces', () => {
    render(<TableSearch {...defaultProps} />);

    const searchInput = screen.getByPlaceholderText('Search');
    fireEvent.change(searchInput, { target: { value: '  spaced  value  ' } });

    expect(mockSetSelectedKeys).toHaveBeenCalledWith(['  spaced  value  ']);
  });

  it('handles multiple rapid input changes', () => {
    render(<TableSearch {...defaultProps} />);

    const searchInput = screen.getByPlaceholderText('Search');

    fireEvent.change(searchInput, { target: { value: 'first' } });
    fireEvent.change(searchInput, { target: { value: 'second' } });
    fireEvent.change(searchInput, { target: { value: 'third' } });

    expect(mockSetSelectedKeys).toHaveBeenCalledTimes(3);
    expect(mockSetSelectedKeys).toHaveBeenLastCalledWith(['third']);
  });

  it('handles clear button click', () => {
    const propsWithValue = {
      ...defaultProps,
      selectedKeys: ['existing value'],
    };

    render(<TableSearch {...propsWithValue} />);

    const clearButton = screen.getByRole('button', { name: /close/i });
    fireEvent.click(clearButton);

    // The clear button triggers onChange with empty value
    expect(mockSetSelectedKeys).toHaveBeenCalledWith([]);
    expect(mockConfirm).toHaveBeenCalledTimes(2);
  });

  it('handles empty selectedKeys array', () => {
    const propsWithEmptyKeys = {
      ...defaultProps,
      selectedKeys: [],
    };

    render(<TableSearch {...propsWithEmptyKeys} />);

    const searchInput = screen.getByPlaceholderText('Search');
    expect(searchInput).toHaveValue('');
  });

  it('handles selectedKeys with multiple values (uses first)', () => {
    const propsWithMultipleKeys = {
      ...defaultProps,
      selectedKeys: ['first', 'second', 'third'],
    };

    render(<TableSearch {...propsWithMultipleKeys} />);

    const searchInput = screen.getByDisplayValue('first');
    expect(searchInput).toBeInTheDocument();
  });

  it('handles undefined selectedKeys', () => {
    const propsWithUndefinedKeys = {
      ...defaultProps,
      selectedKeys: undefined as any,
    };

    expect(() => {
      render(<TableSearch {...propsWithUndefinedKeys} />);
    }).toThrow("Cannot read properties of undefined (reading '0')");
  });

  it('handles null selectedKeys', () => {
    const propsWithNullKeys = {
      ...defaultProps,
      selectedKeys: null as any,
    };

    expect(() => {
      render(<TableSearch {...propsWithNullKeys} />);
    }).toThrow("Cannot read properties of null (reading '0')");
  });

  it('handles very long input values', () => {
    render(<TableSearch {...defaultProps} />);

    const searchInput = screen.getByPlaceholderText('Search');
    const longValue = 'a'.repeat(1000);

    fireEvent.change(searchInput, { target: { value: longValue } });

    expect(mockSetSelectedKeys).toHaveBeenCalledWith([longValue]);
  });
});
