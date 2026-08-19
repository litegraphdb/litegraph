import { FormInstance } from 'antd';
import { SearchData } from './type';

export const initialSearchData = {
  expr: null,
  tags: [],
  labels: [],
  embeddings: null,
};

export const validateAtLeastOne =
  (form: FormInstance<SearchData>, t?: (...args: any[]) => string) => (_: any, value: any) => {
    const { expr, tags, labels, embeddings } = form.getFieldsValue();
    if (
      Object.keys(expr || {}).length > 0 ||
      tags?.length > 0 ||
      labels?.length > 0 ||
      Object.keys(embeddings || {}).length > 0
    ) {
      return Promise.resolve();
    }
    return Promise.reject(
      new Error(t ? t('atLeastOneRequired') : 'At least one field is required.')
    );
  };
