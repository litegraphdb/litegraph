import React, { useEffect, useState } from 'react';
import { Table, TableProps } from 'antd';
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

interface LitegraphTableProps extends TableProps {
  showTotal?: boolean;
}

const LitegraphTable = (props: LitegraphTableProps) => {
  const { columns, dataSource, showTotal = true, pagination, ...rest } = props;
  const [columnsState, setColumnsState] = useState(columns);

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

  const columnsWithResizable = columnsState?.map((col: any, index: number) => ({
    ...col,
    onHeaderCell: (column: any) => ({
      width: column.width,
      onResize: handleResize(index),
    }),
  }));

  useEffect(() => {
    setColumnsState(columns);
  }, [columns]);

  const totalRecords = Array.isArray(dataSource) ? dataSource.length : 0;

  const paginationWithTotal = pagination !== false ? {
    ...((typeof pagination === 'object' ? pagination : {}) as object),
    showTotal: showTotal ? (total: number) => (
      <LitegraphText style={{ marginRight: 8 }}>
        Total: <strong>{total}</strong> records
      </LitegraphText>
    ) : undefined,
  } : false;

  return (
    <Table
      {...rest}
      dataSource={dataSource}
      columns={columnsWithResizable}
      pagination={paginationWithTotal}
      components={{
        header: {
          cell: ResizableTitle,
        },
      }}
    />
  );
};

export default LitegraphTable;
