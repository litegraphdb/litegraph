'use client';
import React, { useEffect, useState } from 'react';
import { Form, Input } from 'antd';
import { localStorageKeys } from '@/constants/constant';
import toast from 'react-hot-toast';
import {
  setAccessKey,
  setEndpoint,
  useGetTenants,
  useValidateConnectivity,
} from '@/lib/sdk/litegraph.service';
import { useAdminCredentialsToLogin } from '@/hooks/authHooks';
import { useAppDispatch } from '@/lib/store/hooks';
import { storeTenant } from '@/lib/store/litegraph/actions';
import LitegraphButton from '@/components/base/button/Button';
import LoginLayout from '@/components/layout/LoginLayout';
import { useCurrentlyHostedDomainAsServerUrl } from '@/hooks/appHooks';

interface AdminLoginFormData {
  url: string;
  accessKey: string;
}

const AdminLoginPage = () => {
  const dispatch = useAppDispatch();
  const [form] = Form.useForm();
  const [isServerValid, setIsServerValid] = useState(false);
  const { getTenants, isLoading } = useGetTenants();
  const loginWithAdminCredentials = useAdminCredentialsToLogin();
  const { validateConnectivity, isLoading: isValidating } = useValidateConnectivity();
  const serverUrl = useCurrentlyHostedDomainAsServerUrl();

  const handleValidateServerUrl = async () => {
    const url = form.getFieldValue('url');
    if (!url) return;
    setEndpoint(url);
    const isValid = await validateConnectivity();
    setIsServerValid(!!isValid);
  };

  const handleSubmit = async (values: AdminLoginFormData) => {
    try {
      setAccessKey(values.accessKey);
      const data = await getTenants('Login failed. Please try again.');
      if (data) {
        localStorage.setItem(localStorageKeys.adminAccessKey, values.accessKey);
        localStorage.setItem(localStorageKeys.serverUrl, values.url);
        loginWithAdminCredentials(values.accessKey);
        dispatch(storeTenant(data[0]));
      }
    } catch (error) {
      toast.error('Login failed. Please try again.');
    }
  };
  useEffect(() => {
    if (!serverUrl) return;
    form.setFieldValue('url', serverUrl);
    handleValidateServerUrl();
  }, [serverUrl]);

  return (
    <LoginLayout isAdmin>
      <Form
        form={form}
        layout="vertical"
        onFinish={handleSubmit}
        autoComplete="off"
        onKeyPress={(e) => {
          if (e.key === 'Enter') {
            form.submit();
          }
        }}
      >
        <Form.Item
          label="LiteGraph Server URL"
          name="url"
          rules={[
            { required: true, message: 'Please enter the LiteGraph Server URL!' },
            {
              validator: (_, value) => {
                if (!value) return Promise.resolve();
                try {
                  const parsedUrl = new URL(value);
                  if (!['http:', 'https:'].includes(parsedUrl.protocol)) {
                    return Promise.reject('Only HTTP or HTTPS URLs are allowed!');
                  }
                  return Promise.resolve();
                } catch (err) {
                  return Promise.reject('Please enter a valid URL!');
                }
              },
            },
          ]}
        >
          <Input
            placeholder="https://your-litegraph-server.com"
            onBlur={handleValidateServerUrl}
            disabled={isValidating}
          />
        </Form.Item>

        <Form.Item
          label="Access Key"
          name="accessKey"
          rules={[{ required: true, message: 'Please input your access key!' }]}
        >
          <Input.Password
            key={isServerValid ? 'enabled' : 'disabled'}
            placeholder="Enter your access key"
            onPressEnter={() => form.submit()}
            disabled={!isServerValid}
            autoFocus
          />
        </Form.Item>

        <Form.Item>
          <LitegraphButton
            type="primary"
            htmlType="submit"
            className="w-100"
            loading={isLoading || isValidating}
            disabled={isLoading || isValidating}
          >
            Login
          </LitegraphButton>
        </Form.Item>
      </Form>
    </LoginLayout>
  );
};

export default AdminLoginPage;
