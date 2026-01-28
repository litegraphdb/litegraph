import { Button, Modal } from 'antd';
import React from 'react';

const ConfirmationModal = ({
  title,
  content,
  onCancel,
  onConfirm,
  open,
  loading,
}: {
  title: string;
  content: string;
  onCancel: () => void;
  onConfirm: () => void;
  open: boolean;
  loading?: boolean;
}) => {
  return (
    <Modal
      onCancel={onCancel}
      onOk={onConfirm}
      open={open}
      title={title}
      okButtonProps={{ loading }}
      cancelButtonProps={{ disabled: loading }}
    >
      {content}
    </Modal>
  );
};

export default ConfirmationModal;
