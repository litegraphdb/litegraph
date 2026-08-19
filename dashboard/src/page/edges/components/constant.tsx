type Translator = (key: string, values?: Record<string, string | number>) => string;

export const makeValidationRules = (t: Translator) => ({
  Name: [{ required: true, message: t('form.nameRequired') }],
  From: [
    { required: true, message: t('form.fromRequired') },
    ({ getFieldValue }: any) => ({
      validator(_: any, value: any) {
        if (value && value === getFieldValue('To')) {
          return Promise.reject(new Error(t('form.fromSameAsTo')));
        }
        return Promise.resolve();
      },
    }),
  ],
  To: [
    { required: true, message: t('form.toRequired') },
    ({ getFieldValue }: any) => ({
      validator(_: any, value: any) {
        if (value && value === getFieldValue('From')) {
          return Promise.reject(new Error(t('form.toSameAsFrom')));
        }
        return Promise.resolve();
      },
    }),
  ],
  Cost: [{ required: true, message: t('form.costRequired') }],
});
