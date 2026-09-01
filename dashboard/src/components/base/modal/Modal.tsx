import { Modal, ModalProps } from 'antd';
import React from 'react';

export type LitegraphModalProps = Omit<ModalProps, 'destroyOnClose'> & {
  destroyOnClose?: boolean;
};

/**
 * Shared modal wrapper. Every modal opens fully inside the viewport: pinned
 * near the top, body capped to the remaining height and scrolling internally,
 * so the page itself never has to scroll to reveal a modal's bottom edge.
 * Callers may still override `style`/`styles` — their values are merged on top.
 */
const LitegraphModal = ({ getContainer, destroyOnClose, style, styles, ...props }: LitegraphModalProps) => {
  return (
    <Modal
      getContainer={getContainer || (() => document.getElementById('root-div') as HTMLElement)}
      destroyOnHidden={destroyOnClose}
      style={{ top: 16, paddingBottom: 0, ...style }}
      styles={{
        ...styles,
        body: {
          maxHeight: 'calc(100vh - 130px)',
          overflowY: 'auto',
          ...styles?.body,
        },
      }}
      {...props}
      maskClosable
    />
  );
};

export default LitegraphModal;
