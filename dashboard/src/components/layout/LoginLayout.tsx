import React from 'react';
import { useTranslations } from 'next-intl';
import LitegraphFlex from '@/components/base/flex/Flex';
import ThemeModeSwitch from '@/components/theme-mode-switch/ThemeModeSwitch';
import classNames from 'classnames';
import LitegraphTitle from '../base/typograpghy/Title';
import LitegraphParagraph from '../base/typograpghy/Paragraph';
import styles from './login-layout.module.scss';

const LoginLayout = ({
  children,
  footer,
}: {
  children: React.ReactNode;
  footer?: React.ReactNode;
}) => {
  const t = useTranslations('login');
  return (
    <LitegraphFlex className={styles.userLoginPage} vertical gap={20}>
      <LitegraphFlex
        className={classNames(styles.userLoginPageHeader)}
        align="center"
        justify="space-between"
        style={{ padding: '16px 24px' }}
      >
        <img src="/favicon.png" alt={t('logoAlt')} height={40} />
        <LitegraphFlex align="center" gap={10}>
          <ThemeModeSwitch />
        </LitegraphFlex>
      </LitegraphFlex>
      <div className={styles.loginTitle}>
        <LitegraphTitle fontSize={22} weight={600}>
          {t('heading')}
        </LitegraphTitle>
        <LitegraphParagraph className={styles.loginDescription}>
          {t('subheading')}
        </LitegraphParagraph>
      </div>
      <div className={styles.loginBox}>{children}</div>
      {footer ? <div className={styles.loginFooter}>{footer}</div> : null}
    </LitegraphFlex>
  );
};

export default LoginLayout;
