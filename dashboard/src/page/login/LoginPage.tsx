'use client';
import { useEffect, useRef, useState } from 'react';
import { Form, Input, InputRef } from 'antd';
import styles from './login.module.scss';
import LitegraphInput from '@/components/base/input/Input';
import LitegraphSelect from '@/components/base/select/Select';
import LitegraphButton from '@/components/base/button/Button';
import { LightGraphTheme } from '@/theme/theme';
import { setEndpoint, useValidateConnectivity } from '@/lib/sdk/litegraph.service';
import { TenantMetaData } from 'litegraphdb/dist/types/types';
import toast from 'react-hot-toast';
import { useCredentialsToLogin } from '@/hooks/authHooks';
import { localStorageKeys } from '@/constants/constant';
import LitegraphFlex from '@/components/base/flex/Flex';
import { useGenerateTokenMutation, useGetTenantsForEmailMutation } from '@/lib/store/slice/slice';
import LoginLayout from '@/components/layout/LoginLayout';
import { useCurrentlyHostedDomainAsServerUrl } from '@/hooks/appHooks';

interface LoginFormData {
  url: string;
  email: string;
  tenant: string;
  username: string;
  password: string;
}

const LoginPage = () => {
  const emailInputRef = useRef<InputRef | null>(null);
  const passwordInputRef = useRef<InputRef | null>(null);
  const [currentStep, setCurrentStep] = useState<number>(0);
  const [formData, setFormData] = useState<Partial<LoginFormData>>({});
  const [isServerValid, setIsServerValid] = useState<boolean>(false);
  const [form] = Form.useForm();
  const [generateToken, { isLoading: isGeneratingToken }] = useGenerateTokenMutation();
  const loginWithCredentials = useCredentialsToLogin();
  const [getTenantsForEmail, { isLoading: isLoadingTenant }] = useGetTenantsForEmailMutation();
  const [tenants, setTenants] = useState<TenantMetaData[]>([]);
  const [showTenantSelect, setShowTenantSelect] = useState<boolean>(false);
  const { validateConnectivity, isLoading: isValidatingConnectivity } = useValidateConnectivity();
  const serverUrl = useCurrentlyHostedDomainAsServerUrl();

  const tenantOptions =
    tenants?.map((tenant) => ({
      label: tenant.Name,
      value: tenant.GUID,
    })) || [];

  const handleNext = async () => {
    try {
      const values = await form.validateFields();
      setFormData((prev) => ({ ...prev, ...values }));
      switch (currentStep) {
        case 0:
          setEndpoint(values.url);
          const isValid = await validateConnectivity();
          if (isValid) {
            setIsServerValid(true);
            setFormData((prev) => ({ ...prev, ...values }));
            setCurrentStep(1);
          }
          break;
        case 1:
          setFormData((prev) => ({ ...prev, ...values }));
          if (values.email) {
            setCurrentStep(1);
            getTenantsForEmail(values.email)
              .then(({ data: res = [] }) => {
                if (res) {
                  setTenants(res);
                  if (res && res.length > 1) {
                    setShowTenantSelect(true);
                    setCurrentStep(2);
                  } else if (res?.length === 1) {
                    setFormData((prev) => ({ ...prev, tenant: res[0].GUID }));
                    form.setFieldValue('tenant', res[0].GUID);
                    setShowTenantSelect(false);
                    setCurrentStep(3);
                  }
                } else {
                  setCurrentStep(1);
                }
              })
              .catch((err) => {
                setCurrentStep(1);
              });
          }
          break;
        case 2:
          setFormData((prev) => ({ ...prev, ...values }));
          setCurrentStep(3);
          break;

        default:
          break;
      }
    } catch (error) {
      console.error('Validation failed:', error);
    }
  };

  const handleCancel = () => {
    form.resetFields(['email', 'tenant', 'password']);
    setFormData({ url: form.getFieldValue('url') });
    setIsServerValid(false);
    setTenants([]);
    setShowTenantSelect(false);
    setCurrentStep(0);
  };

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      const finalData: LoginFormData = { ...formData, ...values };

      const selectedTenant = tenants?.find((t) => t.GUID === finalData.tenant);
      if (!selectedTenant) {
        toast.error('Tenant not found');
        return;
      }
      const { data: token } = await generateToken({
        email: finalData.email,
        password: finalData.password,
        tenantId: finalData.tenant,
      });
      if (token && selectedTenant) {
        localStorage.setItem(localStorageKeys.token, JSON.stringify(token));
        localStorage.setItem(localStorageKeys.tenant, JSON.stringify(selectedTenant));
        localStorage.setItem(localStorageKeys.serverUrl, finalData.url);
        loginWithCredentials(token, selectedTenant);
      }
    } catch (error) {
      console.error('Validation failed:', error);
    }
  };

  useEffect(() => {
    if (!serverUrl) return;
    form.setFieldValue('url', serverUrl);
  }, [serverUrl]);

  useEffect(() => {
    if (currentStep === 1 && emailInputRef.current) {
      emailInputRef.current.focus({ cursor: 'start' });
    }
  }, [currentStep]);

  useEffect(() => {
    if (currentStep === 3 && passwordInputRef.current) {
      passwordInputRef.current.focus();
    }
  }, [currentStep]);

  return (
    <LoginLayout>
      <LitegraphFlex vertical gap={20}>
        <Form form={form} layout="vertical" initialValues={formData}>
          {/* Step 0: Server URL - always visible */}
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
            <LitegraphInput
              placeholder="https://your-litegraph-server.com"
              size="large"
              disabled={isValidatingConnectivity || currentStep > 0}
              data-testid="litegraph-input"
            />
          </Form.Item>

          {/* Step 1: Email - visible once server is validated */}
          <Form.Item
            label="Email"
            name="email"
            rules={
              currentStep >= 1
                ? [
                    { required: true, message: 'Please input your email!' },
                    { type: 'email', message: 'Please enter a valid email!' },
                  ]
                : []
            }
          >
            <LitegraphInput
              placeholder="Email"
              size="large"
              ref={emailInputRef}
              disabled={currentStep < 1 || currentStep > 1 || isLoadingTenant}
            />
          </Form.Item>

          {/* Step 2: Tenant selection - visible only if multiple tenants */}
          {showTenantSelect && (
            <Form.Item
              name="tenant"
              label="Tenant"
              rules={
                currentStep >= 2 ? [{ required: true, message: 'Please select a tenant!' }] : []
              }
            >
              <LitegraphSelect
                loading={isLoadingTenant}
                disabled={currentStep < 2 || currentStep > 2}
                placeholder="Select tenant"
                options={tenantOptions}
                size="large"
              />
            </Form.Item>
          )}

          {/* Step 3: Password - visible once tenant is determined */}
          <Form.Item
            label="Password"
            name="password"
            rules={
              currentStep >= 3 ? [{ required: true, message: 'Please input your password!' }] : []
            }
          >
            <Input.Password
              placeholder="Password"
              size="large"
              ref={passwordInputRef}
              disabled={currentStep < 3}
            />
          </Form.Item>

          <div className={styles.loginButtonContainer}>
            {currentStep > 0 && (
              <LitegraphButton className={styles.cancelButton} onClick={handleCancel}>
                Cancel
              </LitegraphButton>
            )}
            <LitegraphButton
              type="primary"
              htmlType={'submit'}
              loading={isGeneratingToken || isLoadingTenant || isValidatingConnectivity}
              className={styles.loginButton}
              onClick={currentStep === 3 ? handleSubmit : handleNext}
            >
              {isLoadingTenant || isValidatingConnectivity
                ? 'Loading...'
                : currentStep === 3
                  ? 'Login'
                  : 'Next'}
            </LitegraphButton>
          </div>
        </Form>
        <div className={styles.stepIndicatorContainer}>
          {[0, 1, 2, 3].map((step) => (
            <div
              key={step}
              className={styles.stepIndicator}
              style={{
                backgroundColor: currentStep >= step ? LightGraphTheme.primary : '#d9d9d9',
              }}
            />
          ))}
        </div>
      </LitegraphFlex>
    </LoginLayout>
  );
};

export default LoginPage;
