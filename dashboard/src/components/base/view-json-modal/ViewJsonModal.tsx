import React from 'react';
import LitegraphModal from '@/components/base/modal/Modal';
import CopyButton from '@/components/base/copy-button/CopyButton';
import LitegraphFlex from '@/components/base/flex/Flex';

interface ViewJsonModalProps {
  open: boolean;
  onClose: () => void;
  data: any;
  title?: string;
}

const ViewJsonModal = ({ open, onClose, data, title = 'View JSON' }: ViewJsonModalProps) => {
  const jsonString = data ? JSON.stringify(data, null, 2) : '';

  return (
    <LitegraphModal
      title={
        <LitegraphFlex align="center" gap={8}>
          <span>{title}</span>
          <CopyButton text={jsonString} tooltipTitle="Copy JSON" />
        </LitegraphFlex>
      }
      open={open}
      onCancel={onClose}
      footer={null}
      width={700}
      centered
    >
      <pre
        style={{
          background: '#f5f5f5',
          padding: 16,
          borderRadius: 8,
          maxHeight: 500,
          overflow: 'auto',
          fontSize: 13,
          fontFamily: 'monospace',
          whiteSpace: 'pre-wrap',
          wordBreak: 'break-word',
        }}
      >
        {jsonString}
      </pre>
    </LitegraphModal>
  );
};

export default ViewJsonModal;
