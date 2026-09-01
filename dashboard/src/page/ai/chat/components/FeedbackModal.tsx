'use client';
import React, { useState } from 'react';
import { useTranslations } from 'next-intl';
import { Input } from 'antd';
import { DislikeOutlined, LikeOutlined } from '@ant-design/icons';
import toast from 'react-hot-toast';
import LitegraphModal from '@/components/base/modal/Modal';
import LitegraphFlex from '@/components/base/flex/Flex';
import LitegraphText from '@/components/base/typograpghy/Text';
import { ChatFeedbackRating } from '@/lib/sdk/chat';
import { useSubmitChatFeedbackMutation } from '@/lib/store/slice/slice';
import { globalToastId } from '@/constants/config';

interface FeedbackModalProps {
  tenantGuid: string;
  turnGuid: string;
  rating: ChatFeedbackRating;
  onClose: () => void;
}

/** Optional-comment modal shown after a thumbs up/down click. */
const FeedbackModal = ({ tenantGuid, turnGuid, rating, onClose }: FeedbackModalProps) => {
  const t = useTranslations('ai.chat');
  const [comment, setComment] = useState('');
  const [submitFeedback, { isLoading }] = useSubmitChatFeedbackMutation();

  const handleSubmit = async () => {
    const { error } = await submitFeedback({
      tenantGuid,
      turnGuid,
      body: { Rating: rating, FeedbackText: comment.trim() || null },
    });
    if (error) {
      toast.error(t('feedback.failed'), { id: globalToastId });
      return;
    }
    toast.success(t('feedback.submitted'), { id: globalToastId });
    onClose();
  };

  return (
    <LitegraphModal
      open
      title={t('feedback.title')}
      onOk={handleSubmit}
      onCancel={onClose}
      okText={t('feedback.submit')}
      confirmLoading={isLoading}
      data-testid="chat-feedback-modal"
    >
      <LitegraphFlex vertical gap={12}>
        <LitegraphFlex align="center" gap={8}>
          {rating === 'ThumbsUp' ? (
            <LikeOutlined style={{ color: 'var(--ant-color-success)' }} />
          ) : (
            <DislikeOutlined style={{ color: 'var(--ant-color-error)' }} />
          )}
          <LitegraphText>
            {rating === 'ThumbsUp' ? t('feedback.ratedUp') : t('feedback.ratedDown')}
          </LitegraphText>
        </LitegraphFlex>
        <Input.TextArea
          value={comment}
          onChange={(e) => setComment(e.target.value)}
          placeholder={t('feedback.commentPlaceholder')}
          autoSize={{ minRows: 3, maxRows: 6 }}
          data-testid="chat-feedback-comment"
        />
      </LitegraphFlex>
    </LitegraphModal>
  );
};

export default FeedbackModal;
