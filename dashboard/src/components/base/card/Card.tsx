import { Card, CardProps } from 'antd';
import React from 'react';

export type LiteGraphCardProps = CardProps & {};
const LiteGraphCard = (props: LiteGraphCardProps) => {
  return <Card {...props} />;
};

export default LiteGraphCard;
