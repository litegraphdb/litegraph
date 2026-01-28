import { Space, SpaceProps } from 'antd';
import React from 'react';

export type LiteGraphSpaceProps = SpaceProps & {};
const LiteGraphSpace = (props: LiteGraphSpaceProps) => {
  return <Space {...props} />;
};

export default LiteGraphSpace;
