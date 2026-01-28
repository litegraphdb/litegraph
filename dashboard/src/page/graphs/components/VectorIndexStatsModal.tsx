import React, { useEffect } from 'react';
import { Modal, Descriptions, Tag, Space } from 'antd';
import { useReadVectorIndexStatisticsQuery } from '@/lib/store/slice/slice';
import PageLoading from '@/components/base/loading/PageLoading';
import { formatDateTime } from '@/utils/dateUtils';

interface VectorIndexStats {
  VectorCount: number;
  Dimensions: number;
  IndexType: string;
  M: number;
  EfConstruction: number;
  DefaultEf: number;
  IndexFile: string;
  IndexFileSizeBytes: number;
  EstimatedMemoryBytes: number;
  LastRebuildUtc: string;
  IsLoaded: boolean;
  DistanceMetric: string;
}

interface VectorIndexStatsModalProps {
  isVisible: boolean;
  setIsVisible: (visible: boolean) => void;
  graphId: string;
}

const VectorIndexStatsModal: React.FC<VectorIndexStatsModalProps> = ({
  isVisible,
  setIsVisible,
  graphId,
}) => {
  const {
    data: stats,
    isLoading,
    isFetching,
    error: statsError,
    isError: isStatsError,
  } = useReadVectorIndexStatisticsQuery(graphId, {
    skip: !isVisible || !graphId,
  });

  const isStatsLoading = isLoading || isFetching;

  // Show error message in modal if statistics read fails
  useEffect(() => {
    if (isStatsError && statsError) {
      console.log('Statistics read failed, showing error in modal');
      console.log('Stats Error:', statsError);
    }
  }, [isStatsError, statsError]);

  const handleCancel = () => {
    setIsVisible(false);
  };

  const formatBytes = (bytes: number): string => {
    if (bytes === 0) return '0 B';
    const k = 1024;
    const sizes = ['B', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  const renderValue = (key: string, value: any) => {
    switch (key) {
      case 'IsLoaded':
        return <Tag color={value ? 'green' : 'red'}>{value ? 'Loaded' : 'Not Loaded'}</Tag>;
      case 'LastRebuildUtc':
        return value ? formatDateTime(value) : 'Never';
      case 'IndexFileSizeBytes':
      case 'EstimatedMemoryBytes':
        return formatBytes(value);
      case 'VectorCount':
        return value.toLocaleString();
      case 'Dimensions':
        return `${value}D`;
      case 'M':
      case 'EfConstruction':
      case 'DefaultEf':
        return value.toString();
      case 'IndexType':
        return <Tag color="blue">{value}</Tag>;
      case 'DistanceMetric':
        return <Tag color="purple">{value}</Tag>;
      case 'IndexFile':
        return <code style={{ fontSize: '12px' }}>{value}</code>;
      default:
        return String(value);
    }
  };

  const getLabelDisplay = (key: string): string => {
    const labelMap: Record<string, string> = {
      VectorCount: 'Vector Count',
      Dimensions: 'Dimensions',
      IndexType: 'Index Type',
      M: 'M Parameter',
      EfConstruction: 'EF Construction',
      DefaultEf: 'Default EF',
      IndexFile: 'Index File Path',
      IndexFileSizeBytes: 'Index File Size',
      EstimatedMemoryBytes: 'Estimated Memory Usage',
      LastRebuildUtc: 'Last Rebuild',
      IsLoaded: 'Status',
      DistanceMetric: 'Distance Metric',
    };
    return labelMap[key] || key;
  };

  const getDescription = (key: string): string => {
    const descriptionMap: Record<string, string> = {
      VectorCount: 'Total number of vectors in the index',
      Dimensions: 'Dimensionality of the vectors',
      IndexType: 'Type of vector index algorithm',
      M: 'Number of connections per element in the graph',
      EfConstruction: 'EF parameter used during index construction',
      DefaultEf: 'Default EF parameter for search queries',
      IndexFile: 'File path where the index is stored',
      IndexFileSizeBytes: 'Size of the index file on disk',
      EstimatedMemoryBytes: 'Estimated memory usage of the index',
      LastRebuildUtc: 'Timestamp of the last index rebuild',
      IsLoaded: 'Whether the index is currently loaded in memory',
      DistanceMetric: 'Distance metric used for similarity calculations',
    };
    return descriptionMap[key] || '';
  };

  return (
    <Modal
      title="Vector Index Statistics"
      open={isVisible}
      onCancel={handleCancel}
      footer={null}
      width={900}
    >
      {isStatsLoading ? (
        <PageLoading />
      ) : isStatsError ? (
        <div style={{ textAlign: 'center', padding: '40px 20px' }}>
          <div style={{ color: '#d32f2f', fontSize: '16px', marginBottom: '12px' }}>
            Failed to load vector index statistics
          </div>
          <div style={{ color: '#666', fontSize: '14px', marginBottom: '20px' }}>
            {statsError &&
              ((statsError as any)?.data?.Description ||
                (statsError as any)?.Description ||
                'Unable to retrieve statistics details')}
          </div>
          <div style={{ fontSize: '12px', color: '#999' }}>
            The vector index may not be enabled or there was an issue accessing the statistics.
          </div>
        </div>
      ) : stats ? (
        <div>
          <Descriptions
            column={2}
            bordered
            size="small"
            labelStyle={{ fontWeight: 600, width: '200px' }}
            contentStyle={{ padding: '8px 12px' }}
          >
            {(() => {
              try {
                return Object.entries(stats).map(([key, value]) => (
                  <Descriptions.Item
                    key={key}
                    label={
                      <Space direction="vertical" size={0}>
                        <span>{getLabelDisplay(key)}</span>
                        <span style={{ fontSize: '11px', color: '#666', fontWeight: 400 }}>
                          {getDescription(key)}
                        </span>
                      </Space>
                    }
                  >
                    {renderValue(key, value)}
                  </Descriptions.Item>
                ));
              } catch (error) {
                console.log('Vector Index Statistics Display Error:', error);
                console.log('Error details:', {
                  status: (error as any)?.status,
                  data: (error as any)?.data,
                  message: (error as any)?.message,
                  stack: (error as any)?.stack,
                });
                // Extract error description from API response
                const errorDescription =
                  (error as any)?.data?.Description ||
                  (error as any)?.Description ||
                  'Error processing statistics data';
                return (
                  <Descriptions.Item label="Error" span={2}>
                    <div style={{ color: '#d32f2f' }}>{errorDescription}</div>
                  </Descriptions.Item>
                );
              }
            })()}
          </Descriptions>

          {/* <div
            style={{
              marginTop: '16px',
              padding: '12px',
              backgroundColor: '#f5f5f5',
              borderRadius: '6px',
            }}
          >
            <div style={{ fontSize: '12px', color: '#666' }}>
              <strong>Note:</strong> The vector index provides fast similarity search capabilities
              for high-dimensional vector data.
              {stats.IsLoaded
                ? ' Index is currently loaded and ready for queries.'
                : ' Index is not currently loaded in memory.'}
            </div>
          </div> */}
        </div>
      ) : (
        <div style={{ textAlign: 'center', padding: '20px' }}>
          <div style={{ color: '#666', marginBottom: '8px' }}>
            No vector index statistics available
          </div>
          <div style={{ fontSize: '12px', color: '#999' }}>
            This graph may not have a vector index enabled or the statistics are not accessible.
          </div>
        </div>
      )}
    </Modal>
  );
};

export default VectorIndexStatsModal;
