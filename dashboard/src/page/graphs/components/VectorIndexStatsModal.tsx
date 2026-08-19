import React, { useEffect } from 'react';
import { Modal, Descriptions, Tag, Space } from 'antd';
import { useTranslations } from 'next-intl';
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
  const t = useTranslations('vectorIndex');
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
        return (
          <Tag color={value ? 'green' : 'red'}>
            {value ? t('stats.loaded') : t('stats.notLoaded')}
          </Tag>
        );
      case 'LastRebuildUtc':
        return value ? formatDateTime(value) : t('stats.never');
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

  // Maps an API statistics key to its catalog sub-key under `vectorIndex.stats`.
  const statKeyMap: Record<string, string> = {
    VectorCount: 'vectorCount',
    Dimensions: 'dimensions',
    IndexType: 'indexType',
    M: 'm',
    EfConstruction: 'efConstruction',
    DefaultEf: 'defaultEf',
    IndexFile: 'indexFile',
    IndexFileSizeBytes: 'indexFileSize',
    EstimatedMemoryBytes: 'estimatedMemory',
    LastRebuildUtc: 'lastRebuild',
    IsLoaded: 'status',
    DistanceMetric: 'distanceMetric',
  };

  const getLabelDisplay = (key: string): string => {
    const subKey = statKeyMap[key];
    return subKey ? t(`stats.label.${subKey}`) : key;
  };

  const getDescription = (key: string): string => {
    const subKey = statKeyMap[key];
    return subKey ? t(`stats.desc.${subKey}`) : '';
  };

  return (
    <Modal
      title={t('statsTitle')}
      open={isVisible}
      onCancel={handleCancel}
      footer={null}
      width={900}
      maskClosable
    >
      {isStatsLoading ? (
        <PageLoading />
      ) : isStatsError ? (
        <div style={{ textAlign: 'center', padding: '40px 20px' }}>
          <div style={{ color: '#d32f2f', fontSize: '16px', marginBottom: '12px' }}>
            {t('stats.loadFailed')}
          </div>
          <div style={{ color: '#666', fontSize: '14px', marginBottom: '20px' }}>
            {statsError &&
              ((statsError as any)?.data?.Description ||
                (statsError as any)?.Description ||
                t('stats.unableToRetrieve'))}
          </div>
          <div style={{ fontSize: '12px', color: '#999' }}>{t('stats.hint')}</div>
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
                  t('stats.errorProcessing');
                return (
                  <Descriptions.Item label={t('stats.error')} span={2}>
                    <div style={{ color: '#d32f2f' }}>{errorDescription}</div>
                  </Descriptions.Item>
                );
              }
            })()}
          </Descriptions>
        </div>
      ) : (
        <div style={{ textAlign: 'center', padding: '20px' }}>
          <div style={{ color: '#666', marginBottom: '8px' }}>{t('stats.noStats')}</div>
          <div style={{ fontSize: '12px', color: '#999' }}>{t('stats.noStatsHint')}</div>
        </div>
      )}
    </Modal>
  );
};

export default VectorIndexStatsModal;
