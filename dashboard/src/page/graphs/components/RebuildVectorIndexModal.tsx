import React from 'react';
import { message } from 'antd';
import { useRebuildVectorIndexMutation } from '@/lib/store/slice/slice';
import LitegraphButton from '@/components/base/button/Button';
import LitegraphParagraph from '@/components/base/typograpghy/Paragraph';
import LitegraphModal from '@/components/base/modal/Modal';

interface RebuildVectorIndexModalProps {
  isVisible: boolean;
  setIsVisible: (visible: boolean) => void;
  graphId: string;
  onSuccess?: () => void;
}

const RebuildVectorIndexModal: React.FC<RebuildVectorIndexModalProps> = ({
  isVisible,
  setIsVisible,
  graphId,
  onSuccess,
}) => {
  const [rebuildVectorIndex, { isLoading }] = useRebuildVectorIndexMutation();

  const handleRebuild = async () => {
    try {
      await rebuildVectorIndex(graphId).unwrap();
      message.success('Vector index rebuild started successfully');
      setIsVisible(false);
      onSuccess?.();
    } catch (error) {
      console.error('Failed to rebuild vector index:', error);
      console.log('Rebuild Vector Index Error:', error);
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
        'Failed to rebuild vector index';
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
      title="Are you sure you want to rebuild the vector index?"
      centered
      open={isVisible}
      onCancel={handleCancel}
      footer={
        <LitegraphButton type="primary" onClick={handleRebuild} loading={isLoading}>
          Confirm
        </LitegraphButton>
      }
    >
      <LitegraphParagraph>
        This action will rebuild the vector index for this graph. This process may take some time
        and will temporarily affect vector search performance.
      </LitegraphParagraph>
    </LitegraphModal>
  );
};

export default RebuildVectorIndexModal;
