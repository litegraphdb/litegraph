'use client';
import React, { useEffect, useMemo, useState } from 'react';
import { useTranslations } from 'next-intl';
import { InboxOutlined } from '@ant-design/icons';
import { Upload } from 'antd';
import type { UploadProps } from 'antd';
import toast from 'react-hot-toast';
import LitegraphModal from '@/components/base/modal/Modal';
import LitegraphButton from '@/components/base/button/Button';
import LitegraphFlex from '@/components/base/flex/Flex';
import LitegraphText from '@/components/base/typograpghy/Text';
import LitegraphSelect from '@/components/base/select/Select';
import CopyButton from '@/components/base/copy-button/CopyButton';
import { useCurrentTenant } from '@/hooks/entityHooks';
import {
  useImportGraphAsNewJsonlMutation,
  useImportGraphJsonlMutation,
} from '@/lib/store/slice/slice';
import { GraphImportResult, GuidStrategy, ImportOnError } from '@/lib/sdk/importExport';

interface ImportJsonlModalProps {
  isVisible: boolean;
  setIsVisible: (visible: boolean) => void;
  mode: 'merge' | 'createNew';
  targetGraphGuid?: string;
  onSuccess?: () => void;
}

interface JsonlPreview {
  headerLines: number;
  nodes: number;
  edges: number;
  header: string;
}

const GUID_STRATEGY_VALUES: GuidStrategy[] = ['preserve', 'regenerate', 'skip', 'overwrite'];
const ON_ERROR_VALUES: ImportOnError[] = ['abort', 'skip'];

const parsePreview = (text: string): JsonlPreview => {
  const lines = text.split(/\r?\n/);
  let headerLines = 0;
  let nodes = 0;
  let edges = 0;
  const headerParts: string[] = [];
  for (const line of lines) {
    const trimmed = line.trim();
    if (!trimmed) continue;
    if (trimmed.startsWith('#')) {
      headerLines += 1;
      headerParts.push(trimmed);
      continue;
    }
    const collapsed = trimmed.replace(/\s+/g, '');
    if (collapsed.includes('"Type":"Node"')) nodes += 1;
    else if (collapsed.includes('"Type":"Edge"')) edges += 1;
  }
  return { headerLines, nodes, edges, header: headerParts.join('\n') };
};

const ImportJsonlModal: React.FC<ImportJsonlModalProps> = ({
  isVisible,
  setIsVisible,
  mode,
  targetGraphGuid,
  onSuccess,
}) => {
  const t = useTranslations('importExport');
  const tCommon = useTranslations('common');
  const tenant = useCurrentTenant();

  const defaultGuidStrategy: GuidStrategy = mode === 'merge' ? 'regenerate' : 'preserve';

  const [fileName, setFileName] = useState<string | null>(null);
  const [fileContent, setFileContent] = useState<string | null>(null);
  const [preview, setPreview] = useState<JsonlPreview | null>(null);
  const [guidStrategy, setGuidStrategy] = useState<GuidStrategy>(defaultGuidStrategy);
  const [onError, setOnError] = useState<ImportOnError>('abort');
  const [result, setResult] = useState<GraphImportResult | null>(null);

  const [importGraphJsonl, { isLoading: isMerging }] = useImportGraphJsonlMutation();
  const [importGraphAsNewJsonl, { isLoading: isCreatingNew }] = useImportGraphAsNewJsonlMutation();
  const isLoading = isMerging || isCreatingNew;

  useEffect(() => {
    if (isVisible) {
      setFileName(null);
      setFileContent(null);
      setPreview(null);
      setGuidStrategy(defaultGuidStrategy);
      setOnError('abort');
      setResult(null);
    }
    // defaultGuidStrategy is derived from the stable `mode` prop.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isVisible, mode]);

  const guidStrategyOptions = useMemo(
    () => GUID_STRATEGY_VALUES.map((value) => ({ value, label: t(`guidStrategy.${value}`) })),
    [t]
  );
  const onErrorOptions = useMemo(
    () => ON_ERROR_VALUES.map((value) => ({ value, label: t(`onError.${value}`) })),
    [t]
  );

  const handleFile: NonNullable<UploadProps['beforeUpload']> = (file) => {
    void (async () => {
      try {
        const text = await file.text();
        setFileName(file.name);
        setFileContent(text);
        setPreview(parsePreview(text));
        setResult(null);
      } catch {
        toast.error(t('toast.importFailed'));
      }
    })();
    return false;
  };

  const handleClose = () => {
    setIsVisible(false);
  };

  const handleSubmit = async () => {
    if (!fileContent || !tenant?.GUID) return;
    try {
      const importResult =
        mode === 'merge'
          ? await importGraphJsonl({
              tenantGuid: tenant.GUID,
              graphGuid: targetGraphGuid || '',
              jsonl: fileContent,
              options: { guidStrategy, onError },
            }).unwrap()
          : await importGraphAsNewJsonl({
              tenantGuid: tenant.GUID,
              jsonl: fileContent,
              options: { guidStrategy, onError },
            }).unwrap();
      setResult(importResult);
      toast.success(t('toast.importSuccess'));
      onSuccess?.();
    } catch (error) {
      const message =
        (error as any)?.data?.message ||
        (error as any)?.message ||
        (error as any)?.data?.Description ||
        t('toast.importFailed');
      toast.error(message);
    }
  };

  const renderCount = (label: string, value: number) => (
    <LitegraphFlex justify="space-between" gap={20}>
      <LitegraphText type="secondary">{label}</LitegraphText>
      <LitegraphText weight={600}>{value}</LitegraphText>
    </LitegraphFlex>
  );

  return (
    <LitegraphModal
      data-testid="import-jsonl-modal"
      title={mode === 'merge' ? t('modal.importTitle') : t('modal.importNewTitle')}
      centered
      open={isVisible}
      onCancel={handleClose}
      width={640}
      styles={{ body: { maxHeight: '70vh', overflowY: 'auto' } }}
      footer={
        result ? (
          <LitegraphButton type="primary" onClick={handleClose}>
            {tCommon('actions.close')}
          </LitegraphButton>
        ) : (
          <LitegraphFlex gap={8} justify="flex-end">
            <LitegraphButton onClick={handleClose}>{tCommon('actions.cancel')}</LitegraphButton>
            <LitegraphButton
              type="primary"
              onClick={handleSubmit}
              loading={isLoading}
              disabled={!fileContent}
            >
              {t('actions.import')}
            </LitegraphButton>
          </LitegraphFlex>
        )
      }
    >
      {result ? (
        <LitegraphFlex vertical gap={10} data-testid="import-jsonl-result">
          <LitegraphText weight={600}>{t('result.title')}</LitegraphText>
          {mode === 'createNew' && (
            <LitegraphFlex justify="space-between" align="center" gap={20}>
              <LitegraphText type="secondary">{t('result.newGraphGuid')}</LitegraphText>
              <LitegraphFlex align="center" gap={4}>
                <LitegraphText style={{ fontFamily: 'monospace', fontSize: 12 }}>
                  {result.GraphGUID}
                </LitegraphText>
                {result.GraphGUID && (
                  <CopyButton text={result.GraphGUID} tooltipTitle={t('copyGuid')} />
                )}
              </LitegraphFlex>
            </LitegraphFlex>
          )}
          {renderCount(t('result.graphsCreated'), result.GraphsCreated)}
          {renderCount(t('result.nodesCreated'), result.NodesCreated)}
          {renderCount(t('result.nodesUpdated'), result.NodesUpdated)}
          {renderCount(t('result.nodesSkipped'), result.NodesSkipped)}
          {renderCount(t('result.edgesCreated'), result.EdgesCreated)}
          {renderCount(t('result.edgesUpdated'), result.EdgesUpdated)}
          {renderCount(t('result.edgesSkipped'), result.EdgesSkipped)}
          {renderCount(t('result.linesRead'), result.LinesRead)}
          {renderCount(t('result.linesIgnored'), result.LinesIgnored)}
          <LitegraphText weight={600}>{t('result.warnings')}</LitegraphText>
          {result.Warnings && result.Warnings.length > 0 ? (
            <ul style={{ margin: 0, paddingLeft: 18 }}>
              {result.Warnings.map((warning, index) => (
                <li key={index}>
                  <LitegraphText type="secondary">{warning}</LitegraphText>
                </li>
              ))}
            </ul>
          ) : (
            <LitegraphText type="secondary">{t('result.noWarnings')}</LitegraphText>
          )}
        </LitegraphFlex>
      ) : (
        <LitegraphFlex vertical gap={16}>
          <Upload.Dragger
            accept=".jsonl,.ndjson,.txt"
            multiple={false}
            maxCount={1}
            showUploadList={false}
            beforeUpload={handleFile}
            data-testid="import-jsonl-upload"
          >
            <p className="ant-upload-drag-icon">
              <InboxOutlined />
            </p>
            <p className="ant-upload-text">{t('upload.prompt')}</p>
            <p className="ant-upload-hint">{t('upload.hint')}</p>
          </Upload.Dragger>

          {fileName && preview && (
            <LitegraphFlex vertical gap={8} data-testid="import-jsonl-preview">
              <LitegraphFlex justify="space-between" gap={20}>
                <LitegraphText type="secondary">{t('upload.selected')}</LitegraphText>
                <LitegraphText weight={600}>{fileName}</LitegraphText>
              </LitegraphFlex>
              {renderCount(t('preview.headerLines'), preview.headerLines)}
              {renderCount(t('preview.nodes'), preview.nodes)}
              {renderCount(t('preview.edges'), preview.edges)}
              {preview.header && (
                <pre
                  style={{
                    margin: 0,
                    padding: 8,
                    background: 'var(--ant-color-fill-quaternary, rgba(0,0,0,0.04))',
                    borderRadius: 6,
                    fontSize: 12,
                    whiteSpace: 'pre-wrap',
                    maxHeight: 120,
                    overflowY: 'auto',
                  }}
                >
                  {preview.header}
                </pre>
              )}
            </LitegraphFlex>
          )}

          <LitegraphFlex vertical gap={4}>
            <LitegraphText weight={600}>{t('fields.guidStrategy')}</LitegraphText>
            <LitegraphSelect
              data-testid="import-guid-strategy"
              value={guidStrategy}
              options={guidStrategyOptions}
              onChange={(value) => setGuidStrategy(value as GuidStrategy)}
            />
            <LitegraphText type="secondary" fontSize={12}>
              {t(`guidStrategyHelp.${guidStrategy}`)}
            </LitegraphText>
          </LitegraphFlex>

          <LitegraphFlex vertical gap={4}>
            <LitegraphText weight={600}>{t('fields.onError')}</LitegraphText>
            <LitegraphSelect
              data-testid="import-on-error"
              value={onError}
              options={onErrorOptions}
              onChange={(value) => setOnError(value as ImportOnError)}
            />
            <LitegraphText type="secondary" fontSize={12}>
              {t(`onErrorHelp.${onError}`)}
            </LitegraphText>
          </LitegraphFlex>
        </LitegraphFlex>
      )}
    </LitegraphModal>
  );
};

export default ImportJsonlModal;
