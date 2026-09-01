import '@testing-library/jest-dom';
import React from 'react';
import { fireEvent, render, screen } from '@testing-library/react';
import LitegraphTable from '@/components/base/table/Table';
import { TableProps } from 'antd';

interface TestRecord {
  key: string;
  name: string;
}

describe('LitegraphTable', () => {
  const dataSource: TestRecord[] = [{ key: '1', name: 'Alpha' }];

  beforeEach(() => {
    window.localStorage.clear();
  });

  it('calls onRowClick when a non-interactive row cell is clicked', () => {
    const onRowClick = jest.fn();
    const columns: TableProps<TestRecord>['columns'] = [
      { title: 'Name', dataIndex: 'name', key: 'name' },
    ];

    render(
      <LitegraphTable
        columns={columns}
        dataSource={dataSource}
        pagination={false}
        onRowClick={onRowClick}
      />
    );

    fireEvent.click(screen.getByText('Alpha'));

    expect(onRowClick).toHaveBeenCalledTimes(1);
    expect(onRowClick).toHaveBeenCalledWith(dataSource[0], expect.any(Object));
  });

  it('does not call onRowClick when a button inside the row is clicked', () => {
    const onRowClick = jest.fn();
    const onButtonClick = jest.fn();
    const columns: TableProps<TestRecord>['columns'] = [
      { title: 'Name', dataIndex: 'name', key: 'name' },
      {
        title: 'Actions',
        key: 'actions',
        render: () => <button onClick={onButtonClick}>Action</button>,
      },
    ];

    render(
      <LitegraphTable
        columns={columns}
        dataSource={dataSource}
        pagination={false}
        onRowClick={onRowClick}
      />
    );

    fireEvent.click(screen.getByRole('button', { name: 'Action' }));

    expect(onButtonClick).toHaveBeenCalledTimes(1);
    expect(onRowClick).not.toHaveBeenCalled();
  });

  it('does not call onRowClick when an icon inside an action button is clicked', () => {
    const onRowClick = jest.fn();
    const onButtonClick = jest.fn();
    const columns: TableProps<TestRecord>['columns'] = [
      { title: 'Name', dataIndex: 'name', key: 'name' },
      {
        title: 'Actions',
        key: 'actions',
        render: () => (
          <button aria-label="Action" onClick={onButtonClick}>
            <svg data-testid="action-icon" />
          </button>
        ),
      },
    ];

    render(
      <LitegraphTable
        columns={columns}
        dataSource={dataSource}
        pagination={false}
        onRowClick={onRowClick}
      />
    );

    fireEvent.click(screen.getByTestId('action-icon'));

    expect(onButtonClick).toHaveBeenCalledTimes(1);
    expect(onRowClick).not.toHaveBeenCalled();
  });

  it('does not call onRowClick when a link inside the row is clicked', () => {
    const onRowClick = jest.fn();
    const columns: TableProps<TestRecord>['columns'] = [
      { title: 'Name', dataIndex: 'name', key: 'name' },
      {
        title: 'Details',
        key: 'details',
        render: () => (
          <a href="/details" onClick={(event) => event.preventDefault()}>
            Details
          </a>
        ),
      },
    ];

    render(
      <LitegraphTable
        columns={columns}
        dataSource={dataSource}
        pagination={false}
        onRowClick={onRowClick}
      />
    );

    fireEvent.click(screen.getByRole('link', { name: 'Details' }));

    expect(onRowClick).not.toHaveBeenCalled();
  });

  it('does not render a resize handle on the last column', () => {
    const columns: TableProps<TestRecord>['columns'] = [
      { title: 'Name', dataIndex: 'name', key: 'name', width: 200 },
      {
        title: 'Actions',
        key: 'actions',
        width: 80,
        render: () => <button>Action</button>,
      },
    ];

    const { container } = render(
      <LitegraphTable columns={columns} dataSource={dataSource} pagination={false} />
    );

    expect(container.querySelectorAll('.react-resizable-handle')).toHaveLength(1);
    expect(
      screen.getByRole('columnheader', { name: 'Actions' }).querySelector('.react-resizable-handle')
    ).not.toBeInTheDocument();
  });

  it('applies fixed-layout no-horizontal-scroll mode when requested', () => {
    const columns: TableProps<TestRecord>['columns'] = [
      { title: 'Name', dataIndex: 'name', key: 'name', width: 200 },
    ];

    const { container } = render(
      <LitegraphTable
        hideHorizontalScroll
        columns={columns}
        dataSource={dataSource}
        pagination={false}
      />
    );

    expect(container.querySelector('.litegraph-table-no-horizontal-scroll')).toBeInTheDocument();
  });

  it('uses a theme-colored outline sad face for empty table data', () => {
    const columns: TableProps<TestRecord>['columns'] = [
      { title: 'Name', dataIndex: 'name', key: 'name', width: 200 },
    ];

    const { container } = render(
      <LitegraphTable columns={columns} dataSource={[]} pagination={false} />
    );

    expect(screen.getByTestId('litegraph-table-empty').getAttribute('style')).toContain(
      'var(--ant-color-text-description)'
    );
    expect(screen.getByTestId('litegraph-table-empty-icon')).toBeInTheDocument();
    expect(screen.getByText('No data')).toBeInTheDocument();
    expect(container.querySelector('.ant-empty-img-simple')).not.toBeInTheDocument();
  });

  it('renders the required pagination controls above the table', () => {
    const onRefresh = jest.fn();
    const onChange = jest.fn();
    const columns: TableProps<TestRecord>['columns'] = [
      { title: 'Name', dataIndex: 'name', key: 'name' },
    ];

    const { container } = render(
      <LitegraphTable
        columns={columns}
        dataSource={dataSource}
        onRefresh={onRefresh}
        pagination={{
          current: 2,
          pageSize: 10,
          total: 42,
          onChange,
        }}
      />
    );

    expect(screen.getByTestId('litegraph-table-pagination-bar')).toBeInTheDocument();
    expect(screen.getByTestId('litegraph-table-total-records')).toHaveTextContent(
      'Total records: 42'
    );
    expect(screen.getByTestId('litegraph-table-total-pages')).toHaveTextContent('Total pages: 5');
    expect(screen.getByTestId('litegraph-table-first-page')).toBeEnabled();
    expect(screen.getByTestId('litegraph-table-previous-page')).toBeEnabled();
    expect(screen.getByTestId('litegraph-table-next-page')).toBeEnabled();
    expect(screen.getByTestId('litegraph-table-last-page')).toBeEnabled();
    expect(screen.getByLabelText('Jump to page')).toBeInTheDocument();
    expect(screen.getByTestId('litegraph-table-page-size')).toBeInTheDocument();
    expect(container.querySelector('.ant-table-pagination')).not.toBeInTheDocument();

    fireEvent.click(screen.getByTestId('litegraph-table-refresh'));

    expect(onRefresh).toHaveBeenCalledTimes(1);
  });

  it('routes pagination bar navigation through the table pagination callback', () => {
    const onChange = jest.fn();
    const columns: TableProps<TestRecord>['columns'] = [
      { title: 'Name', dataIndex: 'name', key: 'name' },
    ];

    render(
      <LitegraphTable
        columns={columns}
        dataSource={dataSource}
        pagination={{
          current: 3,
          pageSize: 10,
          total: 100,
          onChange,
        }}
      />
    );

    fireEvent.click(screen.getByTestId('litegraph-table-first-page'));
    fireEvent.click(screen.getByTestId('litegraph-table-previous-page'));
    fireEvent.click(screen.getByTestId('litegraph-table-next-page'));
    fireEvent.click(screen.getByTestId('litegraph-table-last-page'));
    fireEvent.change(screen.getByLabelText('Jump to page'), { target: { value: '7' } });

    expect(onChange).toHaveBeenNthCalledWith(1, 1, 10);
    expect(onChange).toHaveBeenNthCalledWith(2, 2, 10);
    expect(onChange).toHaveBeenNthCalledWith(3, 4, 10);
    expect(onChange).toHaveBeenNthCalledWith(4, 10, 10);
    expect(onChange).toHaveBeenLastCalledWith(7, 10);
  });

  it('changes page size from the top pagination bar', () => {
    const onChange = jest.fn();
    const columns: TableProps<TestRecord>['columns'] = [
      { title: 'Name', dataIndex: 'name', key: 'name' },
    ];

    render(
      <LitegraphTable
        columns={columns}
        dataSource={dataSource}
        pagination={{
          current: 2,
          pageSize: 10,
          pageSizeOptions: [10, 25, 50],
          total: 100,
          onChange,
        }}
      />
    );

    fireEvent.mouseDown(
      screen.getByTestId('litegraph-table-page-size').querySelector('.ant-select-selector')!
    );
    fireEvent.click(screen.getByTitle('25'));

    expect(onChange).toHaveBeenCalledWith(1, 25);
  });

  it('uses the top controls for local pagination', () => {
    const columns: TableProps<TestRecord>['columns'] = [
      { title: 'Name', dataIndex: 'name', key: 'name' },
    ];
    const localData = Array.from({ length: 12 }, (_, index) => ({
      key: String(index + 1),
      name: `Row ${index + 1}`,
    }));

    render(
      <LitegraphTable
        columns={columns}
        dataSource={localData}
        pagination={{ pageSize: 5, pageSizeOptions: [5, 10] }}
      />
    );

    expect(screen.getByText('Row 1')).toBeInTheDocument();
    expect(screen.queryByText('Row 6')).not.toBeInTheDocument();

    fireEvent.click(screen.getByTestId('litegraph-table-next-page'));

    expect(screen.queryByText('Row 1')).not.toBeInTheDocument();
    expect(screen.getByText('Row 6')).toBeInTheDocument();
  });

  it('renders an auto-refresh selector defaulting to None and fires refresh on interval', () => {
    jest.useFakeTimers();
    const onRefresh = jest.fn();
    const columns: TableProps<TestRecord>['columns'] = [
      { title: 'Name', dataIndex: 'name', key: 'name' },
    ];

    render(
      <LitegraphTable
        columns={columns}
        dataSource={dataSource}
        onRefresh={onRefresh}
        persistKey="test-table"
        pagination={{ pageSize: 10 }}
      />
    );

    const selector = screen.getByTestId('litegraph-table-auto-refresh');
    expect(selector).toHaveTextContent('None');

    fireEvent.mouseDown(selector.querySelector('.ant-select-selector')!);
    fireEvent.click(screen.getByTitle('10 seconds'));

    jest.advanceTimersByTime(10_000);
    expect(onRefresh).toHaveBeenCalledTimes(1);
    jest.advanceTimersByTime(20_000);
    expect(onRefresh).toHaveBeenCalledTimes(3);

    expect(window.localStorage.getItem('litegraph.table.test-table.autoRefresh')).toBe('10');
    jest.useRealTimers();
  });

  it('restores the persisted auto-refresh interval on mount', () => {
    window.localStorage.setItem('litegraph.table.test-table.autoRefresh', '30');
    const columns: TableProps<TestRecord>['columns'] = [
      { title: 'Name', dataIndex: 'name', key: 'name' },
    ];

    render(
      <LitegraphTable
        columns={columns}
        dataSource={dataSource}
        onRefresh={jest.fn()}
        persistKey="test-table"
        pagination={{ pageSize: 10 }}
      />
    );

    expect(screen.getByTestId('litegraph-table-auto-refresh')).toHaveTextContent('30 seconds');
  });

  it('persists page size and restores it across mounts', () => {
    const columns: TableProps<TestRecord>['columns'] = [
      { title: 'Name', dataIndex: 'name', key: 'name' },
    ];
    const localData = Array.from({ length: 12 }, (_, index) => ({
      key: String(index + 1),
      name: `Row ${index + 1}`,
    }));

    const { unmount } = render(
      <LitegraphTable
        columns={columns}
        dataSource={localData}
        persistKey="persist-table"
        pagination={{ pageSize: 5, pageSizeOptions: [5, 10] }}
      />
    );

    expect(screen.queryByText('Row 6')).not.toBeInTheDocument();

    fireEvent.mouseDown(
      screen.getByTestId('litegraph-table-page-size').querySelector('.ant-select-selector')!
    );
    fireEvent.click(screen.getByTitle('10'));

    expect(screen.getByText('Row 6')).toBeInTheDocument();
    expect(window.localStorage.getItem('litegraph.table.persist-table.pageSize')).toBe('10');

    unmount();

    render(
      <LitegraphTable
        columns={columns}
        dataSource={localData}
        persistKey="persist-table"
        pagination={{ pageSize: 5, pageSizeOptions: [5, 10] }}
      />
    );

    expect(screen.getByText('Row 6')).toBeInTheDocument();
  });

  it('replays the persisted page size to controlled pagination on mount', () => {
    window.localStorage.setItem('litegraph.table.controlled-table.pageSize', '25');
    const onChange = jest.fn();
    const columns: TableProps<TestRecord>['columns'] = [
      { title: 'Name', dataIndex: 'name', key: 'name' },
    ];

    render(
      <LitegraphTable
        columns={columns}
        dataSource={dataSource}
        persistKey="controlled-table"
        pagination={{
          current: 1,
          pageSize: 10,
          total: 100,
          onChange,
        }}
      />
    );

    expect(onChange).toHaveBeenCalledWith(1, 25);
  });
});
