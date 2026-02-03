import LitegraphFlex from '@/components/base/flex/Flex';
import LabelInput from '@/components/inputs/label-input/LabelInput';
import TagsInput from '@/components/inputs/tags-input/TagsInput';
import { Form } from 'antd';
import React from 'react';

const ApiSearchFilters = () => {
  const [form] = Form.useForm();
  return (
    <Form form={form} layout="vertical">
      <LitegraphFlex>
        <LabelInput className="w-100" name="labels" />
        <TagsInput name="tags" />
      </LitegraphFlex>
    </Form>
  );
};

export default ApiSearchFilters;
