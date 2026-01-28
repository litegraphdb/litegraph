import React from 'react';
import { LoadingOutlined } from '@ant-design/icons';
import styles from './pageLoding.module.css';
import LitegraphText from '../typograpghy/Text';
import LitegraphFlex from '../flex/Flex';
import PageContainer from '../pageContainer/PageContainer';

const PageLoading = ({
  message = 'Loading...',
  withoutWhiteBG = false,
  className,
}: {
  message?: string | JSX.Element;
  withoutWhiteBG?: boolean;
  className?: string;
}) => {
  return (
    <PageContainer withoutWhiteBG={withoutWhiteBG} className={className}>
      <LitegraphFlex justify="center" align="center" vertical>
        <LitegraphText data-testid="loading-message">{message}</LitegraphText>
        <LoadingOutlined className={styles.pageLoader} />
      </LitegraphFlex>
    </PageContainer>
  );
};

export default PageLoading;
