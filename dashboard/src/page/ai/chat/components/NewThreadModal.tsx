'use client';
import React, { useState } from 'react';
import { useTranslations } from 'next-intl';
import toast from 'react-hot-toast';
import LitegraphModal from '@/components/base/modal/Modal';
import LitegraphFlex from '@/components/base/flex/Flex';
import LitegraphText from '@/components/base/typograpghy/Text';
import LitegraphSelect from '@/components/base/select/Select';
import { useCreateChatThreadMutation, useGetAllGraphsQuery } from '@/lib/store/slice/slice';
import { globalToastId } from '@/constants/config';

interface NewThreadModalProps {
  tenantGuid: string;
  onClose: () => void;
  onCreated: (threadGuid: string) => void;
}

/** New-conversation modal with an optional graph binding picker. */
const NewThreadModal = ({ tenantGuid, onClose, onCreated }: NewThreadModalProps) => {
  const t = useTranslations('ai.chat');
  const [graphGuid, setGraphGuid] = useState<string | undefined>(undefined);
  const { data: graphs = [], isLoading: isGraphsLoading } = useGetAllGraphsQuery();
  const [createThread, { isLoading }] = useCreateChatThreadMutation();

  const handleCreate = async () => {
    const { data, error } = await createThread({
      tenantGuid,
      body: { GraphGUID: graphGuid || null },
    });
    if (error || !data) {
      toast.error(t('newThreadModal.failed'), { id: globalToastId });
      return;
    }
    onCreated(data.GUID);
    onClose();
  };

  return (
    <LitegraphModal
      open
      title={t('newThreadModal.title')}
      onOk={handleCreate}
      onCancel={onClose}
      okText={t('newThreadModal.create')}
      confirmLoading={isLoading}
      data-testid="chat-new-thread-modal"
    >
      <LitegraphFlex vertical gap={8}>
        <LitegraphText style={{ fontSize: 13 }}>{t('newThreadModal.graphLabel')}</LitegraphText>
        <LitegraphSelect
          allowClear
          showSearch
          optionFilterProp="label"
          loading={isGraphsLoading}
          placeholder={t('newThreadModal.graphPlaceholder')}
          value={graphGuid}
          onChange={(value) => setGraphGuid(value as string | undefined)}
          options={graphs.map((graph) => ({ label: graph.Name || graph.GUID, value: graph.GUID }))}
          data-testid="chat-graph-select"
        />
        <LitegraphText style={{ fontSize: 12, color: 'var(--ant-color-text-tertiary)' }}>
          {t('newThreadModal.graphHint')}
        </LitegraphText>
      </LitegraphFlex>
    </LitegraphModal>
  );
};

export default NewThreadModal;
