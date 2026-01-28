import React from 'react';
import LitegraphFlex from '@/components/base/flex/Flex';
import ThemeModeSwitch from '@/components/theme-mode-switch/ThemeModeSwitch';
import classNames from 'classnames';
import Link from 'next/link';
import LitegraphTitle from '../base/typograpghy/Title';
import LitegraphParagraph from '../base/typograpghy/Paragraph';
import { paths } from '@/constants/constant';
import styles from './login-layout.module.scss';
import LitegraphText from '../base/typograpghy/Text';

const LoginLayout = ({ children, isAdmin }: { children: React.ReactNode; isAdmin?: boolean }) => {
  return (
    <LitegraphFlex className={styles.userLoginPage} vertical gap={20}>
      <LitegraphFlex
        className={classNames(styles.userLoginPageHeader, 'mb pb pt pr pl')}
        align="center"
        justify="space-between"
      >
        <img src="/favicon.png" alt="Litegraph Logo" height={40} />
        <LitegraphFlex align="center" gap={10}>
          <ThemeModeSwitch />
          {isAdmin ? (
            <Link href={paths.login}>
              <LitegraphText>Login as User</LitegraphText>
            </Link>
          ) : (
            <Link href={paths.adminLogin}>
              <LitegraphText>Login as Administrator</LitegraphText>
            </Link>
          )}
        </LitegraphFlex>
      </LitegraphFlex>
      <div className={styles.loginTitle}>
        <LitegraphTitle fontSize={22} weight={600}>
          {isAdmin ? 'Admin Login' : 'Login'}
        </LitegraphTitle>
        <LitegraphParagraph className={styles.loginDescription}>
          {isAdmin
            ? 'Please enter your access key to login'
            : 'Please enter your email and password to login'}
        </LitegraphParagraph>
      </div>
      <div className={styles.loginBox}>{children}</div>
    </LitegraphFlex>
  );
};

export default LoginLayout;
