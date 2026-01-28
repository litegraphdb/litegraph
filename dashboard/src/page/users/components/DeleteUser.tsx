'use client';
import LitegraphModal from '@/components/base/modal/Modal';
import LitegraphParagraph from '@/components/base/typograpghy/Paragraph';
import toast from 'react-hot-toast';
import { useDeleteUserMutation } from '@/lib/store/slice/slice';
import { UserMetadata } from 'litegraphdb/dist/types/types';

interface DeleteUserProps {
  title: string;
  paragraphText: string;
  isDeleteModelVisible: boolean;
  setIsDeleteModelVisible: (visible: boolean) => void;
  selectedUser: UserMetadata | null | undefined;
  setSelectedUser: (user: UserMetadata | null) => void;

  onUserDeleted?: () => Promise<void>;
}

const DeleteUser = ({
  title,
  paragraphText,
  isDeleteModelVisible,
  setIsDeleteModelVisible,
  selectedUser,
  setSelectedUser,

  onUserDeleted,
}: DeleteUserProps) => {
  const [deleteUserById, { isLoading }] = useDeleteUserMutation();

  const handleDelete = async () => {
    if (selectedUser) {
      const res = await deleteUserById(selectedUser.GUID);
      if (res) {
        toast.success('User deleted successfully');
        setIsDeleteModelVisible(false);
        setSelectedUser(null);
        onUserDeleted && onUserDeleted();
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
        setSelectedUser(null);
      }}
      confirmLoading={isLoading}
      okText="Delete"
      okButtonProps={{ danger: true }}
    >
      <LitegraphParagraph>{paragraphText}</LitegraphParagraph>
    </LitegraphModal>
  );
};

export default DeleteUser;
