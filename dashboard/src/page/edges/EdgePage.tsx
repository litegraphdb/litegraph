'use client';
import { useMemo, useState } from 'react';
import { useTranslations } from 'next-intl';
import { PlusSquareOutlined, SearchOutlined } from '@ant-design/icons';
import { useAppSelector } from '@/lib/store/hooks';
import { RootState } from '@/lib/store/store';
import LitegraphTable from '@/components/base/table/Table';
import LitegraphButton from '@/components/base/button/Button';
import FallBack from '@/components/base/fallback/FallBack';
import { EdgeType } from '@/types/types';
import { tableColumns } from './constant';
import AddEditEdge from './components/AddEditEdge';
import DeleteEdge from './components/DeleteEdge';
import { transformEdgeDataForTable } from './utils';
import PageContainer from '@/components/base/pageContainer/PageContainer';
import LitegraphFlex from '@/components/base/flex/Flex';
import LitegraphText from '@/components/base/typograpghy/Text';
import { SearchData } from '@/components/search/type';
import { convertTagsToRecord } from '@/components/inputs/tags-input/utils';
import SearchByTLDModal from '@/components/search/SearchModal';
import { getNodeAndEdgeGUIDsByEntityList, hasScoreOrDistanceInData } from '@/utils/dataUtils';
import { usePagination } from '@/hooks/appHooks';
import { tablePaginationConfig } from '@/constants/pagination';
import { useEnumerateAndSearchEdgeQuery, useGetManyNodesQuery } from '@/lib/store/slice/slice';
import { EnumerateAndSearchRequest } from 'litegraphdb/dist/types/types';
import AppliedFilter from '@/components/table-filter/AppliedFilter';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';
import ViewJsonModal from '@/components/base/view-json-modal/ViewJsonModal';

const EdgePage = () => {
  const t = useTranslations('edges');
  const tCommon = useTranslations('common');
  // Redux state for the list of graphs
  const [searchParams, setSearchParams] = useState<EnumerateAndSearchRequest>({});
  const selectedGraphRedux = useAppSelector((state: RootState) => state.liteGraph.selectedGraph);
  const { page, pageSize, skip, handlePageChange } = usePagination();

  const {
    data: edgesList,
    refetch: fetchEdgesList,
    isLoading,
    isFetching,
    error: isEdgesError,
  } = useEnumerateAndSearchEdgeQuery(
    {
      graphId: selectedGraphRedux,
      request: {
        ...searchParams,
        IncludeData: true,
        IncludeSubordinates: true,
        MaxResults: pageSize,
        Skip: skip,
      },
    },
    { skip: !selectedGraphRedux }
  );
  const { nodeGUIDs: toGUIDs } = getNodeAndEdgeGUIDsByEntityList(edgesList?.Objects || [], 'To');
  const { nodeGUIDs: fromGUIDs } = getNodeAndEdgeGUIDsByEntityList(
    edgesList?.Objects || [],
    'From'
  );
  const nodeGUIDs = [...toGUIDs, ...fromGUIDs];
  const isEdgesLoading = isLoading || isFetching;
  const { data: nodesList, isLoading: isNodesLoading } = useGetManyNodesQuery(
    {
      graphId: selectedGraphRedux,
      nodeIds: nodeGUIDs,
    },
    { skip: !nodeGUIDs.length }
  );

  const [selectedEdge, setSelectedEdge] = useState<EdgeType | null | undefined>(null);

  const [isAddEditEdgeVisible, setIsAddEditEdgeVisible] = useState<boolean>(false);
  const [isDeleteModelVisisble, setIsDeleteModelVisisble] = useState<boolean>(false);
  const [showSearchModal, setShowSearchModal] = useState(false);
  const [jsonViewRecord, setJsonViewRecord] = useState<any>(null);

  const transformedEdgesList = transformEdgeDataForTable(
    edgesList?.Objects || [],
    nodesList || [],
    tCommon
  );

  const hasScoreOrDistance = useMemo(
    () => hasScoreOrDistanceInData(transformedEdgesList),
    [transformedEdgesList]
  );

  const handleCreateEdge = () => {
    setSelectedEdge(null);
    setIsAddEditEdgeVisible(true);
  };

  const handleEditEdge = (data: EdgeType) => {
    setSelectedEdge(data);
    setIsAddEditEdgeVisible(true);
  };

  const handleDelete = (record: EdgeType) => {
    setSelectedEdge(record);
    setIsDeleteModelVisisble(true);
  };
  const onSearch = async (values: SearchData) => {
    setSearchParams({
      Labels: values.labels,
      Expr: values.expr,
      Tags: convertTagsToRecord(values.tags),
    });
  };

  return (
    <PageContainer
      id="edges"
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
        <>
          {selectedGraphRedux && (
            <LitegraphTooltip title={t('createTooltip')}>
              <LitegraphButton
                type="link"
                icon={<PlusSquareOutlined />}
                onClick={handleCreateEdge}
                weight={500}
              >
                {t('createEdge')}
              </LitegraphButton>
            </LitegraphTooltip>
          )}
        </>
      }
    >
      {!!isEdgesError && !isEdgesLoading ? (
        <FallBack retry={fetchEdgesList}>{tCommon('states.somethingWentWrong')}</FallBack>
      ) : (
        <>
          <LitegraphFlex
            style={{ marginTop: '-10px' }}
            gap={20}
            justify="space-between"
            align="center"
            className="mb-sm"
          >
            {!isEdgesLoading && (
              <AppliedFilter
                entityName={t('entityName')}
                searchParams={searchParams}
                totalRecords={edgesList?.TotalRecords || 0}
                onClear={() => setSearchParams({})}
              />
            )}
          </LitegraphFlex>
          <LitegraphTable
            columns={tableColumns(
              t,
              tCommon,
              handleEditEdge,
              handleDelete,
              hasScoreOrDistance,
              isNodesLoading,
              setJsonViewRecord
            )}
            dataSource={transformedEdgesList}
            loading={isEdgesLoading}
            rowKey={'GUID'}
            onRowClick={handleEditEdge}
            onRefresh={fetchEdgesList}
            isRefreshing={isEdgesLoading}
            pagination={{
              ...tablePaginationConfig,
              total: edgesList?.TotalRecords,
              pageSize: pageSize,
              current: page,
              onChange: handlePageChange,
            }}
          />
        </>
      )}

      <AddEditEdge
        isAddEditEdgeVisible={isAddEditEdgeVisible}
        setIsAddEditEdgeVisible={setIsAddEditEdgeVisible}
        edge={selectedEdge ? selectedEdge : null}
        selectedGraph={selectedGraphRedux}
      />

      <DeleteEdge
        title={t('deleteTitle', { name: selectedEdge?.Name || '' })}
        paragraphText={t('deleteBody')}
        isDeleteModelVisisble={isDeleteModelVisisble}
        setIsDeleteModelVisisble={setIsDeleteModelVisisble}
        selectedEdge={selectedEdge}
        setSelectedEdge={setSelectedEdge}
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
        title={t('edgeJson')}
      />
    </PageContainer>
  );
};

export default EdgePage;
