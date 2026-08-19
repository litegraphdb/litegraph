'use client';
import { useTranslations } from 'next-intl';
import { TagType } from '@/types/types';
import LitegraphModal from '@/components/base/modal/Modal';
import LitegraphParagraph from '@/components/base/typograpghy/Paragraph';
import toast from 'react-hot-toast';
import { useDeleteTagMutation } from '@/lib/store/slice/slice';

interface DeleteTagProps {
  title: string;
  paragraphText: string;
  isDeleteModelVisible: boolean;
  setIsDeleteModelVisible: (visible: boolean) => void;
  selectedTag: TagType | null | undefined;
  setSelectedTag: (tag: TagType | null | undefined) => void;
  onTagDeleted?: () => Promise<void>;
}

const DeleteTag = ({
  title,
  paragraphText,
  isDeleteModelVisible,
  setIsDeleteModelVisible,
  selectedTag,
  setSelectedTag,
  onTagDeleted,
}: DeleteTagProps) => {
  const t = useTranslations('tags');
  const tCommon = useTranslations('common');
  const [deleteTagById, { isLoading }] = useDeleteTagMutation();

  const handleDelete = async () => {
    if (selectedTag) {
      const res = await deleteTagById(selectedTag.GUID);
      if (res) {
        toast.success(t('toast.deleted'));
        setIsDeleteModelVisible(false);
        setSelectedTag(null);
        onTagDeleted && onTagDeleted();
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
        setSelectedTag(null);
      }}
      confirmLoading={isLoading}
      okText={tCommon('actions.delete')}
      okButtonProps={{ danger: true }}
      data-testid="delete-tag-modal"
    >
      <LitegraphParagraph>{paragraphText}</LitegraphParagraph>
    </LitegraphModal>
  );
};

export default DeleteTag;
