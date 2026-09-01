'use client';
import React, { useEffect, useMemo, useState } from 'react';
import { useTranslations } from 'next-intl';
import { ReloadOutlined } from '@ant-design/icons';
import {
  getRequestHistorySummary,
  RequestHistorySummary,
  RequestHistorySummaryBucket,
} from '@/lib/sdk/requestHistory';
import styles from './RequestHistoryChart.module.scss';

export type TimeRange = 'hour' | 'day' | 'week' | 'month';

type RangeConfig = {
  value: TimeRange;
  interval: 'minute' | '15minute' | 'hour' | '6hour';
  stepMs: number;
  bucketCount: number;
  xLabelEvery: number;
  xLabelFormat: (d: Date) => string;
};

const TIME_RANGES: RangeConfig[] = [
  {
    value: 'hour',
    interval: 'minute',
    stepMs: 60_000,
    bucketCount: 60,
    xLabelEvery: 10,
    xLabelFormat: (d) =>
      d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', hour12: false }),
  },
  {
    value: 'day',
    interval: '15minute',
    stepMs: 15 * 60_000,
    bucketCount: 96,
    xLabelEvery: 16,
    xLabelFormat: (d) =>
      d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit', hour12: false }),
  },
  {
    value: 'week',
    interval: 'hour',
    stepMs: 60 * 60_000,
    bucketCount: 168,
    xLabelEvery: 24,
    xLabelFormat: (d) => d.toLocaleDateString([], { month: 'numeric', day: 'numeric' }),
  },
  {
    value: 'month',
    interval: '6hour',
    stepMs: 6 * 60 * 60_000,
    bucketCount: 120,
    xLabelEvery: 20,
    xLabelFormat: (d) => d.toLocaleDateString([], { month: 'numeric', day: 'numeric' }),
  },
];

const floorToStep = (ts: number, stepMs: number) => Math.floor(ts / stepMs) * stepMs;

const buildWindow = (cfg: RangeConfig) => {
  const endExclusive = floorToStep(Date.now(), cfg.stepMs) + cfg.stepMs;
  const start = endExclusive - cfg.bucketCount * cfg.stepMs;
  return { startUtc: new Date(start), endUtc: new Date(endExclusive - 1) };
};

const mergeBuckets = (
  cfg: RangeConfig,
  window: { startUtc: Date; endUtc: Date },
  data: RequestHistorySummaryBucket[] | undefined
): RequestHistorySummaryBucket[] => {
  const apiMap = new Map<number, RequestHistorySummaryBucket>();
  (data || []).forEach((b) => {
    const ts = new Date(b.TimestampUtc).getTime();
    apiMap.set(floorToStep(ts, cfg.stepMs), b);
  });

  const startMs = window.startUtc.getTime();
  const out: RequestHistorySummaryBucket[] = [];
  for (let i = 0; i < cfg.bucketCount; i++) {
    const ts = startMs + i * cfg.stepMs;
    const existing = apiMap.get(ts);
    if (existing) {
      out.push(existing);
    } else {
      out.push({
        TimestampUtc: new Date(ts).toISOString(),
        SuccessCount: 0,
        FailureCount: 0,
        TotalCount: 0,
      });
    }
  }
  return out;
};

type Props = {
  tenantGuid?: string;
  refreshKey?: number;
};

const CHART_WIDTH = 800;
const CHART_HEIGHT = 200;
const PAD_LEFT = 50;
const PAD_RIGHT = 16;
const PAD_TOP = 20;
const PAD_BOTTOM = 40;
const BAR_AREA_HEIGHT = CHART_HEIGHT - PAD_TOP - PAD_BOTTOM;
const BAR_AREA_WIDTH = CHART_WIDTH - PAD_LEFT - PAD_RIGHT;

const RequestHistoryChart: React.FC<Props> = ({ tenantGuid, refreshKey }) => {
  const t = useTranslations('requestHistory');
  const tCommon = useTranslations('common');
  const [range, setRange] = useState<TimeRange>('day');
  const [summary, setSummary] = useState<RequestHistorySummary | null>(null);
  const [loading, setLoading] = useState(false);
  const [hoveredBar, setHoveredBar] = useState<number | null>(null);
  const [localRefresh, setLocalRefresh] = useState(0);

  const config = useMemo(
    () => TIME_RANGES.find((r) => r.value === range) || TIME_RANGES[1],
    [range]
  );
  const window = useMemo(() => buildWindow(config), [config]);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    getRequestHistorySummary({
      interval: config.interval,
      startUtc: window.startUtc.toISOString(),
      endUtc: window.endUtc.toISOString(),
      tenantGuid,
    })
      .then((data) => {
        if (!cancelled) setSummary(data);
      })
      .catch(() => {
        if (!cancelled) setSummary(null);
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [config, window.startUtc, window.endUtc, tenantGuid, refreshKey, localRefresh]);

  const buckets = useMemo(
    () => mergeBuckets(config, window, summary?.Data),
    [config, window, summary]
  );

  const stats = useMemo(() => {
    const totalSuccess = buckets.reduce((acc, b) => acc + b.SuccessCount, 0);
    const totalFailure = buckets.reduce((acc, b) => acc + b.FailureCount, 0);
    return {
      total: totalSuccess + totalFailure,
      success: totalSuccess,
      failure: totalFailure,
    };
  }, [buckets]);

  const rawMax = useMemo(
    () => Math.max(1, ...buckets.map((b) => b.SuccessCount + b.FailureCount)),
    [buckets]
  );

  const { niceMax, yTicks } = useMemo(() => {
    // Choose a nice step that divides rawMax into roughly 4 intervals.
    const targetSteps = 4;
    const roughStep = rawMax / targetSteps;
    const magnitude = Math.pow(10, Math.floor(Math.log10(roughStep)));
    const normalized = roughStep / magnitude;
    let niceStep: number;
    if (normalized <= 1) niceStep = 1 * magnitude;
    else if (normalized <= 2) niceStep = 2 * magnitude;
    else if (normalized <= 5) niceStep = 5 * magnitude;
    else niceStep = 10 * magnitude;

    const top = Math.ceil(rawMax / niceStep) * niceStep;
    const ticks: number[] = [];
    for (let v = 0; v <= top + niceStep / 2; v += niceStep) ticks.push(v);
    return { niceMax: top, yTicks: ticks };
  }, [rawMax]);

  const maxCount = niceMax;

  const barGroupWidth = BAR_AREA_WIDTH / Math.max(1, buckets.length);
  const barWidth = Math.max(2, Math.min(40, barGroupWidth * 0.7));

  const tooltipIndex = hoveredBar;
  const tooltipBucket = tooltipIndex !== null ? buckets[tooltipIndex] : null;
  const tooltipLeftPct =
    tooltipIndex !== null ? ((tooltipIndex + 0.5) / buckets.length) * 100 : 0;

  return (
    <div className={styles.wrapper}>
      <div className={styles.headerRow}>
        <div className={styles.headerTitle}>{t('chart.title')}</div>
        <div style={{ display: 'flex', gap: 12, alignItems: 'center' }}>
          <div className={styles.timeTabs} role="tablist">
            {TIME_RANGES.map((r) => (
              <button
                key={r.value}
                type="button"
                className={`${styles.timeTab} ${range === r.value ? styles.timeTabActive : ''}`}
                onClick={() => setRange(r.value)}
                aria-selected={range === r.value}
                role="tab"
              >
                {t(`chart.ranges.${r.value}`)}
              </button>
            ))}
          </div>
          <button
            type="button"
            className={styles.refreshBtn}
            onClick={() => setLocalRefresh((n) => n + 1)}
            aria-label={tCommon('actions.refresh')}
            title={tCommon('actions.refresh')}
          >
            <ReloadOutlined spin={loading} />
          </button>
        </div>
      </div>

      <div className={styles.statsRow}>
        <div className={styles.statItem}>
          <span className={styles.statLabel}>{t('chart.total')}</span>
          <span className={styles.statValue}>{stats.total.toLocaleString()}</span>
        </div>
        <div className={styles.statItem}>
          <span className={styles.statLabel}>{t('chart.success')}</span>
          <span className={`${styles.statValue} ${styles.statValueSuccess}`}>
            {stats.success.toLocaleString()}
          </span>
        </div>
        <div className={styles.statItem}>
          <span className={styles.statLabel}>{t('chart.failed')}</span>
          <span className={`${styles.statValue} ${styles.statValueFailure}`}>
            {stats.failure.toLocaleString()}
          </span>
        </div>
      </div>

      <div className={styles.chartArea}>
        <svg
          viewBox={`0 0 ${CHART_WIDTH} ${CHART_HEIGHT}`}
          preserveAspectRatio="xMidYMid meet"
          className={styles.chartSvg}
          onMouseLeave={() => setHoveredBar(null)}
        >
          {yTicks.map((tick, i) => {
            const y = PAD_TOP + BAR_AREA_HEIGHT * (1 - tick / maxCount);
            const isZero = tick === 0;
            return (
              <g key={`y-${i}`}>
                <line
                  x1={PAD_LEFT}
                  x2={CHART_WIDTH - PAD_RIGHT}
                  y1={y}
                  y2={y}
                  stroke="var(--ant-color-border)"
                  strokeWidth={0.5}
                  strokeDasharray={isZero ? 'none' : '4,4'}
                />
                <text
                  x={PAD_LEFT - 8}
                  y={y + 3}
                  fontSize={5}
                  fill="var(--ant-color-text-secondary)"
                  textAnchor="end"
                >
                  {tick}
                </text>
              </g>
            );
          })}

          {buckets.map((b, i) => {
            const slotX = PAD_LEFT + i * barGroupWidth;
            const centerX = slotX + barGroupWidth / 2;
            const barX = centerX - barWidth / 2;
            const total = b.SuccessCount + b.FailureCount;
            const opacity = hoveredBar === i ? 1 : 0.85;

            const failHeight = (b.FailureCount / maxCount) * BAR_AREA_HEIGHT;
            const successHeight = (b.SuccessCount / maxCount) * BAR_AREA_HEIGHT;

            return (
              <g key={`bar-${i}`}>
                {b.FailureCount > 0 && (
                  <rect
                    x={barX}
                    y={PAD_TOP + BAR_AREA_HEIGHT - failHeight}
                    width={barWidth}
                    height={failHeight}
                    rx={2}
                    fill="#ef4444"
                    opacity={opacity}
                  />
                )}
                {b.SuccessCount > 0 && (
                  <rect
                    x={barX}
                    y={PAD_TOP + BAR_AREA_HEIGHT - failHeight - successHeight}
                    width={barWidth}
                    height={successHeight}
                    rx={2}
                    fill="var(--ant-color-primary)"
                    opacity={opacity}
                  />
                )}
                <rect
                  x={slotX}
                  y={PAD_TOP}
                  width={barGroupWidth}
                  height={BAR_AREA_HEIGHT}
                  fill="transparent"
                  onMouseEnter={() => setHoveredBar(i)}
                />
                {i % config.xLabelEvery === 0 && (
                  <text
                    x={centerX}
                    y={CHART_HEIGHT - 6}
                    fontSize={5}
                    fill="var(--ant-color-text-secondary)"
                    textAnchor="middle"
                  >
                    {config.xLabelFormat(new Date(b.TimestampUtc))}
                  </text>
                )}
                {/* marker */}
                {total === 0 && null}
              </g>
            );
          })}
        </svg>

        {tooltipBucket && tooltipIndex !== null && (
          <div
            className={styles.tooltip}
            style={{ left: `${tooltipLeftPct}%` }}
            role="tooltip"
          >
            <div className={styles.tooltipTitle}>
              {new Date(tooltipBucket.TimestampUtc).toLocaleString()}
            </div>
            <div className={styles.tooltipSuccess}>
              {t('chart.tooltip.success', { count: tooltipBucket.SuccessCount })}
            </div>
            <div className={styles.tooltipFailure}>
              {t('chart.tooltip.failed', { count: tooltipBucket.FailureCount })}
            </div>
            <div>
              {t('chart.tooltip.total', {
                count: tooltipBucket.SuccessCount + tooltipBucket.FailureCount,
              })}
            </div>
          </div>
        )}
      </div>

      <div className={styles.legend}>
        <div className={styles.legendItem}>
          <span
            className={styles.legendSwatch}
            style={{ background: 'var(--ant-color-primary)' }}
          />
          <span>{t('chart.success')}</span>
        </div>
        <div className={styles.legendItem}>
          <span className={styles.legendSwatch} style={{ background: '#ef4444' }} />
          <span>{t('chart.failed')}</span>
        </div>
      </div>
    </div>
  );
};

export default RequestHistoryChart;
