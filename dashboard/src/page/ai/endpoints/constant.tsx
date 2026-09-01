import React from 'react';
import { Badge, Dropdown, TableProps, Tag } from 'antd';
import {
  CheckCircleFilled,
  CloseCircleFilled,
  MoreOutlined,
} from '@ant-design/icons';
import { Button } from 'antd';
import CopyButton from '@/components/base/copy-button/CopyButton';
import { ChatEndpoint, ChatEndpointHealth } from '@/lib/sdk/chat';
import { formatDateTime } from '@/utils/dateUtils';
import { columnTooltip } from '@/utils/tooltipUtils';

type Translator = (key: string, values?: Record<string, string | number>) => string;

const healthBadge = (t: Translator, health?: ChatEndpointHealth) => {
  if (!health || !health.Monitored) {
    return <Badge status="default" text={t('health.notMonitored')} />;
  }
  if (health.Healthy === true) {
    return <Badge status="success" text={t('health.healthy')} />;
  }
  if (health.Healthy === false) {
    return <Badge status="error" text={t('health.unhealthy')} />;
  }
  return <Badge status="processing" text={t('health.pending')} />;
};

export const endpointTableColumns = (
  t: Translator,
  healthByGuid: Record<string, ChatEndpointHealth>,
  handleEdit: (endpoint: ChatEndpoint) => void,
  handleTest: (endpoint: ChatEndpoint) => void,
  handleHealthDetail: (endpoint: ChatEndpoint) => void,
  handleToggleActive: (endpoint: ChatEndpoint) => void,
  handleDelete: (endpoint: ChatEndpoint) => void
): TableProps<ChatEndpoint>['columns'] => [
  {
    title: columnTooltip(t('columns.name'), t('columns.nameDesc')),
    dataIndex: 'Name',
    key: 'Name',
    width: 170,
    ellipsis: true,
    render: (name: string, record: ChatEndpoint) => (
      <span
        style={{ display: 'inline-flex', alignItems: 'center', gap: 4, maxWidth: '100%' }}
        title={name}
      >
        <span style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
          {name}
        </span>
        <CopyButton text={record.GUID} tooltipTitle={t('copyGuid')} />
      </span>
    ),
  },
  {
    title: columnTooltip(t('columns.type'), t('columns.typeDesc')),
    dataIndex: 'EndpointType',
    key: 'EndpointType',
    width: 120,
    render: (type: string) => (
      <Tag color={type === 'Completion' ? 'blue' : 'purple'}>
        {type === 'Completion' ? t('types.completion') : t('types.embedding')}
      </Tag>
    ),
  },
  {
    title: columnTooltip(t('columns.provider'), t('columns.providerDesc')),
    dataIndex: 'Provider',
    key: 'Provider',
    width: 110,
  },
  {
    title: columnTooltip(t('columns.model'), t('columns.modelDesc')),
    dataIndex: 'Model',
    key: 'Model',
    width: 170,
    ellipsis: true,
    render: (model: string) => <code style={{ fontSize: 12 }}>{model}</code>,
  },
  {
    title: columnTooltip(t('columns.endpoint'), t('columns.endpointDesc')),
    dataIndex: 'Endpoint',
    key: 'Endpoint',
    width: 220,
    ellipsis: true,
    render: (endpoint: string) => (
      <span style={{ fontFamily: 'monospace', fontSize: 12 }} title={endpoint}>
        {endpoint}
      </span>
    ),
  },
  {
    title: columnTooltip(t('columns.concurrency'), t('columns.concurrencyDesc')),
    dataIndex: 'MaxConcurrentRequests',
    key: 'MaxConcurrentRequests',
    width: 110,
    align: 'center',
  },
  {
    title: columnTooltip(t('columns.active'), t('columns.activeDesc')),
    dataIndex: 'Active',
    key: 'Active',
    width: 80,
    align: 'center',
    render: (active: boolean) =>
      active ? (
        <CheckCircleFilled style={{ color: 'var(--ant-color-success)' }} />
      ) : (
        <CloseCircleFilled style={{ color: 'var(--ant-color-error)' }} />
      ),
  },
  {
    title: columnTooltip(t('columns.health'), t('columns.healthDesc')),
    key: 'Health',
    width: 140,
    render: (_: unknown, record: ChatEndpoint) => (
      <Button
        type="text"
        size="small"
        style={{ padding: 0 }}
        onClick={(e) => {
          e.stopPropagation();
          handleHealthDetail(record);
        }}
        data-testid={`endpoint-health-${record.GUID}`}
      >
        {healthBadge(t, healthByGuid[record.GUID])}
      </Button>
    ),
  },
  {
    title: columnTooltip(t('columns.created'), t('columns.createdDesc')),
    dataIndex: 'CreatedUtc',
    key: 'CreatedUtc',
    width: 160,
    render: (createdUtc: string) => (createdUtc ? formatDateTime(createdUtc) : '—'),
  },
  {
    title: '',
    key: 'actions',
    width: 50,
    align: 'center',
    render: (_: unknown, record: ChatEndpoint) => (
      <Dropdown
        menu={{
          items: [
            { key: 'edit', label: t('actions.edit') },
            { key: 'test', label: t('actions.test') },
            { key: 'health', label: t('actions.healthDetail') },
            {
              key: 'toggle',
              label: record.Active ? t('actions.deactivate') : t('actions.activate'),
            },
            { type: 'divider' },
            { key: 'delete', label: t('actions.delete'), danger: true },
          ],
          onClick: ({ key, domEvent }) => {
            domEvent.stopPropagation();
            if (key === 'edit') handleEdit(record);
            if (key === 'test') handleTest(record);
            if (key === 'health') handleHealthDetail(record);
            if (key === 'toggle') handleToggleActive(record);
            if (key === 'delete') handleDelete(record);
          },
        }}
        trigger={['click']}
      >
        <Button
          type="text"
          size="small"
          icon={<MoreOutlined />}
          onClick={(e) => e.stopPropagation()}
          aria-label={t('actions.menu')}
          data-testid={`endpoint-actions-${record.GUID}`}
        />
      </Dropdown>
    ),
  },
];
