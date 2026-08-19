import React from 'react';
import { message } from 'antd';
import { useTranslations } from 'next-intl';
import { useDeleteVectorIndexMutation } from '@/lib/store/slice/slice';
import LitegraphButton from '@/components/base/button/Button';
import LitegraphParagraph from '@/components/base/typograpghy/Paragraph';
import LitegraphModal from '@/components/base/modal/Modal';

interface DeleteVectorIndexModalProps {
  isVisible: boolean;
  setIsVisible: (visible: boolean) => void;
  graphId: string;
  onSuccess?: () => void;
}

const DeleteVectorIndexModal: React.FC<DeleteVectorIndexModalProps> = ({
  isVisible,
  setIsVisible,
  graphId,
  onSuccess,
}) => {
  const t = useTranslations('vectorIndex');
  const tCommon = useTranslations('common');
  const [deleteVectorIndex, { isLoading }] = useDeleteVectorIndexMutation();

  const handleDelete = async () => {
    try {
      await deleteVectorIndex(graphId).unwrap();
      message.success(t('deleteSuccess'));
      setIsVisible(false);
      onSuccess?.();
    } catch (error) {
      console.error('Failed to delete vector index:', error);
      console.log('Delete Vector Index Error:', error);
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
        t('deleteFailed');
      message.error(errorDescription);

      // Close modal on error as well
      setIsVisible(false);
      onSuccess?.();
    }
  };

  const handleCancel = () => {
    setIsVisible(false);
  };

  return (
    <LitegraphModal
      title={t('deleteConfirm')}
      centered
      open={isVisible}
      onCancel={handleCancel}
      footer={
        <LitegraphButton type="primary" danger onClick={handleDelete} loading={isLoading}>
          {tCommon('actions.confirm')}
        </LitegraphButton>
      }
    >
      <LitegraphParagraph>{t('deleteBody')}</LitegraphParagraph>
    </LitegraphModal>
  );
};

export default DeleteVectorIndexModal;
