'use client';
import React, { useCallback, useEffect, useMemo, useReducer, useRef, useState } from 'react';
import { useTranslations } from 'next-intl';
import { useParams } from 'next/navigation';
import { Alert, Drawer, Grid, Switch } from 'antd';
import { MenuUnfoldOutlined } from '@ant-design/icons';
import toast from 'react-hot-toast';
import PageContainer from '@/components/base/pageContainer/PageContainer';
import LitegraphButton from '@/components/base/button/Button';
import LitegraphFlex from '@/components/base/flex/Flex';
import LitegraphText from '@/components/base/typograpghy/Text';
import LitegraphSelect from '@/components/base/select/Select';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';
import ConfirmationModal from '@/components/confirmation-modal/ConfirmationModal';
import { useCan } from '@/hooks/permissionHooks';
import { useAppDynamicNavigation } from '@/hooks/hooks';
import { useAppSelector } from '@/lib/store/hooks';
import { RootState } from '@/lib/store/store';
import { paths } from '@/constants/constant';
import { globalToastId } from '@/constants/config';
import { ChatFeedbackRating, ChatThread, ChatTurn } from '@/lib/sdk/chat';
import { completeChatCompletion, streamChatCompletion } from '@/lib/sdk/chatSse';
import {
  useDeleteChatThreadMutation,
  useGetChatSettingsQuery,
  useListChatModelsQuery,
  useListChatThreadTurnsQuery,
  useListChatThreadsQuery,
  usePreloadChatEndpointMutation,
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
import RenameThreadModal from './components/RenameThreadModal';

/** A slash-command notice pinned after the display item index it was issued at. */
type ChatNotice = { id: number; afterCount: number; content: string };

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
  stats: {
    provider: turn.Provider,
    model: turn.Model,
    promptTokens: turn.PromptTokens,
    completionTokens: turn.CompletionTokens,
    ttftMs: turn.TimeToFirstTokenMs,
    ttltMs: turn.TimeToLastTokenMs,
    totalDurationMs: turn.TotalDurationMs,
    tpsOverall: turn.TokensPerSecondOverall,
    tpsGeneration: turn.TokensPerSecondGeneration,
    toolCalls: turn.ToolCallCount,
    toolIterations: turn.ToolLoopIterations,
    ragChunks: turn.RetrievedChunkCount,
    retries: turn.RetryCount,
  },
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
  stats: exchange.usage
    ? {
        provider: exchange.usage.Provider,
        model: exchange.usage.Model,
        promptTokens: exchange.usage.PromptTokens,
        completionTokens: exchange.usage.CompletionTokens,
        ttftMs: exchange.usage.TimeToFirstTokenMs,
        ttltMs: exchange.usage.TimeToLastTokenMs,
        totalDurationMs: exchange.usage.TotalDurationMs,
        tpsOverall: exchange.usage.TokensPerSecondOverall,
        toolCalls: exchange.usage.ToolCallCount,
        toolIterations: exchange.usage.ToolLoopIterations,
        ragChunks: exchange.usage.RetrievedChunkCount,
        retries: exchange.usage.RetryCount,
      }
    : null,
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
    data: threadsEnvelope,
    isLoading: isThreadsLoading,
    refetch: refetchThreads,
  } = useListChatThreadsQuery({ tenantGuid }, { skip: !tenantGuid });
  const threads = useMemo(() => threadsEnvelope?.Objects ?? [], [threadsEnvelope]);

  const [selectedThreadGuid, setSelectedThreadGuid] = useState<string | null>(null);
  const { data: serverTurnsEnvelope, refetch: refetchTurns } = useListChatThreadTurnsQuery(
    { tenantGuid, threadGuid: selectedThreadGuid as string },
    { skip: !tenantGuid || !selectedThreadGuid }
  );
  const serverTurns = useMemo(() => serverTurnsEnvelope?.Objects ?? [], [serverTurnsEnvelope]);

  const [streamState, dispatch] = useReducer(chatStreamReducer, initialChatStreamState);
  const [isStreaming, setIsStreaming] = useState(false);
  const abortRef = useRef<AbortController | null>(null);
  const localIdRef = useRef(0);
  const noticeIdRef = useRef(0);

  const selectedGraph = useAppSelector((state: RootState) => state.liteGraph.selectedGraph);
  const tenant = useAppSelector((state: RootState) => state.liteGraph.tenant);

  const { data: chatModelsEnvelope } = useListChatModelsQuery(
    { tenantGuid },
    { skip: !tenantGuid }
  );
  const completionModels = useMemo(
    () =>
      (chatModelsEnvelope?.Objects ?? []).filter((model) => model.EndpointType === 'Completion'),
    [chatModelsEnvelope]
  );
  const [completionEndpointGuid, setCompletionEndpointGuid] = useState<string | undefined>(
    undefined
  );
  const [streamingEnabled, setStreamingEnabled] = useState(true);
  const [notices, setNotices] = useState<ChatNotice[]>([]);

  const [isNewThreadModalOpen, setIsNewThreadModalOpen] = useState(false);
  const [threadToRename, setThreadToRename] = useState<ChatThread | null>(null);
  const [threadToDelete, setThreadToDelete] = useState<ChatThread | null>(null);
  const [feedbackTarget, setFeedbackTarget] = useState<{
    turnGuid: string;
    rating: ChatFeedbackRating;
  } | null>(null);
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);

  const [deleteThread, { isLoading: isDeletingThread }] = useDeleteChatThreadMutation();
  const [preloadChatEndpoint] = usePreloadChatEndpointMutation();

  // Warm the selected model on its inference server so the first completion feels
  // instant. Errors are ignored (preloading is best-effort) and the last-preloaded
  // GUID is tracked so re-selecting the same model does not spam the server.
  const lastPreloadedGuidRef = useRef<string | null>(null);
  const preloadModel = useCallback(
    (endpointGuid?: string) => {
      if (!tenantGuid || !endpointGuid) return;
      if (lastPreloadedGuidRef.current === endpointGuid) return;
      lastPreloadedGuidRef.current = endpointGuid;
      preloadChatEndpoint({ tenantGuid, endpointGuid });
    },
    [tenantGuid, preloadChatEndpoint]
  );

  const defaultCompletionModel = useMemo(
    () => completionModels.find((model) => model.IsDefault) ?? completionModels[0],
    [completionModels]
  );

  // Preload the effective default completion model once the catalog arrives; the
  // ref guard keeps refetches from re-firing it.
  const initialPreloadFiredRef = useRef(false);
  useEffect(() => {
    if (initialPreloadFiredRef.current) return;
    if (!defaultCompletionModel) return;
    initialPreloadFiredRef.current = true;
    preloadModel(defaultCompletionModel.GUID);
  }, [defaultCompletionModel, preloadModel]);

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
    setNotices([]);
    setSelectedThreadGuid(threadGuid);
    setIsDrawerOpen(false);
  }, []);

  const baseItems = useMemo<ChatDisplayItem[]>(() => {
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

  const baseItemCountRef = useRef(0);
  useEffect(() => {
    baseItemCountRef.current = baseItems.length;
  }, [baseItems]);

  const lastUsage =
    streamState.live?.usage ??
    (streamState.completed.length > 0
      ? streamState.completed[streamState.completed.length - 1].usage
      : null);

  const selectedModel = completionEndpointGuid
    ? completionModels.find((model) => model.GUID === completionEndpointGuid)
    : (completionModels.find((model) => model.IsDefault) ?? completionModels[0]);

  const pushNotice = useCallback((content: string) => {
    noticeIdRef.current += 1;
    setNotices((prev) => [
      ...prev,
      { id: noticeIdRef.current, afterCount: baseItemCountRef.current, content },
    ]);
  }, []);

  const handleSlashCommand = useCallback(
    (input: string): boolean => {
      const command = input.split(/\s/)[0].toLowerCase();
      switch (command) {
        case '/clear':
          selectThread(null);
          return true;
        case '/context': {
          const rows: string[] = [
            `| ${t('commands.contextField')} | ${t('commands.contextValue')} |`,
            '|---|---|',
            `| ${t('commands.contextTenant')} | ${tenant?.Name ? `${tenant.Name} (\`${tenantGuid}\`)` : `\`${tenantGuid}\``} |`,
            `| ${t('commands.contextGraph')} | ${selectedGraph ? `\`${selectedGraph}\`` : t('commands.none')} |`,
            `| ${t('commands.contextThread')} | ${selectedThreadGuid ? `\`${selectedThreadGuid}\`` : t('commands.none')} |`,
            `| ${t('commands.contextModel')} | ${selectedModel ? `${selectedModel.Name} (${selectedModel.Model})` : t('commands.none')} |`,
            `| ${t('commands.contextStreaming')} | ${streamingEnabled ? t('commands.on') : t('commands.off')} |`,
          ];
          if (lastUsage) {
            rows.push(
              `| ${t('commands.contextTokens')} | ${(lastUsage.PromptTokens ?? 0) + (lastUsage.CompletionTokens ?? 0)} |`
            );
          }
          pushNotice(rows.join('\n'));
          return true;
        }
        case '/help':
        case '/?':
          pushNotice(
            [
              `| ${t('commands.helpCommand')} | ${t('commands.helpDescription')} |`,
              '|---------|-------------|',
              `| \`/clear\` | ${t('commands.clearDesc')} |`,
              `| \`/context\` | ${t('commands.contextDesc')} |`,
              `| \`/?\` ${t('commands.or')} \`/help\` | ${t('commands.helpDesc')} |`,
            ].join('\n')
          );
          return true;
        default:
          pushNotice(t('commands.unknown', { command }));
          return true;
      }
    },
    [
      selectThread,
      pushNotice,
      t,
      tenant,
      tenantGuid,
      selectedGraph,
      selectedThreadGuid,
      selectedModel,
      streamingEnabled,
      lastUsage,
    ]
  );

  const handleSend = useCallback(
    async (message: string) => {
      if (!tenantGuid || isStreaming || !chatAvailable) return;
      if (message.trimStart().startsWith('/')) {
        handleSlashCommand(message.trim());
        return;
      }
      localIdRef.current += 1;
      const localId = `local-${localIdRef.current}`;
      dispatch({ type: 'sendStarted', localId, userMessage: message, threadGuid: selectedThreadGuid });
      setIsStreaming(true);
      const controller = new AbortController();
      abortRef.current = controller;
      let startedThreadGuid: string | null = null;
      const request = {
        ThreadGUID: selectedThreadGuid,
        Message: message,
        Stream: streamingEnabled,
        CompletionEndpointGUID: completionEndpointGuid || null,
        GraphGUID: selectedGraph || null,
      };
      const transport = streamingEnabled ? streamChatCompletion : completeChatCompletion;
      try {
        await transport(
          tenantGuid,
          request,
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
    [
      tenantGuid,
      isStreaming,
      chatAvailable,
      selectedThreadGuid,
      streamingEnabled,
      completionEndpointGuid,
      selectedGraph,
      handleSlashCommand,
      refetchThreads,
      refetchTurns,
      t,
    ]
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
    if (notices.length === 0) return baseItems;
    const merged: ChatDisplayItem[] = [];
    let noticeIndex = 0;
    for (let i = 0; i <= baseItems.length; i++) {
      while (noticeIndex < notices.length && notices[noticeIndex].afterCount <= i) {
        const notice = notices[noticeIndex];
        merged.push({
          key: `notice-${notice.id}`,
          turnGuid: null,
          userMessage: '',
          assistant: '',
          thinking: '',
          tools: [],
          retrieval: [],
          error: null,
          streaming: false,
          notice: notice.content,
        });
        noticeIndex += 1;
      }
      if (i < baseItems.length) merged.push(baseItems[i]);
    }
    return merged;
  }, [baseItems, notices]);

  const usage = lastUsage;

  const sidebar = (
    <ThreadSidebar
      threads={threads}
      isLoading={isThreadsLoading}
      selectedThreadGuid={selectedThreadGuid}
      onSelect={(threadGuid) => selectThread(threadGuid)}
      onNewThread={() => setIsNewThreadModalOpen(true)}
      onRenameThread={(thread) => setThreadToRename(thread)}
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
          <LitegraphFlex
            align="center"
            gap={12}
            wrap="wrap"
            style={{
              paddingInline: 16,
              paddingBlock: 6,
              borderTop: '1px solid var(--ant-color-border-secondary)',
            }}
            data-testid="chat-toolbar"
          >
            <LitegraphFlex align="center" gap={6}>
              <LitegraphText style={{ fontSize: 12, color: 'var(--ant-color-text-secondary)' }}>
                {t('toolbar.model')}
              </LitegraphText>
              <LitegraphTooltip title={t('toolbar.modelTooltip')}>
                <span>
                  <LitegraphSelect
                    size="small"
                    showSearch
                    optionFilterProp="label"
                    style={{ minWidth: 220 }}
                    placeholder={t('toolbar.modelDefault')}
                    value={completionEndpointGuid}
                    allowClear
                    onChange={(value) => {
                      const guid = (value as string) || undefined;
                      setCompletionEndpointGuid(guid);
                      // Warm the newly selected model; clearing back to the
                      // default warms the effective default instead.
                      preloadModel(guid ?? defaultCompletionModel?.GUID);
                    }}
                    options={completionModels.map((model) => ({
                      label: model.IsDefault
                        ? t('toolbar.defaultModelLabel', { name: model.Name, model: model.Model })
                        : `${model.Name} (${model.Model})`,
                      value: model.GUID,
                    }))}
                    disabled={!chatAvailable}
                    data-testid="chat-model-select"
                  />
                </span>
              </LitegraphTooltip>
            </LitegraphFlex>
            <LitegraphFlex align="center" gap={6}>
              <LitegraphTooltip title={t('toolbar.streamingTooltip')}>
                <Switch
                  size="small"
                  checked={streamingEnabled}
                  onChange={setStreamingEnabled}
                  disabled={!chatAvailable || isStreaming}
                  aria-label={t('toolbar.streaming')}
                  data-testid="chat-streaming-toggle"
                />
              </LitegraphTooltip>
              <LitegraphText style={{ fontSize: 12, color: 'var(--ant-color-text-secondary)' }}>
                {t('toolbar.streaming')}
              </LitegraphText>
            </LitegraphFlex>
          </LitegraphFlex>
          <ChatInput
            disabled={!chatAvailable}
            isStreaming={isStreaming}
            onSend={handleSend}
            onStop={handleStop}
          />
          <LitegraphText
            style={{
              fontSize: 11.5,
              color: 'var(--ant-color-text-tertiary)',
              textAlign: 'center',
              paddingBlock: 4,
            }}
            data-testid="chat-disclaimer"
          >
            {t('disclaimer')}
          </LitegraphText>
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

      {threadToRename && (
        <RenameThreadModal
          tenantGuid={tenantGuid}
          thread={threadToRename}
          onClose={() => setThreadToRename(null)}
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
