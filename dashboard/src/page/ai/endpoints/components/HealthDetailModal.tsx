'use client';
import React from 'react';
import { useTranslations } from 'next-intl';
import { Badge, Descriptions } from 'antd';
import LitegraphModal from '@/components/base/modal/Modal';
import LitegraphText from '@/components/base/typograpghy/Text';
import { ChatEndpoint, ChatEndpointHealth } from '@/lib/sdk/chat';
import { formatDateTime } from '@/utils/dateUtils';

interface HealthDetailModalProps {
  endpoint: ChatEndpoint;
  health: ChatEndpointHealth | undefined;
  onClose: () => void;
}

/**
 * Inline SVG bar strip of recent health samples: one bar per check, height
 * scaled by duration, colored by success. No chart library involved.
 */
const HealthHistoryStrip = ({
  samples,
  ariaLabel,
}: {
  samples: ChatEndpointHealth['CheckHistory'];
  ariaLabel: string;
}) => {
  const width = 480;
  const height = 56;
  const count = samples.length;
  if (count === 0) return null;
  const barGap = 1;
  const barWidth = Math.max(1, Math.floor(width / count) - barGap);
  const maxDuration = Math.max(...samples.map((s) => s.DurationMs), 1);
  return (
    <svg
      viewBox={`0 0 ${width} ${height}`}
      role="img"
      aria-label={ariaLabel}
      style={{ width: '100%', height: 'auto', display: 'block' }}
      data-testid="health-history-strip"
    >
      {samples.map((sample, index) => {
        const barHeight = Math.max(3, (sample.DurationMs / maxDuration) * (height - 4));
        const x = index * (barWidth + barGap);
        return (
          <rect
            key={`${sample.TimestampUtc}-${index}`}
            x={x}
            y={height - barHeight}
            width={barWidth}
            height={barHeight}
            rx={1}
            fill={sample.Success ? 'var(--ant-color-success)' : 'var(--ant-color-error)'}
            opacity={sample.Success ? 0.75 : 0.95}
          >
            <title>{`${sample.TimestampUtc} — ${sample.DurationMs.toFixed(1)} ms`}</title>
          </rect>
        );
      })}
    </svg>
  );
};

/** Health detail modal: status, uptime, counters, last error, 24h histogram. */
const HealthDetailModal = ({ endpoint, health, onClose }: HealthDetailModalProps) => {
  const t = useTranslations('ai.endpoints');

  return (
    <LitegraphModal
      title={t('healthDetail.title', { name: endpoint.Name })}
      open
      onCancel={onClose}
      onOk={onClose}
      cancelButtonProps={{ style: { display: 'none' } }}
      okText={t('healthDetail.close')}
      width={620}
      data-testid="health-detail-modal"
    >
      {!health ? (
        <LitegraphText>{t('healthDetail.noData')}</LitegraphText>
      ) : (
        <>
          <Descriptions column={2} size="small" bordered style={{ marginBottom: 16 }}>
            <Descriptions.Item label={t('healthDetail.status')} span={2}>
              {!health.Monitored ? (
                <Badge status="default" text={t('health.notMonitored')} />
              ) : health.Healthy === true ? (
                <Badge status="success" text={t('health.healthy')} />
              ) : health.Healthy === false ? (
                <Badge status="error" text={t('health.unhealthy')} />
              ) : (
                <Badge status="processing" text={t('health.pending')} />
              )}
            </Descriptions.Item>
            <Descriptions.Item label={t('healthDetail.uptime')}>
              {health.UptimePercentage != null
                ? `${health.UptimePercentage.toFixed(2)}%`
                : t('healthDetail.notAvailable')}
            </Descriptions.Item>
            <Descriptions.Item label={t('healthDetail.lastChecked')}>
              {health.LastCheckedUtc
                ? formatDateTime(health.LastCheckedUtc)
                : t('healthDetail.notAvailable')}
            </Descriptions.Item>
            <Descriptions.Item label={t('healthDetail.consecutiveSuccesses')}>
              {health.ConsecutiveSuccesses}
            </Descriptions.Item>
            <Descriptions.Item label={t('healthDetail.consecutiveFailures')}>
              {health.ConsecutiveFailures}
            </Descriptions.Item>
            {health.LastError && (
              <Descriptions.Item label={t('healthDetail.lastError')} span={2}>
                <LitegraphText style={{ color: 'var(--ant-color-error)', fontSize: 12 }}>
                  {health.LastError}
                </LitegraphText>
              </Descriptions.Item>
            )}
          </Descriptions>
          {health.CheckHistory && health.CheckHistory.length > 0 ? (
            <>
              <LitegraphText
                style={{ display: 'block', marginBottom: 8, fontSize: 12.5, fontWeight: 500 }}
              >
                {t('healthDetail.historyTitle')}
              </LitegraphText>
              <HealthHistoryStrip
                samples={health.CheckHistory}
                ariaLabel={t('healthDetail.historyTitle')}
              />
            </>
          ) : (
            <LitegraphText style={{ fontSize: 12, color: 'var(--ant-color-text-tertiary)' }}>
              {t('healthDetail.noHistory')}
            </LitegraphText>
          )}
        </>
      )}
    </LitegraphModal>
  );
};

export default HealthDetailModal;
