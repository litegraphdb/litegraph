'use client';
import { useMemo, useState } from 'react';
import { useTranslations } from 'next-intl';
import { PlusSquareOutlined, SearchOutlined } from '@ant-design/icons';
import { tableColumns } from './constant';
import { useGetGraphGexfContentByIdMutation } from '@/lib/store/slice/slice';
import { GraphData } from '@/types/types';
import toast from 'react-hot-toast';
import FallBack from '@/components/base/fallback/FallBack';
import LitegraphButton from '@/components/base/button/Button';
import LitegraphParagraph from '@/components/base/typograpghy/Paragraph';
import LitegraphModal from '@/components/base/modal/Modal';
import LitegraphTable from '@/components/base/table/Table';
import PageContainer from '@/components/base/pageContainer/PageContainer';
import dynamic from 'next/dynamic';
import { saveAs } from 'file-saver';
import LitegraphText from '@/components/base/typograpghy/Text';
import LitegraphFlex from '@/components/base/flex/Flex';
import SearchByTLDModal from '@/components/search/SearchModal';
import { convertTagsToRecord } from '@/components/inputs/tags-input/utils';
import { SearchData } from '@/components/search/type';
import { hasScoreOrDistanceInData } from '@/utils/dataUtils';
import { usePagination } from '@/hooks/appHooks';
import { useDeleteGraphMutation, useSearchAndEnumerateGraphQuery } from '@/lib/store/slice/slice';
import { tablePaginationConfig } from '@/constants/pagination';
import { EnumerateAndSearchRequest } from 'litegraphdb/dist/types/types';
import AddEditGraph from './components/AddEditGraph';
import AppliedFilter from '@/components/table-filter/AppliedFilter';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';
import EnableVectorIndexModal from './components/EnableVectorIndexModal';
import VectorIndexStatsModal from './components/VectorIndexStatsModal';
import RebuildVectorIndexModal from './components/RebuildVectorIndexModal';
import DeleteVectorIndexModal from './components/DeleteVectorIndexModal';
import ViewJsonModal from '@/components/base/view-json-modal/ViewJsonModal';

const GraphPage = () => {
  const t = useTranslations('graphs');
  const tCommon = useTranslations('common');
  const { page, pageSize, skip, handlePageChange } = usePagination();
  const [searchParams, setSearchParams] = useState<EnumerateAndSearchRequest>({});
  const {
    data,
    isLoading,
    isFetching,
    refetch: refetchGraphs,
    isError: graphError,
  } = useSearchAndEnumerateGraphQuery({
    ...searchParams,
    MaxResults: pageSize,
    Skip: skip,
    IncludeSubordinates: true,
  });
  const isGraphsLoading = isLoading || isFetching;
  const graphsList = data?.Objects || [];
  const [showSearchModal, setShowSearchModal] = useState(false);
  const [isAddEditGraphVisible, setIsAddEditGraphVisible] = useState<boolean>(false);
  const [isDeleteModelVisisble, setIsDeleteModelVisisble] = useState<boolean>(false);
  const [isEnableVectorIndexModalVisible, setIsEnableVectorIndexModalVisible] =
    useState<boolean>(false);
  const [viewVectorIndexConfigModalVisible, setViewVectorIndexConfigModalVisible] =
    useState<boolean>(false);
  const [vectorIndexStatsModalVisible, setVectorIndexStatsModalVisible] = useState<boolean>(false);
  const [rebuildVectorIndexModalVisible, setRebuildVectorIndexModalVisible] =
    useState<boolean>(false);
  const [deleteVectorIndexModalVisible, setDeleteVectorIndexModalVisible] =
    useState<boolean>(false);
  const [selectedGraph, setSelectedGraph] = useState<GraphData | null>(null);
  const [jsonViewRecord, setJsonViewRecord] = useState<any>(null);
  const [fetchGexfByGraphId, { isLoading: isFetchGexfByGraphIdLoading }] =
    useGetGraphGexfContentByIdMutation();

  const [deleteGraphById, { isLoading: isDeleteGraphLoading }] = useDeleteGraphMutation();

  const handleCreateGraph = () => {
    setSelectedGraph(null);
    setIsAddEditGraphVisible(true);
  };
  const onSearch = async (values: SearchData) => {
    setSearchParams({
      Ordering: 'CreatedDescending',
      Labels: values.labels,
      Expr: values.expr,
      Tags: convertTagsToRecord(values.tags),
    });
  };
  const handleEdit = async (data: GraphData) => {
    setSelectedGraph(data);
    setIsAddEditGraphVisible(true);
  };

  const handleDelete = (record: GraphData) => {
    setSelectedGraph(record);
    setIsDeleteModelVisisble(true);
  };

  const handleExportGexf = async (graph: GraphData) => {
    try {
      const res = await fetchGexfByGraphId({ graphId: graph.GUID });
      const gexfContent = res?.data;
      if (!gexfContent) {
        throw new Error(t('toast.noGexf'));
      }

      // Create a blob from the GEXF content
      const blob = new Blob([gexfContent], { type: 'application/xml' });
      saveAs(blob, `graph-${graph.GUID}.gexf`);
      toast.success(t('toast.exported'));
    } catch (error) {
      console.error('Export error:', error);
      toast.error(t('toast.exportFailed'));
    }
  };

  const handleEnableVectorIndex = async (graph: GraphData) => {
    setSelectedGraph(graph);
    setIsEnableVectorIndexModalVisible(true);
  };

  const handleReadVectorIndexConfig = async (graph: GraphData) => {
    setSelectedGraph(graph);
    setViewVectorIndexConfigModalVisible(true);
  };

  const handleReadVectorIndexStats = async (graph: GraphData) => {
    setSelectedGraph(graph);
    setVectorIndexStatsModalVisible(true);
  };

  const handleRebuildVectorIndex = async (graph: GraphData) => {
    setSelectedGraph(graph);
    setRebuildVectorIndexModalVisible(true);
  };

  const handleDeleteVectorIndex = async (graph: GraphData) => {
    setSelectedGraph(graph);
    setDeleteVectorIndexModalVisible(true);
  };

  const handleDeleteGraph = async () => {
    if (selectedGraph) {
      const res = await deleteGraphById(selectedGraph.GUID);
      if (res) {
        toast.success(t('toast.deleted'));
        setIsDeleteModelVisisble(false);
        setSelectedGraph(null);
      }
    }
  };

  const graphDataSource = graphsList || [];
  const hasScoreOrDistance = useMemo(
    () => hasScoreOrDistanceInData(graphDataSource),
    [graphDataSource]
  );

  if (graphError) {
    return (
      <FallBack retry={refetchGraphs}>
        {graphError
          ? tCommon('states.somethingWentWrong')
          : tCommon('states.cantViewDetails')}
      </FallBack>
    );
  }

  return (
    <PageContainer
      id="graphs"
      pageTitle={
        <LitegraphFlex align="center" gap={10}>
          <LitegraphText>{t('title')}</LitegraphText>
          <LitegraphTooltip title={t('searchTooltip')}>
            <SearchOutlined className="cursor-pointer" onClick={() => setShowSearchModal(true)} />
          </LitegraphTooltip>
        </LitegraphFlex>
      }
      pageTitleRightContent={
        <LitegraphTooltip title={t('createTooltip')}>
          <LitegraphButton
            type="link"
            icon={<PlusSquareOutlined />}
            onClick={handleCreateGraph}
            weight={500}
          >
            {t('createGraph')}
          </LitegraphButton>
        </LitegraphTooltip>
      }
    >
      <>
        <LitegraphFlex
          style={{ marginTop: '-10px' }}
          gap={20}
          justify="space-between"
          align="center"
          className="mb-sm"
        >
          {!isGraphsLoading && (
            <AppliedFilter
              entityName={t('entityName')}
              searchParams={searchParams}
              totalRecords={data?.TotalRecords || 0}
              onClear={() => setSearchParams({})}
            />
          )}
        </LitegraphFlex>
        <LitegraphTable
          columns={tableColumns(
            t,
            handleEdit,
            handleDelete,
            handleExportGexf,
            handleEnableVectorIndex,
            handleReadVectorIndexConfig,
            handleReadVectorIndexStats,
            handleRebuildVectorIndex,
            handleDeleteVectorIndex,
            hasScoreOrDistance,
            setJsonViewRecord
          )}
          dataSource={graphDataSource}
          loading={isGraphsLoading || isFetchGexfByGraphIdLoading}
          rowKey={'GUID'}
          onRowClick={handleEdit}
          onRefresh={refetchGraphs}
          isRefreshing={isGraphsLoading}
          pagination={{
            ...tablePaginationConfig,
            total: data?.TotalRecords,
            pageSize: pageSize,
            current: page,
            onChange: handlePageChange,
          }}
        />
      </>

      <AddEditGraph
        isAddEditGraphVisible={isAddEditGraphVisible}
        setIsAddEditGraphVisible={setIsAddEditGraphVisible}
        graph={selectedGraph ? selectedGraph : null}
        onDone={() => {
          refetchGraphs();
        }}
      />

      <LitegraphModal
        title={t('deleteTitle')}
        centered
        open={isDeleteModelVisisble}
        onCancel={() => setIsDeleteModelVisisble(false)}
        footer={
          <LitegraphButton
            type="primary"
            onClick={handleDeleteGraph}
            loading={isDeleteGraphLoading}
          >
            {tCommon('actions.confirm')}
          </LitegraphButton>
        }
      >
        <LitegraphParagraph>{t('deleteBody')}</LitegraphParagraph>
      </LitegraphModal>
      <SearchByTLDModal
        setIsSearchModalVisible={setShowSearchModal}
        isSearchModalVisible={showSearchModal}
        onSearch={onSearch}
      />
      <EnableVectorIndexModal
        isEnableVectorIndexModalVisible={isEnableVectorIndexModalVisible}
        setIsEnableVectorIndexModalVisible={setIsEnableVectorIndexModalVisible}
        graphId={selectedGraph?.GUID || ''}
        viewMode={false}
        onSuccess={() => {
          setIsEnableVectorIndexModalVisible(false);
          setSelectedGraph(null);
        }}
      />
      {viewVectorIndexConfigModalVisible && (
        <EnableVectorIndexModal
          isEnableVectorIndexModalVisible={viewVectorIndexConfigModalVisible}
          setIsEnableVectorIndexModalVisible={setViewVectorIndexConfigModalVisible}
          graphId={selectedGraph?.GUID || ''}
          viewMode={true}
          onSuccess={() => {
            setViewVectorIndexConfigModalVisible(false);
            setSelectedGraph(null);
          }}
        />
      )}
      <VectorIndexStatsModal
        isVisible={vectorIndexStatsModalVisible}
        setIsVisible={setVectorIndexStatsModalVisible}
        graphId={selectedGraph?.GUID || ''}
      />
      <RebuildVectorIndexModal
        isVisible={rebuildVectorIndexModalVisible}
        setIsVisible={setRebuildVectorIndexModalVisible}
        graphId={selectedGraph?.GUID || ''}
        onSuccess={() => {
          setRebuildVectorIndexModalVisible(false);
          setSelectedGraph(null);
        }}
      />
      <DeleteVectorIndexModal
        isVisible={deleteVectorIndexModalVisible}
        setIsVisible={setDeleteVectorIndexModalVisible}
        graphId={selectedGraph?.GUID || ''}
        onSuccess={() => {
          setDeleteVectorIndexModalVisible(false);
          setSelectedGraph(null);
        }}
      />
      <ViewJsonModal
        open={!!jsonViewRecord}
        onClose={() => setJsonViewRecord(null)}
        data={jsonViewRecord}
        title={t('graphJson')}
      />
    </PageContainer>
  );
};

export default GraphPage;
