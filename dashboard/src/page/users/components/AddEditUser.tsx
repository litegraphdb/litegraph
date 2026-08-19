'use client';
import { useEffect, useState } from 'react';
import { useTranslations } from 'next-intl';
import { Form, Switch, Input } from 'antd';
import LitegraphModal from '@/components/base/modal/Modal';
import LitegraphFormItem from '@/components/base/form/FormItem';
import LitegraphInput from '@/components/base/input/Input';
import toast from 'react-hot-toast';
import { useCreateUserMutation, useUpdateUserMutation } from '@/lib/store/slice/slice';
import { UserMetadata } from 'litegraphdb/dist/types/types';
import { FlaggedUser, FlaggedUserWriteRequest } from '@/types/types';
import { usePrincipal } from '@/hooks/permissionHooks';

interface AddEditUserProps {
  isAddEditUserVisible: boolean;
  setIsAddEditUserVisible: (visible: boolean) => void;
  user: UserMetadata | null;
  onUserUpdated?: () => Promise<void>;
}

const AddEditUser = ({
  isAddEditUserVisible,
  setIsAddEditUserVisible,
  user,
  onUserUpdated,
}: AddEditUserProps) => {
  const t = useTranslations('users');
  const [form] = Form.useForm();
  const [formValid, setFormValid] = useState(false);
  const [createUser, { isLoading: isCreateLoading }] = useCreateUserMutation();
  const [updateUserById, { isLoading: isUpdateLoading }] = useUpdateUserMutation();
  const principal = usePrincipal();
  // Capability flags are only settable by a principal permitted to grant them.
  const canSetSystemAdmin = principal.isSystemAdmin || principal.isBreakGlass;
  const canSetTenantAdmin = canSetSystemAdmin || principal.isTenantAdmin;
  const flaggedUser = user as FlaggedUser | null;

  // Add form validation watcher
  const [formValues, setFormValues] = useState({});
  useEffect(() => {
    form
      .validateFields({ validateOnly: true })
      .then(() => setFormValid(true))
      .catch(() => setFormValid(false));
  }, [formValues, form]);

  useEffect(() => {
    if (user) {
      form.setFieldsValue({
        FirstName: user.FirstName,
        LastName: user.LastName,
        Email: user.Email,
        Password: user.Password,
        Active: user.Active,
        IsSystemAdmin: Boolean(flaggedUser?.IsSystemAdmin),
        IsTenantAdmin: Boolean(flaggedUser?.IsTenantAdmin),
      });
    } else {
      form.resetFields();
      form.setFieldsValue({ Active: true, IsSystemAdmin: false, IsTenantAdmin: false });
    }
  }, [user, form, flaggedUser]);

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      if (user) {
        // Update existing user
        const updatedUser: FlaggedUser = {
          GUID: user.GUID,
          FirstName: values.FirstName,
          LastName: values.LastName,
          Email: values.Email,
          Password: values.Password,
          Active: values.Active,
          CreatedUtc: user.CreatedUtc,
          LastUpdateUtc: new Date().toISOString(),
          IsSystemAdmin: canSetSystemAdmin
            ? Boolean(values.IsSystemAdmin)
            : Boolean(flaggedUser?.IsSystemAdmin),
          IsTenantAdmin: canSetTenantAdmin
            ? Boolean(values.IsTenantAdmin)
            : Boolean(flaggedUser?.IsTenantAdmin),
        };

        const res = await updateUserById(updatedUser as UserMetadata);

        if (res) {
          toast.success(t('toast.updated'));
          setIsAddEditUserVisible(false);
          form.resetFields();
          onUserUpdated && onUserUpdated();
        } else {
          toast.error(t('toast.updateNoResponse'));
        }
      } else {
        // Create new user
        const newUser: FlaggedUserWriteRequest = {
          FirstName: values.FirstName,
          LastName: values.LastName,
          Email: values.Email,
          Password: values.Password,
          Active: values.Active,
          IsSystemAdmin: canSetSystemAdmin ? Boolean(values.IsSystemAdmin) : false,
          IsTenantAdmin: canSetTenantAdmin ? Boolean(values.IsTenantAdmin) : false,
        };
        const res = await createUser(newUser as any);
        if (res) {
          toast.success(t('toast.created'));
          setIsAddEditUserVisible(false);
          form.resetFields();
          onUserUpdated && onUserUpdated();
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
      title={user ? t('modalTitle.edit') : t('modalTitle.create')}
      open={isAddEditUserVisible}
      onOk={handleSubmit}
      onCancel={() => {
        setIsAddEditUserVisible(false);
        form.resetFields();
      }}
      confirmLoading={isCreateLoading || isUpdateLoading}
      okButtonProps={{ disabled: !formValid }}
      data-testid="add-edit-user-modal"
    >
      <Form
        form={form}
        layout="vertical"
        onValuesChange={(_, allValues) => setFormValues(allValues)}
      >
        <LitegraphFormItem
          label={t('form.firstName')}
          name="FirstName"
          tooltip={t('form.firstNameTooltip')}
          rules={[{ required: true, message: t('form.firstNameRequired') }]}
        >
          <LitegraphInput placeholder={t('form.firstNamePlaceholder')} />
        </LitegraphFormItem>

        <LitegraphFormItem
          label={t('form.lastName')}
          name="LastName"
          tooltip={t('form.lastNameTooltip')}
          rules={[{ required: true, message: t('form.lastNameRequired') }]}
        >
          <LitegraphInput placeholder={t('form.lastNamePlaceholder')} />
        </LitegraphFormItem>

        <LitegraphFormItem
          label={t('form.email')}
          name="Email"
          tooltip={t('form.emailTooltip')}
          rules={[
            { required: true, message: t('form.emailRequired') },
            { type: 'email', message: t('form.emailInvalid') },
          ]}
        >
          <LitegraphInput placeholder={t('form.emailPlaceholder')} size="large" autoComplete="off" />
        </LitegraphFormItem>

        <LitegraphFormItem
          label={t('form.password')}
          name="Password"
          tooltip={t('form.passwordTooltip')}
          rules={[{ required: true, message: t('form.passwordRequired') }]}
        >
          <Input.Password placeholder={t('form.passwordPlaceholder')} autoComplete="new-password" />
        </LitegraphFormItem>

        <LitegraphFormItem
          label={t('form.active')}
          name="Active"
          tooltip={t('form.activeTooltip')}
          valuePropName="checked"
        >
          <Switch data-testid="active-switch" />
        </LitegraphFormItem>

        {canSetTenantAdmin && (
          <LitegraphFormItem
            label={t('form.tenantAdmin')}
            name="IsTenantAdmin"
            tooltip={t('form.tenantAdminTooltip')}
            valuePropName="checked"
          >
            <Switch data-testid="tenant-admin-switch" />
          </LitegraphFormItem>
        )}

        {canSetSystemAdmin && (
          <LitegraphFormItem
            label={t('form.systemAdmin')}
            name="IsSystemAdmin"
            tooltip={t('form.systemAdminTooltip')}
            valuePropName="checked"
          >
            <Switch data-testid="system-admin-switch" />
          </LitegraphFormItem>
        )}
      </Form>
    </LitegraphModal>
  );
};

export default AddEditUser;
