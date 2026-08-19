import type { AbstractIntlMessages } from 'next-intl';
import en from './en.json';
import es from './es.json';

/** All translation catalogs keyed by locale code. */
export const messagesByLocale: Record<string, AbstractIntlMessages> = {
  en: en as AbstractIntlMessages,
  es: es as AbstractIntlMessages,
};

/** English is the source catalog and the fallback for any missing locale. */
export const defaultMessages: AbstractIntlMessages = en as AbstractIntlMessages;

/** Returns the catalog for a locale, falling back to English. */
export const getMessages = (locale: string): AbstractIntlMessages => {
  return messagesByLocale[locale] ?? defaultMessages;
};

export type Messages = typeof en;
