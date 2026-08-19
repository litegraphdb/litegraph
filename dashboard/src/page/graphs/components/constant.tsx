type Translator = (key: string, values?: Record<string, string | number>) => string;

export const validationRules = {
  name: [{ required: true, message: 'Graph Name is required' }],
};

/**
 * Builds a vector-index file validator whose rejection messages are localized
 * via the provided translator (namespaced to `vectorIndex`).
 */
export const makeValidateVectorIndexFile = (t: Translator) => (_: any, value: string) => {
  if (!value) {
    return Promise.resolve();
  }

  if (/\s/.test(value)) {
    return Promise.reject(new Error(t('fileNoSpaces')));
  }

  if (!value.endsWith('.db')) {
    return Promise.reject(new Error(t('fileEndsDb')));
  }

  return Promise.resolve();
};
