import LitegraphFlex from '@/components/base/flex/Flex';
import LabelInput from '@/components/inputs/label-input/LabelInput';
import TagsInput from '@/components/inputs/tags-input/TagsInput';
import { Form, Tag } from 'antd';
import React from 'react';

const ApiSearchFilters = () => {
  const [form] = Form.useForm();
  return (
    <LitegraphFlex>
      <LabelInput className="w-100" name="labels" />
      <TagsInput name="tags" />
    </LitegraphFlex>
  );
};

export default ApiSearchFilters;
