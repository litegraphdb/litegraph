'use client';
import { VectorType } from '@/types/types';
import LitegraphModal from '@/components/base/modal/Modal';
import LitegraphParagraph from '@/components/base/typograpghy/Paragraph';
import toast from 'react-hot-toast';
import { useDeleteVectorMutation } from '@/lib/store/slice/slice';

interface DeleteVectorProps {
  title: string;
  paragraphText: string;
  isDeleteModelVisible: boolean;
  setIsDeleteModelVisible: (visible: boolean) => void;
  selectedVector: VectorType | null | undefined;
  setSelectedVector: (vector: VectorType | null | undefined) => void;
  onVectorDeleted?: () => Promise<void>;
}

const DeleteVector = ({
  title,
  paragraphText,
  isDeleteModelVisible,
  setIsDeleteModelVisible,
  selectedVector,
  setSelectedVector,
  onVectorDeleted,
}: DeleteVectorProps) => {
  const [deleteVectorById, { isLoading }] = useDeleteVectorMutation();

  const handleDelete = async () => {
    if (selectedVector) {
      const res = await deleteVectorById(selectedVector.GUID);
      if (res) {
        toast.success('Vector deleted successfully');
        setIsDeleteModelVisible(false);
        setSelectedVector(null);
        onVectorDeleted && onVectorDeleted();
      }
    }
  };

  return (
    <LitegraphModal
      title={title}
      centered
      open={isDeleteModelVisible}
      onOk={handleDelete}
      onCancel={() => {
        setIsDeleteModelVisible(false);
        setSelectedVector(null);
      }}
      confirmLoading={isLoading}
      okText="Delete"
      okButtonProps={{ danger: true }}
      data-testid="delete-vector-modal"
    >
      <LitegraphParagraph>{paragraphText}</LitegraphParagraph>
    </LitegraphModal>
  );
};

export default DeleteVector;
