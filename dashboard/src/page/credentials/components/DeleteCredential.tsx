'use client';
import { useTranslations } from 'next-intl';
import { CredentialType } from '@/types/types';
import LitegraphModal from '@/components/base/modal/Modal';
import LitegraphParagraph from '@/components/base/typograpghy/Paragraph';
import toast from 'react-hot-toast';
import { useDeleteCredentialMutation } from '@/lib/store/slice/slice';

interface DeleteCredentialProps {
  title: string;
  paragraphText: string;
  isDeleteModelVisible: boolean;
  setIsDeleteModelVisible: (visible: boolean) => void;
  selectedCredential: CredentialType | null | undefined;
  setSelectedCredential: (credential: CredentialType | null) => void;

  onCredentialDeleted?: () => Promise<void>;
}

const DeleteCredential = ({
  title,
  paragraphText,
  isDeleteModelVisible,
  setIsDeleteModelVisible,
  selectedCredential,
  setSelectedCredential,

  onCredentialDeleted,
}: DeleteCredentialProps) => {
  const t = useTranslations('credentials');
  const tCommon = useTranslations('common');
  const [deleteCredentialById, { isLoading }] = useDeleteCredentialMutation();

  const handleDelete = async () => {
    if (selectedCredential) {
      const res = await deleteCredentialById(selectedCredential.GUID);
      if (res) {
        toast.success(t('toast.deleted'));
        setIsDeleteModelVisible(false);
        setSelectedCredential(null);

        onCredentialDeleted && onCredentialDeleted();
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
        setSelectedCredential(null);
      }}
      confirmLoading={isLoading}
      okText={tCommon('actions.delete')}
      okButtonProps={{ danger: true }}
      data-testid="delete-credential-modal"
    >
      <LitegraphParagraph>{paragraphText}</LitegraphParagraph>
    </LitegraphModal>
  );
};

export default DeleteCredential;
