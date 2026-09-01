'use client';
import React, { useEffect } from 'react';
import { useTranslations } from 'next-intl';
import { Descriptions, Spin, Tag } from 'antd';
import LitegraphModal from '@/components/base/modal/Modal';
import LitegraphFlex from '@/components/base/flex/Flex';
import LitegraphText from '@/components/base/typograpghy/Text';
import { ChatEndpoint } from '@/lib/sdk/chat';
import { useTestChatEndpointMutation } from '@/lib/store/slice/slice';

interface TestEndpointModalProps {
  tenantGuid: string;
  endpoint: ChatEndpoint;
  onClose: () => void;
}

/** Runs a connectivity test on open and renders reachability/model results. */
const TestEndpointModal = ({ tenantGuid, endpoint, onClose }: TestEndpointModalProps) => {
  const t = useTranslations('ai.endpoints');
  const [runTest, { data: result, isLoading, error }] = useTestChatEndpointMutation();

  useEffect(() => {
    runTest({ tenantGuid, endpointGuid: endpoint.GUID });
  }, [runTest, tenantGuid, endpoint.GUID]);

  return (
    <LitegraphModal
      title={t('test.title', { name: endpoint.Name })}
      open
      onCancel={onClose}
      onOk={onClose}
      cancelButtonProps={{ style: { display: 'none' } }}
      okText={t('test.close')}
      data-testid="test-endpoint-modal"
    >
      {isLoading ? (
        <LitegraphFlex align="center" gap={8} style={{ padding: 24 }} justify="center">
          <Spin />
          <LitegraphText>{t('test.running')}</LitegraphText>
        </LitegraphFlex>
      ) : error || !result ? (
        <LitegraphText style={{ color: 'var(--ant-color-error)' }}>
          {t('test.failed')}
        </LitegraphText>
      ) : (
        <Descriptions column={1} size="small" bordered>
          <Descriptions.Item label={t('test.reachable')}>
            {result.Reachable ? (
              <Tag color="green">{t('test.yes')}</Tag>
            ) : (
              <Tag color="red">{t('test.no')}</Tag>
            )}
          </Descriptions.Item>
          <Descriptions.Item label={t('test.modelExists')}>
            {result.ModelExists == null ? (
              <Tag>{t('test.unknown')}</Tag>
            ) : result.ModelExists ? (
              <Tag color="green">{t('test.yes')}</Tag>
            ) : (
              <Tag color="orange">{t('test.no')}</Tag>
            )}
          </Descriptions.Item>
          <Descriptions.Item label={t('test.runtime')}>
            {`${result.RuntimeMs.toFixed(1)} ms`}
          </Descriptions.Item>
          {result.Error && (
            <Descriptions.Item label={t('test.error')}>
              <LitegraphText style={{ color: 'var(--ant-color-error)' }}>
                {result.Error}
              </LitegraphText>
            </Descriptions.Item>
          )}
          {result.Models && result.Models.length > 0 && (
            <Descriptions.Item label={t('test.models')}>
              <LitegraphFlex wrap="wrap" gap={4} style={{ maxHeight: 160, overflowY: 'auto' }}>
                {result.Models.map((model) => (
                  <Tag key={model} style={{ fontFamily: 'monospace', fontSize: 11 }}>
                    {model}
                  </Tag>
                ))}
              </LitegraphFlex>
            </Descriptions.Item>
          )}
        </Descriptions>
      )}
    </LitegraphModal>
  );
};

export default TestEndpointModal;
