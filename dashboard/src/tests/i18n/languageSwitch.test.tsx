import React from 'react';
import { screen, fireEvent } from '@testing-library/react';
import { NextIntlClientProvider } from 'next-intl';
import { useTranslations } from 'next-intl';
import { renderWithRedux } from '@/tests/store/utils';
import { useAppDispatch, useAppSelector } from '@/lib/store/hooks';
import { storeLocale } from '@/lib/store/litegraph/actions';
import { getMessages } from '@messages';

/**
 * Bridges the redux locale to next-intl exactly like the app's LocalizedApp,
 * so a `storeLocale` dispatch re-renders translated content.
 */
const IntlBridge = ({ children }: { children: React.ReactNode }) => {
  const locale = useAppSelector((state) => state.liteGraph.locale);
  return (
    <NextIntlClientProvider locale={locale} messages={getMessages(locale)} timeZone="UTC">
      {children}
    </NextIntlClientProvider>
  );
};

const Consumer = () => {
  const t = useTranslations('graphs');
  const dispatch = useAppDispatch();
  return (
    <div>
      <span data-testid="graphs-title">{t('title')}</span>
      <button data-testid="to-es" onClick={() => dispatch(storeLocale('es'))}>
        switch
      </button>
    </div>
  );
};

describe('language switching', () => {
  it('swaps a known string from English to Spanish when the locale changes', () => {
    renderWithRedux(
      <IntlBridge>
        <Consumer />
      </IntlBridge>
    );

    // Default locale is English.
    expect(screen.getByTestId('graphs-title')).toHaveTextContent('Graphs');

    // Switch the locale via the redux action the language switcher dispatches.
    fireEvent.click(screen.getByTestId('to-es'));

    // The same key now renders the Spanish translation.
    expect(screen.getByTestId('graphs-title')).toHaveTextContent('Grafos');
    expect(screen.getByTestId('graphs-title')).not.toHaveTextContent('Graphs');
  });
});
