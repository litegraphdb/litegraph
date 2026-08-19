'use client';
import { useEffect, useState } from 'react';
import { useTranslations } from 'next-intl';
import { Form, Switch, Select } from 'antd';
import LitegraphModal from '@/components/base/modal/Modal';
import LitegraphFormItem from '@/components/base/form/FormItem';
import LitegraphInput from '@/components/base/input/Input';
import { useAppDispatch } from '@/lib/store/hooks';
import toast from 'react-hot-toast';
import { useCreateTenantMutation, useUpdateTenantMutation } from '@/lib/store/slice/slice';
import { TenantMetaData, TenantMetaDataCreateRequest } from 'litegraphdb/dist/types/types';

interface AddEditTenantProps {
  isAddEditTenantVisible: boolean;
  setIsAddEditTenantVisible: (visible: boolean) => void;
  tenant: TenantMetaData | null;
  onTenantUpdated?: () => Promise<void>;
}

const AddEditTenant = ({
  isAddEditTenantVisible,
  setIsAddEditTenantVisible,
  tenant,
  onTenantUpdated,
}: AddEditTenantProps) => {
  const t = useTranslations('tenants');
  const dispatch = useAppDispatch();
  const [form] = Form.useForm();
  const [formValid, setFormValid] = useState(false);
  const [createTenants, { isLoading: isCreateLoading }] = useCreateTenantMutation();
  const [updateTenantById, { isLoading: isUpdateLoading }] = useUpdateTenantMutation();

  // Add form validation watcher
  const [formValues, setFormValues] = useState({});
  useEffect(() => {
    form
      .validateFields({ validateOnly: true })
      .then(() => setFormValid(true))
      .catch(() => setFormValid(false));
  }, [formValues, form]);

  useEffect(() => {
    if (tenant) {
      form.setFieldsValue({
        Name: tenant.Name,
        Active: tenant.Active,
      });
    } else {
      form.resetFields();
      form.setFieldsValue({ Active: true });
    }
  }, [tenant, form]);

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      if (tenant) {
        // Update existing tenant
        const updatedTenant: TenantMetaData = {
          GUID: tenant.GUID,
          Name: values.Name,
          Active: values.Active,
          CreatedUtc: tenant.CreatedUtc,
          LastUpdateUtc: new Date().toISOString(),
        };

        const res = await updateTenantById(updatedTenant);

        if (res) {
          toast.success(t('toast.updated'));
          setIsAddEditTenantVisible(false);
          form.resetFields();
          onTenantUpdated && onTenantUpdated();
        } else {
          toast.error(t('toast.updateNoResponse'));
        }
      } else {
        // Create new tenant
        const newTenant: TenantMetaDataCreateRequest = {
          Name: values.Name,
          Active: values.Active,
        };
        const res = await createTenants(newTenant);
        if (res) {
          toast.success(t('toast.created'));
          setIsAddEditTenantVisible(false);
          form.resetFields();
          onTenantUpdated && onTenantUpdated();
        }
      }
    } catch (error: unknown) {}
  };

  return (
    <LitegraphModal
      title={tenant ? t('modalTitle.edit') : t('modalTitle.create')}
      open={isAddEditTenantVisible}
      onOk={handleSubmit}
      onCancel={() => {
        setIsAddEditTenantVisible(false);
        form.resetFields();
      }}
      confirmLoading={isCreateLoading || isUpdateLoading}
      okButtonProps={{ disabled: !formValid }}
    >
      <Form
        form={form}
        layout="vertical"
        onValuesChange={(_, allValues) => setFormValues(allValues)}
      >
        <LitegraphFormItem
          label={t('form.name')}
          name="Name"
          tooltip={t('form.nameTooltip')}
          rules={[{ required: true, message: t('form.nameRequired') }]}
        >
          <LitegraphInput placeholder={t('form.namePlaceholder')} />
        </LitegraphFormItem>

        <LitegraphFormItem
          label={t('form.active')}
          name="Active"
          tooltip={t('form.activeTooltip')}
          valuePropName="checked"
        >
          <Switch />
        </LitegraphFormItem>
      </Form>
    </LitegraphModal>
  );
};

export default AddEditTenant;
