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
  const [currentStep, setCurrentStep] = useState<number>(0);
  const [formData, setFormData] = useState<Partial<LoginFormData>>({});
  const [isServerValid, setIsServerValid] = useState<boolean>(false);
  const [form] = Form.useForm();
  const [generateToken, { isLoading: isGeneratingToken }] = useGenerateTokenMutation();
  const loginWithCredentials = useCredentialsToLogin();
  const [getTenantsForEmail, { isLoading: isLoadingTenant }] = useGetTenantsForEmailMutation();
  const [tenants, setTenants] = useState<TenantMetaData[]>([]);
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
                    setCurrentStep(2);
                  } else if (res?.length === 1) {
                    setFormData((prev) => ({ ...prev, tenant: res[0].GUID }));
                    form.setFieldValue('tenant', res[0].GUID);
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

  const handleBack = () => {
    setCurrentStep((prev) => prev - 1);
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

  const renderStep = () => {
    switch (currentStep) {
      case 0:
        return (
          <>
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
                disabled={isValidatingConnectivity}
                data-testid="litegraph-input"
              />
            </Form.Item>
          </>
        );
      case 1:
        return (
          <Form.Item
            label="Email"
            name="email"
            rules={[
              { required: true, message: 'Please input your email!' },
              { type: 'email', message: 'Please enter a valid email!' },
            ]}
          >
            <LitegraphInput
              placeholder="Email"
              size="large"
              ref={emailInputRef}
              disabled={!isServerValid}
              autoFocus
            />
          </Form.Item>
        );

      case 2:
        return (
          <Form.Item
            name="tenant"
            label="Tenants"
            rules={[{ required: true, message: 'Please select a tenant!' }]}
          >
            <LitegraphSelect
              loading={isLoadingTenant}
              disabled={isLoadingTenant}
              placeholder="Select tenant"
              options={tenantOptions}
              size="large"
              autoFocus
            />
          </Form.Item>
        );
      case 3:
        return (
          <>
            <Form.Item
              name="password"
              rules={[{ required: true, message: 'Please input your password!' }]}
            >
              <Input.Password placeholder="Password" size="large" autoFocus />
            </Form.Item>
          </>
        );
      default:
        return null;
    }
  };

  useEffect(() => {
    if (!serverUrl) return;
    form.setFieldValue('url', serverUrl);
  }, [serverUrl]);

  useEffect(() => {
    if (emailInputRef.current) {
      emailInputRef.current.focus({
        cursor: 'start',
      });
    }
  }, [emailInputRef.current]);

  return (
    <LoginLayout>
      <LitegraphFlex vertical gap={20}>
        <Form form={form} layout="vertical" initialValues={formData}>
          {renderStep()}

          <div className={styles.loginButtonContainer}>
            {currentStep > 0 && (
              <LitegraphButton className={styles.backButton} onClick={handleBack}>
                Back
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
                backgroundColor: currentStep === step ? LightGraphTheme.primary : '#d9d9d9',
              }}
            />
          ))}
        </div>
      </LitegraphFlex>
    </LoginLayout>
  );
};

export default LoginPage;
