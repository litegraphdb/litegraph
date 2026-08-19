type Translator = (key: string, values?: Record<string, string | number>) => string;

export const makeValidationRules = (t: Translator) => ({
  name: [{ required: true, message: t('form.nameRequired') }],
});
