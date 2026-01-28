'use client';
import { useState } from 'react';
import { LoadingOutlined, PlusSquareOutlined, RedoOutlined } from '@ant-design/icons';
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
import LitegraphText from '@/components/base/typograpghy/Text';
import LitegraphFlex from '@/components/base/flex/Flex';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';

const VectorPage = () => {
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
      pageTitle={
        <LitegraphFlex align="center" gap={10}>
          <LitegraphText>Vectors</LitegraphText>
          {isVectorsLoading ? (
            <LoadingOutlined className="loading-icon" />
          ) : (
            <LitegraphTooltip title="Refresh Data" placement="right">
              <RedoOutlined className="cursor-pointer" onClick={fetchVectorsList} />
            </LitegraphTooltip>
          )}
        </LitegraphFlex>
      }
      pageTitleRightContent={
        <>
          {selectedGraphRedux && (
            <LitegraphButton
              type="link"
              icon={<PlusSquareOutlined />}
              onClick={handleCreateVector}
              weight={500}
            >
              Create Vector
            </LitegraphButton>
          )}
        </>
      }
    >
      {isVectorsError && !isVectorsLoading ? (
        <FallBack retry={fetchVectorsList}>Something went wrong.</FallBack>
      ) : (
        <LitegraphTable
          loading={isGraphsLoading || isVectorsLoading}
          columns={tableColumns(handleEditVector, handleDelete, isNodesLoading, isEdgesLoading)}
          dataSource={transformedVectorsList}
          rowKey={'GUID'}
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
          title={`Are you sure you want to delete this vector?`}
          paragraphText={'This action will delete vector.'}
          isDeleteModelVisible={isDeleteModelVisible}
          setIsDeleteModelVisible={setIsDeleteModelVisible}
          selectedVector={selectedVector}
          setSelectedVector={setSelectedVector}
          onVectorDeleted={async () => await fetchNodesAndEdges()}
        />
      )}
    </PageContainer>
  );
};

export default VectorPage;
