'use client';
import React, { useEffect, useState } from 'react';
import { Form, message } from 'antd';
import LitegraphModal from '@/components/base/modal/Modal';
import LitegraphFormItem from '@/components/base/form/FormItem';
import LitegraphInput from '@/components/base/input/Input';
import {
  useEnableVectorIndexMutation,
  useReadVectorIndexConfigurationQuery,
} from '@/lib/store/slice/slice';
import { validateVectorIndexFile } from './constant';
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
      message.success('Vector index enabled successfully');
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
      title={viewMode ? 'Vector Index Configuration' : 'Enable Vector Index'}
      open={isEnableVectorIndexModalVisible}
      onOk={viewMode ? undefined : handleSubmit}
      onCancel={handleCancel}
      confirmLoading={viewMode ? false : isCreatingVectorIndex}
      okButtonProps={viewMode ? { onClick: handleCancel } : { disabled: !formValid }}
      data-testid="enable-vector-index-modal"
      width={800}
    >
      {viewMode && isVectorIndexConfigLoading ? (
        <PageLoading />
      ) : viewMode && isConfigError ? (
        <div style={{ textAlign: 'center', padding: '40px 20px' }}>
          <div style={{ color: '#d32f2f', fontSize: '16px', marginBottom: '12px' }}>
            Failed to load vector index configuration
          </div>
          <div style={{ color: '#666', fontSize: '14px', marginBottom: '20px' }}>
            {configError &&
              ((configError as any)?.data?.Description ||
                (configError as any)?.Description ||
                'Unable to retrieve configuration details')}
          </div>
          <div style={{ fontSize: '12px', color: '#999' }}>
            The vector index may not be enabled or there was an issue accessing the configuration.
          </div>
        </div>
      ) : !viewMode ? (
        <Form
          form={form}
          layout="vertical"
          onValuesChange={(_, allValues) => setFormValues(allValues)}
          requiredMark={true}
        >
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: '8px' }}>
            <LitegraphFormItem
              label="Vector Index Type"
              name="VectorIndexType"
              rules={[{ required: true, message: 'Please select Vector Index Type!' }]}
            >
              <LitegraphSelect
                placeholder="Select Vector Index Type"
                options={[
                  { label: 'HNSW (Sqlite)', value: VectorIndexType.HnswSqlite },
                  { label: 'HNSW (RAM)', value: VectorIndexType.HnswRam },
                  { label: 'None', value: VectorIndexType.None },
                ]}
                variant="outlined"
              />
            </LitegraphFormItem>

            <LitegraphFormItem
              label="Vector Index File"
              name="VectorIndexFile"
              rules={[
                { required: true, message: 'Please input Vector Index File!' },
                { validator: validateVectorIndexFile },
              ]}
              extra={<small>File should end with .db and should not contain spaces</small>}
            >
              <LitegraphInput
                placeholder="e.g., graph-00000000-0000-0000-0000-000000000000-hnsw.db"
                variant="outlined"
              />
            </LitegraphFormItem>

            <LitegraphFormItem label="Vector Index Threshold" name="VectorIndexThreshold">
              <LitegraphInput type="number" placeholder="Enter threshold" variant="outlined" />
            </LitegraphFormItem>

            <LitegraphFormItem
              label="Vector Dimensionality"
              name="VectorDimensionality"
              rules={[{ required: true, message: 'Please input Vector Dimensionality!' }]}
            >
              <LitegraphInput
                type="number"
                placeholder="Enter dimensionality (e.g., 1536)"
                min={1}
                variant="outlined"
              />
            </LitegraphFormItem>

            <LitegraphFormItem
              label="Vector Index M"
              name="VectorIndexM"
              rules={[{ required: true, message: 'Please input Vector Index M!' }]}
              extra={<small>Number of connections per layer in HNSW index</small>}
            >
              <LitegraphInput
                type="number"
                placeholder="Enter M value (e.g., 16)"
                min={1}
                variant="outlined"
              />
            </LitegraphFormItem>

            <LitegraphFormItem
              label="Vector Index Ef"
              name="VectorIndexEf"
              rules={[{ required: true, message: 'Please input Vector Index Ef!' }]}
              extra={<small>Search parameter for HNSW index</small>}
            >
              <LitegraphInput
                type="number"
                placeholder="Enter Ef value (e.g., 100)"
                min={1}
                variant="outlined"
              />
            </LitegraphFormItem>

            <LitegraphFormItem
              label="Vector Index Ef Construction"
              name="VectorIndexEfConstruction"
              rules={[{ required: true, message: 'Please input Vector Index Ef Construction!' }]}
              extra={<small>Construction parameter for HNSW index</small>}
            >
              <LitegraphInput
                type="number"
                placeholder="Enter Ef Construction value (e.g., 200)"
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
            <LitegraphFormItem label="Vector Index Type" name="VectorIndexType">
              <LitegraphSelect
                readonly
                placeholder="Select Vector Index Type"
                options={[
                  { label: 'HNSW (Sqlite)', value: VectorIndexType.HnswSqlite },
                  { label: 'HNSW (RAM)', value: VectorIndexType.HnswRam },
                  { label: 'None', value: VectorIndexType.None },
                ]}
                variant="borderless"
              />
            </LitegraphFormItem>

            <LitegraphFormItem label="Vector Index File" name="VectorIndexFile">
              <LitegraphInput variant="borderless" readOnly />
            </LitegraphFormItem>

            <LitegraphFormItem label="Vector Index Threshold" name="VectorIndexThreshold">
              <LitegraphInput variant="borderless" readOnly />
            </LitegraphFormItem>

            <LitegraphFormItem label="Vector Dimensionality" name="VectorDimensionality">
              <LitegraphInput variant="borderless" readOnly />
            </LitegraphFormItem>

            <LitegraphFormItem label="Vector Index M" name="VectorIndexM">
              <LitegraphInput variant="borderless" readOnly />
            </LitegraphFormItem>

            <LitegraphFormItem label="Vector Index Ef" name="VectorIndexEf">
              <LitegraphInput variant="borderless" readOnly />
            </LitegraphFormItem>

            <LitegraphFormItem
              label="Vector Index Ef Construction"
              name="VectorIndexEfConstruction"
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
