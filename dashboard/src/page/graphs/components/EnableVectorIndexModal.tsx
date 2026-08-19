'use client';
import React, { useEffect, useState } from 'react';
import { Form, message } from 'antd';
import { useTranslations } from 'next-intl';
import LitegraphModal from '@/components/base/modal/Modal';
import LitegraphFormItem from '@/components/base/form/FormItem';
import LitegraphInput from '@/components/base/input/Input';
import {
  useEnableVectorIndexMutation,
  useReadVectorIndexConfigurationQuery,
} from '@/lib/store/slice/slice';
import { makeValidateVectorIndexFile } from './constant';
import { VectorIndexData, EnableVectorIndexModalProps, VectorIndexType } from './types';
import PageLoading from '@/components/base/loading/PageLoading';
import LitegraphSelect from '@/components/base/select/Select';

const EnableVectorIndexModal = ({
  isEnableVectorIndexModalVisible,
  setIsEnableVectorIndexModalVisible,
  graphId,
  onSuccess,
  viewMode = false,
}: EnableVectorIndexModalProps) => {
  const t = useTranslations('vectorIndex');
  const validateVectorIndexFile = makeValidateVectorIndexFile(t);
  const [form] = Form.useForm<VectorIndexData>();
  const [formValid, setFormValid] = useState(false);
  const [enableVectorIndex, { isLoading: isCreatingVectorIndex }] = useEnableVectorIndexMutation();
  const {
    data: vectorIndexConfig,
    isLoading: isL1,
    isFetching,
    error: configError,
    isError: isConfigError,
  } = useReadVectorIndexConfigurationQuery(graphId, { skip: !viewMode });
  const isVectorIndexConfigLoading = isL1 || isFetching;
  // console.log('viewMode', viewMode);
  // Add form validation watcher
  const [formValues, setFormValues] = useState({});
  useEffect(() => {
    form
      .validateFields({ validateOnly: true })
      .then(() => setFormValid(true))
      .catch(() => setFormValid(false));
  }, [formValues, form]);

  useEffect(() => {
    if (isEnableVectorIndexModalVisible && !viewMode) {
      form.resetFields();
      form.setFieldsValue({
        VectorIndexType: VectorIndexType.HnswSqlite,
        VectorIndexThreshold: null,
        VectorDimensionality: 1536,
        VectorIndexM: 16,
        VectorIndexEf: 100,
        VectorIndexEfConstruction: 200,
      });
    } else if (isEnableVectorIndexModalVisible && viewMode && vectorIndexConfig) {
      // Convert the API response to match our form types
      const convertedConfig = {
        ...vectorIndexConfig,
        VectorIndexType: vectorIndexConfig.VectorIndexType as VectorIndexType,
      };
      form.setFieldsValue(convertedConfig);
    }
  }, [isEnableVectorIndexModalVisible, form, viewMode, vectorIndexConfig]);

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      await enableVectorIndex({
        graphId: graphId,
        request: {
          VectorIndexType: values.VectorIndexType,
          VectorIndexFile: values.VectorIndexFile,
          VectorDimensionality: Number(values.VectorDimensionality),
          VectorIndexM: Number(values.VectorIndexM),
          VectorIndexEf: Number(values.VectorIndexEf),
          VectorIndexEfConstruction: Number(values.VectorIndexEfConstruction),
          VectorIndexThreshold: Number(values.VectorIndexThreshold),
        },
      }).unwrap();

      setIsEnableVectorIndexModalVisible(false);
      form.resetFields();
      onSuccess();
      message.success(t('enabledSuccess'));
    } catch (error) {
      console.error('Failed to enable vector index:', error);
    }
  };

  const handleCancel = () => {
    setIsEnableVectorIndexModalVisible(false);
    form.resetFields();
  };

  return (
    <LitegraphModal
      title={viewMode ? t('viewTitle') : t('enableTitle')}
      open={isEnableVectorIndexModalVisible}
      onOk={viewMode ? undefined : handleSubmit}
      onCancel={handleCancel}
      confirmLoading={viewMode ? false : isCreatingVectorIndex}
      okButtonProps={viewMode ? { onClick: handleCancel } : { disabled: !formValid }}
      data-testid="enable-vector-index-modal"
      width={800}
      forceRender
    >
      {!isEnableVectorIndexModalVisible ? (
        <Form form={form} style={{ display: 'none' }} />
      ) : viewMode && isVectorIndexConfigLoading ? (
        <>
          <Form form={form} style={{ display: 'none' }} />
          <PageLoading />
        </>
      ) : viewMode && isConfigError ? (
        <>
          <Form form={form} style={{ display: 'none' }} />
          <div style={{ textAlign: 'center', padding: '40px 20px' }}>
            <div style={{ color: '#d32f2f', fontSize: '16px', marginBottom: '12px' }}>
              {t('loadConfigFailed')}
            </div>
            <div style={{ color: '#666', fontSize: '14px', marginBottom: '20px' }}>
              {configError &&
                ((configError as any)?.data?.Description ||
                  (configError as any)?.Description ||
                  t('unableToRetrieveConfig'))}
            </div>
            <div style={{ fontSize: '12px', color: '#999' }}>{t('configHint')}</div>
          </div>
        </>
      ) : !viewMode ? (
        <Form
          form={form}
          layout="vertical"
          onValuesChange={(_, allValues) => setFormValues(allValues)}
          requiredMark={true}
        >
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '8px' }}>
            <LitegraphFormItem
              label={t('type')}
              name="VectorIndexType"
              tooltip={t('typeTooltip')}
              rules={[{ required: true, message: t('typeRequired') }]}
            >
              <LitegraphSelect
                placeholder={t('selectType')}
                options={[
                  { label: t('typeHnswSqlite'), value: VectorIndexType.HnswSqlite },
                  { label: t('typeHnswRam'), value: VectorIndexType.HnswRam },
                  { label: t('typeNone'), value: VectorIndexType.None },
                ]}
                variant="outlined"
              />
            </LitegraphFormItem>

            <LitegraphFormItem
              label={t('file')}
              name="VectorIndexFile"
              tooltip={t('fileTooltip')}
              rules={[
                { required: true, message: t('fileRequired') },
                { validator: validateVectorIndexFile },
              ]}
              extra={<small>{t('fileHint')}</small>}
            >
              <LitegraphInput
                placeholder={t('filePlaceholder')}
                variant="outlined"
              />
            </LitegraphFormItem>

            <LitegraphFormItem
              label={t('threshold')}
              name="VectorIndexThreshold"
              tooltip={t('thresholdTooltip')}
            >
              <LitegraphInput type="number" placeholder={t('thresholdPlaceholder')} variant="outlined" />
            </LitegraphFormItem>

            <LitegraphFormItem
              label={t('dimensionality')}
              name="VectorDimensionality"
              tooltip={t('dimensionalityTooltip')}
              rules={[{ required: true, message: t('dimensionalityRequired') }]}
            >
              <LitegraphInput
                type="number"
                placeholder={t('dimensionalityPlaceholder')}
                min={1}
                variant="outlined"
              />
            </LitegraphFormItem>

            <LitegraphFormItem
              label={t('m')}
              name="VectorIndexM"
              tooltip={t('mTooltip')}
              rules={[{ required: true, message: t('mRequired') }]}
              extra={<small>{t('mHint')}</small>}
            >
              <LitegraphInput
                type="number"
                placeholder={t('mPlaceholder')}
                min={1}
                variant="outlined"
              />
            </LitegraphFormItem>

            <LitegraphFormItem
              label={t('ef')}
              name="VectorIndexEf"
              tooltip={t('efTooltip')}
              rules={[{ required: true, message: t('efRequired') }]}
              extra={<small>{t('efHint')}</small>}
            >
              <LitegraphInput
                type="number"
                placeholder={t('efPlaceholder')}
                min={1}
                variant="outlined"
              />
            </LitegraphFormItem>

            <LitegraphFormItem
              label={t('efConstruction')}
              name="VectorIndexEfConstruction"
              tooltip={t('efConstructionTooltip')}
              rules={[{ required: true, message: t('efConstructionRequired') }]}
              extra={<small>{t('efConstructionHint')}</small>}
            >
              <LitegraphInput
                type="number"
                placeholder={t('efConstructionPlaceholder')}
                min={1}
                variant="outlined"
              />
            </LitegraphFormItem>
          </div>
        </Form>
      ) : (
        <Form
          form={form}
          layout="vertical"
          onValuesChange={(_, allValues) => setFormValues(allValues)}
          requiredMark={false}
        >
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '8px' }}>
            <LitegraphFormItem
              label={t('type')}
              name="VectorIndexType"
              tooltip={t('typeTooltip')}
            >
              <LitegraphSelect
                readonly
                placeholder={t('selectType')}
                options={[
                  { label: t('typeHnswSqlite'), value: VectorIndexType.HnswSqlite },
                  { label: t('typeHnswRam'), value: VectorIndexType.HnswRam },
                  { label: t('typeNone'), value: VectorIndexType.None },
                ]}
                variant="borderless"
              />
            </LitegraphFormItem>

            <LitegraphFormItem
              label={t('file')}
              name="VectorIndexFile"
              tooltip={t('fileTooltip')}
            >
              <LitegraphInput variant="borderless" readOnly />
            </LitegraphFormItem>

            <LitegraphFormItem
              label={t('threshold')}
              name="VectorIndexThreshold"
              tooltip={t('thresholdTooltip')}
            >
              <LitegraphInput variant="borderless" readOnly />
            </LitegraphFormItem>

            <LitegraphFormItem
              label={t('dimensionality')}
              name="VectorDimensionality"
              tooltip={t('dimensionalityTooltip')}
            >
              <LitegraphInput variant="borderless" readOnly />
            </LitegraphFormItem>

            <LitegraphFormItem
              label={t('m')}
              name="VectorIndexM"
              tooltip={t('mTooltip')}
            >
              <LitegraphInput variant="borderless" readOnly />
            </LitegraphFormItem>

            <LitegraphFormItem
              label={t('ef')}
              name="VectorIndexEf"
              tooltip={t('efTooltip')}
            >
              <LitegraphInput variant="borderless" readOnly />
            </LitegraphFormItem>

            <LitegraphFormItem
              label={t('efConstruction')}
              name="VectorIndexEfConstruction"
              tooltip={t('efConstructionTooltip')}
            >
              <LitegraphInput variant="borderless" readOnly />
            </LitegraphFormItem>
          </div>
        </Form>
      )}
    </LitegraphModal>
  );
};

export default EnableVectorIndexModal;
