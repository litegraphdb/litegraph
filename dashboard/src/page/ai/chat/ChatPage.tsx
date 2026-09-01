'use client';
import React, { useCallback, useEffect, useMemo, useReducer, useRef, useState } from 'react';
import { useTranslations } from 'next-intl';
import { useParams } from 'next/navigation';
import { Alert, Drawer, Grid } from 'antd';
import { MenuUnfoldOutlined } from '@ant-design/icons';
import toast from 'react-hot-toast';
import PageContainer from '@/components/base/pageContainer/PageContainer';
import LitegraphButton from '@/components/base/button/Button';
import LitegraphFlex from '@/components/base/flex/Flex';
import LitegraphText from '@/components/base/typograpghy/Text';
import ConfirmationModal from '@/components/confirmation-modal/ConfirmationModal';
import { useCan } from '@/hooks/permissionHooks';
import { useAppDynamicNavigation } from '@/hooks/hooks';
import { paths } from '@/constants/constant';
import { globalToastId } from '@/constants/config';
import { ChatFeedbackRating, ChatThread, ChatTurn } from '@/lib/sdk/chat';
import { streamChatCompletion } from '@/lib/sdk/chatSse';
import {
  useDeleteChatThreadMutation,
  useGetChatSettingsQuery,
  useListChatThreadTurnsQuery,
  useListChatThreadsQuery,
} from '@/lib/store/slice/slice';
import {
  chatStreamReducer,
  initialChatStreamState,
  parseToolTranscript,
  ChatExchange,
} from './chatStream';
import MessageList, { ChatDisplayItem } from './components/MessageList';
import ThreadSidebar from './components/ThreadSidebar';
import ChatInput from './components/ChatInput';
import FeedbackModal from './components/FeedbackModal';
import NewThreadModal from './components/NewThreadModal';

const turnToDisplayItem = (turn: ChatTurn): ChatDisplayItem => ({
  key: turn.GUID,
  turnGuid: turn.GUID,
  userMessage: turn.UserMessage || '',
  assistant: turn.AssistantResponse || '',
  thinking: turn.Reasoning || '',
  tools: parseToolTranscript(turn.ToolTranscriptJson),
  retrieval: [],
  error: turn.Success ? null : turn.Error || '',
  streaming: false,
});

const exchangeToDisplayItem = (exchange: ChatExchange, streaming: boolean): ChatDisplayItem => ({
  key: exchange.localId,
  turnGuid: exchange.turnGuid,
  userMessage: exchange.userMessage,
  assistant: exchange.assistant,
  thinking: exchange.thinking,
  tools: exchange.tools,
  retrieval: exchange.retrieval,
  error: exchange.error,
  streaming,
});

const ChatPage = () => {
  const t = useTranslations('ai.chat');
  const params = useParams();
  const tenantGuid = (params?.tenantId as string) || '';
  const { can } = useCan();
  const { serializePath } = useAppDynamicNavigation();
  const screens = Grid.useBreakpoint();
  const isNarrow = !screens.md;
  // Respect the document direction (fa is RTL): the thread drawer opens from
  // the inline-start edge rather than a hardcoded physical side.
  const isRtl = typeof document !== 'undefined' && document.documentElement.dir === 'rtl';

  const isAdmin = can('view', 'aiEndpoints');

  const { data: chatSettings, isLoading: isSettingsLoading } = useGetChatSettingsQuery(
    { tenantGuid },
    { skip: !tenantGuid }
  );
  const {
    data: threads = [],
    isLoading: isThreadsLoading,
    refetch: refetchThreads,
  } = useListChatThreadsQuery({ tenantGuid }, { skip: !tenantGuid });

  const [selectedThreadGuid, setSelectedThreadGuid] = useState<string | null>(null);
  const {
    data: serverTurns = [],
    refetch: refetchTurns,
  } = useListChatThreadTurnsQuery(
    { tenantGuid, threadGuid: selectedThreadGuid as string },
    { skip: !tenantGuid || !selectedThreadGuid }
  );

  const [streamState, dispatch] = useReducer(chatStreamReducer, initialChatStreamState);
  const [isStreaming, setIsStreaming] = useState(false);
  const abortRef = useRef<AbortController | null>(null);
  const localIdRef = useRef(0);

  const [isNewThreadModalOpen, setIsNewThreadModalOpen] = useState(false);
  const [threadToDelete, setThreadToDelete] = useState<ChatThread | null>(null);
  const [feedbackTarget, setFeedbackTarget] = useState<{
    turnGuid: string;
    rating: ChatFeedbackRating;
  } | null>(null);
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);

  const [deleteThread, { isLoading: isDeletingThread }] = useDeleteChatThreadMutation();

  // Reconcile locally-completed exchanges against server turns after refetch.
  useEffect(() => {
    if (serverTurns.length > 0) {
      dispatch({
        type: 'pruneAgainstServer',
        serverTurnGuids: serverTurns.map((turn) => turn.GUID),
      });
    }
  }, [serverTurns]);

  const chatEnabled = chatSettings ? chatSettings.EnableChat !== false : true;
  const hasCompletionEndpoint = chatSettings
    ? !!chatSettings.DefaultCompletionEndpointGUID
    : true;
  const chatAvailable = chatEnabled && hasCompletionEndpoint;

  const selectThread = useCallback((threadGuid: string | null) => {
    abortRef.current?.abort();
    setIsStreaming(false);
    dispatch({ type: 'reset' });
    setSelectedThreadGuid(threadGuid);
    setIsDrawerOpen(false);
  }, []);

  const handleSend = useCallback(
    async (message: string) => {
      if (!tenantGuid || isStreaming || !chatAvailable) return;
      localIdRef.current += 1;
      const localId = `local-${localIdRef.current}`;
      dispatch({ type: 'sendStarted', localId, userMessage: message, threadGuid: selectedThreadGuid });
      setIsStreaming(true);
      const controller = new AbortController();
      abortRef.current = controller;
      let startedThreadGuid: string | null = null;
      try {
        await streamChatCompletion(
          tenantGuid,
          { ThreadGUID: selectedThreadGuid, Message: message, Stream: true },
          (event) => {
            if (event.event === 'started') {
              startedThreadGuid = event.threadGuid;
            }
            dispatch({ type: 'sseEvent', event });
          },
          controller.signal
        );
      } catch (error: unknown) {
        if (!(error instanceof DOMException && error.name === 'AbortError')) {
          const messageText = error instanceof Error ? error.message : String(error);
          dispatch({ type: 'streamFailed', message: messageText });
        } else {
          dispatch({ type: 'streamFailed', message: t('stopped') });
        }
      } finally {
        setIsStreaming(false);
        abortRef.current = null;
        // A new implicit thread was created server-side: adopt it; the turns
        // query starts automatically once the skip flag flips.
        if (startedThreadGuid && !selectedThreadGuid) {
          setSelectedThreadGuid(startedThreadGuid);
        }
        // Titles are generated after the first exchange; refresh both lists.
        refetchThreads();
        if (selectedThreadGuid) {
          refetchTurns();
        }
      }
    },
    [tenantGuid, isStreaming, chatAvailable, selectedThreadGuid, refetchThreads, refetchTurns, t]
  );

  const handleStop = useCallback(() => {
    abortRef.current?.abort();
  }, []);

  const handleDeleteThread = async () => {
    if (!threadToDelete) return;
    const { error } = await deleteThread({ tenantGuid, threadGuid: threadToDelete.GUID });
    if (error) {
      toast.error(t('deleteThreadFailed'), { id: globalToastId });
      return;
    }
    toast.success(t('deleteThreadDone'), { id: globalToastId });
    if (threadToDelete.GUID === selectedThreadGuid) {
      selectThread(null);
    }
    setThreadToDelete(null);
  };

  const displayItems = useMemo<ChatDisplayItem[]>(() => {
    const items: ChatDisplayItem[] = serverTurns.map(turnToDisplayItem);
    const serverGuids = new Set(serverTurns.map((turn) => turn.GUID));
    for (const exchange of streamState.completed) {
      if (exchange.turnGuid && serverGuids.has(exchange.turnGuid)) continue;
      items.push(exchangeToDisplayItem(exchange, false));
    }
    if (streamState.live) {
      items.push(exchangeToDisplayItem(streamState.live, true));
    }
    return items;
  }, [serverTurns, streamState]);

  const usage =
    streamState.live?.usage ??
    (streamState.completed.length > 0
      ? streamState.completed[streamState.completed.length - 1].usage
      : null);

  const sidebar = (
    <ThreadSidebar
      threads={threads}
      isLoading={isThreadsLoading}
      selectedThreadGuid={selectedThreadGuid}
      onSelect={(threadGuid) => selectThread(threadGuid)}
      onNewThread={() => setIsNewThreadModalOpen(true)}
      onDeleteThread={(thread) => setThreadToDelete(thread)}
      disabled={!chatAvailable}
    />
  );

  const disabledBanner = !isSettingsLoading && !chatAvailable && (
    <Alert
      type="warning"
      showIcon
      style={{ margin: 12 }}
      message={!chatEnabled ? t('disabled.chatOffTitle') : t('disabled.noEndpointTitle')}
      description={
        <LitegraphFlex vertical gap={8}>
          <LitegraphText>
            {!chatEnabled
              ? isAdmin
                ? t('disabled.chatOffAdmin')
                : t('disabled.chatOffUser')
              : isAdmin
                ? t('disabled.noEndpointAdmin')
                : t('disabled.noEndpointUser')}
          </LitegraphText>
          {isAdmin && (
            <LitegraphFlex gap={8} wrap="wrap">
              <a href={serializePath(paths.aiEndpoints)} data-testid="chat-goto-endpoints">
                {t('disabled.goToEndpoints')}
              </a>
              <a href={serializePath(paths.aiSettings)} data-testid="chat-goto-settings">
                {t('disabled.goToSettings')}
              </a>
            </LitegraphFlex>
          )}
        </LitegraphFlex>
      }
      data-testid="chat-disabled-banner"
    />
  );

  return (
    <PageContainer
      id="ai-chat"
      pageTitle={t('title')}
      pageTitleRightContent={
        isNarrow ? (
          <LitegraphButton
            icon={<MenuUnfoldOutlined />}
            onClick={() => setIsDrawerOpen(true)}
            data-testid="chat-open-threads"
          >
            {t('threads')}
          </LitegraphButton>
        ) : undefined
      }
    >
      <LitegraphFlex
        style={{ height: 'calc(100vh - 220px)', minHeight: 380 }}
        data-testid="chat-panel"
      >
        {!isNarrow && (
          <div
            style={{
              width: 280,
              flexShrink: 0,
              borderInlineEnd: '1px solid var(--ant-color-border-secondary)',
              height: '100%',
              minHeight: 0,
            }}
          >
            {sidebar}
          </div>
        )}
        <LitegraphFlex vertical style={{ flex: 1, minWidth: 0, height: '100%', minHeight: 0 }}>
          {disabledBanner}
          <div style={{ flex: 1, minHeight: 0, overflowY: 'auto' }} data-testid="chat-scroll">
            {displayItems.length === 0 && !isStreaming ? (
              <LitegraphFlex
                vertical
                align="center"
                justify="center"
                style={{ height: '100%', padding: 24, textAlign: 'center' }}
              >
                <LitegraphText style={{ fontSize: 15, fontWeight: 600, marginBottom: 4 }}>
                  {t('emptyTitle')}
                </LitegraphText>
                <LitegraphText style={{ color: 'var(--ant-color-text-secondary)', fontSize: 13 }}>
                  {t('emptyBody')}
                </LitegraphText>
              </LitegraphFlex>
            ) : (
              <MessageList
                items={displayItems}
                onFeedback={(turnGuid, rating) => setFeedbackTarget({ turnGuid, rating })}
              />
            )}
          </div>
          {usage && (
            <LitegraphFlex
              gap={16}
              wrap="wrap"
              style={{
                paddingInline: 16,
                paddingBlock: 4,
                borderTop: '1px solid var(--ant-color-border-secondary)',
                fontSize: 11.5,
                color: 'var(--ant-color-text-tertiary)',
              }}
              data-testid="chat-status-bar"
            >
              <span>
                {t('status.tokens', {
                  prompt: usage.PromptTokens ?? 0,
                  completion: usage.CompletionTokens ?? 0,
                })}
              </span>
              {usage.TimeToFirstTokenMs != null && (
                <span>{t('status.ttft', { ms: Math.round(usage.TimeToFirstTokenMs) })}</span>
              )}
              {usage.TokensPerSecondOverall != null && (
                <span>{t('status.tps', { tps: usage.TokensPerSecondOverall.toFixed(1) })}</span>
              )}
              <span>{t('status.duration', { ms: Math.round(usage.TotalDurationMs) })}</span>
            </LitegraphFlex>
          )}
          <ChatInput
            disabled={!chatAvailable}
            isStreaming={isStreaming}
            onSend={handleSend}
            onStop={handleStop}
          />
        </LitegraphFlex>
      </LitegraphFlex>

      {isNarrow && (
        <Drawer
          open={isDrawerOpen}
          onClose={() => setIsDrawerOpen(false)}
          title={t('threads')}
          placement={isRtl ? 'right' : 'left'}
          width={300}
          styles={{ body: { padding: 8 } }}
        >
          {sidebar}
        </Drawer>
      )}

      {isNewThreadModalOpen && (
        <NewThreadModal
          tenantGuid={tenantGuid}
          onClose={() => setIsNewThreadModalOpen(false)}
          onCreated={(threadGuid) => selectThread(threadGuid)}
        />
      )}

      {feedbackTarget && (
        <FeedbackModal
          tenantGuid={tenantGuid}
          turnGuid={feedbackTarget.turnGuid}
          rating={feedbackTarget.rating}
          onClose={() => setFeedbackTarget(null)}
        />
      )}

      <ConfirmationModal
        open={!!threadToDelete}
        title={t('deleteThreadTitle')}
        content={t('deleteThreadBody', { title: threadToDelete?.Title || t('untitledThread') })}
        onCancel={() => setThreadToDelete(null)}
        onConfirm={handleDeleteThread}
        loading={isDeletingThread}
      />
    </PageContainer>
  );
};

export default ChatPage;
