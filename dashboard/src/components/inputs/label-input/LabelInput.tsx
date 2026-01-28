'use client';
import React from 'react';
import { Form, Input, Button, Select } from 'antd';
import { CloseCircleFilled, PlusOutlined } from '@ant-design/icons';
import styles from './styles.module.scss';
import LitegraphText from '@/components/base/typograpghy/Text';
import LitegraphFormItem from '@/components/base/form/FormItem';
import LitegraphSelect from '@/components/base/select/Select';

interface LabelInputProps {
  value?: string[];
  onChange?: (values: string[]) => void;
  placeholder?: string;
  name?: string;
  readonly?: boolean;
  className?: string;
}

const LabelInput: React.FC<LabelInputProps> = ({
  value = [],
  onChange,
  placeholder = 'Enter labels, separated by commas',
  name,
  readonly,
  className,
}) => {
  return (
    <LitegraphFormItem label="Labels" name={name} className={className}>
      <LitegraphSelect
        variant={readonly ? 'borderless' : 'outlined'}
        readonly={readonly}
        mode="tags"
        style={{ width: '100%' }}
        placeholder={readonly ? 'N/A' : placeholder}
        tokenSeparators={[',']}
        open={false}
        // value={labels}
        // onChange={handleChange}
      />
    </LitegraphFormItem>
  );
};

export default LabelInput;
