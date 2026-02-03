import { Modal, ModalProps } from 'antd';
import React from 'react';

export type LitegraphModalProps = Omit<ModalProps, 'destroyOnClose'> & {
  destroyOnClose?: boolean;
};

const LitegraphModal = ({ getContainer, destroyOnClose, ...props }: LitegraphModalProps) => {
  return (
    <Modal
      getContainer={getContainer || (() => document.getElementById('root-div') as HTMLElement)}
      destroyOnHidden={destroyOnClose}
      {...props}
    />
  );
};

export default LitegraphModal;
