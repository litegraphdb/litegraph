'use client';
import { useEffect, useState } from 'react';
import { Form } from 'antd';
import { TagType } from '@/types/types';
import LitegraphModal from '@/components/base/modal/Modal';
import LitegraphFormItem from '@/components/base/form/FormItem';
import LitegraphInput from '@/components/base/input/Input';
import toast from 'react-hot-toast';
import { useCreateTagMutation, useUpdateTagMutation } from '@/lib/store/slice/slice';
import { TagMetaData, TagMetaDataCreateRequest } from 'litegraphdb/dist/types/types';
import NodeSelector from '@/components/node-selector/NodeSelector';
import EdgeSelector from '@/components/edge-selector/EdgeSelector';

interface AddEditTagProps {
  isAddEditTagVisible: boolean;
  setIsAddEditTagVisible: (visible: boolean) => void;
  tag: TagType | null;
  selectedGraph: string;
  onTagUpdated?: () => Promise<void>;
}

const AddEditTag = ({
  isAddEditTagVisible,
  setIsAddEditTagVisible,
  tag,
  selectedGraph,
  onTagUpdated,
}: AddEditTagProps) => {
  const [form] = Form.useForm();
  const [formValid, setFormValid] = useState(false);
  const [createTag, { isLoading: isCreateLoading }] = useCreateTagMutation();
  const [updateTagById, { isLoading: isUpdateLoading }] = useUpdateTagMutation();

  // Add form validation watcher
  const [formValues, setFormValues] = useState({});
  useEffect(() => {
    form
      .validateFields({ validateOnly: true })
      .then(() => setFormValid(true))
      .catch(() => setFormValid(false));
  }, [formValues, form]);

  useEffect(() => {
    if (tag) {
      form.setFieldsValue({
        Key: tag.Key,
        Value: tag.Value,
        NodeGUID: tag.NodeGUID,
        EdgeGUID: tag.EdgeGUID,
      });
    } else {
      form.resetFields();
    }
  }, [tag, form]);

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      if (tag) {
        // Update existing tag
        const updatedTag: TagMetaData = {
          GUID: tag.GUID,
          GraphGUID: tag.GraphGUID,
          TenantGUID: tag.TenantGUID,
          CreatedUtc: tag.CreatedUtc,
          Key: values.Key,
          Value: values.Value,
          NodeGUID: values.NodeGUID,
          EdgeGUID: values.EdgeGUID,
          LastUpdateUtc: new Date().toISOString(),
        };

        if (!updatedTag.GUID) {
          throw new Error('Tag GUID is missing');
        }

        const res = await updateTagById(updatedTag);

        if (res) {
          toast.success('Tag updated successfully');
          setIsAddEditTagVisible(false);
          form.resetFields();
          onTagUpdated && (await onTagUpdated());
        } else {
          toast.error('Failed to update tag - no response received');
        }
      } else {
        // Create new tag
        const newTag: TagMetaDataCreateRequest = {
          Key: values.Key,
          Value: values.Value,
          NodeGUID: values.NodeGUID,
          EdgeGUID: values.EdgeGUID,
          GraphGUID: selectedGraph,
        };
        const res = await createTag(newTag);
        if (res) {
          toast.success('Tag created successfully');
          setIsAddEditTagVisible(false);
          form.resetFields();
          onTagUpdated && (await onTagUpdated());
        }
      }
    } catch (error: unknown) {
      console.error('Failed to submit:', error);
      const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
      toast.error(`Failed to update tag: ${errorMessage}`);
    }
  };

  return (
    <LitegraphModal
      title={tag ? 'Edit Tag' : 'Create Tag'}
      open={isAddEditTagVisible}
      onOk={handleSubmit}
      onCancel={() => {
        setIsAddEditTagVisible(false);
        form.resetFields();
      }}
      confirmLoading={isCreateLoading || isUpdateLoading}
      okButtonProps={{ disabled: !formValid }}
      data-testid="add-edit-tag-modal"
    >
      <Form
        form={form}
        layout="vertical"
        onValuesChange={(_, allValues) => setFormValues(allValues)}
      >
        <LitegraphFormItem
          label="Key"
          name="Key"
          rules={[{ required: true, message: 'Please input tag key!' }]}
        >
          <LitegraphInput placeholder="Enter tag key" />
        </LitegraphFormItem>

        <LitegraphFormItem
          label="Value"
          name="Value"
          rules={[{ required: true, message: 'Please input tag value!' }]}
        >
          <LitegraphInput placeholder="Enter tag value" />
        </LitegraphFormItem>
        <NodeSelector name="NodeGUID" label="Node" />

        {/* <LitegraphFormItem label="Node" name="NodeGUID">
          <LitegraphSelect placeholder="Select Node" options={nodeOptions} allowClear />
        </LitegraphFormItem> */}

        <EdgeSelector name="EdgeGUID" label="Edge" />

        {/* <LitegraphFormItem label="Edge" name="EdgeGUID">
          <LitegraphSelect placeholder="Select Edge" options={edgeOptions} allowClear />
        </LitegraphFormItem> */}
      </Form>
    </LitegraphModal>
  );
};

export default AddEditTag;
