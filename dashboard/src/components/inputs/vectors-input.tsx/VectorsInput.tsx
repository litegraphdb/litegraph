'use client';
import React, { useEffect, useState } from 'react';
import { Form, Button, Input } from 'antd';
import { CloseCircleFilled, PlusOutlined } from '@ant-design/icons';
import LitegraphFormItem from '@/components/base/form/FormItem';
import LitegraphInput from '@/components/base/input/Input';
import { v4 } from 'uuid';
import styles from './styles.module.scss';
import LitegraphFlex from '@/components/base/flex/Flex';
import CopyButton from '@/components/base/copy-button/CopyButton';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';
import {
  parseVectorValuesInput,
  vectorValuesToInputText,
} from '@/components/inputs/vectors-input.tsx/utils';

interface VectorsInputProps {
  value?: any[];
  onChange?: (values: any[]) => void;
  name: string;
  readonly?: boolean;
}

interface VectorValuesTextAreaProps {
  value?: unknown;
  onChange?: (value: string) => void;
  readonly?: boolean;
}

const VectorValuesTextArea = ({ value, onChange, readonly }: VectorValuesTextAreaProps) => (
  <Input.TextArea
    data-testid="vector-values-input"
    value={vectorValuesToInputText(value)}
    onChange={(event) => onChange?.(event.target.value)}
    readOnly={readonly}
    variant={readonly ? 'borderless' : 'outlined'}
    autoSize={{ minRows: 3, maxRows: 8 }}
    placeholder="[0.1, 0.2, 0.3]"
    className={styles.vectorValuesInput}
  />
);

const VectorsInput: React.FC<VectorsInputProps> = ({ value = [], name, readonly }) => {
  const [uniqueKeys, setUniqueKeys] = useState<string[]>([]);
  const form = Form.useFormInstance();

  const validateVectorValues = (fieldName: number, vectorValues: unknown) => {
    const parsedVectorValues = parseVectorValuesInput(vectorValues);

    if (!parsedVectorValues.valid) {
      return Promise.reject(new Error('Please input vector values as a non-empty JSON array.'));
    }

    const dimensionality = Number(form.getFieldValue([name, fieldName, 'Dimensionality']));
    if (
      Number.isFinite(dimensionality) &&
      dimensionality > 0 &&
      parsedVectorValues.values.length !== dimensionality
    ) {
      return Promise.reject(new Error('Vector value count must match dimensionality.'));
    }

    return Promise.resolve();
  };

  useEffect(() => {
    const current = form.getFieldValue(name);
    if ((current === undefined || current === null) && value.length > 0) {
      form.setFieldValue(name, value);
    }
  }, [form, name, value]);

  useEffect(() => {
    // Generate unique keys for each vector entry
    setUniqueKeys(value.map(() => v4()));
  }, [value.length]);

  return (
    <Form.List name={name}>
      {(fields, { add, remove }, { errors }) => (
        <>
          {fields.length > 0
            ? fields.map((field, index) => (
                <div key={field.key} className={styles.vectorInput}>
                  {!readonly && (
                    <LitegraphTooltip title="Remove this vector">
                      <CloseCircleFilled
                        onClick={() => remove(field.name)}
                        className={styles.closeIcon}
                      />
                    </LitegraphTooltip>
                  )}

                  <LitegraphFlex gap={10}>
                    <LitegraphFormItem
                      className="flex-1"
                      label="Model"
                      name={[field.name, 'Model']}
                      rules={[{ required: true, message: 'Please input Model!' }]}
                    >
                      <LitegraphInput
                        placeholder="Enter Model"
                        readOnly={readonly}
                        variant={readonly ? 'borderless' : 'outlined'}
                      />
                    </LitegraphFormItem>

                    <LitegraphFormItem
                      className="flex-1"
                      label="Dimensionality"
                      name={[field.name, 'Dimensionality']}
                      rules={[{ required: true, message: 'Please input Dimensionality!' }]}
                    >
                      <LitegraphInput
                        type="number"
                        placeholder="Enter Dimensionality"
                        readOnly={readonly}
                        variant={readonly ? 'borderless' : 'outlined'}
                      />
                    </LitegraphFormItem>
                  </LitegraphFlex>

                  <LitegraphFormItem
                    label="Content"
                    name={[field.name, 'Content']}
                    rules={[{ required: true, message: 'Please input Content!' }]}
                  >
                    <LitegraphInput
                      placeholder="Enter Content"
                      readOnly={readonly}
                      variant={readonly ? 'borderless' : 'outlined'}
                    />
                  </LitegraphFormItem>
                  <LitegraphFormItem
                    name={[field.name, 'Vectors']}
                    dependencies={[[name, field.name, 'Dimensionality']]}
                    label={
                      <LitegraphFlex align="center" gap={8}>
                        <span>Vector Values</span>
                        {readonly && (
                          <CopyButton
                            getText={() =>
                              JSON.stringify(
                                parseVectorValuesInput(
                                  form.getFieldValue([name, field.name, 'Vectors'])
                                ).values,
                                null,
                                2
                              )
                            }
                            tooltipTitle="Copy vector values to clipboard"
                          />
                        )}
                      </LitegraphFlex>
                    }
                    rules={[
                      { required: true, message: 'Please input vector values!' },
                      {
                        validator: (_, vectorValues) =>
                          validateVectorValues(field.name, vectorValues),
                      },
                    ]}
                  >
                    <VectorValuesTextArea key={uniqueKeys[index] || field.key} readonly={readonly} />
                  </LitegraphFormItem>
                </div>
              ))
            : readonly && <>N/A</>}

          {!readonly && (
            <Form.Item>
              <LitegraphTooltip title="Add a new vector entry">
                <Button
                  type="dashed"
                  onClick={() => {
                    add({
                      Model: '',
                      Dimensionality: 3,
                      Content: '',
                      Vectors: [0.1, 0.2, 0.3],
                    });
                    setUniqueKeys([...uniqueKeys, v4()]);
                  }}
                  icon={<PlusOutlined />}
                  style={{ width: '100%' }}
                >
                  Add Vector
                </Button>
              </LitegraphTooltip>
              <Form.ErrorList errors={errors} />
            </Form.Item>
          )}
        </>
      )}
    </Form.List>
  );
};

export default VectorsInput;
