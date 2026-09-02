'use client';
import React, { useState } from 'react';
import { useTranslations } from 'next-intl';
import { PlusSquareOutlined } from '@ant-design/icons';
import PageContainer from '@/components/base/pageContainer/PageContainer';
import LitegraphButton from '@/components/base/button/Button';
import LitegraphTable from '@/components/base/table/Table';
import FallBack from '@/components/base/fallback/FallBack';
import { tableColumns } from './constant';
import DeleteBackup from './components/DeleteBackup';
import AddEditBackup from './components/AddEditBackup';
import { downloadBase64File } from '@/utils/appUtils';
import { toast } from 'react-hot-toast';
import { globalToastId } from '@/constants/config';
import { useReadAllBackupsQuery, useReadBackupMutation } from '@/lib/store/slice/slice';
import { BackupMetaData } from 'litegraphdb/dist/types/types';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';

const BackupPage = () => {
  const t = useTranslations('backups');
  const tCommon = useTranslations('common');
  const [isDeleteBackupVisible, setIsDeleteBackupVisible] = useState(false);
  const [isAddEditBackupVisible, setIsAddEditBackupVisible] = useState(false);
  const [selectedBackup, setSelectedBackup] = useState<BackupMetaData | null>(null);
  const {
    data: backupsEnvelope,
    refetch: fetchBackupsList,
    isLoading,
    isFetching,
    error,
  } = useReadAllBackupsQuery();
  const backupsList = backupsEnvelope?.Objects ?? [];
  const [fetchBackupByFilename, { isLoading: isDownloading }] = useReadBackupMutation();
  const isBackupsLoading = isLoading || isFetching;
  const handleCreateBackup = () => {
    setSelectedBackup(null);
    setIsAddEditBackupVisible(true);
  };

  const handleDeleteBackup = (backup: BackupMetaData) => {
    setSelectedBackup(backup);
    setIsDeleteBackupVisible(true);
  };

  const handleDownload = async (backup: BackupMetaData) => {
    if (!backup.Filename) {
      toast.error(t('toast.missingFilename'), { id: globalToastId });
      return;
    }
    const { data } = await fetchBackupByFilename(backup.Filename);
    if (data && data.Data) {
      const downloadFilename = backup.Filename.endsWith('.litegraph.db')
        ? backup.Filename
        : `${backup.Filename}.litegraph.db`;
      downloadBase64File(data.Data, downloadFilename);
    } else {
      toast.error(t('toast.unableToDownload'), { id: globalToastId });
    }
  };

  return (
    <PageContainer
      id="backups"
      pageTitle={t('title')}
      pageTitleRightContent={
        <LitegraphTooltip title={t('createTooltip')}>
          <LitegraphButton
            type="link"
            icon={<PlusSquareOutlined />}
            onClick={handleCreateBackup}
            weight={500}
          >
            {t('createBackup')}
          </LitegraphButton>
        </LitegraphTooltip>
      }
    >
      {error && !isBackupsLoading ? (
        <FallBack retry={fetchBackupsList}>{tCommon('states.somethingWentWrong')}</FallBack>
      ) : (
        <LitegraphTable
          hideHorizontalScroll
          loading={isBackupsLoading || isDownloading}
          columns={tableColumns(t, handleDeleteBackup, handleDownload, isDownloading)}
          dataSource={backupsList}
          rowKey={'Filename'}
          onRefresh={fetchBackupsList}
          isRefreshing={isBackupsLoading}
        />
      )}

      {isAddEditBackupVisible && (
        <AddEditBackup
          isAddEditBackupVisible={isAddEditBackupVisible}
          setIsAddEditBackupVisible={setIsAddEditBackupVisible}
          backup={selectedBackup || null}
        />
      )}

      {isDeleteBackupVisible && selectedBackup && (
        <DeleteBackup
          title={t('deleteTitle', { name: selectedBackup.Filename })}
          paragraphText={t('deleteBody')}
          isDeleteModelVisible={isDeleteBackupVisible}
          setIsDeleteModelVisible={setIsDeleteBackupVisible}
          selectedBackup={selectedBackup}
          setSelectedBackup={setSelectedBackup}
        />
      )}
    </PageContainer>
  );
};

export default BackupPage;
