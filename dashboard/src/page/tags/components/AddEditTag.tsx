'use client';
import { useEffect, useState } from 'react';
import { useTranslations } from 'next-intl';
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
  const t = useTranslations('tags');
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
          toast.success(t('toast.updated'));
          setIsAddEditTagVisible(false);
          form.resetFields();
          onTagUpdated && (await onTagUpdated());
        } else {
          toast.error(t('toast.updateFailedNoResponse'));
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
          toast.success(t('toast.created'));
          setIsAddEditTagVisible(false);
          form.resetFields();
          onTagUpdated && (await onTagUpdated());
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
      title={tag ? t('modalTitle.edit') : t('modalTitle.create')}
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
          label={t('form.key')}
          name="Key"
          tooltip={t('form.keyTooltip')}
          rules={[{ required: true, message: t('form.keyRequired') }]}
        >
          <LitegraphInput placeholder={t('form.keyPlaceholder')} />
        </LitegraphFormItem>

        <LitegraphFormItem
          label={t('form.value')}
          name="Value"
          tooltip={t('form.valueTooltip')}
          rules={[{ required: true, message: t('form.valueRequired') }]}
        >
          <LitegraphInput placeholder={t('form.valuePlaceholder')} />
        </LitegraphFormItem>
        <NodeSelector name="NodeGUID" label={t('form.node')} tooltip={t('form.nodeTooltip')} />

        <EdgeSelector name="EdgeGUID" label={t('form.edge')} tooltip={t('form.edgeTooltip')} />
      </Form>
    </LitegraphModal>
  );
};

export default AddEditTag;
