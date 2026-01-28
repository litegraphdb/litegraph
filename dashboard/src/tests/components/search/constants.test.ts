import '@testing-library/jest-dom';
import { FormInstance } from 'antd';
import { initialSearchData, validateAtLeastOne } from '@/components/search/constants';
import { SearchData } from '@/components/search/type';

// Mock FormInstance
const createMockFormInstance = (): FormInstance<SearchData> => ({
  getFieldsValue: jest.fn(),
  getFieldValue: jest.fn(),
  getFieldError: jest.fn(),
  getFieldsError: jest.fn(),
  isFieldsTouched: jest.fn(),
  isFieldTouched: jest.fn(),
  isFieldValidating: jest.fn(),
  isFieldsValidating: jest.fn(),
  resetFields: jest.fn(),
  setFields: jest.fn(),
  setFieldsValue: jest.fn(),
  validateFields: jest.fn(),
  submit: jest.fn(),
  getInternalHooks: jest.fn(),
  registerWatch: jest.fn(),
  scrollToField: jest.fn(),
  getFieldInstance: jest.fn(),
  getInternalHooks: jest.fn(),
  getContainer: jest.fn(),
  getFieldsValue: jest.fn(),
  getFieldValue: jest.fn(),
  getFieldError: jest.fn(),
  getFieldsError: jest.fn(),
  isFieldsTouched: jest.fn(),
  isFieldTouched: jest.fn(),
  isFieldValidating: jest.fn(),
  isFieldsValidating: jest.fn(),
  resetFields: jest.fn(),
  setFields: jest.fn(),
  setFieldsValue: jest.fn(),
  validateFields: jest.fn(),
  submit: jest.fn(),
  registerWatch: jest.fn(),
  scrollToField: jest.fn(),
  getFieldInstance: jest.fn(),
  getContainer: jest.fn(),
});

describe('Search Constants', () => {
  describe('initialSearchData', () => {
    it('should have the correct initial structure', () => {
      expect(initialSearchData).toEqual({
        expr: null,
        tags: [],
        labels: [],
        embeddings: null,
      });
    });

    it('should have expr as null', () => {
      expect(initialSearchData.expr).toBeNull();
    });

    it('should have tags as empty array', () => {
      expect(initialSearchData.tags).toEqual([]);
    });

    it('should have labels as empty array', () => {
      expect(initialSearchData.labels).toEqual([]);
    });

    it('should have embeddings as null', () => {
      expect(initialSearchData.embeddings).toBeNull();
    });
  });

  describe('validateAtLeastOne', () => {
    let mockForm: FormInstance<SearchData>;
    let validator: ReturnType<typeof validateAtLeastOne>;

    beforeEach(() => {
      mockForm = createMockFormInstance();
      validator = validateAtLeastOne(mockForm);
    });

    it('should resolve when expr has content', async () => {
      (mockForm.getFieldsValue as jest.Mock).mockReturnValue({
        expr: { field: 'value' },
        tags: [],
        labels: [],
        embeddings: null,
      });

      await expect(validator(undefined, undefined)).resolves.toBeUndefined();
    });

    it('should resolve when tags has content', async () => {
      (mockForm.getFieldsValue as jest.Mock).mockReturnValue({
        expr: null,
        tags: ['tag1', 'tag2'],
        labels: [],
        embeddings: null,
      });

      await expect(validator(undefined, undefined)).resolves.toBeUndefined();
    });

    it('should resolve when labels has content', async () => {
      (mockForm.getFieldsValue as jest.Mock).mockReturnValue({
        expr: null,
        tags: [],
        labels: ['label1'],
        embeddings: null,
      });

      await expect(validator(undefined, undefined)).resolves.toBeUndefined();
    });

    it('should resolve when embeddings has content', async () => {
      (mockForm.getFieldsValue as jest.Mock).mockReturnValue({
        expr: null,
        tags: [],
        labels: [],
        embeddings: { vector: [1, 2, 3] },
      });

      await expect(validator(undefined, undefined)).resolves.toBeUndefined();
    });

    it('should resolve when multiple fields have content', async () => {
      (mockForm.getFieldsValue as jest.Mock).mockReturnValue({
        expr: { field: 'value' },
        tags: ['tag1'],
        labels: ['label1'],
        embeddings: { vector: [1, 2, 3] },
      });

      await expect(validator(undefined, undefined)).resolves.toBeUndefined();
    });

    it('should reject when all fields are empty', async () => {
      (mockForm.getFieldsValue as jest.Mock).mockReturnValue({
        expr: null,
        tags: [],
        labels: [],
        embeddings: null,
      });

      await expect(validator(undefined, undefined)).rejects.toThrow(
        'At least one field is required.'
      );
    });

    it('should reject when all fields are empty objects/arrays', async () => {
      (mockForm.getFieldsValue as jest.Mock).mockReturnValue({
        expr: {},
        tags: [],
        labels: [],
        embeddings: {},
      });

      await expect(validator(undefined, undefined)).rejects.toThrow(
        'At least one field is required.'
      );
    });

    it('should reject when expr is empty object', async () => {
      (mockForm.getFieldsValue as jest.Mock).mockReturnValue({
        expr: {},
        tags: [],
        labels: [],
        embeddings: null,
      });

      await expect(validator(undefined, undefined)).rejects.toThrow(
        'At least one field is required.'
      );
    });

    it('should reject when embeddings is empty object', async () => {
      (mockForm.getFieldsValue as jest.Mock).mockReturnValue({
        expr: null,
        tags: [],
        labels: [],
        embeddings: {},
      });

      await expect(validator(undefined, undefined)).rejects.toThrow(
        'At least one field is required.'
      );
    });

    it('should resolve when tags has non-empty array', async () => {
      (mockForm.getFieldsValue as jest.Mock).mockReturnValue({
        expr: null,
        tags: ['tag1'],
        labels: [],
        embeddings: null,
      });

      await expect(validator(undefined, undefined)).resolves.toBeUndefined();
    });

    it('should resolve when labels has non-empty array', async () => {
      (mockForm.getFieldsValue as jest.Mock).mockReturnValue({
        expr: null,
        tags: [],
        labels: ['label1'],
        embeddings: null,
      });

      await expect(validator(undefined, undefined)).resolves.toBeUndefined();
    });

    it('should handle edge case with undefined values', async () => {
      (mockForm.getFieldsValue as jest.Mock).mockReturnValue({
        expr: undefined,
        tags: undefined,
        labels: undefined,
        embeddings: undefined,
      });

      await expect(validator(undefined, undefined)).rejects.toThrow(
        'At least one field is required.'
      );
    });

    it('should handle edge case with null values in arrays', async () => {
      (mockForm.getFieldsValue as jest.Mock).mockReturnValue({
        expr: null,
        tags: [null, undefined],
        labels: [null],
        embeddings: null,
      });

      // Arrays with null/undefined values still have length > 0, so validation passes
      await expect(validator(undefined, undefined)).resolves.toBeUndefined();
    });

    it('should call getFieldsValue with correct parameters', async () => {
      (mockForm.getFieldsValue as jest.Mock).mockReturnValue({
        expr: { field: 'value' },
        tags: [],
        labels: [],
        embeddings: null,
      });

      await validator(undefined, undefined);

      expect(mockForm.getFieldsValue).toHaveBeenCalledWith();
    });
  });
});
