import { createTranslator } from 'next-intl';
import { getMessages } from '@messages';
import { AppLocale, DEFAULT_LOCALE } from './locales';

/**
 * Non-hook translator for use outside React components (utilities, store
 * side-effects, class code, etc.). Inside components prefer the
 * `useTranslations('namespace')` hook from `next-intl`.
 *
 * @example
 *   const t = getTranslator(locale, 'graphs');
 *   toast.success(t('toast.deleted'));
 */
export const getTranslator = (locale: AppLocale = DEFAULT_LOCALE, namespace?: string) => {
  return createTranslator({
    locale,
    messages: getMessages(locale),
    namespace,
  });
};
