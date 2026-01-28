import { Tooltip, TooltipProps } from 'antd';
import React from 'react';

const LitegraphTooltip = (props: TooltipProps) => {
  const { title, placement, children, ...rest } = props;
  return (
    <Tooltip title={title} placement={placement} {...rest}>
      {children}
    </Tooltip>
  );
};

export default LitegraphTooltip;
