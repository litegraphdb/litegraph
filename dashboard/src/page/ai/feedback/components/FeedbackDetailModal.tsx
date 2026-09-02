'use client';
import React, { useMemo } from 'react';
import { useTranslations } from 'next-intl';
import { Descriptions, Skeleton, Tag } from 'antd';
import { DislikeOutlined, LikeOutlined } from '@ant-design/icons';
import LitegraphModal from '@/components/base/modal/Modal';
import LitegraphFlex from '@/components/base/flex/Flex';
import LitegraphText from '@/components/base/typograpghy/Text';
import { ChatFeedback } from '@/lib/sdk/chat';
import { useListChatThreadTurnsQuery } from '@/lib/store/slice/slice';
import { formatDateTime } from '@/utils/dateUtils';
import Markdown from '@/page/ai/chat/components/Markdown';

interface FeedbackDetailModalProps {
  tenantGuid: string;
  feedback: ChatFeedback;
  onClose: () => void;
}

/** Full-context feedback drill-down: rating, comment, and the rated exchange. */
const FeedbackDetailModal = ({ tenantGuid, feedback, onClose }: FeedbackDetailModalProps) => {
  const t = useTranslations('ai.feedback');
  const { data: turnsEnvelope, isLoading } = useListChatThreadTurnsQuery({
    tenantGuid,
    threadGuid: feedback.ThreadGUID,
  });

  const turn = useMemo(
    () =>
      (turnsEnvelope?.Objects ?? []).find((candidate) => candidate.GUID === feedback.TurnGUID) ||
      null,
    [turnsEnvelope, feedback.TurnGUID]
  );

  return (
    <LitegraphModal
      title={t('detail.title')}
      open
      onCancel={onClose}
      onOk={onClose}
      cancelButtonProps={{ style: { display: 'none' } }}
      okText={t('detail.close')}
      width={960}
      data-testid="feedback-detail-modal"
    >
      <LitegraphFlex vertical gap={16}>
        <Descriptions column={2} size="small" bordered>
          <Descriptions.Item label={t('detail.rating')}>
            {feedback.Rating === 'ThumbsUp' ? (
              <Tag icon={<LikeOutlined />} color="green">
                {t('ratings.thumbsUp')}
              </Tag>
            ) : (
              <Tag icon={<DislikeOutlined />} color="red">
                {t('ratings.thumbsDown')}
              </Tag>
            )}
          </Descriptions.Item>
          <Descriptions.Item label={t('detail.created')}>
            {formatDateTime(feedback.CreatedUtc)}
          </Descriptions.Item>
          <Descriptions.Item label={t('detail.comment')} span={2}>
            {feedback.FeedbackText || (
              <LitegraphText style={{ color: 'var(--ant-color-text-tertiary)' }}>
                {t('detail.noComment')}
              </LitegraphText>
            )}
          </Descriptions.Item>
        </Descriptions>

        {isLoading ? (
          <Skeleton active paragraph={{ rows: 3 }} />
        ) : turn ? (
          <>
            <div>
              <LitegraphText
                style={{ display: 'block', fontWeight: 600, marginBottom: 4, fontSize: 13 }}
              >
                {t('detail.userMessage')}
              </LitegraphText>
              <div
                style={{
                  background: 'var(--ant-color-fill-quaternary)',
                  borderRadius: 8,
                  padding: 10,
                  fontSize: 12.5,
                  whiteSpace: 'pre-wrap',
                  maxHeight: 240,
                  overflowY: 'auto',
                }}
              >
                {turn.UserMessage}
              </div>
            </div>
            <div>
              <LitegraphText
                style={{ display: 'block', fontWeight: 600, marginBottom: 4, fontSize: 13 }}
              >
                {t('detail.assistantResponse')}
              </LitegraphText>
              <div
                style={{
                  background: 'var(--ant-color-fill-quaternary)',
                  borderRadius: 8,
                  padding: 10,
                  fontSize: 12.5,
                  maxHeight: 330,
                  overflowY: 'auto',
                }}
                data-testid="feedback-assistant-markdown"
              >
                <Markdown content={turn.AssistantResponse || ''} />
              </div>
            </div>
          </>
        ) : (
          <LitegraphText style={{ color: 'var(--ant-color-text-tertiary)', fontSize: 12.5 }}>
            {t('detail.turnMissing')}
          </LitegraphText>
        )}
      </LitegraphFlex>
    </LitegraphModal>
  );
};

export default FeedbackDetailModal;
