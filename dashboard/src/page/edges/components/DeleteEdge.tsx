import React, { Dispatch, SetStateAction } from 'react';
import toast from 'react-hot-toast';
import LitegraphModal from '@/components/base/modal/Modal';
import LitegraphParagraph from '@/components/base/typograpghy/Paragraph';
import LitegraphButton from '@/components/base/button/Button';
import { EdgeType } from '@/types/types';
import { useDeleteEdgeMutation } from '@/lib/store/slice/slice';

type DeleteEdgeProps = {
  title: string;
  paragraphText: string;
  isDeleteModelVisisble: boolean;
  setIsDeleteModelVisisble: Dispatch<SetStateAction<boolean>>;
  selectedEdge: EdgeType | null | undefined;
  setSelectedEdge: Dispatch<SetStateAction<EdgeType | null | undefined>>;
  onEdgeDeleted?: () => Promise<void>;
  // Local state update functions for graph viewer
  removeLocalEdge?: (edgeId: string) => void;
};

const DeleteEdge = ({
  title,
  paragraphText,
  isDeleteModelVisisble,
  setIsDeleteModelVisisble,
  selectedEdge,
  setSelectedEdge,
  onEdgeDeleted,
  removeLocalEdge,
}: DeleteEdgeProps) => {
  const [deleteEdgeById, { isLoading: isDeleteEdgeLoading }] = useDeleteEdgeMutation();

  const handleDeleteEdge = async () => {
    if (selectedEdge) {
      if (removeLocalEdge) {
        // Use local state update for graph viewer
        const edgeId = selectedEdge.GUID; // Handle both API edges (GUID) and local edges (id)
        removeLocalEdge(edgeId);
        toast.success('Delete Edge successfully');
        setIsDeleteModelVisisble(false);
        setSelectedEdge(null);
        onEdgeDeleted && onEdgeDeleted();
      } else {
        // Fallback to API call for other contexts
        const res = await deleteEdgeById({
          graphId: selectedEdge.GraphGUID,
          edgeId: selectedEdge.GUID,
        });
        if (res) {
          toast.success('Delete Edge successfully');
          setIsDeleteModelVisisble(false);
          setSelectedEdge(null);
          onEdgeDeleted && onEdgeDeleted();
        }
      }
    }
  };

  return (
    <LitegraphModal
      title={title}
      centered
      open={isDeleteModelVisisble}
      onCancel={() => setIsDeleteModelVisisble(false)}
      footer={
        <LitegraphButton type="primary" onClick={handleDeleteEdge} loading={isDeleteEdgeLoading}>
          Confirm
        </LitegraphButton>
      }
    >
      <LitegraphParagraph>{paragraphText}</LitegraphParagraph>
    </LitegraphModal>
  );
};

export default DeleteEdge;
