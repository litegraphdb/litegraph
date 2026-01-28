'use client';
import React, { useEffect, useState } from 'react';
import { Form, Button } from 'antd';
import { CloseCircleFilled, CopyOutlined, PlusOutlined } from '@ant-design/icons';
import LitegraphFormItem from '@/components/base/form/FormItem';
import LitegraphInput from '@/components/base/input/Input';
import { JsonEditor } from 'jsoneditor-react';
import { v4 } from 'uuid';
import styles from './styles.module.scss';
import LitegraphFlex from '@/components/base/flex/Flex';
import LitegraphText from '@/components/base/typograpghy/Text';
import { toast } from 'react-hot-toast';
import { copyJsonToClipboard } from '@/utils/jsonCopyUtils';

interface VectorsInputProps {
  value?: any[];
  onChange?: (values: any[]) => void;
  name: string;
  readonly?: boolean;
}

const VectorsInput: React.FC<VectorsInputProps> = ({ value = [], onChange, name, readonly }) => {
  const [uniqueKeys, setUniqueKeys] = useState<string[]>([]);
  const form = Form.useFormInstance();

  useEffect(() => {
    // Generate unique keys for each vector entry
    setUniqueKeys(value.map(() => v4()));
  }, [value.length]);

  return (
    <Form.List name={name} initialValue={value}>
      {(fields, { add, remove }, { errors }) => (
        <>
          {fields.length > 0
            ? fields.map((field, index) => (
                <div key={field.key} className={styles.vectorInput}>
                  {!readonly && (
                    <CloseCircleFilled
                      onClick={() => remove(field.name)}
                      className={styles.closeIcon}
                    />
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
                    label={
                      <LitegraphFlex align="center" gap={8}>
                        <span>Vectors</span>
                        {readonly && (
                          <CopyOutlined
                            style={{ cursor: 'pointer' }}
                            onClick={() => {
                              const vectors =
                                form.getFieldValue([name, field.name, 'Vectors']) || [];
                              copyJsonToClipboard(vectors, 'Vectors');
                            }}
                          />
                        )}
                      </LitegraphFlex>
                    }
                    rules={[{ required: true, message: 'Please input Vectors!' }]}
                  >
                    <JsonEditor
                      key={uniqueKeys[index]}
                      value={form.getFieldValue([field.name, 'Vectors']) || []}
                      onChange={(json: any) => {
                        form.setFieldValue([name, field.name, 'Vectors'], json);
                      }}
                      mode={readonly ? 'view' : 'code'}
                      enableSort={false}
                      enableTransform={false}
                      mainMenuBar={!readonly}
                      statusBar={!readonly}
                      navigationBar={!readonly}
                    />
                  </LitegraphFormItem>
                </div>
              ))
            : readonly && <>N/A</>}

          {!readonly && (
            <Form.Item>
              <Button
                type="dashed"
                onClick={() => {
                  add({
                    Model: '',
                    Dimensionality: 0,
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
              <Form.ErrorList errors={errors} />
            </Form.Item>
          )}
        </>
      )}
    </Form.List>
  );
};

export default VectorsInput;
