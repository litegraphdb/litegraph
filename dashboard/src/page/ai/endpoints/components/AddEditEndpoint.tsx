'use client';
import React, { useEffect, useState } from 'react';
import { useTranslations } from 'next-intl';
import { Divider, Form, InputNumber, Switch } from 'antd';
import toast from 'react-hot-toast';
import LitegraphModal from '@/components/base/modal/Modal';
import LitegraphFormItem from '@/components/base/form/FormItem';
import LitegraphInput from '@/components/base/input/Input';
import LitegraphSelect from '@/components/base/select/Select';
import LitegraphText from '@/components/base/typograpghy/Text';
import {
  ChatEndpoint,
  ChatEndpointType,
  ChatProviderType,
  PROVIDER_BASE_URL_EXAMPLES,
  isRedactedApiKey,
  providersForType,
  validateProviderTypeCombo,
} from '@/lib/sdk/chat';
import {
  useCreateChatEndpointMutation,
  useUpdateChatEndpointMutation,
} from '@/lib/store/slice/slice';
import { globalToastId } from '@/constants/config';
import { validateEndpointUrlForProvider } from '../validation';

interface AddEditEndpointProps {
  tenantGuid: string;
  endpoint: ChatEndpoint | null;
  onClose: () => void;
}

/** Create/edit modal for a chat endpoint with grouped fields and redacted-key handling. */
const AddEditEndpoint = ({ tenantGuid, endpoint, onClose }: AddEditEndpointProps) => {
  const t = useTranslations('ai.endpoints');
  const [form] = Form.useForm();
  const [formValid, setFormValid] = useState(false);
  const [formValues, setFormValues] = useState({});
  const [createEndpoint, { isLoading: isCreating }] = useCreateChatEndpointMutation();
  const [updateEndpoint, { isLoading: isUpdating }] = useUpdateChatEndpointMutation();

  const endpointType: ChatEndpointType = Form.useWatch('EndpointType', form) || 'Completion';
  const provider: ChatProviderType | undefined = Form.useWatch('Provider', form);
  const comboError = provider ? validateProviderTypeCombo(provider, endpointType) : null;

  useEffect(() => {
    form
      .validateFields({ validateOnly: true })
      .then(() => setFormValid(true))
      .catch(() => setFormValid(false));
  }, [formValues, form]);

  useEffect(() => {
    if (endpoint) {
      form.setFieldsValue({ ...endpoint });
    } else {
      form.resetFields();
      form.setFieldsValue({
        EndpointType: 'Completion',
        Provider: 'OpenAI',
        Active: true,
        HealthCheckEnabled: true,
        HealthCheckUseAuth: false,
        MaxOutputTokens: 4096,
        TimeoutMs: 120000,
        MaxConcurrentRequests: 2,
      });
    }
  }, [endpoint, form]);

  // When the endpoint type changes, drop a provider that became invalid.
  useEffect(() => {
    if (provider && validateProviderTypeCombo(provider, endpointType)) {
      form.setFieldValue('Provider', undefined);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [endpointType]);

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      if (endpoint) {
        // Preserve the stored key by echoing the redacted value back unchanged.
        const updated: ChatEndpoint = {
          ...endpoint,
          ...values,
          GUID: endpoint.GUID,
          TenantGUID: endpoint.TenantGUID,
          ApiKey: values.ApiKey,
        };
        const { error } = await updateEndpoint({ tenantGuid, endpoint: updated });
        if (error) {
          toast.error(t('toast.updateFailed'), { id: globalToastId });
          return;
        }
        toast.success(t('toast.updated'), { id: globalToastId });
      } else {
        const { error } = await createEndpoint({ tenantGuid, endpoint: values });
        if (error) {
          toast.error(t('toast.createFailed'), { id: globalToastId });
          return;
        }
        toast.success(t('toast.created'), { id: globalToastId });
      }
      onClose();
    } catch {
      // Validation errors render inline; nothing else to do.
    }
  };

  return (
    <LitegraphModal
      title={endpoint ? t('modal.editTitle') : t('modal.createTitle')}
      open
      onOk={handleSubmit}
      onCancel={onClose}
      confirmLoading={isCreating || isUpdating}
      okButtonProps={{ disabled: !formValid || !!comboError }}
      width={620}
      data-testid="add-edit-endpoint-modal"
    >
      <Form
        form={form}
        layout="vertical"
        onValuesChange={(_, allValues) => setFormValues(allValues)}
      >
        <Divider orientation="left" plain style={{ marginTop: 0 }}>
          {t('modal.groupGeneral')}
        </Divider>
        <LitegraphFormItem
          label={t('form.name')}
          name="Name"
          tooltip={t('form.nameTooltip')}
          rules={[{ required: true, message: t('form.nameRequired') }]}
        >
          <LitegraphInput placeholder={t('form.namePlaceholder')} data-testid="endpoint-name" />
        </LitegraphFormItem>
        <LitegraphFormItem
          label={t('form.type')}
          name="EndpointType"
          tooltip={t('form.typeTooltip')}
          rules={[{ required: true, message: t('form.typeRequired') }]}
        >
          <LitegraphSelect
            options={[
              { label: t('types.completion'), value: 'Completion' },
              { label: t('types.embedding'), value: 'Embedding' },
            ]}
            data-testid="endpoint-type"
          />
        </LitegraphFormItem>
        <LitegraphFormItem
          label={t('form.provider')}
          name="Provider"
          tooltip={t('form.providerTooltip')}
          rules={[{ required: true, message: t('form.providerRequired') }]}
        >
          <LitegraphSelect
            options={providersForType(endpointType).map((p) => ({ label: p, value: p }))}
            data-testid="endpoint-provider"
          />
        </LitegraphFormItem>
        {comboError && (
          <LitegraphText
            style={{ display: 'block', marginBottom: 12, color: 'var(--ant-color-error)' }}
            data-testid="endpoint-combo-error"
          >
            {comboError === 'anthropicEmbedding'
              ? t('form.comboAnthropicEmbedding')
              : t('form.comboVoyageCompletion')}
          </LitegraphText>
        )}
        <LitegraphFormItem
          label={t('form.endpoint')}
          name="Endpoint"
          tooltip={
            <div>
              <div>{t('form.endpointTooltip')}</div>
              <div style={{ marginTop: 6 }}>{t('form.endpointExamplesIntro')}</div>
              <ul style={{ margin: '4px 0 0', paddingInlineStart: 16 }}>
                {(Object.keys(PROVIDER_BASE_URL_EXAMPLES) as ChatProviderType[]).map((p) => (
                  <li key={p}>
                    {p}: <code>{PROVIDER_BASE_URL_EXAMPLES[p]}</code>
                  </li>
                ))}
              </ul>
            </div>
          }
          rules={[
            { required: true, message: t('form.endpointRequired') },
            {
              validator: (_, value) => {
                const urlError = validateEndpointUrlForProvider(provider, value);
                if (!urlError) return Promise.resolve();
                return Promise.reject(
                  new Error(
                    urlError === 'notBaseUrl' ? t('form.endpointNotBaseUrl') : t('form.endpointInvalid')
                  )
                );
              },
            },
          ]}
        >
          <LitegraphInput
            placeholder={provider ? PROVIDER_BASE_URL_EXAMPLES[provider] : 'https://api.openai.com'}
            data-testid="endpoint-url"
          />
        </LitegraphFormItem>
        <LitegraphFormItem
          label={t('form.apiKey')}
          name="ApiKey"
          tooltip={endpoint ? t('form.apiKeyRedactedTooltip') : t('form.apiKeyTooltip')}
        >
          <LitegraphInput
            placeholder={t('form.apiKeyPlaceholder')}
            autoComplete="new-password"
            data-testid="endpoint-apikey"
          />
        </LitegraphFormItem>
        {endpoint && isRedactedApiKey(endpoint.ApiKey) && (
          <LitegraphText
            style={{
              display: 'block',
              marginTop: -12,
              marginBottom: 12,
              fontSize: 12,
              color: 'var(--ant-color-text-tertiary)',
            }}
          >
            {t('form.apiKeyRedactedHint')}
          </LitegraphText>
        )}
        <LitegraphFormItem
          label={t('form.model')}
          name="Model"
          tooltip={t('form.modelTooltip')}
          rules={[{ required: true, message: t('form.modelRequired') }]}
        >
          <LitegraphInput placeholder={t('form.modelPlaceholder')} data-testid="endpoint-model" />
        </LitegraphFormItem>

        <Divider orientation="left" plain>
          {t('modal.groupPerformance')}
        </Divider>
        <LitegraphFormItem
          label={t('form.maxOutputTokens')}
          name="MaxOutputTokens"
          tooltip={t('form.maxOutputTokensTooltip')}
        >
          <InputNumber min={1} style={{ width: '100%' }} />
        </LitegraphFormItem>
        {endpointType === 'Completion' && (
          <LitegraphFormItem
            label={t('form.contextWindowTokens')}
            name="ContextWindowTokens"
            tooltip={t('form.contextWindowTokensTooltip')}
          >
            <InputNumber
              min={0}
              style={{ width: '100%' }}
              placeholder={t('form.contextWindowTokensPlaceholder')}
              data-testid="endpoint-context-window"
            />
          </LitegraphFormItem>
        )}
        <LitegraphFormItem
          label={t('form.timeoutMs')}
          name="TimeoutMs"
          tooltip={t('form.timeoutMsTooltip')}
        >
          <InputNumber min={1} style={{ width: '100%' }} />
        </LitegraphFormItem>
        <LitegraphFormItem
          label={t('form.maxConcurrentRequests')}
          name="MaxConcurrentRequests"
          tooltip={t('form.maxConcurrentRequestsTooltip')}
          rules={[
            {
              validator: (_, value) =>
                value == null || value >= 1
                  ? Promise.resolve()
                  : Promise.reject(new Error(t('form.maxConcurrentRequestsMin'))),
            },
          ]}
        >
          <InputNumber min={1} style={{ width: '100%' }} data-testid="endpoint-concurrency" />
        </LitegraphFormItem>
        <LitegraphFormItem
          label={t('form.active')}
          name="Active"
          valuePropName="checked"
          tooltip={t('form.activeTooltip')}
        >
          <Switch data-testid="endpoint-active" />
        </LitegraphFormItem>

        <Divider orientation="left" plain>
          {t('modal.groupHealth')}
        </Divider>
        <LitegraphFormItem
          label={t('form.healthCheckEnabled')}
          name="HealthCheckEnabled"
          valuePropName="checked"
          tooltip={t('form.healthCheckEnabledTooltip')}
        >
          <Switch />
        </LitegraphFormItem>
        <LitegraphFormItem
          label={t('form.healthCheckUrl')}
          name="HealthCheckUrl"
          tooltip={t('form.healthCheckUrlTooltip')}
        >
          <LitegraphInput placeholder={t('form.healthCheckUrlPlaceholder')} />
        </LitegraphFormItem>
        <LitegraphFormItem
          label={t('form.healthCheckUseAuth')}
          name="HealthCheckUseAuth"
          valuePropName="checked"
          tooltip={t('form.healthCheckUseAuthTooltip')}
        >
          <Switch />
        </LitegraphFormItem>
      </Form>
    </LitegraphModal>
  );
};

export default AddEditEndpoint;
