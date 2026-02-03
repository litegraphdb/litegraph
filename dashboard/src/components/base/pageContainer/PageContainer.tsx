import React from 'react';
import classNames from 'classnames';
import LitegraphFlex from '../flex/Flex';
import LitegraphText from '../typograpghy/Text';
import styles from './pageContainer.module.scss';
import { Content } from 'antd/es/layout/layout';

const PageContainer = ({
  children,
  className,
  withoutWhiteBG = false,
  id,
  pageTitle,
  pageTitleRightContent,
}: {
  children: React.ReactNode;
  className?: string;
  withoutWhiteBG?: boolean;
  id?: string;
  pageTitle?: JSX.Element | string;
  pageTitleRightContent?: JSX.Element;
  showGraphSelector?: boolean;
}) => {
  return (
    <Content className={classNames(className, !withoutWhiteBG && styles.pageContainer)} id={id}>
      {(pageTitle || pageTitleRightContent) && (
        <>
          <LitegraphFlex
            className={styles.pageTitleContainer}
            wrap
            gap="small"
            align="center"
            justify="space-between"
            style={{ padding: '16px 24px' }}
          >
            <LitegraphText fontSize={16} weight={600} data-testid="heading">
              {pageTitle}
            </LitegraphText>
            {pageTitleRightContent}
          </LitegraphFlex>
        </>
      )}
      <div className={styles.pageContent}>{children}</div>
    </Content>
  );
};

export default PageContainer;
