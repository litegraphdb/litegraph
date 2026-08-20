/**
 * Locale registry for the LiteGraph dashboard.
 *
 * This is the single source of truth for the set of locales the UI supports.
 * Adding a new locale is a data-only change here plus a matching catalog under
 * `dashboard/messages/<code>.json`; no application logic needs to change.
 */

export type AppLocale =
  | 'en'
  | 'es'
  | 'pt'
  | 'fr'
  | 'de'
  | 'it'
  | 'ja'
  | 'fa'
  | 'yue'
  | 'zh';

export interface LocaleDescriptor {
  /** BCP-47 locale code used as the canonical identifier. */
  code: AppLocale;
  /** English display name of the language. */
  englishName: string;
  /** Native display name (autonym) shown in the language selector. */
  nativeName: string;
  /** Text direction for the locale. */
  dir: 'ltr' | 'rtl';
  /** Locale used when a translation key is missing. */
  fallback: AppLocale;
}

/** The single source locale and default fallback locale. */
export const DEFAULT_LOCALE: AppLocale = 'en';

/** Ordered registry of every supported locale. */
export const LOCALE_REGISTRY: LocaleDescriptor[] = [
  { code: 'en', englishName: 'English', nativeName: 'English', dir: 'ltr', fallback: 'en' },
  { code: 'es', englishName: 'Spanish', nativeName: 'Español', dir: 'ltr', fallback: 'en' },
  { code: 'pt', englishName: 'Portuguese', nativeName: 'Português', dir: 'ltr', fallback: 'en' },
  { code: 'fr', englishName: 'French', nativeName: 'Français', dir: 'ltr', fallback: 'en' },
  { code: 'de', englishName: 'German', nativeName: 'Deutsch', dir: 'ltr', fallback: 'en' },
  { code: 'it', englishName: 'Italian', nativeName: 'Italiano', dir: 'ltr', fallback: 'en' },
  { code: 'ja', englishName: 'Japanese', nativeName: '日本語', dir: 'ltr', fallback: 'en' },
  { code: 'fa', englishName: 'Farsi', nativeName: 'فارسی', dir: 'rtl', fallback: 'en' },
  { code: 'yue', englishName: 'Cantonese', nativeName: '粵語', dir: 'ltr', fallback: 'en' },
  { code: 'zh', englishName: 'Mandarin', nativeName: '中文', dir: 'ltr', fallback: 'en' },
];

/** List of supported locale codes. */
export const SUPPORTED_LOCALES: AppLocale[] = LOCALE_REGISTRY.map((locale) => locale.code);

/**
 * Canonicalize an arbitrary locale-ish string to a supported locale code.
 * Maps product aliases and region variants (e.g. `es-MX`) to a supported base
 * locale, and falls back to {@link DEFAULT_LOCALE} when nothing matches.
 */
export const canonicalizeLocale = (input?: string | null): AppLocale => {
  if (!input) return DEFAULT_LOCALE;
  const normalized = input.toLowerCase().trim();
  const base = normalized.split(/[-_]/)[0] as AppLocale;
  if (SUPPORTED_LOCALES.includes(normalized as AppLocale)) return normalized as AppLocale;
  if (SUPPORTED_LOCALES.includes(base)) return base;
  return DEFAULT_LOCALE;
};

/** Returns the text direction for a locale. */
export const getLocaleDirection = (locale: AppLocale): 'ltr' | 'rtl' => {
  return LOCALE_REGISTRY.find((entry) => entry.code === locale)?.dir ?? 'ltr';
};
