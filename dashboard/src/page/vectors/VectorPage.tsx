'use client';
import { useState } from 'react';
import { useTranslations } from 'next-intl';
import { PlusSquareOutlined } from '@ant-design/icons';
import LitegraphTable from '@/components/base/table/Table';
import LitegraphButton from '@/components/base/button/Button';
import FallBack from '@/components/base/fallback/FallBack';
import { VectorType } from '@/types/types';
import { tableColumns } from './constant';
import PageContainer from '@/components/base/pageContainer/PageContainer';
import AddEditVector from './components/AddEditVector';
import DeleteVector from './components/DeleteVector';
import { transformVectorsDataForTable } from './utils';
import { useSelectedGraph } from '@/hooks/entityHooks';
import { useLayoutContext } from '@/components/layout/context';
import {
  useEnumerateAndSearchVectorQuery,
  useGetManyEdgesQuery,
  useGetManyNodesQuery,
} from '@/lib/store/slice/slice';
import { usePagination } from '@/hooks/appHooks';
import { tablePaginationConfig } from '@/constants/pagination';
import { getNodeAndEdgeGUIDsByEntityList } from '@/utils/dataUtils';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';
import ViewJsonModal from '@/components/base/view-json-modal/ViewJsonModal';

const VectorPage = () => {
  const t = useTranslations('vectors');
  const tCommon = useTranslations('common');
  // Redux state for the list of graphs
  const selectedGraphRedux = useSelectedGraph();
  const { isGraphsLoading } = useLayoutContext();
  const { page, pageSize, skip, handlePageChange } = usePagination();
  const {
    data,
    refetch: fetchVectorsList,
    isLoading,
    isFetching,
    error: isVectorsError,
  } = useEnumerateAndSearchVectorQuery(
    {
      GraphGUID: selectedGraphRedux,
      MaxResults: pageSize,
      Skip: skip,
      Ordering: 'CreatedDescending',
    },
    {
      skip: !selectedGraphRedux,
    }
  );
  const isVectorsLoading = isLoading || isFetching;
  const vectorsList = data?.Objects || [];
  const { nodeGUIDs, edgeGUIDs } = getNodeAndEdgeGUIDsByEntityList(
    vectorsList,
    'NodeGUID',
    'EdgeGUID'
  );

  const {
    data: nodesList,
    isLoading: isNodesLoading,
    refetch: fetchNodesList,
  } = useGetManyNodesQuery(
    {
      graphId: selectedGraphRedux,
      nodeIds: nodeGUIDs,
    },
    {
      skip: !nodeGUIDs.length,
    }
  );
  const {
    data: edgesList,
    isLoading: isEdgesLoading,
    refetch: fetchEdgesList,
  } = useGetManyEdgesQuery(
    {
      graphId: selectedGraphRedux,
      edgeIds: edgeGUIDs,
    },
    {
      skip: !edgeGUIDs.length,
    }
  );
  const fetchNodesAndEdges = async () => {
    fetchNodesList();
    fetchEdgesList();
  };
  const transformedVectorsList = transformVectorsDataForTable(
    vectorsList,
    nodesList || [],
    edgesList || []
  );
  const [selectedVector, setSelectedVector] = useState<VectorType | null | undefined>(null);
  const [isAddEditVectorVisible, setIsAddEditVectorVisible] = useState<boolean>(false);
  const [isDeleteModelVisible, setIsDeleteModelVisible] = useState<boolean>(false);
  const [jsonViewRecord, setJsonViewRecord] = useState<any>(null);

  const handleCreateVector = () => {
    setSelectedVector(null);
    setIsAddEditVectorVisible(true);
  };

  const handleEditVector = (data: VectorType) => {
    setSelectedVector(data);
    setIsAddEditVectorVisible(true);
  };

  const handleDelete = (record: VectorType) => {
    setSelectedVector(record);
    setIsDeleteModelVisible(true);
  };

  return (
    <PageContainer
      id="vectors"
      pageTitle={t('title')}
      pageTitleRightContent={
        <>
          {selectedGraphRedux && (
            <LitegraphTooltip title={t('createTooltip')}>
              <LitegraphButton
                type="link"
                icon={<PlusSquareOutlined />}
                onClick={handleCreateVector}
                weight={500}
              >
                {t('createVector')}
              </LitegraphButton>
            </LitegraphTooltip>
          )}
        </>
      }
    >
      {isVectorsError && !isVectorsLoading ? (
        <FallBack retry={fetchVectorsList}>{tCommon('states.somethingWentWrong')}</FallBack>
      ) : (
        <LitegraphTable
          loading={isGraphsLoading || isVectorsLoading}
          columns={tableColumns(
            t,
            handleEditVector,
            handleDelete,
            isNodesLoading,
            isEdgesLoading,
            setJsonViewRecord
          )}
          dataSource={transformedVectorsList}
          rowKey={'GUID'}
          onRowClick={handleEditVector}
          onRefresh={fetchVectorsList}
          isRefreshing={isVectorsLoading}
          pagination={{
            ...tablePaginationConfig,
            total: data?.TotalRecords,
            pageSize: pageSize,
            current: page,
            onChange: handlePageChange,
          }}
        />
      )}

      {isAddEditVectorVisible && (
        <AddEditVector
          isAddEditVectorVisible={isAddEditVectorVisible}
          setIsAddEditVectorVisible={setIsAddEditVectorVisible}
          vector={selectedVector || null}
          selectedGraph={selectedGraphRedux || 'dummy-graph-id'}
          onVectorUpdated={async () => {
            await fetchVectorsList();
            await fetchNodesAndEdges();
          }}
        />
      )}

      {isDeleteModelVisible && selectedVector && (
        <DeleteVector
          title={t('deleteTitle')}
          paragraphText={t('deleteBody')}
          isDeleteModelVisible={isDeleteModelVisible}
          setIsDeleteModelVisible={setIsDeleteModelVisible}
          selectedVector={selectedVector}
          setSelectedVector={setSelectedVector}
          onVectorDeleted={async () => await fetchNodesAndEdges()}
        />
      )}
      <ViewJsonModal
        open={!!jsonViewRecord}
        onClose={() => setJsonViewRecord(null)}
        data={jsonViewRecord}
        title={t('vectorJson')}
      />
    </PageContainer>
  );
};

export default VectorPage;
