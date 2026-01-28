import React from 'react';
import { message } from 'antd';
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
  const [deleteVectorIndex, { isLoading }] = useDeleteVectorIndexMutation();

  const handleDelete = async () => {
    try {
      await deleteVectorIndex(graphId).unwrap();
      message.success('Vector index deleted successfully');
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
        'Failed to delete vector index';
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
      title="Are you sure you want to delete the vector index?"
      centered
      open={isVisible}
      onCancel={handleCancel}
      footer={
        <LitegraphButton type="primary" danger onClick={handleDelete} loading={isLoading}>
          Confirm
        </LitegraphButton>
      }
    >
      <LitegraphParagraph>
        This action will delete the vector index for this graph.
      </LitegraphParagraph>
    </LitegraphModal>
  );
};

export default DeleteVectorIndexModal;
