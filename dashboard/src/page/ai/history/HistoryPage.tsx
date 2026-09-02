'use client';
import React, { useMemo, useState } from 'react';
import { useTranslations } from 'next-intl';
import { useParams, useSearchParams } from 'next/navigation';
import { Tag } from 'antd';
import { ArrowLeftOutlined, CheckCircleFilled, CloseCircleFilled } from '@ant-design/icons';
import PageContainer from '@/components/base/pageContainer/PageContainer';
import LitegraphButton from '@/components/base/button/Button';
import LitegraphTable from '@/components/base/table/Table';
import LitegraphText from '@/components/base/typograpghy/Text';
import CopyButton from '@/components/base/copy-button/CopyButton';
import FallBack from '@/components/base/fallback/FallBack';
import { ChatThread, ChatTurn } from '@/lib/sdk/chat';
import {
  useGetAllGraphsQuery,
  useGetAllUsersQuery,
  useListChatThreadTurnsQuery,
  useListChatThreadsQuery,
} from '@/lib/store/slice/slice';
import { useAppDynamicNavigation } from '@/hooks/hooks';
import { paths } from '@/constants/constant';
import { formatDateTime } from '@/utils/dateUtils';
import { columnTooltip } from '@/utils/tooltipUtils';
import TurnDetailModal from './components/TurnDetailModal';

const HistoryPage = () => {
  const t = useTranslations('ai.history');
  const tCommon = useTranslations('common');
  const params = useParams();
  const searchParams = useSearchParams();
  const tenantGuid = (params?.tenantId as string) || '';
  const initialThreadGuid = searchParams?.get('thread') || null;

  const [selectedThreadGuid, setSelectedThreadGuid] = useState<string | null>(initialThreadGuid);
  const [turnDetail, setTurnDetail] = useState<ChatTurn | null>(null);

  const {
    data: threadsEnvelope,
    isLoading: isThreadsLoading,
    isFetching: isThreadsFetching,
    error: threadsError,
    refetch: refetchThreads,
  } = useListChatThreadsQuery({ tenantGuid, all: true }, { skip: !tenantGuid });
  const threads = useMemo(() => threadsEnvelope?.Objects ?? [], [threadsEnvelope]);

  const {
    data: turnsEnvelope,
    isLoading: isTurnsLoading,
    isFetching: isTurnsFetching,
    error: turnsError,
    refetch: refetchTurns,
  } = useListChatThreadTurnsQuery(
    { tenantGuid, threadGuid: selectedThreadGuid as string },
    { skip: !tenantGuid || !selectedThreadGuid }
  );
  const turns = useMemo(() => turnsEnvelope?.Objects ?? [], [turnsEnvelope]);

  const selectedThread = useMemo(
    () => threads.find((thread) => thread.GUID === selectedThreadGuid) || null,
    [threads, selectedThreadGuid]
  );

  const { serializePath } = useAppDynamicNavigation();
  const { data: usersEnvelope } = useGetAllUsersQuery();
  const users = usersEnvelope?.Objects ?? [];
  const { data: graphsEnvelope } = useGetAllGraphsQuery();
  const graphs = graphsEnvelope?.Objects ?? [];

  const userLabel = (userGuid: string): string => {
    const user = users.find((candidate) => candidate.GUID === userGuid);
    if (!user) return `${userGuid.slice(0, 8)}…`;
    return user.Email || [user.FirstName, user.LastName].filter(Boolean).join(' ') || user.GUID;
  };

  const threadColumns = [
    {
      title: columnTooltip(t('columns.updated'), t('columns.updatedDesc')),
      dataIndex: 'LastUpdateUtc',
      key: 'LastUpdateUtc',
      width: 170,
      render: (lastUpdateUtc: string) => formatDateTime(lastUpdateUtc),
    },
    {
      title: columnTooltip(t('columns.created'), t('columns.createdDesc')),
      dataIndex: 'CreatedUtc',
      key: 'CreatedUtc',
      width: 170,
      render: (createdUtc: string) => formatDateTime(createdUtc),
    },
    {
      title: columnTooltip(t('columns.title'), t('columns.titleDesc')),
      dataIndex: 'Title',
      key: 'Title',
      ellipsis: true,
      render: (title: string, record: ChatThread) => (
        <span style={{ display: 'inline-flex', alignItems: 'center', gap: 4, maxWidth: '100%' }}>
          <span style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
            {title || t('untitledThread')}
          </span>
          <CopyButton text={record.GUID} tooltipTitle={t('copyGuid')} />
        </span>
      ),
    },
    {
      title: columnTooltip(t('columns.user'), t('columns.userDesc')),
      dataIndex: 'UserGUID',
      key: 'UserGUID',
      width: 210,
      ellipsis: true,
      render: (userGuid: string) => (
        <a
          href={`${paths.users}?user=${userGuid}`}
          onClick={(e) => e.stopPropagation()}
          style={{ fontSize: 12.5 }}
          data-testid={`history-user-link-${userGuid}`}
        >
          {userLabel(userGuid)}
        </a>
      ),
    },
    {
      title: columnTooltip(t('columns.graph'), t('columns.graphDesc')),
      dataIndex: 'GraphGUID',
      key: 'GraphGUID',
      width: 200,
      ellipsis: true,
      render: (graphGuid?: string | null) => {
        if (!graphGuid) return <Tag>{t('noGraph')}</Tag>;
        const graph = graphs.find((candidate) => candidate.GUID === graphGuid);
        return (
          <a
            href={`${serializePath(paths.graphs)}?graph=${graphGuid}`}
            onClick={(e) => e.stopPropagation()}
            style={{ fontSize: 12.5 }}
            data-testid={`history-graph-link-${graphGuid}`}
          >
            {graph?.Name || `${graphGuid.slice(0, 8)}…`}
          </a>
        );
      },
    },
  ];

  const turnColumns = [
    {
      title: columnTooltip(t('columns.turnCreated'), t('columns.turnCreatedDesc')),
      dataIndex: 'CreatedUtc',
      key: 'CreatedUtc',
      width: 170,
      render: (createdUtc: string) => formatDateTime(createdUtc),
    },
    {
      title: columnTooltip(t('columns.userMessage'), t('columns.userMessageDesc')),
      dataIndex: 'UserMessage',
      key: 'UserMessage',
      ellipsis: true,
      render: (message?: string | null) => (
        <span style={{ fontSize: 12.5 }}>{message || '—'}</span>
      ),
    },
    {
      title: columnTooltip(t('columns.success'), t('columns.successDesc')),
      dataIndex: 'Success',
      key: 'Success',
      width: 90,
      align: 'center' as const,
      render: (success: boolean) =>
        success ? (
          <CheckCircleFilled style={{ color: 'var(--ant-color-success)' }} />
        ) : (
          <CloseCircleFilled style={{ color: 'var(--ant-color-error)' }} />
        ),
    },
    {
      title: columnTooltip(t('columns.tokens'), t('columns.tokensDesc')),
      key: 'tokens',
      width: 130,
      render: (_: unknown, record: ChatTurn) =>
        `${record.PromptTokens ?? '—'} / ${record.CompletionTokens ?? '—'}`,
    },
    {
      title: columnTooltip(t('columns.ttft'), t('columns.ttftDesc')),
      dataIndex: 'TimeToFirstTokenMs',
      key: 'TimeToFirstTokenMs',
      width: 100,
      render: (ttftMs?: number | null) => (ttftMs != null ? `${Math.round(ttftMs)} ms` : '—'),
    },
    {
      title: columnTooltip(t('columns.duration'), t('columns.durationDesc')),
      dataIndex: 'TotalDurationMs',
      key: 'TotalDurationMs',
      width: 110,
      render: (durationMs: number) => `${Math.round(durationMs)} ms`,
    },
    {
      title: columnTooltip(t('columns.toolCalls'), t('columns.toolCallsDesc')),
      dataIndex: 'ToolCallCount',
      key: 'ToolCallCount',
      width: 100,
      align: 'center' as const,
    },
  ];

  const isThreadTableLoading = isThreadsLoading || isThreadsFetching;
  const isTurnTableLoading = isTurnsLoading || isTurnsFetching;

  return (
    <PageContainer
      id="ai-history"
      pageTitle={
        selectedThreadGuid ? (
          <span style={{ display: 'inline-flex', alignItems: 'center', gap: 12 }}>
            <LitegraphButton
              icon={<ArrowLeftOutlined />}
              onClick={() => setSelectedThreadGuid(null)}
              data-testid="history-back"
            >
              {t('backToThreads')}
            </LitegraphButton>
            {t('turnsTitle', {
              title: selectedThread?.Title || t('untitledThread'),
            })}
          </span>
        ) : (
          t('title')
        )
      }
    >
      {!selectedThreadGuid ? (
        threadsError && !isThreadTableLoading ? (
          <FallBack retry={refetchThreads}>{tCommon('states.somethingWentWrong')}</FallBack>
        ) : (
          <>
            <LitegraphText
              style={{ display: 'block', marginBottom: 12, fontSize: 12.5, color: 'var(--ant-color-text-secondary)' }}
            >
              {t('subtitle')}
            </LitegraphText>
            <LitegraphTable
              loading={isThreadTableLoading}
              columns={threadColumns}
              dataSource={threads}
              rowKey="GUID"
              onRowClick={(thread: ChatThread) => setSelectedThreadGuid(thread.GUID)}
              onRefresh={refetchThreads}
              isRefreshing={isThreadTableLoading}
            />
          </>
        )
      ) : turnsError && !isTurnTableLoading ? (
        <FallBack retry={refetchTurns}>{tCommon('states.somethingWentWrong')}</FallBack>
      ) : (
        <LitegraphTable
          loading={isTurnTableLoading}
          columns={turnColumns}
          dataSource={turns}
          rowKey="GUID"
          onRowClick={(turn: ChatTurn) => setTurnDetail(turn)}
          onRefresh={refetchTurns}
          isRefreshing={isTurnTableLoading}
        />
      )}

      {turnDetail && <TurnDetailModal turn={turnDetail} onClose={() => setTurnDetail(null)} />}
    </PageContainer>
  );
};

export default HistoryPage;
