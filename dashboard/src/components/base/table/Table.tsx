import React, { useEffect, useMemo, useState } from 'react';
import { Button, InputNumber, Select, Space, Table, TableProps, Tooltip } from 'antd';
import type { TablePaginationConfig } from 'antd';
import { ReloadOutlined } from '@ant-design/icons';
import { Resizable } from 'react-resizable';
import LitegraphText from '../typograpghy/Text';

const ResizableTitle = (props: any) => {
  const { onResize, width, ...restProps } = props;

  if (!width) {
    return <th {...restProps} />;
  }

  return (
    <Resizable
      width={width}
      height={0}
      handle={
        <span
          className="react-resizable-handle"
          onClick={(e) => {
            e.stopPropagation();
          }}
        />
      }
      onResize={onResize}
      draggableOpts={{ enableUserSelectHack: false }}
    >
      <th {...restProps} />
    </Resizable>
  );
};

interface LitegraphTableProps<T = any> extends TableProps<T> {
  showTotal?: boolean;
  hideHorizontalScroll?: boolean;
  onRowClick?: (record: T, event: React.MouseEvent<HTMLElement>) => void;
  onRefresh?: () => void;
  isRefreshing?: boolean;
  refreshTooltip?: string;
}

const DEFAULT_PAGE_SIZE = 10;
const DEFAULT_PAGE_SIZE_OPTIONS = [10, 25, 50, 100];

const interactiveRowClickSelector = [
  'a[href]',
  'button',
  'input',
  'textarea',
  'select',
  'label',
  '[role="button"]',
  '[role="link"]',
  '[role="menuitem"]',
  '[data-row-click-ignore="true"]',
  '.ant-btn',
  '.ant-checkbox-wrapper',
  '.ant-dropdown',
  '.ant-dropdown-trigger',
  '.ant-pagination',
  '.ant-select',
  '.ant-switch',
].join(',');

const shouldIgnoreRowClick = (target: EventTarget | null) => {
  return target instanceof Element && Boolean(target.closest(interactiveRowClickSelector));
};

const positiveNumberOrDefault = (value: unknown, defaultValue: number) => {
  const parsed = Number(value);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : defaultValue;
};

const clamp = (value: number, min: number, max: number) => {
  return Math.min(Math.max(value, min), max);
};

const normalizePageSizeOptions = (
  options: TablePaginationConfig['pageSizeOptions'],
  currentPageSize: number
) => {
  const values = (options || DEFAULT_PAGE_SIZE_OPTIONS)
    .map((option) => Number(option))
    .filter((option) => Number.isFinite(option) && option > 0);

  return Array.from(new Set([...values, currentPageSize]))
    .sort((a, b) => a - b)
    .map((value) => ({ label: value.toLocaleString(), value }));
};

const LitegraphTableEmpty = () => (
  <div
    className="ant-empty ant-empty-normal"
    data-testid="litegraph-table-empty"
    style={{ color: 'var(--ant-color-text-description)', margin: '32px 0' }}
  >
    <div className="ant-empty-image" style={{ height: 48, marginBottom: 8 }}>
      <svg
        aria-hidden="true"
        data-testid="litegraph-table-empty-icon"
        fill="none"
        height="48"
        viewBox="0 0 48 48"
        width="48"
        style={{ color: 'inherit' }}
      >
        <circle cx="24" cy="24" r="18" stroke="currentColor" strokeWidth="2" />
        <circle cx="18" cy="20" r="1.75" stroke="currentColor" strokeWidth="2" />
        <circle cx="30" cy="20" r="1.75" stroke="currentColor" strokeWidth="2" />
        <path
          d="M16 32c2.2-3 4.8-4.5 8-4.5s5.8 1.5 8 4.5"
          stroke="currentColor"
          strokeLinecap="round"
          strokeWidth="2"
        />
      </svg>
    </div>
    <div className="ant-empty-description" style={{ color: 'inherit' }}>
      No data
    </div>
  </div>
);

const LitegraphTable = <T extends object = any>(props: LitegraphTableProps<T>) => {
  const {
    columns,
    dataSource,
    showTotal: _showTotal,
    pagination,
    onRow,
    onRowClick,
    hideHorizontalScroll,
    className,
    tableLayout,
    onRefresh,
    isRefreshing,
    refreshTooltip = 'Refresh',
    locale,
    ...rest
  } = props;
  const [columnsState, setColumnsState] = useState(columns);
  const paginationConfig = (
    typeof pagination === 'object' ? pagination : {}
  ) as TablePaginationConfig;
  const initialPageSize = positiveNumberOrDefault(
    paginationConfig.defaultPageSize ?? paginationConfig.pageSize,
    DEFAULT_PAGE_SIZE
  );
  const [internalPage, setInternalPage] = useState(
    positiveNumberOrDefault(paginationConfig.defaultCurrent ?? paginationConfig.current, 1)
  );
  const [internalPageSize, setInternalPageSize] = useState(initialPageSize);

  const handleResize =
    (index: number) =>
    (e: any, { size }: any) => {
      setColumnsState((prev: any) => {
        const nextColumns = [...prev];
        nextColumns[index] = {
          ...nextColumns[index],
          width: size.width,
        };
        return nextColumns;
      });
    };

  const columnsWithResizable = columnsState?.map((col: any, index: number) => {
    const isLastColumn = index === columnsState.length - 1;

    if (isLastColumn) {
      return col;
    }

    return {
      ...col,
      onHeaderCell: (column: any) => ({
        width: column.width,
        onResize: handleResize(index),
      }),
    };
  });

  useEffect(() => {
    setColumnsState(columns);
  }, [columns]);

  const dataLength = Array.isArray(dataSource) ? dataSource.length : 0;
  const paginationEnabled = pagination !== false;
  const controlledPagination =
    paginationEnabled &&
    (paginationConfig.current !== undefined ||
      paginationConfig.onChange !== undefined ||
      paginationConfig.onShowSizeChange !== undefined);
  const pageSize = controlledPagination
    ? positiveNumberOrDefault(paginationConfig.pageSize, internalPageSize)
    : internalPageSize;
  const totalRecords = paginationEnabled
    ? positiveNumberOrDefault(paginationConfig.total, dataLength)
    : dataLength;
  const totalPages = Math.max(1, Math.ceil(totalRecords / pageSize));
  const currentPage = clamp(
    controlledPagination
      ? positiveNumberOrDefault(paginationConfig.current, internalPage)
      : internalPage,
    1,
    totalPages
  );
  const pageSizeOptions = useMemo(
    () => normalizePageSizeOptions(paginationConfig.pageSizeOptions, pageSize),
    [paginationConfig.pageSizeOptions, pageSize]
  );

  useEffect(() => {
    if (!controlledPagination) {
      setInternalPageSize(initialPageSize);
    }
  }, [controlledPagination, initialPageSize]);

  useEffect(() => {
    if (!controlledPagination && internalPage > totalPages) {
      setInternalPage(totalPages);
    }
  }, [controlledPagination, internalPage, totalPages]);

  const changePage = (nextPage: number, nextPageSize = pageSize) => {
    const nextTotalPages = Math.max(1, Math.ceil(totalRecords / nextPageSize));
    const safePage = clamp(nextPage, 1, nextTotalPages);

    if (!controlledPagination) {
      setInternalPage(safePage);
      setInternalPageSize(nextPageSize);
    }

    paginationConfig.onChange?.(safePage, nextPageSize);
  };

  const changePageSize = (nextPageSize: number) => {
    const safePageSize = positiveNumberOrDefault(nextPageSize, pageSize);

    if (!controlledPagination) {
      setInternalPage(1);
      setInternalPageSize(safePageSize);
    }

    paginationConfig.onShowSizeChange?.(1, safePageSize);
    paginationConfig.onChange?.(1, safePageSize);
  };

  const paginationWithTotal: false | TablePaginationConfig = paginationEnabled
    ? {
        ...paginationConfig,
        current: currentPage,
        pageSize,
        total: totalRecords,
        position: ['none'],
        showTotal: undefined,
        showSizeChanger: false,
        showQuickJumper: false,
        onChange: changePage,
      }
    : false;

  const paginationBar = paginationEnabled ? (
    <div
      data-testid="litegraph-table-pagination-bar"
      style={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        gap: 8,
        flexWrap: 'wrap',
        marginBottom: 6,
      }}
    >
      <Space size={[6, 6]} wrap>
        {onRefresh && (
          <Tooltip title={refreshTooltip}>
            <Button
              size="small"
              aria-label="Refresh table"
              data-testid="litegraph-table-refresh"
              icon={<ReloadOutlined spin={isRefreshing} />}
              onClick={onRefresh}
              disabled={isRefreshing}
            />
          </Tooltip>
        )}
        <LitegraphText data-testid="litegraph-table-total-records">
          Total records: <strong>{totalRecords.toLocaleString()}</strong>
        </LitegraphText>
        <LitegraphText data-testid="litegraph-table-total-pages">
          Total pages: <strong>{totalPages.toLocaleString()}</strong>
        </LitegraphText>
      </Space>
      <Space size={[6, 6]} wrap style={{ justifyContent: 'flex-end' }}>
        <Button
          size="small"
          data-testid="litegraph-table-first-page"
          onClick={() => changePage(1)}
          disabled={currentPage <= 1}
        >
          First
        </Button>
        <Button
          size="small"
          data-testid="litegraph-table-previous-page"
          onClick={() => changePage(currentPage - 1)}
          disabled={currentPage <= 1}
        >
          Previous
        </Button>
        <LitegraphText>Page</LitegraphText>
        <InputNumber
          size="small"
          aria-label="Jump to page"
          data-testid="litegraph-table-page-jump"
          min={1}
          max={totalPages}
          value={currentPage}
          onChange={(value) => {
            if (value !== null) {
              changePage(Number(value));
            }
          }}
          style={{ width: 72 }}
        />
        <LitegraphText>of {totalPages.toLocaleString()}</LitegraphText>
        <Button
          size="small"
          data-testid="litegraph-table-next-page"
          onClick={() => changePage(currentPage + 1)}
          disabled={currentPage >= totalPages}
        >
          Next
        </Button>
        <Button
          size="small"
          data-testid="litegraph-table-last-page"
          onClick={() => changePage(totalPages)}
          disabled={currentPage >= totalPages}
        >
          Last
        </Button>
        <Select
          size="small"
          aria-label="Records per page"
          data-testid="litegraph-table-page-size"
          value={pageSize}
          options={pageSizeOptions}
          onChange={changePageSize}
          style={{ width: 96 }}
        />
        <LitegraphText>per page</LitegraphText>
      </Space>
    </div>
  ) : null;

  const getRowProps: TableProps<T>['onRow'] = (record, index) => {
    const rowProps = onRow?.(record, index) || {};

    if (!onRowClick) {
      return rowProps;
    }

    const { onClick, style, className, ...restRowProps } = rowProps;

    return {
      ...restRowProps,
      className: [className, 'litegraph-clickable-row'].filter(Boolean).join(' ') || undefined,
      style: { cursor: 'pointer', ...style },
      onClick: (event) => {
        onClick?.(event);

        if (event.defaultPrevented || shouldIgnoreRowClick(event.target)) {
          return;
        }

        onRowClick(record, event);
      },
    };
  };

  return (
    <>
      {paginationBar}
      <Table
        {...rest}
        className={[className, hideHorizontalScroll ? 'litegraph-table-no-horizontal-scroll' : '']
          .filter(Boolean)
          .join(' ')}
        dataSource={dataSource}
        columns={columnsWithResizable}
        pagination={paginationWithTotal}
        locale={{
          ...locale,
          emptyText: locale?.emptyText !== undefined ? locale.emptyText : <LitegraphTableEmpty />,
        }}
        onRow={getRowProps}
        tableLayout={hideHorizontalScroll ? 'fixed' : tableLayout}
        components={{
          header: {
            cell: ResizableTitle,
          },
        }}
      />
    </>
  );
};

export default LitegraphTable;
