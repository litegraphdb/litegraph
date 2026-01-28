import { TablePaginationConfig } from 'antd';

export const paginationConfig = {};

export const tablePaginationConfig: TablePaginationConfig = {
  pageSizeOptions: [50, 100, 1000],
  pageSize: 50,
  showSizeChanger: true,
  position: ['bottomCenter'],
  showTotal: (total: number, range: number[]) => `${range[0]}-${range[1]} of ${total}`,
};
