'use client';
import React, { useState } from 'react';
import { useTranslations } from 'next-intl';
import toast from 'react-hot-toast';
import LitegraphModal from '@/components/base/modal/Modal';
import LitegraphFlex from '@/components/base/flex/Flex';
import LitegraphText from '@/components/base/typograpghy/Text';
import LitegraphInput from '@/components/base/input/Input';
import { ChatThread } from '@/lib/sdk/chat';
import { useUpdateChatThreadMutation } from '@/lib/store/slice/slice';
import { globalToastId } from '@/constants/config';

interface RenameThreadModalProps {
  tenantGuid: string;
  thread: ChatThread;
  onClose: () => void;
}

/** Rename-conversation modal: edits the thread title in place. */
const RenameThreadModal = ({ tenantGuid, thread, onClose }: RenameThreadModalProps) => {
  const t = useTranslations('ai.chat');
  const [title, setTitle] = useState(thread.Title || '');
  const [updateThread, { isLoading }] = useUpdateChatThreadMutation();

  const handleRename = async () => {
    const trimmed = title.trim();
    if (!trimmed) return;
    const { error } = await updateThread({
      tenantGuid,
      threadGuid: thread.GUID,
      body: { Title: trimmed },
    });
    if (error) {
      toast.error(t('renameThreadModal.failed'), { id: globalToastId });
      return;
    }
    toast.success(t('renameThreadModal.done'), { id: globalToastId });
    onClose();
  };

  return (
    <LitegraphModal
      open
      title={t('renameThreadModal.title')}
      onOk={handleRename}
      onCancel={onClose}
      okText={t('renameThreadModal.save')}
      okButtonProps={{ disabled: !title.trim() }}
      confirmLoading={isLoading}
      data-testid="chat-rename-thread-modal"
    >
      <LitegraphFlex vertical gap={8}>
        <LitegraphText style={{ fontSize: 13 }}>{t('renameThreadModal.label')}</LitegraphText>
        <LitegraphInput
          value={title}
          maxLength={200}
          autoFocus
          placeholder={t('untitledThread')}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => setTitle(e.target.value)}
          onPressEnter={handleRename}
          data-testid="chat-rename-thread-input"
        />
      </LitegraphFlex>
    </LitegraphModal>
  );
};

export default RenameThreadModal;
