'use client';
import { useEffect, useState } from 'react';
import { Form } from 'antd';
import LitegraphModal from '@/components/base/modal/Modal';
import LitegraphFormItem from '@/components/base/form/FormItem';
import LitegraphInput from '@/components/base/input/Input';
import toast from 'react-hot-toast';
import { useCreateLabelMutation, useUpdateLabelMutation } from '@/lib/store/slice/slice';
import { LabelMetadata, LabelMetadataCreateRequest } from 'litegraphdb/dist/types/types';
import NodeSelector from '@/components/node-selector/NodeSelector';
import EdgeSelector from '@/components/edge-selector/EdgeSelector';

interface AddEditLabelProps {
  isAddEditLabelVisible: boolean;
  setIsAddEditLabelVisible: (visible: boolean) => void;
  label: LabelMetadata | null;
  selectedGraph: string;
  onLabelUpdated?: () => Promise<void>;
}

const AddEditLabel = ({
  isAddEditLabelVisible,
  setIsAddEditLabelVisible,
  label,
  selectedGraph,
  onLabelUpdated,
}: AddEditLabelProps) => {
  const [form] = Form.useForm();
  const [formValid, setFormValid] = useState(false);
  const [createLabels, { isLoading: isCreateLoading }] = useCreateLabelMutation();
  const [updateLabelById, { isLoading: isUpdateLoading }] = useUpdateLabelMutation();

  // Add form validation watcher
  const [formValues, setFormValues] = useState({});
  useEffect(() => {
    form
      .validateFields({ validateOnly: true })
      .then(() => setFormValid(true))
      .catch(() => setFormValid(false));
  }, [formValues, form]);

  useEffect(() => {
    if (label) {
      form.setFieldsValue({
        Label: label.Label,
        NodeGUID: label.NodeGUID,
        EdgeGUID: label.EdgeGUID,
      });
    } else {
      form.resetFields();
    }
  }, [label, form]);

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      if (label) {
        // Update existing label
        const updatedLabel: LabelMetadata = {
          GUID: label.GUID,
          GraphGUID: label.GraphGUID,
          CreatedUtc: label.CreatedUtc,
          Label: values.Label,
          NodeGUID: values.NodeGUID || label.NodeGUID,
          EdgeGUID: values.EdgeGUID || label.EdgeGUID,
          LastUpdateUtc: label.LastUpdateUtc,
          TenantGUID: label.TenantGUID,
        };

        if (!updatedLabel.GUID) {
          throw new Error('Label GUID is missing');
        }

        const res = await updateLabelById(updatedLabel);

        if (res) {
          toast.success('Label updated successfully');
          setIsAddEditLabelVisible(false);
          form.resetFields();
          onLabelUpdated && (await onLabelUpdated());
        } else {
          toast.error('Failed to update label - no response received');
        }
      } else {
        // Create new label
        const newLabel: LabelMetadataCreateRequest = {
          Label: values.Label,
          GraphGUID: selectedGraph,
          NodeGUID: values.NodeGUID,
          EdgeGUID: values.EdgeGUID,
        };
        const res = await createLabels(newLabel);
        if (res) {
          toast.success('Label created successfully');
          setIsAddEditLabelVisible(false);
          form.resetFields();
          onLabelUpdated && (await onLabelUpdated());
        }
      }
    } catch (error: unknown) {
      console.error('Failed to submit:', error);
      const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
      toast.error(`Failed to update label: ${errorMessage}`);
    }
  };

  return (
    <LitegraphModal
      title={label ? 'Edit Label' : 'Create Label'}
      open={isAddEditLabelVisible}
      onOk={handleSubmit}
      onCancel={() => {
        setIsAddEditLabelVisible(false);
        form.resetFields();
      }}
      confirmLoading={isCreateLoading || isUpdateLoading}
      okButtonProps={{ disabled: !formValid }}
      data-testid="add-edit-label-modal"
    >
      <Form
        form={form}
        layout="vertical"
        onValuesChange={(_, allValues) => setFormValues(allValues)}
      >
        <LitegraphFormItem
          label="Label"
          name="Label"
          rules={[{ required: true, message: 'Please input Label label!' }]}
        >
          <LitegraphInput placeholder="Enter label label" />
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

export default AddEditLabel;
