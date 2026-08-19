'use client';
import { useEffect, useState } from 'react';
import { useTranslations } from 'next-intl';
import { Form } from 'antd';
import { VectorType } from '@/types/types';
import LitegraphModal from '@/components/base/modal/Modal';
import LitegraphFormItem from '@/components/base/form/FormItem';
import LitegraphInput from '@/components/base/input/Input';
import toast from 'react-hot-toast';
import { v4 } from 'uuid';
import JsonEditorWithAce from '@/components/inputs/json-editor/JsonEditorWithAce';
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
  const t = useTranslations('vectors');
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
          toast.success(t('toast.updated'));
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
          toast.success(t('toast.created'));
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
      toast.error(t('toast.updateFailed', { error: errorMessage }));
    }
  };

  return (
    <LitegraphModal
      title={vector ? t('modalTitle.edit') : t('modalTitle.create')}
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
          label={t('form.model')}
          name="Model"
          tooltip={t('form.modelTooltip')}
          rules={[{ required: true, message: t('form.modelRequired') }]}
        >
          <LitegraphInput placeholder={t('form.modelPlaceholder')} />
        </LitegraphFormItem>
        <LitegraphFormItem
          label={t('form.dimensionality')}
          name="Dimensionality"
          tooltip={t('form.dimensionalityTooltip')}
          rules={[{ required: true, message: t('form.dimensionalityRequired') }]}
        >
          <LitegraphInput type="number" placeholder={t('form.dimensionalityPlaceholder')} />
        </LitegraphFormItem>
        <LitegraphFormItem
          label={t('form.content')}
          name="Content"
          tooltip={t('form.contentTooltip')}
          rules={[{ required: true, message: t('form.contentRequired') }]}
        >
          <LitegraphInput placeholder={t('form.contentPlaceholder')} />
        </LitegraphFormItem>
        <LitegraphFormItem
          label={t('form.vectors')}
          name="Vectors"
          tooltip={t('form.vectorsTooltip')}
          rules={[{ required: true, message: t('form.vectorsRequired') }]}
        >
          <JsonEditorWithAce
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
        <NodeSelector name="NodeGUID" label={t('form.node')} tooltip={t('form.nodeTooltip')} />

        <EdgeSelector name="EdgeGUID" label={t('form.edge')} tooltip={t('form.edgeTooltip')} />
      </Form>
    </LitegraphModal>
  );
};

export default AddEditVector;
