export const validationRules = {
  name: [{ required: true, message: 'Graph Name is required' }],
};


export const validateVectorIndexFile = (_: any, value: string) => {
  if (!value) {
    return Promise.resolve();
  }

  if (/\s/.test(value)) {
    return Promise.reject(new Error('Vector Index File should not contain spaces!'));
  }

  if (!value.endsWith('.db')) {
    return Promise.reject(new Error('Vector Index File should end with .db!'));
  }

  return Promise.resolve();
};