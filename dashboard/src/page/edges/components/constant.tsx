export const validationRules = {
  Name: [{ required: true, message: 'Edge Name is required' }],
  From: [
    { required: true, message: 'From Node is required' },
    ({ getFieldValue }: any) => ({
      validator(_: any, value: any) {
        if (value && value === getFieldValue('To')) {
          return Promise.reject(new Error('From Node cannot be the same as To Node'));
        }
        return Promise.resolve();
      },
    }),
  ],
  To: [
    { required: true, message: 'To Node is required' },
    ({ getFieldValue }: any) => ({
      validator(_: any, value: any) {
        if (value && value === getFieldValue('From')) {
          return Promise.reject(new Error('To Node cannot be the same as From Node'));
        }
        return Promise.resolve();
      },
    }),
  ],
  Cost: [{ required: true, message: 'Edge cost is required' }],
};
