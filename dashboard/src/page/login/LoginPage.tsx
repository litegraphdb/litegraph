'use client';
import { useEffect, useRef, useState } from 'react';
import { useTranslations } from 'next-intl';
import { Form, Input, InputRef } from 'antd';
import styles from './login.module.scss';
import LitegraphInput from '@/components/base/input/Input';
import LitegraphSelect from '@/components/base/select/Select';
import LitegraphButton from '@/components/base/button/Button';
import LitegraphText from '@/components/base/typograpghy/Text';
import { LightGraphTheme } from '@/theme/theme';
import {
  setAccessKey,
  setEndpoint,
  useGetTenants,
  useValidateConnectivity,
} from '@/lib/sdk/litegraph.service';
import { TenantMetaData, Token } from 'litegraphdb/dist/types/types';
import toast from 'react-hot-toast';
import {
  useAdminCredentialsToLogin,
  useCredentialsToLogin,
} from '@/hooks/authHooks';
import { localStorageKeys } from '@/constants/constant';
import LitegraphFlex from '@/components/base/flex/Flex';
import { useGenerateTokenMutation, useGetTenantsForEmailMutation } from '@/lib/store/slice/slice';
import LoginLayout from '@/components/layout/LoginLayout';
import { useCurrentlyHostedDomainAsServerUrl } from '@/hooks/appHooks';
import { useAppDispatch } from '@/lib/store/hooks';
import { storeTenant, storeUser } from '@/lib/store/litegraph/actions';
import { FlaggedUser } from '@/types/types';

interface LoginFormData {
  url: string;
  email: string;
  tenant: string;
  username: string;
  password: string;
}

const LoginPage = () => {
  const t = useTranslations('login');
  const urlInputRef = useRef<InputRef | null>(null);
  const emailInputRef = useRef<InputRef | null>(null);
  const passwordInputRef = useRef<InputRef | null>(null);
  const [currentStep, setCurrentStep] = useState<number>(0);
  const [formData, setFormData] = useState<Partial<LoginFormData>>({});
  const [isServerValid, setIsServerValid] = useState<boolean>(false);
  const [form] = Form.useForm();
  const dispatch = useAppDispatch();
  const [generateToken, { isLoading: isGeneratingToken }] = useGenerateTokenMutation();
  const loginWithCredentials = useCredentialsToLogin();
  const loginWithAdminCredentials = useAdminCredentialsToLogin();
  const [getTenantsForEmail, { isLoading: isLoadingTenant }] = useGetTenantsForEmailMutation();
  const [tenants, setTenants] = useState<TenantMetaData[]>([]);
  const [showTenantSelect, setShowTenantSelect] = useState<boolean>(false);
  const { validateConnectivity, isLoading: isValidatingConnectivity } = useValidateConnectivity();
  const serverUrl = useCurrentlyHostedDomainAsServerUrl();

  // Break-glass ("advanced") admin bearer token affordance — clearly separated
  // from the primary email/password flow and never required.
  const [showAdvanced, setShowAdvanced] = useState<boolean>(false);
  const [accessKey, setAccessKeyValue] = useState<string>('');
  const { getTenants, isLoading: isValidatingKey } = useGetTenants();

  const tenantOptions =
    tenants?.map((tenant) => ({
      label: tenant.Name,
      value: tenant.GUID,
    })) || [];

  const stepFields: Record<number, string[]> = {
    0: ['url'],
    1: ['email'],
    2: ['tenant'],
  };

  const handleNext = async () => {
    try {
      // Validate only the current step's field; later-step fields are not yet filled.
      const values = await form.validateFields(stepFields[currentStep] || []);
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
      const values = await form.validateFields(['password']);
      const finalData: LoginFormData = { ...formData, ...values };

      const selectedTenant = tenants?.find((item) => item.GUID === finalData.tenant);
      if (!selectedTenant) {
        toast.error(t('tenantNotFound'));
        return;
      }
      const { data: token, error } = await generateToken({
        email: finalData.email,
        password: finalData.password,
        tenantId: finalData.tenant,
      });
      if (error || !token) {
        toast.error(t('invalidCredentials'));
        return;
      }
      if (token && selectedTenant) {
        // Session carries the user (with capability flags), the active tenant,
        // and the token. Flags drive what renders next.
        const sessionUser = (token as Token).User as FlaggedUser | undefined;
        localStorage.setItem(localStorageKeys.token, JSON.stringify(token));
        localStorage.setItem(localStorageKeys.tenant, JSON.stringify(selectedTenant));
        localStorage.setItem(localStorageKeys.serverUrl, finalData.url);
        if (sessionUser) {
          localStorage.setItem(localStorageKeys.user, JSON.stringify(sessionUser));
          dispatch(storeUser(sessionUser));
        }
        loginWithCredentials(token, selectedTenant);
      }
    } catch (error) {
      console.error('Validation failed:', error);
    }
  };

  const handleBreakGlassLogin = async () => {
    const url = form.getFieldValue('url');
    if (!url) {
      toast.error(t('serverUrlRequired'));
      return;
    }
    if (!accessKey) {
      toast.error(t('advancedTokenRequired'));
      return;
    }
    setEndpoint(url);
    setAccessKey(accessKey);
    try {
      const result = await getTenants();
      if (result) {
        localStorage.setItem(localStorageKeys.adminAccessKey, accessKey);
        localStorage.setItem(localStorageKeys.serverUrl, url);
        if (result[0]) {
          localStorage.setItem(localStorageKeys.tenant, JSON.stringify(result[0]));
          dispatch(storeTenant(result[0]));
        }
        loginWithAdminCredentials(accessKey);
      } else {
        toast.error(t('advancedTokenInvalid'));
      }
    } catch (err) {
      toast.error(t('advancedTokenInvalid'));
    }
  };

  useEffect(() => {
    if (!serverUrl) return;
    form.setFieldValue('url', serverUrl);
  }, [serverUrl]);

  useEffect(() => {
    if (currentStep === 0 && urlInputRef.current) {
      urlInputRef.current.focus({ cursor: 'end' });
    }
  }, [currentStep]);

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
    <LoginLayout footer={<div className={styles.loginHelperText}>{t('defaultCredentials')}</div>}>
      <LitegraphFlex vertical gap={20}>
        <Form
          form={form}
          layout="vertical"
          initialValues={formData}
          onFinish={() => (currentStep === 3 ? handleSubmit() : handleNext())}
        >
          {/* Step 0: Server URL - always visible */}
          <Form.Item
            label={t('serverUrl')}
            name="url"
            rules={[
              { required: true, message: t('serverUrlRequired') },
              {
                validator: (_, value) => {
                  if (!value) return Promise.resolve();
                  try {
                    const parsedUrl = new URL(value);
                    if (!['http:', 'https:'].includes(parsedUrl.protocol)) {
                      return Promise.reject(t('onlyHttp'));
                    }
                    return Promise.resolve();
                  } catch (err) {
                    return Promise.reject(t('validUrl'));
                  }
                },
              },
            ]}
          >
            <LitegraphInput
              placeholder={t('serverUrlPlaceholder')}
              size="large"
              ref={urlInputRef}
              disabled={isValidatingConnectivity || currentStep > 0}
              data-testid="litegraph-input"
            />
          </Form.Item>

          {/* Step 1: Email - visible once server is validated */}
          <Form.Item
            label={t('email')}
            name="email"
            rules={
              currentStep >= 1
                ? [
                    { required: true, message: t('emailRequired') },
                    { type: 'email', message: t('validEmail') },
                  ]
                : []
            }
          >
            <LitegraphInput
              placeholder={t('emailPlaceholder')}
              size="large"
              ref={emailInputRef}
              disabled={currentStep < 1 || currentStep > 1 || isLoadingTenant}
            />
          </Form.Item>

          {/* Step 2: Tenant selection - visible only if multiple tenants */}
          {showTenantSelect && (
            <Form.Item
              name="tenant"
              label={t('tenant')}
              rules={currentStep >= 2 ? [{ required: true, message: t('tenantRequired') }] : []}
            >
              <LitegraphSelect
                loading={isLoadingTenant}
                disabled={currentStep < 2 || currentStep > 2}
                placeholder={t('selectTenant')}
                options={tenantOptions}
                size="large"
              />
            </Form.Item>
          )}

          {/* Step 3: Password - visible once tenant is determined */}
          <Form.Item
            label={t('password')}
            name="password"
            rules={currentStep >= 3 ? [{ required: true, message: t('passwordRequired') }] : []}
          >
            <Input.Password
              placeholder={t('passwordPlaceholder')}
              size="large"
              ref={passwordInputRef}
              disabled={currentStep < 3}
            />
          </Form.Item>

          <div className={styles.loginButtonContainer}>
            {currentStep > 0 && (
              <LitegraphButton className={styles.cancelButton} onClick={handleCancel}>
                {t('cancel')}
              </LitegraphButton>
            )}
            <LitegraphButton
              type="primary"
              htmlType="submit"
              loading={isGeneratingToken || isLoadingTenant || isValidatingConnectivity}
              className={styles.loginButton}
            >
              {isLoadingTenant || isValidatingConnectivity
                ? t('loading')
                : currentStep === 3
                  ? t('login')
                  : t('next')}
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

        {/* Advanced: break-glass admin bearer token (optional, never required). */}
        <div className={styles.advancedSection}>
          <LitegraphButton
            type="link"
            size="small"
            onClick={() => setShowAdvanced((prev) => !prev)}
            data-testid="advanced-toggle"
          >
            {showAdvanced ? t('advancedHide') : t('advancedToggle')}
          </LitegraphButton>
          {showAdvanced && (
            <LitegraphFlex vertical gap={8}>
              <LitegraphText fontSize={12} className="ant-color-text-secondary">
                {t('advancedHelp')}
              </LitegraphText>
              <Input.Password
                placeholder={t('advancedTokenPlaceholder')}
                value={accessKey}
                onChange={(e) => setAccessKeyValue(e.target.value)}
                size="large"
                data-testid="break-glass-input"
              />
              <LitegraphButton
                onClick={handleBreakGlassLogin}
                loading={isValidatingKey}
                data-testid="break-glass-login"
              >
                {t('advancedLogin')}
              </LitegraphButton>
            </LitegraphFlex>
          )}
        </div>
      </LitegraphFlex>
    </LoginLayout>
  );
};

export default LoginPage;
