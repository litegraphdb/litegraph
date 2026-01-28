import React from 'react';
import { Select, SelectProps } from 'antd';
import LitegraphTooltip from '../tooltip/Tooltip';

export interface OptionProps {
  value: string | number;
  label: string;
}

interface LitegraphSelectProps extends Omit<SelectProps<string | number | string[]>, 'options'> {
  options?: OptionProps[];
  readonly?: boolean;
  tooltip?: boolean;
}

const LitegraphSelect = (props: LitegraphSelectProps) => {
  const { options, placeholder, style, readonly, tooltip, ...restProps } = props;

  return (
    <Select style={style} placeholder={placeholder} {...restProps} aria-readonly={readonly}>
      {options?.map((option: OptionProps) => (
        <Select.Option key={option.value} value={option.value}>
          {tooltip ? (
            <LitegraphTooltip title={option.label}>
              <span>{option.label}</span>
            </LitegraphTooltip>
          ) : (
            <span>{option.label}</span>
          )}
        </Select.Option>
      ))}
    </Select>
  );
};

export default LitegraphSelect;
