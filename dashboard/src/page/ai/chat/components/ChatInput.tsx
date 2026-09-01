'use client';
import React, { useState } from 'react';
import { useTranslations } from 'next-intl';
import { Input } from 'antd';
import { SendOutlined, StopOutlined } from '@ant-design/icons';
import LitegraphButton from '@/components/base/button/Button';
import LitegraphFlex from '@/components/base/flex/Flex';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';

interface ChatInputProps {
  disabled: boolean;
  isStreaming: boolean;
  onSend: (message: string) => void;
  onStop: () => void;
}

/** Message composer with Enter-to-send and a stop button while streaming. */
const ChatInput = ({ disabled, isStreaming, onSend, onStop }: ChatInputProps) => {
  const t = useTranslations('ai.chat');
  const [value, setValue] = useState('');

  const handleSend = () => {
    const trimmed = value.trim();
    if (!trimmed || disabled || isStreaming) return;
    onSend(trimmed);
    setValue('');
  };

  return (
    <LitegraphFlex gap={8} align="flex-end" style={{ padding: 12 }} data-testid="chat-input">
      <Input.TextArea
        value={value}
        onChange={(e) => setValue(e.target.value)}
        placeholder={t('inputPlaceholder')}
        autoSize={{ minRows: 1, maxRows: 6 }}
        disabled={disabled}
        onPressEnter={(e) => {
          if (!e.shiftKey) {
            e.preventDefault();
            handleSend();
          }
        }}
        data-testid="chat-input-textarea"
      />
      {isStreaming ? (
        <LitegraphTooltip title={t('stopTooltip')}>
          <LitegraphButton
            danger
            icon={<StopOutlined />}
            onClick={onStop}
            aria-label={t('stopTooltip')}
            data-testid="chat-stop"
          />
        </LitegraphTooltip>
      ) : (
        <LitegraphTooltip title={t('sendTooltip')}>
          <LitegraphButton
            type="primary"
            icon={<SendOutlined />}
            onClick={handleSend}
            disabled={disabled || !value.trim()}
            aria-label={t('sendTooltip')}
            data-testid="chat-send"
          />
        </LitegraphTooltip>
      )}
    </LitegraphFlex>
  );
};

export default ChatInput;
