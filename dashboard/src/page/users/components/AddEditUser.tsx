'use client';
import { useEffect, useState } from 'react';
import { Form, Switch, Input } from 'antd';
import LitegraphModal from '@/components/base/modal/Modal';
import LitegraphFormItem from '@/components/base/form/FormItem';
import LitegraphInput from '@/components/base/input/Input';
import toast from 'react-hot-toast';
import { useCreateUserMutation, useUpdateUserMutation } from '@/lib/store/slice/slice';
import { UserMetadata, UserMetadataCreateRequest } from 'litegraphdb/dist/types/types';

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
  const [form] = Form.useForm();
  const [formValid, setFormValid] = useState(false);
  const [createUser, { isLoading: isCreateLoading }] = useCreateUserMutation();
  const [updateUserById, { isLoading: isUpdateLoading }] = useUpdateUserMutation();

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
      });
    } else {
      form.resetFields();
      form.setFieldsValue({ Active: true });
    }
  }, [user, form]);

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      if (user) {
        // Update existing user
        const updatedUser: UserMetadata = {
          GUID: user.GUID,
          FirstName: values.FirstName,
          LastName: values.LastName,
          Email: values.Email,
          Password: values.Password,
          Active: values.Active,
          CreatedUtc: user.CreatedUtc,
          LastUpdateUtc: new Date().toISOString(),
        };

        const res = await updateUserById(updatedUser);

        if (res) {
          toast.success('User updated successfully');
          setIsAddEditUserVisible(false);
          form.resetFields();
          onUserUpdated && onUserUpdated();
        } else {
          toast.error('Failed to update user - no response received');
        }
      } else {
        // Create new user
        const newUser: UserMetadataCreateRequest = {
          FirstName: values.FirstName,
          LastName: values.LastName,
          Email: values.Email,
          Password: values.Password,
          Active: values.Active,
        };
        const res = await createUser(newUser);
        if (res) {
          toast.success('User created successfully');
          setIsAddEditUserVisible(false);
          form.resetFields();
          onUserUpdated && onUserUpdated();
        }
      }
    } catch (error: unknown) {
      console.error('Failed to submit:', error);
      const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
      toast.error(`Failed to update user: ${errorMessage}`);
    }
  };

  return (
    <LitegraphModal
      title={user ? 'Edit User' : 'Create User'}
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
          label="First Name"
          name="FirstName"
          rules={[{ required: true, message: 'Please input First Name!' }]}
        >
          <LitegraphInput placeholder="Enter First Name" />
        </LitegraphFormItem>

        <LitegraphFormItem
          label="Last Name"
          name="LastName"
          rules={[{ required: true, message: 'Please input Last Name!' }]}
        >
          <LitegraphInput placeholder="Enter Last Name" />
        </LitegraphFormItem>

        <LitegraphFormItem
          label="Email"
          name="Email"
          rules={[
            { required: true, message: 'Please input your email!' },
            { type: 'email', message: 'Please enter a valid email!' },
          ]}
        >
          <LitegraphInput placeholder="Email" size="large" autoComplete="off" />
        </LitegraphFormItem>

        <LitegraphFormItem
          label="Password"
          name="Password"
          rules={[{ required: true, message: 'Please input Password!' }]}
        >
          <Input.Password placeholder="Enter password" autoComplete="new-password" />
        </LitegraphFormItem>

        <LitegraphFormItem label="Active" name="Active" valuePropName="checked">
          <Switch data-testid="active-switch" />
        </LitegraphFormItem>
      </Form>
    </LitegraphModal>
  );
};

export default AddEditUser;
