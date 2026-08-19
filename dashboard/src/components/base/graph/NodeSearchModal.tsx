'use client';
import { useState, useEffect } from 'react';
import { useTranslations } from 'next-intl';
import { Input, Empty, Spin } from 'antd';
import { SearchOutlined } from '@ant-design/icons';
import CopyButton from '@/components/base/copy-button/CopyButton';
import { columnTooltip } from '@/utils/tooltipUtils';
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
  const t = useTranslations('graphViewer');
  const tCommon = useTranslations('common');
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
      title: columnTooltip(t('nodeSearch.columns.name'), t('nodeSearch.columns.nameTooltip')),
      dataIndex: 'Name',
      key: 'Name',
      render: (text: string) => text || '-',
    },
    {
      title: columnTooltip(t('nodeSearch.columns.labels'), t('nodeSearch.columns.labelsTooltip')),
      dataIndex: 'Labels',
      key: 'Labels',
      render: (labels: string[]) => (labels && labels.length > 0 ? labels.join(', ') : '-'),
    },
    {
      title: columnTooltip(t('nodeSearch.columns.guid'), t('nodeSearch.columns.guidTooltip')),
      dataIndex: 'GUID',
      key: 'GUID',
      render: (text: string) => <span style={{ display: 'inline-flex', alignItems: 'center', gap: 4, fontFamily: 'monospace', fontSize: 12, whiteSpace: 'nowrap' }}>{text}<CopyButton text={text} tooltipTitle={tCommon('copy.copyGuid')} /></span>,
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
      title={t('nodeSearch.title')}
      open={isVisible}
      onCancel={() => setIsVisible(false)}
      footer={null}
      width={800}
      centered
    >
      <div style={{ marginBottom: 16 }}>
        <Input.Search
          placeholder={t('nodeSearch.placeholder')}
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
        <Empty description={t('nodeSearch.searchFailed')} />
      )}

      {!isLoading && !isError && nodes.length === 0 && searchValue.trim() && (
        <Empty description={t('nodeSearch.noNodesFound')} />
      )}

      {!isLoading && !isError && nodes.length > 0 && (
        <LitegraphTable
          columns={columns}
          dataSource={nodes}
          rowKey="GUID"
          pagination={{
            pageSize: 10,
            showSizeChanger: true,
            showTotal: (total) => t('nodeSearch.total', { count: total }),
          }}
          onRow={(record) => ({
            onClick: () => handleRowClick(record),
            style: { cursor: 'pointer' },
          })}
          size="small"
        />
      )}

      {!isLoading && !isError && !searchValue.trim() && (
        <Empty description={t('nodeSearch.enterSearchTerm')} />
      )}
    </LitegraphModal>
  );
};

export default NodeSearchModal;

