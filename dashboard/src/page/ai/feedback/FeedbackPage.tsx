'use client';
import React, { useState } from 'react';
import { useTranslations } from 'next-intl';
import { useParams } from 'next/navigation';
import { Button, Tag } from 'antd';
import { DeleteOutlined, DislikeOutlined, LikeOutlined } from '@ant-design/icons';
import toast from 'react-hot-toast';
import PageContainer from '@/components/base/pageContainer/PageContainer';
import LitegraphTable from '@/components/base/table/Table';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';
import FallBack from '@/components/base/fallback/FallBack';
import ConfirmationModal from '@/components/confirmation-modal/ConfirmationModal';
import { ChatFeedback } from '@/lib/sdk/chat';
import {
  useDeleteChatFeedbackMutation,
  useListChatFeedbackQuery,
} from '@/lib/store/slice/slice';
import { useAppDynamicNavigation } from '@/hooks/hooks';
import { paths } from '@/constants/constant';
import { globalToastId } from '@/constants/config';
import { formatDateTime } from '@/utils/dateUtils';
import { columnTooltip } from '@/utils/tooltipUtils';
import FeedbackDetailModal from './components/FeedbackDetailModal';

const excerpt = (text?: string | null, max = 80): string => {
  if (!text) return '';
  return text.length > max ? `${text.slice(0, max)}…` : text;
};

const FeedbackPage = () => {
  const t = useTranslations('ai.feedback');
  const tCommon = useTranslations('common');
  const params = useParams();
  const tenantGuid = (params?.tenantId as string) || '';
  const { serializePath } = useAppDynamicNavigation();

  const {
    data: feedbackList = [],
    isLoading,
    isFetching,
    error,
    refetch,
  } = useListChatFeedbackQuery({ tenantGuid }, { skip: !tenantGuid });
  const [deleteFeedback, { isLoading: isDeleting }] = useDeleteChatFeedbackMutation();

  const [detailTarget, setDetailTarget] = useState<ChatFeedback | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<ChatFeedback | null>(null);

  const handleDelete = async () => {
    if (!deleteTarget) return;
    const { error: deleteError } = await deleteFeedback({
      tenantGuid,
      feedbackGuid: deleteTarget.GUID,
    });
    if (deleteError) {
      toast.error(t('toast.deleteFailed'), { id: globalToastId });
      return;
    }
    toast.success(t('toast.deleted'), { id: globalToastId });
    setDeleteTarget(null);
  };

  const columns = [
    {
      title: columnTooltip(t('columns.rating'), t('columns.ratingDesc')),
      dataIndex: 'Rating',
      key: 'Rating',
      width: 140,
      filters: [
        { text: t('ratings.thumbsUp'), value: 'ThumbsUp' },
        { text: t('ratings.thumbsDown'), value: 'ThumbsDown' },
      ],
      onFilter: (value: unknown, record: ChatFeedback) => record.Rating === value,
      render: (rating: ChatFeedback['Rating']) =>
        rating === 'ThumbsUp' ? (
          <Tag icon={<LikeOutlined />} color="green">
            {t('ratings.thumbsUp')}
          </Tag>
        ) : (
          <Tag icon={<DislikeOutlined />} color="red">
            {t('ratings.thumbsDown')}
          </Tag>
        ),
    },
    {
      title: columnTooltip(t('columns.comment'), t('columns.commentDesc')),
      dataIndex: 'FeedbackText',
      key: 'FeedbackText',
      ellipsis: true,
      render: (text?: string | null) =>
        text ? (
          <span style={{ fontSize: 12.5 }}>{excerpt(text)}</span>
        ) : (
          <span style={{ color: 'var(--ant-color-text-tertiary)', fontSize: 12 }}>
            {t('detail.noComment')}
          </span>
        ),
    },
    {
      title: columnTooltip(t('columns.created'), t('columns.createdDesc')),
      dataIndex: 'CreatedUtc',
      key: 'CreatedUtc',
      width: 170,
      render: (createdUtc: string) => formatDateTime(createdUtc),
    },
    {
      title: columnTooltip(t('columns.thread'), t('columns.threadDesc')),
      dataIndex: 'ThreadGUID',
      key: 'ThreadGUID',
      width: 160,
      render: (threadGuid: string) => (
        <a
          href={`${serializePath(paths.aiHistory)}?thread=${threadGuid}`}
          onClick={(e) => e.stopPropagation()}
          style={{ fontFamily: 'monospace', fontSize: 12 }}
          data-testid={`feedback-thread-link-${threadGuid}`}
        >
          {`${threadGuid.slice(0, 8)}…`}
        </a>
      ),
    },
    {
      title: columnTooltip(t('columns.turn'), t('columns.turnDesc')),
      dataIndex: 'TurnGUID',
      key: 'TurnGUID',
      width: 140,
      render: (turnGuid: string) => (
        <span style={{ fontFamily: 'monospace', fontSize: 12 }}>{`${turnGuid.slice(0, 8)}…`}</span>
      ),
    },
    {
      title: '',
      key: 'actions',
      width: 60,
      align: 'center' as const,
      render: (_: unknown, record: ChatFeedback) => (
        <LitegraphTooltip title={t('deleteTooltip')}>
          <Button
            type="text"
            size="small"
            danger
            icon={<DeleteOutlined />}
            aria-label={t('deleteTooltip')}
            onClick={(e) => {
              e.stopPropagation();
              setDeleteTarget(record);
            }}
            data-testid={`feedback-delete-${record.GUID}`}
          />
        </LitegraphTooltip>
      ),
    },
  ];

  const isTableLoading = isLoading || isFetching;

  return (
    <PageContainer id="ai-feedback" pageTitle={t('title')}>
      {error && !isTableLoading ? (
        <FallBack retry={refetch}>{tCommon('states.somethingWentWrong')}</FallBack>
      ) : (
        <LitegraphTable
          loading={isTableLoading}
          columns={columns}
          dataSource={feedbackList}
          rowKey="GUID"
          onRowClick={(feedback: ChatFeedback) => setDetailTarget(feedback)}
          onRefresh={refetch}
          isRefreshing={isTableLoading}
        />
      )}

      {detailTarget && (
        <FeedbackDetailModal
          tenantGuid={tenantGuid}
          feedback={detailTarget}
          onClose={() => setDetailTarget(null)}
        />
      )}

      <ConfirmationModal
        open={!!deleteTarget}
        title={t('deleteModal.title')}
        content={t('deleteModal.body')}
        onCancel={() => setDeleteTarget(null)}
        onConfirm={handleDelete}
        loading={isDeleting}
      />
    </PageContainer>
  );
};

export default FeedbackPage;
