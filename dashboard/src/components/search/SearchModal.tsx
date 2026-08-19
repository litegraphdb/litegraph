'use client';
import { useRef } from 'react';
import { useTranslations } from 'next-intl';
import { Form } from 'antd';
import LitegraphModal from '@/components/base/modal/Modal';
import LabelInput from '../inputs/label-input/LabelInput';
import TagsInput from '../inputs/tags-input/TagsInput';
import LitegraphFormItem from '@/components/base/form/FormItem';
import JsonEditorWithAce from '@/components/inputs/json-editor/JsonEditorWithAce';
import { v4 } from 'uuid';
import { initialSearchData, validateAtLeastOne } from './constants';
import { SearchData } from './type';
import LitegraphText from '../base/typograpghy/Text';
import { LightGraphTheme } from '@/theme/theme';

interface SearchModalProps {
  isSearchModalVisible: boolean;
  setIsSearchModalVisible: (visible: boolean) => void;
  onSearch: (values: SearchData) => void;
  onClose?: () => void;
}

const SearchModal = ({
  isSearchModalVisible,
  setIsSearchModalVisible,
  onSearch,
  onClose,
}: SearchModalProps) => {
  const t = useTranslations('search');
  const tCommon = useTranslations('common');
  const [form] = Form.useForm<SearchData>();
  const uniqueKey = useRef(v4());

  const handleSearch = async () => {
    try {
      const values = await form.validateFields();
      onSearch({
        expr: values.expr || {},
        tags: values.tags || {},
        labels: values.labels || [],
        embeddings: values.embeddings,
      });
      setIsSearchModalVisible(false);
      form.resetFields();
    } catch (error) {
      console.error('Search failed:', error);
    }
  };

  return (
    <LitegraphModal
      destroyOnClose={false}
      forceRender
      title={tCommon('actions.search')}
      centered
      open={isSearchModalVisible}
      onCancel={() => {
        setIsSearchModalVisible(false);
        onClose?.();
      }}
      onOk={handleSearch}
      okText={tCommon('actions.search')}
    >
      {!isSearchModalVisible ? (
        <Form form={form} style={{ display: 'none' }} />
      ) : (
        <Form
          initialValues={initialSearchData}
          form={form}
          clearOnDestroy={false}
          layout="vertical"
          onFinish={handleSearch}
        >
          <LabelInput name="labels" tooltip={t('labelsTooltip')} />
          <Form.Item
            label={t('tags')}
            tooltip={t('tagsTooltip')}
            rules={[{ validator: validateAtLeastOne(form, t) }]}
          >
            <TagsInput name="tags" />
          </Form.Item>
          <LitegraphFormItem
            label={t('expression')}
            name="expr"
            tooltip={t('expressionTooltip')}
            rules={[{ validator: validateAtLeastOne(form, t) }]}
            extra={
              <>
                <LitegraphText color={LightGraphTheme.subHeadingColor}>
                  {t('example')}{' '}
                  {`
              {
                "Left": "Key",
                "Operator": "Equals",
                "Right": "Value"
              }
              `}
                </LitegraphText>
              </>
            }
          >
            <JsonEditorWithAce
              key={uniqueKey.current}
              value={form.getFieldValue('expr')}
              onChange={(json: any) => {
                form.setFieldsValue({ expr: json });
              }}
              enableSort={false}
              enableTransform={false}
              mode="code"
              data-testid="node-data-input"
            />
          </LitegraphFormItem>

          {/* <LitegraphFormItem
          label="Embeddings"
          name="embeddings"
          rules={[{ validator: validateAtLeastOne(form) }]}
        >
          <JsonEditorWithAce
            key={uniqueKey.current}
            value={form.getFieldValue('embeddings') || {}}
            onChange={(json: any) => {
              form.setFieldsValue({ embeddings: json });
            }}
            enableSort={false}
            enableTransform={false}
            mode="code"
            data-testid="vector-search-input"
          />
        </LitegraphFormItem> */}
        </Form>
      )}
    </LitegraphModal>
  );
};

export default SearchModal;
