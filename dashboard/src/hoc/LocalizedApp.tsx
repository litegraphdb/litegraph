'use client';
import React, { useEffect } from 'react';
import { StyleProvider, createCache } from '@ant-design/cssinjs';

type Cache = ReturnType<typeof createCache>;
import { AntdRegistry } from '@ant-design/nextjs-registry';
import { ConfigProvider, Flex } from 'antd';
import enUS from 'antd/locale/en_US';
import esES from 'antd/locale/es_ES';
import ptBR from 'antd/locale/pt_BR';
import frFR from 'antd/locale/fr_FR';
import deDE from 'antd/locale/de_DE';
import itIT from 'antd/locale/it_IT';
import jaJP from 'antd/locale/ja_JP';
import faIR from 'antd/locale/fa_IR';
import zhHK from 'antd/locale/zh_HK';
import zhCN from 'antd/locale/zh_CN';
import type { Locale } from 'antd/es/locale';
import { AppstoreOutlined } from '@ant-design/icons';
import { NextIntlClientProvider } from 'next-intl';
import { Toaster } from 'react-hot-toast';
import AuthLayout from '@/components/layout/AuthLayout';
import { ThemeEnum } from '@/types/types';
import { darkTheme, primaryTheme } from '@/theme/theme';
import { useAppDispatch, useAppSelector } from '@/lib/store/hooks';
import { storeLocale } from '@/lib/store/litegraph/actions';
import { getStoredLocale } from '@/i18n/persistence';
import { getLocaleDirection } from '@/i18n/locales';
import { getMessages } from '@messages';
import { getTranslator } from '@/i18n/getTranslator';

const antdLocaleByCode: Record<string, Locale> = {
  en: enUS,
  es: esES,
  pt: ptBR,
  fr: frFR,
  de: deDE,
  it: itIT,
  ja: jaJP,
  fa: faIR,
  yue: zhHK,
  zh: zhCN,
};

/**
 * Client boundary that binds the active locale (from the LiteGraph redux slice)
 * to both next-intl and Ant Design, and re-renders the whole app when the
 * locale changes. Must be rendered inside the redux `StoreProvider`. Theme is
 * passed in as a prop by AppProviders (which owns the theme state).
 */
const LocalizedApp = ({
  children,
  cache,
  theme,
}: {
  children: React.ReactNode;
  cache: Cache;
  theme: ThemeEnum;
}) => {
  const dispatch = useAppDispatch();
  const locale = useAppSelector((state) => state.liteGraph.locale);

  // Hydrate the locale from persisted storage after mount (matches the theme
  // pattern used by AppProviders to avoid a server/client hydration mismatch).
  useEffect(() => {
    const stored = getStoredLocale();
    if (stored && stored !== locale) {
      dispatch(storeLocale(stored));
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Keep <html lang>/<dir> in sync with the active locale.
  useEffect(() => {
    if (typeof document !== 'undefined') {
      document.documentElement.lang = locale;
      document.documentElement.dir = getLocaleDirection(locale);
    }
  }, [locale]);

  const messages = getMessages(locale);
  const antdLocale = antdLocaleByCode[locale] ?? enUS;
  const tCommon = getTranslator(locale, 'common');

  return (
    <StyleProvider cache={cache} hashPriority="high">
      <AntdRegistry>
        <ConfigProvider
          theme={theme === ThemeEnum.LIGHT ? primaryTheme : darkTheme}
          locale={antdLocale}
          renderEmpty={() => (
            <Flex vertical align="center" justify="center" gap={8} style={{ padding: '16px 0' }}>
              <AppstoreOutlined
                style={{ fontSize: 24, color: 'var(--ant-color-text-quaternary)' }}
              />
              <span style={{ color: 'var(--ant-color-text-quaternary)', fontSize: 13 }}>
                {tCommon('states.noData')}
              </span>
            </Flex>
          )}
        >
          <NextIntlClientProvider locale={locale} messages={messages} timeZone="UTC">
            <AuthLayout className={theme === ThemeEnum.DARK ? 'theme-dark-mode' : ''}>
              {children}
            </AuthLayout>
            <Toaster />
          </NextIntlClientProvider>
        </ConfigProvider>
      </AntdRegistry>
    </StyleProvider>
  );
};

export default LocalizedApp;
