'use client';
import { Form, Tag } from 'antd';
import { useTranslations } from 'next-intl';
import LitegraphModal from '@/components/base/modal/Modal';
import LitegraphFormItem from '@/components/base/form/FormItem';
import { Dispatch, SetStateAction, useEffect, useMemo, useState } from 'react';
import LitegraphInput from '@/components/base/input/Input';
import DataJsonEditor from '@/components/inputs/data-json-editor/DataJsonEditor';
import { v4 } from 'uuid';
import { makeValidationRules } from './constant';
import { NodeType } from '@/types/types';
import toast from 'react-hot-toast';
import VectorsInput from '@/components/inputs/vectors-input.tsx/VectorsInput';
import LabelInput from '@/components/inputs/label-input/LabelInput';
import TagsInput from '@/components/inputs/tags-input/TagsInput';
import { convertTagsToRecord } from '@/components/inputs/tags-input/utils';
import { convertVectorsToAPIRecord } from '@/components/inputs/vectors-input.tsx/utils';
import LitegraphFlex from '@/components/base/flex/Flex';
import CopyButton from '@/components/base/copy-button/CopyButton';
import {
  useCreateNodeMutation,
  useGetGraphByIdQuery,
  useGetNodeByIdQuery,
  useUpdateNodeMutation,
} from '@/lib/store/slice/slice';
import { Node, NodeCreateRequest } from 'litegraphdb/dist/types/types';
import PageLoading from '@/components/base/loading/PageLoading';
import { tagsToFormList, toPlainJson, vectorsToFormList } from '@/utils/formValueUtils';
import modalStyles from '@/page/common/entityViewModal.module.scss';

const initialValues = {
  graphName: '',
  guid: '',
  name: '',
  data: {},
  labels: [],
  tags: [],
  vectors: [],
};

interface AddEditNodeProps {
  isAddEditNodeVisible: boolean;
  setIsAddEditNodeVisible: Dispatch<SetStateAction<boolean>>;
  node: NodeType | null;
  selectedGraph: string;
  onNodeUpdated?: () => Promise<void>;
  readonly?: boolean;
  onClose?: () => void;
  // Local state update functions for graph viewer
  updateLocalNode?: (node: any) => void;
  addLocalNode?: (node: any) => void;
  removeLocalNode?: (nodeId: string) => void;
  // Current graph data for immediate updates
  currentNodes?: any[];
  currentEdges?: any[];
}

const AddEditNode = ({
  isAddEditNodeVisible,
  setIsAddEditNodeVisible,
  node: nodeWithOldData,
  selectedGraph,
  onNodeUpdated,
  readonly,
  onClose,
  updateLocalNode,
  addLocalNode,
  removeLocalNode,
  currentNodes,
  currentEdges,
}: AddEditNodeProps) => {
  const t = useTranslations('nodes');
  const tCommon = useTranslations('common');
  const [form] = Form.useForm();
  const validationRules = useMemo(() => makeValidationRules(t), [t]);
  const [formValid, setFormValid] = useState(false);
  const [uniqueKey, setUniqueKey] = useState(v4());
  const isReadonlyView = Boolean(readonly && nodeWithOldData?.GUID);

  const {
    data: graph,
    isLoading: isGraphLoading1,
    isFetching: isGraphFetching,
  } = useGetGraphByIdQuery(
    {
      graphId: selectedGraph,
    },
    { skip: !selectedGraph }
  );
  const isGraphLoading = isGraphLoading1 || isGraphFetching;

  const {
    data: node,
    isLoading: isNodeLoading1,
    isFetching: isNodeFetching,
    refetch: refetchNode,
  } = useGetNodeByIdQuery(
    {
      graphId: selectedGraph,
      nodeId: nodeWithOldData?.GUID || '',
      request: {
        includeData: true,
        includeSubordinates: true,
      },
    },
    { skip: !nodeWithOldData?.GUID || !isAddEditNodeVisible }
  );
  const isNodeLoading = isNodeLoading1 || isNodeFetching;
  const [createNodes, { isLoading: isCreateLoading }] = useCreateNodeMutation();
  const [updateNodeById, { isLoading: isUpdateLoading }] = useUpdateNodeMutation();

  // Add form validation watcher
  const [formValues, setFormValues] = useState({});

  useEffect(() => {
    setUniqueKey(v4());
  }, [readonly]);

  useEffect(() => {
    form
      .validateFields({ validateOnly: true })
      .then(() => setFormValid(true))
      .catch(() => setFormValid(false));
  }, [formValues, form]);

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      const tags: Record<string, string> = convertTagsToRecord(values.tags);

      if (nodeWithOldData?.GUID) {
        // Edit Node
        {
          // Fallback to API call for other contexts
          const data: Node = {
            TenantGUID: node?.TenantGUID || (nodeWithOldData as any)?.TenantGUID || '',
            LastUpdateUtc:
              node?.LastUpdateUtc ||
              (nodeWithOldData as any)?.LastUpdateUtc ||
              new Date().toISOString(),
            GUID: node?.GUID || (nodeWithOldData as any)?.GUID || '',
            GraphGUID: node?.GraphGUID || selectedGraph,
            CreatedUtc:
              node?.CreatedUtc || (nodeWithOldData as any)?.CreatedUtc || new Date().toISOString(),
            Name: values.name,
            Data: values.data,
            Labels: values.labels,
            Tags: tags,
            Vectors: convertVectorsToAPIRecord(values.vectors),
          };
          const res = await updateNodeById(data);
          if (res) {
            // Reflect change locally for immediate UI update
            if (updateLocalNode) {
              const nodeId = data.GUID;
              const existing = (currentNodes || []).find((n: any) => n.id === nodeId);
              const updatedNodeData = {
                id: nodeId,
                label: values.name,
                type: values.labels?.[0] || existing?.type || 'default',
                x: existing?.x ?? 0,
                y: existing?.y ?? 0,
                z: existing?.z ?? 0,
                vx: existing?.vx ?? 0,
                vy: existing?.vy ?? 0,
              };
              updateLocalNode(updatedNodeData);
            }

            // Refetch the node data to ensure UI reflects the latest changes
            if (nodeWithOldData?.GUID) {
              try {
                await refetchNode();
              } catch (error) {
                console.warn('Could not refetch node data:', error);
                // Continue with the update process even if refetch fails
              }
            }

            toast.success(t('toast.updated'));
            setIsAddEditNodeVisible(false);
            onNodeUpdated && (await onNodeUpdated());
          } else {
            throw new Error('Failed to update node - no response received');
          }
        }
      } else {
        // Add Node - always call API, then optionally mirror locally
        const data: NodeCreateRequest = {
          GraphGUID: selectedGraph,
          Name: values.name,
          Data: values.data,
          Labels: values.labels,
          Tags: tags,
          Vectors: convertVectorsToAPIRecord(values.vectors),
        };
        const res = await createNodes(data);
        if (res) {
          // Mirror into local graph state if available so user sees it instantly
          const created: any = (res as any)?.data || res;
          if (addLocalNode) {
            const idForLocal = created?.GUID || v4();
            addLocalNode({
              id: idForLocal,
              label: created?.Name || values.name,
              type: (created?.Labels && created.Labels[0]) || values.labels?.[0] || 'default',
              x: Math.random() * 800,
              y: Math.random() * 600,
              z: 0,
              vx: 0,
              vy: 0,
              Data: created?.Data ?? values.data ?? {},
              Labels: created?.Labels ?? values.labels ?? [],
              Tags: created?.Tags ?? tags ?? {},
              Vectors: created?.Vectors ?? convertVectorsToAPIRecord(values.vectors) ?? [],
            });
          }
          toast.success(t('toast.created'));
          setIsAddEditNodeVisible(false);
          onNodeUpdated && (await onNodeUpdated());
        } else {
          throw new Error('Failed to create node - no response received');
        }
      }
    } catch (error: unknown) {
      console.error('Error submitting form:', error);
      const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
      toast.error(t('toast.updateFailed', { error: errorMessage }));
    }
  };

  useEffect(() => {
    const graphName = typeof graph?.Name === 'string' ? graph.Name : '';

    if (node && nodeWithOldData?.GUID) {
      // Reset the form and set values for the new node
      form.resetFields();
      // Ensure form values are updated when editing
      form.setFieldsValue({
        graphName,
        guid: node.GUID || '',
        name: node.Name || '',
        data: toPlainJson<Record<string, unknown>>(node.Data, {}),
        labels: toPlainJson<string[]>(node.Labels, []),
        tags: tagsToFormList(node.Tags),
        vectors: vectorsToFormList(node.Vectors),
      });
      setUniqueKey(v4());
    } else if (!nodeWithOldData?.GUID) {
      form.resetFields();
      form.setFieldsValue({ ...initialValues, graphName, guid: '' });
      setUniqueKey(v4());
    }
  }, [node, nodeWithOldData?.GUID, selectedGraph, graph?.Name, form]);

  useEffect(() => {
    // Trigger initial validation
    form
      .validateFields({ validateOnly: true })
      .then(() => setFormValid(true))
      .catch(() => setFormValid(false));
  }, [form]);

  const readonlyModalBodyStyles = isReadonlyView
    ? {
        content: {
          height: '95vh',
          maxHeight: '95vh',
          display: 'flex',
          flexDirection: 'column' as const,
          overflow: 'hidden',
        },
        body: {
          flex: 1,
          minHeight: 0,
          overflow: 'auto',
          overscrollBehavior: 'contain' as const,
        },
      }
    : undefined;
  const readonlyLabels = node?.Labels || nodeWithOldData?.Labels || [];

  const modalTitle =
    isGraphLoading || isNodeLoading
      ? t('modalTitle.loading')
      : Boolean(readonly && !!nodeWithOldData)
        ? t('modalTitle.view')
        : nodeWithOldData
          ? t('modalTitle.edit')
          : t('modalTitle.create');

  return (
    <LitegraphModal
      title={modalTitle}
      okText={nodeWithOldData?.GUID ? t('modalTitle.updateOk') : t('modalTitle.createOk')}
      open={isAddEditNodeVisible}
      onOk={handleSubmit}
      confirmLoading={isCreateLoading || isUpdateLoading}
      onCancel={() => {
        setIsAddEditNodeVisible(false);
        onClose && onClose();
      }}
      width={isReadonlyView ? '95vw' : 800}
      centered={isReadonlyView}
      cancelText={readonly ? tCommon('actions.close') : tCommon('actions.cancel')}
      okButtonProps={{
        disabled: isGraphLoading || isNodeLoading || !formValid,
        'data-testid': 'add-node-submit-button',
        hidden: readonly,
      }}
      styles={readonlyModalBodyStyles}
      data-testid="add-edit-node-modal"
      forceRender
    >
      {!isAddEditNodeVisible ? (
        <Form form={form} style={{ display: 'none' }} />
      ) : isGraphLoading || isNodeLoading ? (
        <>
          <Form form={form} style={{ display: 'none' }} />
          <PageLoading />
        </>
      ) : (
        <Form
          initialValues={initialValues}
          form={form}
          layout="vertical"
          labelCol={{ xs: 5, md: 5, lg: 4 }}
          wrapperCol={{ span: 24 }}
          onValuesChange={(_, allValues) => setFormValues(allValues)}
          requiredMark={!readonly}
        >
          {isReadonlyView ? (
            <div
              className={`${modalStyles.summaryGrid} ${modalStyles.summaryGridExpanded}`}
              data-testid="node-view-summary-grid"
            >
              <LitegraphFormItem
                label={t('form.graph')}
                name="graphName"
                tooltip={t('form.graphTooltip')}
              >
                <LitegraphInput readOnly variant="borderless" />
              </LitegraphFormItem>

              <LitegraphFormItem
                label={
                  <LitegraphFlex align="center" gap={8}>
                    <span>{t('form.guid')}</span>
                    <CopyButton
                      getText={() => form.getFieldValue('guid') || ''}
                      tooltipTitle={tCommon('copy.copyGuid')}
                    />
                  </LitegraphFlex>
                }
                name="guid"
                tooltip={t('form.guidTooltip')}
              >
                <LitegraphInput readOnly variant="borderless" />
              </LitegraphFormItem>

              <LitegraphFormItem
                label={t('form.name')}
                name="name"
                tooltip={t('form.nameTooltip')}
                rules={validationRules.name}
              >
                <LitegraphInput
                  placeholder={t('form.namePlaceholder')}
                  data-testid="node-name-input"
                  readOnly={readonly}
                  variant={readonly ? 'borderless' : 'outlined'}
                />
              </LitegraphFormItem>
            </div>
          ) : (
            <>
              <LitegraphFlex gap={readonly ? 10 : 0} vertical={!readonly}>
                <LitegraphFormItem
                  className="flex-1"
                  label={t('form.graph')}
                  name="graphName"
                  tooltip={t('form.graphTooltip')}
                >
                  <LitegraphInput readOnly variant="borderless" />
                </LitegraphFormItem>

                <LitegraphFormItem
                  className="flex-1"
                  label={t('form.name')}
                  name="name"
                  tooltip={t('form.nameTooltip')}
                  rules={validationRules.name}
                >
                  <LitegraphInput
                    placeholder={t('form.namePlaceholder')}
                    data-testid="node-name-input"
                    readOnly={readonly}
                    variant={readonly ? 'borderless' : 'outlined'}
                  />
                </LitegraphFormItem>
              </LitegraphFlex>
              <LabelInput
                name="labels"
                readonly={readonly}
                tooltip={t('form.labelsTooltip')}
              />
            </>
          )}

          {isReadonlyView && (
            <Form.Item
              label={t('form.labels')}
              tooltip={t('form.labelsTooltip')}
              className={modalStyles.fullSpan}
            >
              {readonlyLabels.length > 0 ? (
                <div className={modalStyles.badgeList} data-testid="node-label-badges">
                  {readonlyLabels.map((label, index) => (
                    <Tag key={`${label}-${index}`} bordered={false} className={modalStyles.badge}>
                      {label}
                    </Tag>
                  ))}
                </div>
              ) : (
                <span className={modalStyles.emptyValue}>{tCommon('states.notAvailable')}</span>
              )}
            </Form.Item>
          )}

          <Form.Item
            label={t('form.tags')}
            tooltip={t('form.tagsTooltip')}
            className={isReadonlyView ? modalStyles.fullSpan : undefined}
          >
            <TagsInput name="tags" readonly={readonly} />
          </Form.Item>
          <Form.Item
            label={t('form.vectors')}
            tooltip={t('form.vectorsTooltip')}
            className={isReadonlyView ? modalStyles.fullSpan : undefined}
          >
            <VectorsInput name="vectors" readonly={readonly} />
          </Form.Item>
          <LitegraphFormItem
            className={isReadonlyView ? modalStyles.fullSpan : undefined}
            name="data"
            tooltip={t('form.dataTooltip')}
            label={
              <LitegraphFlex align="center" gap={8}>
                <span>{t('form.data')}</span>
                {readonly && (
                  <CopyButton
                    getText={() => JSON.stringify(form.getFieldValue('data') || {}, null, 2)}
                    tooltipTitle={t('form.copyData')}
                  />
                )}
              </LitegraphFlex>
            }
          >
            <DataJsonEditor uniqueKey={uniqueKey} readonly={readonly} />
          </LitegraphFormItem>
        </Form>
      )}
    </LitegraphModal>
  );
};

export default AddEditNode;
