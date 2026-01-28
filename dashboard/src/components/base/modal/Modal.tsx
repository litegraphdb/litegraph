import { Modal, ModalProps } from 'antd';
import React from 'react';

export type LitegraphModalProps = ModalProps & {};
const LitegraphModal = ({ getContainer, ...props }: LitegraphModalProps) => {
  return (
    <Modal
      getContainer={getContainer || (() => document.getElementById('root-div') as HTMLElement)}
      {...props}
    />
  );
};

export default LitegraphModal;
