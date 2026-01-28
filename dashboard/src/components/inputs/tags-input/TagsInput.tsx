'use client';
import React from 'react';
import { Form, Input, Button } from 'antd';
import { CloseCircleFilled, PlusOutlined } from '@ant-design/icons';
import styles from './styles.module.scss';
import LitegraphText from '@/components/base/typograpghy/Text';
interface TagsInputProps {
  value?: Array<{ key: string; value: string }>;
  onChange?: (values: Record<string, string>) => void;
  name: string;
  readonly?: boolean;
}

const TagsInput: React.FC<TagsInputProps> = ({ value = [], onChange, name, readonly }) => {
  return (
    <Form.List name={name} initialValue={value}>
      {(fields, { add, remove }, { errors }) => (
        <>
          {fields?.length > 0
            ? fields.map((field, index) => (
                <Form.Item {...field} key={field.key} style={{ marginBottom: 8 }}>
                  <Input.Group compact>
                    <Form.Item
                      name={[field.name, 'key']}
                      noStyle
                      rules={[{ required: true, message: 'Key is required' }]}
                    >
                      <Input readOnly={readonly} style={{ width: '50%' }} placeholder="Enter key" />
                    </Form.Item>
                    <Form.Item
                      name={[field.name, 'value']}
                      noStyle
                      rules={[{ required: true, message: 'Value is required' }]}
                    >
                      <Input
                        readOnly={readonly}
                        style={{ width: '50%' }}
                        placeholder="Enter value"
                        suffix={
                          !readonly && (
                            <CloseCircleFilled
                              onClick={() => remove(field.name)}
                              className={styles.closeIcon}
                            />
                          )
                        }
                      />
                    </Form.Item>
                  </Input.Group>
                </Form.Item>
              ))
            : readonly && <LitegraphText>N/A</LitegraphText>}
          {!readonly && (
            <Form.Item>
              <Button
                type="dashed"
                onClick={() => add()}
                icon={<PlusOutlined />}
                style={{ width: '100%' }}
              >
                Add Tag
              </Button>
              <Form.ErrorList errors={errors} />
            </Form.Item>
          )}
        </>
      )}
    </Form.List>
  );
};

export default TagsInput;
