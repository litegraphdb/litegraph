import { Tag, TagProps } from 'antd';
import React from 'react';

type LitegraphTagProps = TagProps & {
  label: string;
};

const LitegraphTag = ({ label, ...props }: LitegraphTagProps) => {
  return <Tag {...props}>{label}</Tag>;
};

export default LitegraphTag;
