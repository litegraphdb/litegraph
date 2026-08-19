'use client';
import { useEffect, useMemo, useState } from 'react';
import { useTranslations } from 'next-intl';
import { PlusSquareOutlined, SearchOutlined } from '@ant-design/icons';
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
import ViewJsonModal from '@/components/base/view-json-modal/ViewJsonModal';

const NodePage = () => {
  const t = useTranslations('nodes');
  const tCommon = useTranslations('common');
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
  const [jsonViewRecord, setJsonViewRecord] = useState<any>(null);

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
          <LitegraphText>{t('title')}</LitegraphText>
          {selectedGraphRedux && (
            <LitegraphTooltip title={t('searchTooltip')}>
              <SearchOutlined className="cursor-pointer" onClick={() => setShowSearchModal(true)} />
            </LitegraphTooltip>
          )}
        </LitegraphFlex>
      }
      pageTitleRightContent={
        selectedGraphRedux ? (
          <LitegraphTooltip title={t('createTooltip')}>
            <LitegraphButton
              type="link"
              icon={<PlusSquareOutlined />}
              onClick={handleCreateNode}
              weight={500}
            >
              {t('createNode')}
            </LitegraphButton>
          </LitegraphTooltip>
        ) : undefined
      }
    >
      {isNodesError && !isNodesLoading ? (
        <FallBack retry={fetchNodesList}>{tCommon('states.somethingWentWrong')}</FallBack>
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
                entityName={t('entityName')}
                onClear={() => setSearchParams({})}
              />
            )}
          </LitegraphFlex>
          <LitegraphTable
            columns={
              hasScoreOrDistance
                ? tableColumns(t, handleEditNode, handleDelete, true, setJsonViewRecord)
                : tableColumns(t, handleEditNode, handleDelete, false, setJsonViewRecord)
            }
            dataSource={dataSource}
            loading={isNodesLoading}
            rowKey={'GUID'}
            onRowClick={handleEditNode}
            onRefresh={fetchNodesList}
            isRefreshing={isNodesLoading}
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
        title={t('deleteTitle', { name: selectedNode?.Name ?? '' })}
        paragraphText={t('deleteBody')}
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
      <ViewJsonModal
        open={!!jsonViewRecord}
        onClose={() => setJsonViewRecord(null)}
        data={jsonViewRecord}
        title={t('nodeJson')}
      />
    </PageContainer>
  );
};

export default NodePage;
