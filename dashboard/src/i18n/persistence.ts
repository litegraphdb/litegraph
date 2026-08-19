'use client';

/**
 * Client-side persistence for the active UI locale.
 *
 * The selected locale is stored in two places so it survives reloads and is
 * available before first paint:
 *  - `localStorage` (alongside the other dashboard preferences), and
 *  - a `NEXT_LOCALE` cookie so the value is also readable by the server.
 */

import { localStorageKeys } from '@/constants/constant';
import { AppLocale, DEFAULT_LOCALE, canonicalizeLocale } from './locales';

/** Cookie name used to persist the locale. */
export const LOCALE_COOKIE = 'NEXT_LOCALE';

/** Reads the persisted locale, preferring localStorage then the cookie. */
export const getStoredLocale = (): AppLocale => {
  if (typeof window === 'undefined') return DEFAULT_LOCALE;
  try {
    const fromStorage = window.localStorage.getItem(localStorageKeys.locale);
    if (fromStorage) return canonicalizeLocale(fromStorage);
    const fromCookie = readLocaleCookie();
    if (fromCookie) return canonicalizeLocale(fromCookie);
  } catch {
    // Access to storage can throw in private-mode / SSR edge cases; fall back.
  }
  return DEFAULT_LOCALE;
};

/** Persists the locale to both localStorage and the cookie. */
export const setStoredLocale = (locale: AppLocale): void => {
  if (typeof window === 'undefined') return;
  try {
    window.localStorage.setItem(localStorageKeys.locale, locale);
  } catch {
    // Ignore storage write failures.
  }
  try {
    // 1 year, root path so every dashboard route sees it.
    document.cookie = `${LOCALE_COOKIE}=${locale}; path=/; max-age=31536000; samesite=lax`;
  } catch {
    // Ignore cookie write failures.
  }
};

const readLocaleCookie = (): string | null => {
  if (typeof document === 'undefined') return null;
  const match = document.cookie.match(new RegExp(`(?:^|; )${LOCALE_COOKIE}=([^;]*)`));
  return match ? decodeURIComponent(match[1]) : null;
};
