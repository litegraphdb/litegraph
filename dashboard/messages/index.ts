import type { AbstractIntlMessages } from 'next-intl';
import en from './en.json';
import es from './es.json';
import pt from './pt.json';
import fr from './fr.json';
import de from './de.json';
import it from './it.json';
import ja from './ja.json';
import fa from './fa.json';
import yue from './yue.json';
import zh from './zh.json';

type MessageTree = { [key: string]: string | MessageTree };

/**
 * Deep-merge a locale catalog over the English source so every locale resolves
 * the full key set. Any key present in English but missing from a locale (for
 * example a freshly added string not yet translated) falls back to the English
 * value instead of surfacing as a missing-message error.
 */
const mergeOverEnglish = (source: MessageTree, override: MessageTree): MessageTree => {
  const result: MessageTree = { ...source };
  for (const key of Object.keys(override)) {
    const overrideValue = override[key];
    const sourceValue = source[key];
    if (
      overrideValue &&
      typeof overrideValue === 'object' &&
      sourceValue &&
      typeof sourceValue === 'object'
    ) {
      result[key] = mergeOverEnglish(sourceValue as MessageTree, overrideValue as MessageTree);
    } else {
      result[key] = overrideValue;
    }
  }
  return result;
};

const englishTree = en as unknown as MessageTree;

const rawCatalogs: Record<string, MessageTree> = {
  en: englishTree,
  es: es as unknown as MessageTree,
  pt: pt as unknown as MessageTree,
  fr: fr as unknown as MessageTree,
  de: de as unknown as MessageTree,
  it: it as unknown as MessageTree,
  ja: ja as unknown as MessageTree,
  fa: fa as unknown as MessageTree,
  yue: yue as unknown as MessageTree,
  zh: zh as unknown as MessageTree,
};

/** All translation catalogs keyed by locale code, each merged over English. */
export const messagesByLocale: Record<string, AbstractIntlMessages> = Object.fromEntries(
  Object.entries(rawCatalogs).map(([code, catalog]) => [
    code,
    (code === 'en' ? englishTree : mergeOverEnglish(englishTree, catalog)) as AbstractIntlMessages,
  ])
);

/** English is the source catalog and the fallback for any missing locale. */
export const defaultMessages: AbstractIntlMessages = messagesByLocale.en;

/** Returns the catalog for a locale, falling back to English. */
export const getMessages = (locale: string): AbstractIntlMessages => {
  return messagesByLocale[locale] ?? defaultMessages;
};

export type Messages = typeof en;
