'use client';
import { useState } from 'react';
import { LoadingOutlined, PlusSquareOutlined, RedoOutlined } from '@ant-design/icons';
import LitegraphTable from '@/components/base/table/Table';
import LitegraphButton from '@/components/base/button/Button';
import FallBack from '@/components/base/fallback/FallBack';
import { TagType } from '@/types/types';
import { tableColumns } from './constant';

import PageContainer from '@/components/base/pageContainer/PageContainer';
import AddEditTag from './components/AddEditTag';
import DeleteTag from './components/DeleteTag';
import { transformTagsDataForTable } from './utils';
import { useSelectedGraph } from '@/hooks/entityHooks';
import { useLayoutContext } from '@/components/layout/context';
import {
  useEnumerateAndSearchTagQuery,
  useGetManyEdgesQuery,
  useGetManyNodesQuery,
} from '@/lib/store/slice/slice';
import { usePagination } from '@/hooks/appHooks';
import { tablePaginationConfig } from '@/constants/pagination';
import { getNodeAndEdgeGUIDsByEntityList } from '@/utils/dataUtils';
import LitegraphFlex from '@/components/base/flex/Flex';
import LitegraphText from '@/components/base/typograpghy/Text';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';

const TagPage = () => {
  // Redux state for the list of graphs
  const selectedGraphRedux = useSelectedGraph();
  const { isGraphsLoading } = useLayoutContext();
  const { page, pageSize, skip, handlePageChange } = usePagination();
  const {
    data,
    refetch: fetchTagsList,
    isLoading,
    isFetching,
    error: isTagsError,
  } = useEnumerateAndSearchTagQuery(
    {
      MaxResults: pageSize,
      Skip: skip,
      GraphGUID: selectedGraphRedux,
    },
    {
      skip: !selectedGraphRedux,
    }
  );
  const isTagsLoading = isLoading || isFetching;
  const { nodeGUIDs, edgeGUIDs } = getNodeAndEdgeGUIDsByEntityList(
    data?.Objects || [],
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
  const tagsList = data?.Objects || [];
  const transformedTagsList = transformTagsDataForTable(tagsList, nodesList || [], edgesList || []);
  const [selectedTag, setSelectedTag] = useState<TagType | null | undefined>(null);
  const [isAddEditTagVisible, setIsAddEditTagVisible] = useState<boolean>(false);
  const [isDeleteModelVisible, setIsDeleteModelVisible] = useState<boolean>(false);

  const handleCreateTag = () => {
    setSelectedTag(null);
    setIsAddEditTagVisible(true);
  };

  const handleEditTag = (data: TagType) => {
    setSelectedTag(data);
    setIsAddEditTagVisible(true);
  };

  const handleDelete = (record: TagType) => {
    setSelectedTag(record);
    setIsDeleteModelVisible(true);
  };

  return (
    <PageContainer
      id="tags"
      pageTitle={
        <LitegraphFlex align="center" gap={10}>
          <LitegraphText>Tags</LitegraphText>
          {isTagsLoading ? (
            <LoadingOutlined className="loading-icon" />
          ) : (
            <LitegraphTooltip title="Refresh Data" placement="right">
              <RedoOutlined className="cursor-pointer" onClick={fetchTagsList} />
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
              onClick={handleCreateTag}
              weight={500}
            >
              Create Tag
            </LitegraphButton>
          )}
        </>
      }
    >
      {isTagsError && !isTagsLoading ? (
        <FallBack retry={fetchTagsList}>Something went wrong.</FallBack>
      ) : (
        <LitegraphTable
          loading={isGraphsLoading || isTagsLoading}
          columns={tableColumns(handleEditTag, handleDelete, isNodesLoading, isEdgesLoading)}
          dataSource={transformedTagsList}
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

      {isAddEditTagVisible && (
        <AddEditTag
          isAddEditTagVisible={isAddEditTagVisible}
          setIsAddEditTagVisible={setIsAddEditTagVisible}
          tag={selectedTag || null}
          selectedGraph={selectedGraphRedux || 'dummy-graph-id'}
          onTagUpdated={async () => {
            await fetchTagsList();
            await fetchNodesAndEdges();
          }}
        />
      )}

      {isDeleteModelVisible && selectedTag && (
        <DeleteTag
          title={`Are you sure you want to delete "${selectedTag.Key}" tag?`}
          paragraphText={'This action will delete tag.'}
          isDeleteModelVisible={isDeleteModelVisible}
          setIsDeleteModelVisible={setIsDeleteModelVisible}
          selectedTag={selectedTag}
          setSelectedTag={setSelectedTag}
          onTagDeleted={async () => await fetchNodesAndEdges()}
        />
      )}
    </PageContainer>
  );
};

export default TagPage;
