'use client';
import React from 'react';
import { useTranslations } from 'next-intl';
import { Badge } from 'antd';
import LitegraphModal from '@/components/base/modal/Modal';
import LitegraphText from '@/components/base/typograpghy/Text';
import { ChatEndpoint, ChatEndpointHealth, ChatEndpointHealthSample } from '@/lib/sdk/chat';
import { formatDateTime } from '@/utils/dateUtils';

interface HealthDetailModalProps {
  endpoint: ChatEndpoint;
  health: ChatEndpointHealth | undefined;
  onClose: () => void;
}

interface HistogramBucket {
  success: number;
  fail: number;
  time: string;
}

/** Format a millisecond span as `Xh Ym` / `Ym`, mirroring AssistantHub. */
export const formatDuration = (ms: number): string => {
  const hours = Math.floor(ms / 3600000);
  const minutes = Math.floor((ms % 3600000) / 60000);
  if (hours > 0) return `${hours}h ${minutes}m`;
  return `${minutes}m`;
};

/**
 * Time-bucketed health histogram mirroring AssistantHub's HealthHistogram:
 * per-sample bars under an hour of history, 1-minute buckets up to six hours,
 * 5-minute buckets beyond; green = all ok, orange = mixed, red = all fail.
 */
export const HealthHistogram = ({
  history,
  width = 120,
  height = 24,
}: {
  history: ChatEndpointHealthSample[] | undefined;
  width?: number;
  height?: number;
}) => {
  const t = useTranslations('ai.endpoints');
  if (!history || history.length === 0) {
    return (
      <LitegraphText style={{ fontSize: 12, color: 'var(--ant-color-text-tertiary)' }}>
        {t('healthDetail.noHistory')}
      </LitegraphText>
    );
  }

  const now = Date.now();
  const sorted = [...history].sort(
    (a, b) => new Date(a.TimestampUtc).getTime() - new Date(b.TimestampUtc).getTime()
  );
  const spanMs = now - new Date(sorted[0].TimestampUtc).getTime();
  const spanHours = spanMs / (1000 * 60 * 60);

  let buckets: HistogramBucket[] = [];
  if (spanHours < 1) {
    buckets = sorted.map((r) => ({
      success: r.Success ? 1 : 0,
      fail: r.Success ? 0 : 1,
      time: r.TimestampUtc,
    }));
  } else {
    const bucketMs = spanHours <= 6 ? 60000 : 300000;
    const bucketMap = new Map<number, { success: number; fail: number }>();
    for (const r of sorted) {
      const key = Math.floor(new Date(r.TimestampUtc).getTime() / bucketMs);
      const b = bucketMap.get(key) ?? { success: 0, fail: 0 };
      if (r.Success) b.success++;
      else b.fail++;
      bucketMap.set(key, b);
    }
    bucketMap.forEach((val, key) => {
      buckets.push({ ...val, time: new Date(key * bucketMs).toISOString() });
    });
  }

  const maxBars = Math.floor(width / 6);
  if (buckets.length > maxBars) buckets = buckets.slice(-maxBars);
  const barWidth = Math.max(4, Math.floor(width / buckets.length) - 2);

  return (
    <div
      data-testid="health-histogram"
      style={{
        display: 'flex',
        alignItems: 'flex-end',
        gap: 2,
        height,
        maxWidth: width,
        overflow: 'hidden',
      }}
    >
      {buckets.map((b, i) => {
        let color = 'var(--ant-color-success)';
        if (b.fail > 0 && b.success === 0) color = 'var(--ant-color-error)';
        else if (b.fail > 0 && b.success > 0) color = 'var(--ant-color-warning)';
        const title = `${new Date(b.time).toLocaleTimeString()} - ${b.success} ok, ${b.fail} fail`;
        return (
          <div
            key={i}
            title={title}
            style={{ width: barWidth, height, backgroundColor: color, borderRadius: 1 }}
          />
        );
      })}
    </div>
  );
};

const statLabelStyle: React.CSSProperties = {
  fontSize: 11,
  fontWeight: 600,
  textTransform: 'uppercase',
  letterSpacing: 0.3,
  color: 'var(--ant-color-text-secondary)',
};

const statCardStyle: React.CSSProperties = {
  display: 'flex',
  flexDirection: 'column',
  alignItems: 'center',
  gap: 6,
  padding: '12px 8px',
  borderRadius: 8,
  background: 'var(--ant-color-fill-quaternary)',
  border: '1px solid var(--ant-color-border-secondary)',
};

const StatCard = ({ label, children }: { label: string; children: React.ReactNode }) => (
  <div style={statCardStyle}>
    <div style={statLabelStyle}>{label}</div>
    <div style={{ fontSize: 18, fontWeight: 700 }}>{children}</div>
  </div>
);

const TimestampItem = ({ label, value }: { label: string; value: string | null | undefined }) => {
  const t = useTranslations('ai.endpoints');
  return (
    <div
      style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 12 }}
    >
      <span style={{ fontSize: 12, fontWeight: 600, color: 'var(--ant-color-text-secondary)', whiteSpace: 'nowrap' }}>
        {label}
      </span>
      <span style={{ fontSize: 12, textAlign: 'right' }}>
        {value ? formatDateTime(value) : t('healthDetail.notAvailable')}
      </span>
    </div>
  );
};

/**
 * Health detail modal mirroring AssistantHub's HealthDetailModal: a stat-card
 * row (status, uptime, history span, consecutive ok/fail), a last-error box,
 * the bucketed health histogram, and a first/last check timestamps grid.
 */
const HealthDetailModal = ({ endpoint, health, onClose }: HealthDetailModalProps) => {
  const t = useTranslations('ai.endpoints');

  const history = health?.CheckHistory ?? [];
  const sorted = [...history].sort(
    (a, b) => new Date(a.TimestampUtc).getTime() - new Date(b.TimestampUtc).getTime()
  );
  const spanMs = sorted.length > 0 ? Date.now() - new Date(sorted[0].TimestampUtc).getTime() : 0;
  const lastHealthy = [...sorted].reverse().find((s) => s.Success)?.TimestampUtc;
  const lastUnhealthy = [...sorted].reverse().find((s) => !s.Success)?.TimestampUtc;

  return (
    <LitegraphModal
      title={t('healthDetail.title', { name: endpoint.Name })}
      open
      onCancel={onClose}
      onOk={onClose}
      cancelButtonProps={{ style: { display: 'none' } }}
      okText={t('healthDetail.close')}
      width={860}
      data-testid="health-detail-modal"
    >
      {!health ? (
        <LitegraphText>{t('healthDetail.noData')}</LitegraphText>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 20 }}>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(5, 1fr)', gap: 12 }}>
            <StatCard label={t('healthDetail.status')}>
              {!health.Monitored ? (
                <Badge status="default" text={t('health.notMonitored')} />
              ) : health.Healthy === true ? (
                <Badge status="success" text={t('health.healthy')} />
              ) : health.Healthy === false ? (
                <Badge status="error" text={t('health.unhealthy')} />
              ) : (
                <Badge status="processing" text={t('health.pending')} />
              )}
            </StatCard>
            <StatCard label={t('healthDetail.uptime')}>
              {health.UptimePercentage != null
                ? `${health.UptimePercentage.toFixed(2)}%`
                : t('healthDetail.notAvailable')}
            </StatCard>
            <StatCard label={t('healthDetail.historySpan')}>
              {spanMs > 0 ? formatDuration(spanMs) : t('healthDetail.notAvailable')}
            </StatCard>
            <StatCard label={t('healthDetail.consecutiveSuccesses')}>
              <span style={{ color: 'var(--ant-color-success)' }}>
                {health.ConsecutiveSuccesses}
              </span>
            </StatCard>
            <StatCard label={t('healthDetail.consecutiveFailures')}>
              <span style={{ color: 'var(--ant-color-error)' }}>{health.ConsecutiveFailures}</span>
            </StatCard>
          </div>

          {health.LastError && (
            <div
              data-testid="health-last-error"
              style={{
                padding: '12px 16px',
                borderRadius: 8,
                background: 'var(--ant-color-error-bg)',
                border: '1px solid var(--ant-color-error-border)',
              }}
            >
              <div style={{ ...statLabelStyle, color: 'var(--ant-color-error)', marginBottom: 4 }}>
                {t('healthDetail.lastError')}
              </div>
              <div style={{ fontSize: 13, color: 'var(--ant-color-error)', wordBreak: 'break-word' }}>
                {health.LastError}
              </div>
            </div>
          )}

          <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
            <div style={statLabelStyle}>{t('healthDetail.historyTitle')}</div>
            <div
              style={{
                padding: 12,
                borderRadius: 8,
                background: 'var(--ant-color-fill-quaternary)',
                border: '1px solid var(--ant-color-border-secondary)',
              }}
            >
              <HealthHistogram history={history} width={770} height={36} />
            </div>
          </div>

          <div
            style={{
              display: 'grid',
              gridTemplateColumns: '1fr 1fr',
              gap: '8px 24px',
              paddingTop: 16,
              borderTop: '1px solid var(--ant-color-border-secondary)',
            }}
          >
            <TimestampItem
              label={t('healthDetail.firstCheck')}
              value={sorted.length > 0 ? sorted[0].TimestampUtc : null}
            />
            <TimestampItem label={t('healthDetail.lastChecked')} value={health.LastCheckedUtc} />
            <TimestampItem label={t('healthDetail.lastHealthy')} value={lastHealthy} />
            <TimestampItem label={t('healthDetail.lastUnhealthy')} value={lastUnhealthy} />
          </div>
        </div>
      )}
    </LitegraphModal>
  );
};

export default HealthDetailModal;
