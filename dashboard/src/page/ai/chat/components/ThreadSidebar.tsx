'use client';
import React from 'react';
import { useTranslations } from 'next-intl';
import { List, Skeleton } from 'antd';
import { DeleteOutlined, PlusOutlined, ShareAltOutlined } from '@ant-design/icons';
import LitegraphButton from '@/components/base/button/Button';
import LitegraphFlex from '@/components/base/flex/Flex';
import LitegraphText from '@/components/base/typograpghy/Text';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';
import { ChatThread } from '@/lib/sdk/chat';
import { formatDateTime } from '@/utils/dateUtils';

interface ThreadSidebarProps {
  threads: ChatThread[];
  isLoading: boolean;
  selectedThreadGuid: string | null;
  onSelect: (threadGuid: string) => void;
  onNewThread: () => void;
  onDeleteThread: (thread: ChatThread) => void;
  disabled?: boolean;
}

/** Thread list pane for the chat panel: select, create, and delete threads. */
const ThreadSidebar = ({
  threads,
  isLoading,
  selectedThreadGuid,
  onSelect,
  onNewThread,
  onDeleteThread,
  disabled,
}: ThreadSidebarProps) => {
  const t = useTranslations('ai.chat');

  return (
    <LitegraphFlex vertical style={{ height: '100%', minHeight: 0 }} data-testid="chat-thread-sidebar">
      <div style={{ padding: '8px 8px 12px' }}>
        <LitegraphButton
          type="primary"
          block
          icon={<PlusOutlined />}
          onClick={onNewThread}
          disabled={disabled}
          data-testid="chat-new-thread"
        >
          {t('newThread')}
        </LitegraphButton>
      </div>
      <div style={{ flex: 1, minHeight: 0, overflowY: 'auto', paddingInline: 4 }}>
        {isLoading ? (
          <div style={{ padding: 8 }}>
            <Skeleton active paragraph={{ rows: 4 }} title={false} />
          </div>
        ) : (
          <List
            size="small"
            dataSource={threads}
            locale={{ emptyText: t('noThreads') }}
            renderItem={(thread) => {
              const selected = thread.GUID === selectedThreadGuid;
              return (
                <List.Item
                  key={thread.GUID}
                  onClick={() => onSelect(thread.GUID)}
                  style={{
                    cursor: 'pointer',
                    borderRadius: 8,
                    paddingInline: 10,
                    marginBottom: 2,
                    border: 'none',
                    background: selected ? 'var(--ant-color-primary-bg)' : 'transparent',
                  }}
                  data-testid={`chat-thread-${thread.GUID}`}
                  actions={[
                    <LitegraphTooltip title={t('deleteThreadTooltip')} key="delete">
                      <LitegraphButton
                        type="text"
                        size="small"
                        danger
                        icon={<DeleteOutlined />}
                        aria-label={t('deleteThreadTooltip')}
                        onClick={(e) => {
                          e.stopPropagation();
                          onDeleteThread(thread);
                        }}
                        data-testid={`chat-thread-delete-${thread.GUID}`}
                      />
                    </LitegraphTooltip>,
                  ]}
                >
                  <List.Item.Meta
                    title={
                      <LitegraphText
                        style={{
                          display: 'block',
                          overflow: 'hidden',
                          textOverflow: 'ellipsis',
                          whiteSpace: 'nowrap',
                          fontWeight: selected ? 600 : 400,
                          fontSize: 13,
                        }}
                      >
                        {thread.Title || t('untitledThread')}
                      </LitegraphText>
                    }
                    description={
                      <LitegraphFlex align="center" gap={6}>
                        {thread.GraphGUID ? (
                          <LitegraphTooltip title={t('boundToGraph')}>
                            <ShareAltOutlined style={{ fontSize: 11 }} />
                          </LitegraphTooltip>
                        ) : null}
                        <LitegraphText
                          style={{ fontSize: 11, color: 'var(--ant-color-text-tertiary)' }}
                        >
                          {formatDateTime(thread.LastUpdateUtc)}
                        </LitegraphText>
                      </LitegraphFlex>
                    }
                  />
                </List.Item>
              );
            }}
          />
        )}
      </div>
    </LitegraphFlex>
  );
};

export default ThreadSidebar;
