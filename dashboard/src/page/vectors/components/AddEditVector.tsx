'use client';
import { useEffect, useState } from 'react';
import { Form } from 'antd';
import { VectorType } from '@/types/types';
import LitegraphModal from '@/components/base/modal/Modal';
import LitegraphFormItem from '@/components/base/form/FormItem';
import LitegraphInput from '@/components/base/input/Input';
import toast from 'react-hot-toast';
import { v4 } from 'uuid';
import { JsonEditor } from 'jsoneditor-react';
import { useCreateVectorMutation, useUpdateVectorMutation } from '@/lib/store/slice/slice';
import { VectorMetadata, VectorCreateRequest } from 'litegraphdb/dist/types/types';
import NodeSelector from '@/components/node-selector/NodeSelector';
import EdgeSelector from '@/components/edge-selector/EdgeSelector';

interface AddEditVectorProps {
  isAddEditVectorVisible: boolean;
  setIsAddEditVectorVisible: (visible: boolean) => void;
  vector: VectorType | null;
  selectedGraph: string;
  onVectorUpdated?: () => Promise<void>;
}

const AddEditVector = ({
  isAddEditVectorVisible,
  setIsAddEditVectorVisible,
  vector,
  selectedGraph,
  onVectorUpdated,
}: AddEditVectorProps) => {
  const [form] = Form.useForm();
  const [formValid, setFormValid] = useState(false);
  const [createVectors, { isLoading: isCreateLoading }] = useCreateVectorMutation();
  const [updateVectorById, { isLoading: isUpdateLoading }] = useUpdateVectorMutation();
  const [uniqueKey, setUniqueKey] = useState(v4());
  // Add form validation watcher
  const [formValues, setFormValues] = useState({});
  useEffect(() => {
    form
      .validateFields({ validateOnly: true })
      .then(() => setFormValid(true))
      .catch(() => setFormValid(false));
  }, [formValues, form]);

  useEffect(() => {
    if (vector) {
      form.setFieldsValue({
        Model: vector.Model,
        Dimensionality: vector.Dimensionality,
        Content: vector.Content,
        Vectors: vector.Vectors,
        NodeGUID: vector.NodeGUID,
        EdgeGUID: vector.EdgeGUID,
      });
      setUniqueKey(v4());
    } else {
      form.resetFields();
      setUniqueKey(v4());
    }
  }, [vector, form]);

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();

      // Parse vectors - handle both string and array cases
      const vectorsArray = Array.isArray(values.Vectors)
        ? values.Vectors
        : values.Vectors.split(',').map((num: string) => parseFloat(num.trim()));

      if (vector) {
        // Update existing vector
        const vectorToUpdate: VectorMetadata = {
          TenantGUID: vector.TenantGUID,
          GUID: vector.GUID,
          GraphGUID: selectedGraph,
          Model: values.Model,
          Dimensionality: Number(values.Dimensionality),
          Content: values.Content,
          Vectors: vectorsArray.map((v: number) => Number(v)),
          NodeGUID: values.NodeGUID === undefined ? null : values.NodeGUID,
          EdgeGUID: values.EdgeGUID === undefined ? null : values.EdgeGUID,
          CreatedUtc: vector.CreatedUtc,
          LastUpdateUtc: new Date().toISOString(),
        };

        const res = await updateVectorById(vectorToUpdate);

        if (res) {
          toast.success('Vector updated successfully');
          setIsAddEditVectorVisible(false);
          form.resetFields();
          onVectorUpdated && (await onVectorUpdated());
        } else {
          throw new Error('Failed to update vector - no response received');
        }
      } else {
        // Create new vector
        const newVector: VectorCreateRequest = {
          GraphGUID: selectedGraph,
          Model: values.Model,
          Dimensionality: Number(values.Dimensionality),
          Content: values.Content,
          Vectors: vectorsArray.map((v: number) => Number(v)),
          NodeGUID: values.NodeGUID,
          EdgeGUID: values.EdgeGUID,
        };

        const res = await createVectors(newVector);
        if (res) {
          toast.success('Vector created successfully');
          setIsAddEditVectorVisible(false);
          form.resetFields();
          onVectorUpdated && (await onVectorUpdated());
        } else {
          throw new Error('Failed to create vector - no response received');
        }
      }
    } catch (error: unknown) {
      console.error('Failed to submit:', error);
      const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
      toast.error(`Failed to update vector: ${errorMessage}`);
    }
  };

  return (
    <LitegraphModal
      title={vector ? 'Edit Vector' : 'Create Vector'}
      open={isAddEditVectorVisible}
      onOk={handleSubmit}
      onCancel={() => {
        setIsAddEditVectorVisible(false);
        form.resetFields();
      }}
      confirmLoading={isCreateLoading || isUpdateLoading}
      okButtonProps={{ disabled: !formValid }}
      data-testid="add-edit-vector-modal"
    >
      <Form
        form={form}
        layout="vertical"
        initialValues={{
          Vectors: [0.1, 0.2, 0.3],
        }}
        onValuesChange={(_, allValues) => setFormValues(allValues)}
      >
        <LitegraphFormItem
          label="Model"
          name="Model"
          rules={[{ required: true, message: 'Please input Model!' }]}
        >
          <LitegraphInput placeholder="Enter Model" />
        </LitegraphFormItem>
        <LitegraphFormItem
          label="Dimensionality"
          name="Dimensionality"
          rules={[{ required: true, message: 'Please input Dimensionality!' }]}
        >
          <LitegraphInput type="number" placeholder="Enter Dimensionality" />
        </LitegraphFormItem>
        <LitegraphFormItem
          label="Content"
          name="Content"
          rules={[{ required: true, message: 'Please input Content!' }]}
        >
          <LitegraphInput placeholder="Enter Content" />
        </LitegraphFormItem>
        <LitegraphFormItem
          label="Vectors"
          name="Vectors"
          rules={[{ required: true, message: 'Please input Vectors!' }]}
        >
          <JsonEditor
            key={uniqueKey}
            value={form.getFieldValue('Vectors') || []}
            onChange={(json: any) => {
              form.setFieldsValue({ Vectors: json });
            }}
            mode="code"
            enableSort={false}
            enableTransform={false}
            data-testid="graph-data-input"
          />
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

export default AddEditVector;
