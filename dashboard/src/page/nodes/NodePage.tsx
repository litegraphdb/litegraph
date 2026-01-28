'use client';
import { useEffect, useMemo, useState } from 'react';
import {
  CloseOutlined,
  LoadingOutlined,
  PlusSquareOutlined,
  RedoOutlined,
  SearchOutlined,
} from '@ant-design/icons';
import { useAppSelector } from '@/lib/store/hooks';
import { RootState } from '@/lib/store/store';
import LitegraphTable from '@/components/base/table/Table';
import { tableColumns } from './constant';
import LitegraphButton from '@/components/base/button/Button';
import { NodeType } from '@/types/types';
import FallBack from '@/components/base/fallback/FallBack';
import AddEditNode from './components/AddEditNode';
import DeleteNode from './components/DeleteNode';
import PageContainer from '@/components/base/pageContainer/PageContainer';
import LitegraphFlex from '@/components/base/flex/Flex';
import LitegraphText from '@/components/base/typograpghy/Text';
import SearchByTLDModal from '@/components/search/SearchModal';
import { SearchData } from '@/components/search/type';
import { convertTagsToRecord } from '@/components/inputs/tags-input/utils';
import { hasScoreOrDistanceInData } from '@/utils/dataUtils';
import { useEnumerateAndSearchNodeQuery } from '@/lib/store/slice/slice';
import { usePagination } from '@/hooks/appHooks';
import { tablePaginationConfig } from '@/constants/pagination';
import { EnumerateAndSearchRequest } from 'litegraphdb/dist/types/types';
import AppliedFilter from '@/components/table-filter/AppliedFilter';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';

const NodePage = () => {
  // Redux state for the list of graphs
  const selectedGraphRedux = useAppSelector((state: RootState) => state.liteGraph.selectedGraph);
  const [isSearching, setIsSearching] = useState<boolean>(false);
  const { page, pageSize, skip, handlePageChange } = usePagination();
  const [searchParams, setSearchParams] = useState<EnumerateAndSearchRequest>({});
  const {
    data: nodesList,
    refetch: fetchNodesList,
    isLoading,
    isFetching,
    isError: isNodesError,
  } = useEnumerateAndSearchNodeQuery(
    {
      graphId: selectedGraphRedux,
      request: {
        ...searchParams,
        Skip: skip,
        MaxResults: pageSize,
        IncludeSubordinates: true,
      },
    },
    { skip: !selectedGraphRedux }
  );
  const isNodesLoading = isLoading || isFetching;
  const [selectedNode, setSelectedNode] = useState<NodeType | null | undefined>(null);

  const [isAddEditNodeVisible, setIsAddEditNodeVisible] = useState<boolean>(false);
  const [isDeleteModelVisible, setIsDeleteModelVisible] = useState<boolean>(false);
  const [showSearchModal, setShowSearchModal] = useState(false);

  const handleCreateNode = () => {
    setSelectedNode(null);
    setIsAddEditNodeVisible(true);
  };

  const handleEditNode = (data: NodeType) => {
    setSelectedNode(data);
    setIsAddEditNodeVisible(true);
  };

  const handleDelete = (record: NodeType) => {
    setSelectedNode(record);
    setIsDeleteModelVisible(true);
  };
  const onSearch = async (values: SearchData) => {
    setSearchParams({
      Ordering: 'CreatedDescending',
      Labels: values.labels,
      Expr: values.expr,
      Tags: convertTagsToRecord(values.tags),
    });
  };

  const dataSource = nodesList?.Objects || [];
  const hasScoreOrDistance = useMemo(
    () => hasScoreOrDistanceInData(dataSource || []),
    [dataSource]
  );

  useEffect(() => {
    setIsSearching(false);
  }, [selectedGraphRedux]);

  return (
    <PageContainer
      showGraphSelector
      id="nodes"
      pageTitle={
        <LitegraphFlex align="center" gap={10}>
          <LitegraphText>Nodes</LitegraphText>
          {selectedGraphRedux && (
            <SearchOutlined className="cursor-pointer" onClick={() => setShowSearchModal(true)} />
          )}
          {isNodesLoading ? (
            <LoadingOutlined className="loading-icon" />
          ) : (
            <LitegraphTooltip title="Refresh Data" placement="right">
              <RedoOutlined className="cursor-pointer" onClick={fetchNodesList} />
            </LitegraphTooltip>
          )}
        </LitegraphFlex>
      }
      pageTitleRightContent={
        selectedGraphRedux ? (
          <LitegraphButton
            type="link"
            icon={<PlusSquareOutlined />}
            onClick={handleCreateNode}
            weight={500}
          >
            Create Node
          </LitegraphButton>
        ) : undefined
      }
    >
      {isNodesError && !isNodesLoading ? (
        <FallBack retry={fetchNodesList}>{'Something went wrong.'}</FallBack>
      ) : (
        <>
          <LitegraphFlex
            style={{ marginTop: '-10px' }}
            gap={20}
            justify="space-between"
            align="center"
            className="mb-sm"
          >
            {!isNodesLoading && (
              <AppliedFilter
                searchParams={searchParams}
                totalRecords={nodesList?.TotalRecords || 0}
                entityName="node(s)"
                onClear={() => setSearchParams({})}
              />
            )}
          </LitegraphFlex>
          <LitegraphTable
            columns={
              hasScoreOrDistance
                ? tableColumns(handleEditNode, handleDelete, true)
                : tableColumns(handleEditNode, handleDelete, false)
            }
            dataSource={dataSource}
            loading={isNodesLoading}
            rowKey={'GUID'}
            pagination={{
              ...tablePaginationConfig,
              total: nodesList?.TotalRecords,
              pageSize: pageSize,
              current: page,
              onChange: handlePageChange,
            }}
          />
        </>
      )}

      <AddEditNode
        isAddEditNodeVisible={isAddEditNodeVisible}
        setIsAddEditNodeVisible={setIsAddEditNodeVisible}
        node={selectedNode ? selectedNode : null}
        selectedGraph={selectedGraphRedux}
      />

      <DeleteNode
        title={`Are you sure you want to delete "${selectedNode?.Name}" node?`}
        paragraphText={'This action will delete node.'}
        isDeleteModelVisible={isDeleteModelVisible}
        setIsDeleteModelVisible={setIsDeleteModelVisible}
        selectedNode={selectedNode}
        setSelectedNode={setSelectedNode}
      />
      <SearchByTLDModal
        setIsSearchModalVisible={setShowSearchModal}
        isSearchModalVisible={showSearchModal}
        onSearch={onSearch}
      />
    </PageContainer>
  );
};

export default NodePage;
