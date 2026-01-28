'use client';
import { useState, useEffect } from 'react';
import { Input, Empty, Spin } from 'antd';
import { SearchOutlined } from '@ant-design/icons';
import LitegraphModal from '@/components/base/modal/Modal';
import LitegraphTable from '@/components/base/table/Table';
import { useSearchNodesMutation } from '@/lib/store/slice/slice';
import { Node } from 'litegraphdb/dist/types/types';
import { ColumnType } from 'antd/es/table';

interface NodeSearchModalProps {
  isVisible: boolean;
  setIsVisible: (visible: boolean) => void;
  graphId: string;
  onNodeSelect?: (node: Node) => void;
}

const NodeSearchModal = ({
  isVisible,
  setIsVisible,
  graphId,
  onNodeSelect,
}: NodeSearchModalProps) => {
  const [searchValue, setSearchValue] = useState('');
  const [searchNodes, { isLoading, isError }] = useSearchNodesMutation();
  const [nodes, setNodes] = useState<Node[]>([]);

  useEffect(() => {
    if (!isVisible) {
      setSearchValue('');
      setNodes([]);
    }
  }, [isVisible]);

  const handleSearch = async (value: string) => {
    if (!value.trim() || !graphId) {
      setNodes([]);
      return;
    }

    try {
      const response = await searchNodes({
        GraphGUID: graphId,
        Name: value.trim(),
        Ordering: 'CreatedDescending',
      });
      
      if (response?.data?.Nodes) {
        setNodes(response.data.Nodes);
      } else {
        setNodes([]);
      }
    } catch (error) {
      console.error('Failed to search nodes:', error);
      setNodes([]);
    }
  };

  const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const value = e.target.value;
    setSearchValue(value);
    if (!value.trim()) {
      setNodes([]);
    }
  };

  const columns: ColumnType<Node>[] = [
    {
      title: 'Name',
      dataIndex: 'Name',
      key: 'Name',
      render: (text: string) => text || '-',
    },
    {
      title: 'Labels',
      dataIndex: 'Labels',
      key: 'Labels',
      render: (labels: string[]) => (labels && labels.length > 0 ? labels.join(', ') : '-'),
    },
    {
      title: 'GUID',
      dataIndex: 'GUID',
      key: 'GUID',
      render: (text: string) => <span style={{ fontFamily: 'monospace', fontSize: '12px' }}>{text}</span>,
    },
  ];

  const handleRowClick = (record: Node) => {
    if (onNodeSelect) {
      onNodeSelect(record);
    }
    setIsVisible(false);
  };

  return (
    <LitegraphModal
      title="Search Nodes"
      open={isVisible}
      onCancel={() => setIsVisible(false)}
      footer={null}
      width={800}
      centered
    >
      <div style={{ marginBottom: 16 }}>
        <Input.Search
          placeholder="Search nodes by name..."
          allowClear
          enterButton={<SearchOutlined />}
          size="large"
          value={searchValue}
          onChange={handleSearchChange}
          onSearch={handleSearch}
          loading={isLoading}
        />
      </div>

      {isLoading && (
        <div style={{ textAlign: 'center', padding: '40px 0' }}>
          <Spin size="large" />
        </div>
      )}

      {!isLoading && isError && (
        <Empty description="Failed to search nodes. Please try again." />
      )}

      {!isLoading && !isError && nodes.length === 0 && searchValue.trim() && (
        <Empty description="No nodes found" />
      )}

      {!isLoading && !isError && nodes.length > 0 && (
        <LitegraphTable
          columns={columns}
          dataSource={nodes}
          rowKey="GUID"
          pagination={{
            pageSize: 10,
            showSizeChanger: true,
            showTotal: (total) => `Total ${total} nodes`,
          }}
          onRow={(record) => ({
            onClick: () => handleRowClick(record),
            style: { cursor: 'pointer' },
          })}
          size="small"
        />
      )}

      {!isLoading && !isError && !searchValue.trim() && (
        <Empty description="Enter a search term to find nodes" />
      )}
    </LitegraphModal>
  );
};

export default NodeSearchModal;

