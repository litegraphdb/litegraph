import React, { LegacyRef } from 'react';
import { Input } from 'antd';
import { InputProps, InputRef } from 'antd/es/input';

type LitegraphInputProps = InputProps;

const LitegraphInput = React.forwardRef((props: LitegraphInputProps, ref?: LegacyRef<InputRef>) => {
  const { ...rest } = props;
  return <Input ref={ref} {...rest} />;
});

LitegraphInput.displayName = 'LitegraphInput';
export default LitegraphInput;
