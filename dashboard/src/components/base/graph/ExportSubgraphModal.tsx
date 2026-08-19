'use client';
import React, { useEffect, useMemo, useState } from 'react';
import { useTranslations } from 'next-intl';
import { InputNumber, Switch } from 'antd';
import { saveAs } from 'file-saver';
import toast from 'react-hot-toast';
import LitegraphModal from '@/components/base/modal/Modal';
import LitegraphButton from '@/components/base/button/Button';
import LitegraphFlex from '@/components/base/flex/Flex';
import LitegraphText from '@/components/base/typograpghy/Text';
import LitegraphSelect from '@/components/base/select/Select';
import { useCurrentTenant } from '@/hooks/entityHooks';
import { useExportSubgraphJsonlMutation } from '@/lib/store/slice/slice';
import { SubgraphDirection } from '@/lib/sdk/importExport';

interface ExportSubgraphModalProps {
  isVisible: boolean;
  setIsVisible: (visible: boolean) => void;
  graphGuid: string;
  startNodeGuid: string;
  onSuccess?: () => void;
}

const DIRECTION_VALUES: SubgraphDirection[] = ['Outbound', 'Inbound', 'Both'];
const DIRECTION_LABEL_KEYS: Record<SubgraphDirection, string> = {
  Outbound: 'direction.outbound',
  Inbound: 'direction.inbound',
  Both: 'direction.both',
};

const ExportSubgraphModal: React.FC<ExportSubgraphModalProps> = ({
  isVisible,
  setIsVisible,
  graphGuid,
  startNodeGuid,
  onSuccess,
}) => {
  const t = useTranslations('importExport');
  const tCommon = useTranslations('common');
  const tenant = useCurrentTenant();

  const [maxDepth, setMaxDepth] = useState<number>(2);
  const [direction, setDirection] = useState<SubgraphDirection>('Outbound');
  const [includeData, setIncludeData] = useState<boolean>(true);
  const [includeSubordinates, setIncludeSubordinates] = useState<boolean>(true);
  const [maxEdgeCost, setMaxEdgeCost] = useState<number | null>(null);

  const [exportSubgraphJsonl, { isLoading }] = useExportSubgraphJsonlMutation();

  useEffect(() => {
    if (isVisible) {
      setMaxDepth(2);
      setDirection('Outbound');
      setIncludeData(true);
      setIncludeSubordinates(true);
      setMaxEdgeCost(null);
    }
  }, [isVisible]);

  const directionOptions = useMemo(
    () => DIRECTION_VALUES.map((value) => ({ value, label: t(DIRECTION_LABEL_KEYS[value]) })),
    [t]
  );

  const handleClose = () => {
    setIsVisible(false);
  };

  const handleSubmit = async () => {
    if (!tenant?.GUID || !startNodeGuid) return;
    try {
      const jsonl = await exportSubgraphJsonl({
        tenantGuid: tenant.GUID,
        graphGuid,
        request: {
          TenantGUID: tenant.GUID,
          GraphGUID: graphGuid,
          StartNodeGUIDs: [startNodeGuid],
          MaxDepth: maxDepth,
          Direction: direction,
          MaxNodes: 0,
          MaxEdges: 0,
          MaxEdgeCost: maxEdgeCost,
          IncludeData: includeData,
          IncludeSubordinates: includeSubordinates,
          // Edge/node label and tag filters are optional; omitted for now.
          // TODO: expose label/tag filters when a reusable widget is available.
        },
      }).unwrap();
      const blob = new Blob([jsonl], { type: 'application/x-ndjson' });
      saveAs(blob, `subgraph-${startNodeGuid}.jsonl`);
      toast.success(t('toast.exportSuccess'));
      setIsVisible(false);
      onSuccess?.();
    } catch (error) {
      const message =
        (error as any)?.data?.message ||
        (error as any)?.message ||
        t('toast.exportFailed');
      toast.error(message);
    }
  };

  return (
    <LitegraphModal
      data-testid="export-subgraph-modal"
      title={t('modal.exportSubgraphTitle')}
      centered
      open={isVisible}
      onCancel={handleClose}
      width={520}
      footer={
        <LitegraphFlex gap={8} justify="flex-end">
          <LitegraphButton onClick={handleClose}>{tCommon('actions.cancel')}</LitegraphButton>
          <LitegraphButton
            type="primary"
            onClick={handleSubmit}
            loading={isLoading}
            disabled={!startNodeGuid}
          >
            {tCommon('actions.export')}
          </LitegraphButton>
        </LitegraphFlex>
      }
    >
      <LitegraphFlex vertical gap={16}>
        <LitegraphFlex vertical gap={4}>
          <LitegraphText weight={600}>{t('fields.startNode')}</LitegraphText>
          <LitegraphText style={{ fontFamily: 'monospace', fontSize: 12 }}>
            {startNodeGuid}
          </LitegraphText>
          <LitegraphText type="secondary" fontSize={12}>
            {t('startNodeHelp')}
          </LitegraphText>
        </LitegraphFlex>

        <LitegraphFlex vertical gap={4}>
          <LitegraphText weight={600}>{t('fields.maxDepth')}</LitegraphText>
          <InputNumber
            data-testid="export-subgraph-max-depth"
            min={1}
            value={maxDepth}
            onChange={(value) => setMaxDepth(typeof value === 'number' ? value : 1)}
            style={{ width: '100%' }}
          />
        </LitegraphFlex>

        <LitegraphFlex vertical gap={4}>
          <LitegraphText weight={600}>{t('fields.direction')}</LitegraphText>
          <LitegraphSelect
            data-testid="export-subgraph-direction"
            value={direction}
            options={directionOptions}
            onChange={(value) => setDirection(value as SubgraphDirection)}
          />
        </LitegraphFlex>

        <LitegraphFlex justify="space-between" align="center" gap={20}>
          <LitegraphText weight={600}>{t('fields.includeData')}</LitegraphText>
          <Switch
            data-testid="export-subgraph-include-data"
            checked={includeData}
            onChange={setIncludeData}
          />
        </LitegraphFlex>

        <LitegraphFlex justify="space-between" align="center" gap={20}>
          <LitegraphText weight={600}>{t('fields.includeSubordinates')}</LitegraphText>
          <Switch
            data-testid="export-subgraph-include-subordinates"
            checked={includeSubordinates}
            onChange={setIncludeSubordinates}
          />
        </LitegraphFlex>

        <LitegraphFlex vertical gap={4}>
          <LitegraphText weight={600}>{t('fields.maxEdgeCost')}</LitegraphText>
          <InputNumber
            data-testid="export-subgraph-max-edge-cost"
            value={maxEdgeCost ?? undefined}
            onChange={(value) => setMaxEdgeCost(typeof value === 'number' ? value : null)}
            style={{ width: '100%' }}
          />
        </LitegraphFlex>
      </LitegraphFlex>
    </LitegraphModal>
  );
};

export default ExportSubgraphModal;
