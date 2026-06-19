'use client';
import { Form, Tag } from 'antd';
import LitegraphModal from '@/components/base/modal/Modal';
import LitegraphFormItem from '@/components/base/form/FormItem';
import { Dispatch, SetStateAction, useEffect, useState } from 'react';
import LitegraphInput from '@/components/base/input/Input';
import DataJsonEditor from '@/components/inputs/data-json-editor/DataJsonEditor';
import { v4 } from 'uuid';
import { validationRules } from './constant';
import { EdgeType } from '@/types/types';
import toast from 'react-hot-toast';
import LabelInput from '@/components/inputs/label-input/LabelInput';
import VectorsInput from '@/components/inputs/vectors-input.tsx/VectorsInput';
import TagsInput from '@/components/inputs/tags-input/TagsInput';
import { convertVectorsToAPIRecord } from '@/components/inputs/vectors-input.tsx/utils';
import { convertTagsToRecord } from '@/components/inputs/tags-input/utils';
import LitegraphFlex from '@/components/base/flex/Flex';
import CopyButton from '@/components/base/copy-button/CopyButton';
import {
  useCreateEdgeMutation,
  useGetEdgeByIdQuery,
  useGetGraphByIdQuery,
  useUpdateEdgeMutation,
} from '@/lib/store/slice/slice';
import { Edge, EdgeCreateRequest } from 'litegraphdb/dist/types/types';
import { getCreateEditViewModelTitle } from '@/utils/appUtils';
import PageLoading from '@/components/base/loading/PageLoading';
import NodeSelector from '@/components/node-selector/NodeSelector';
import { useWatch } from 'antd/es/form/Form';
import { tagsToFormList, toPlainJson, vectorsToFormList } from '@/utils/formValueUtils';
import modalStyles from '@/page/common/entityViewModal.module.scss';

const initialValues = {
  graphName: '',
  guid: '',
  Name: '',
  cost: 0,
  data: {},
  labels: [],
  tags: [],
  vectors: [],
};

interface AddEditEdgeProps {
  isAddEditEdgeVisible: boolean;
  setIsAddEditEdgeVisible: Dispatch<SetStateAction<boolean>>;
  edge: EdgeType | null;
  selectedGraph: string;
  onEdgeUpdated?: () => Promise<void>;
  fromNodeGUID?: string;
  readonly?: boolean;
  onClose?: () => void;
  // Local state update functions for graph viewer
  updateLocalEdge?: (edge: any) => void;
  addLocalEdge?: (edge: any) => void;
  removeLocalEdge?: (edgeId: string) => void;
  // Current graph data for immediate updates
  currentNodes?: any[];
  currentEdges?: any[];
}

const AddEditEdge = ({
  isAddEditEdgeVisible,
  setIsAddEditEdgeVisible,
  edge: edgeWithOldData,
  selectedGraph,
  onEdgeUpdated,
  fromNodeGUID,
  onClose,
  readonly,
  updateLocalEdge,
  addLocalEdge,
  removeLocalEdge,
  currentNodes,
  currentEdges,
}: AddEditEdgeProps) => {
  const [form] = Form.useForm();
  const formValue = useWatch('from', form);
  // Get current GUID from form value
  const currentGUID = formValue;

  const [uniqueKey, setUniqueKey] = useState(v4());
  const [formValid, setFormValid] = useState(false);
  const isReadonlyView = Boolean(readonly && edgeWithOldData?.GUID);
  const {
    data: edge,
    isLoading: isEdgeLoading1,
    isFetching: isEdgeFetching,
    refetch: refetchEdge,
  } = useGetEdgeByIdQuery(
    {
      graphId: selectedGraph,
      edgeId: edgeWithOldData?.GUID || '',
      request: { includeData: true, includeSubordinates: true },
    },
    { skip: !edgeWithOldData?.GUID || !selectedGraph || !!(edgeWithOldData as any)?.isLocal }
  );
  const isEdgeLoading = isEdgeLoading1 || isEdgeFetching;
  const [createEdges, { isLoading: isCreateLoading }] = useCreateEdgeMutation();
  const [updateEdgeById, { isLoading: isUpdateLoading }] = useUpdateEdgeMutation();
  const { data: graph } = useGetGraphByIdQuery({ graphId: selectedGraph });

  // Add form validation watcher
  const [formValues, setFormValues] = useState({});
  useEffect(() => {
    form
      .validateFields({ validateOnly: true })
      .then(() => setFormValid(true))
      .catch(() => setFormValid(false));
  }, [formValues, form]);

  useEffect(() => {
    // Check if this is an existing edge (either API edge with GUID or local edge with id)
    const isExistingEdge = edgeWithOldData?.GUID;
    const graphName = typeof graph?.Name === 'string' ? graph.Name : '';

    if (edge && edgeWithOldData?.GUID && !(edgeWithOldData as any)?.isLocal) {
      // API edge - use API data
      form.resetFields();
      form.setFieldsValue({
        graphName,
        guid: edge.GUID || '',
        name: edge.Name,
        from: edge.From,
        to: edge.To,
        cost: edge.Cost,
        data: toPlainJson<Record<string, unknown>>(edge.Data, {}),
        labels: toPlainJson<string[]>(edge.Labels, []),
        tags: tagsToFormList(edge.Tags),
        vectors: vectorsToFormList(edge.Vectors),
      });
      setUniqueKey(v4());
    } else if (isExistingEdge && (edgeWithOldData as any)?.isLocal) {
      // Local edge - use local data
      form.resetFields();
      const localData =
        (edgeWithOldData as any).Data ||
        ((edgeWithOldData as any).data ? JSON.parse((edgeWithOldData as any).data) : {});
      form.setFieldsValue({
        graphName,
        guid: (edgeWithOldData as any).GUID || '',
        name: (edgeWithOldData as any).Name || (edgeWithOldData as any).label || '',
        from: (edgeWithOldData as any).From || (edgeWithOldData as any).source || '',
        to: (edgeWithOldData as any).To || (edgeWithOldData as any).target || '',
        cost: (edgeWithOldData as any).Cost || (edgeWithOldData as any).cost || 0,
        data: toPlainJson<Record<string, unknown>>(localData, {}),
        labels: toPlainJson<string[]>((edgeWithOldData as any).Labels, []),
        tags: tagsToFormList((edgeWithOldData as any).Tags),
        vectors: vectorsToFormList((edgeWithOldData as any).Vectors),
      });
      setUniqueKey(v4());
    } else if (!isExistingEdge) {
      // New edge
      form.resetFields();
      form.setFieldsValue({
        graphName,
        guid: '',
        from: fromNodeGUID || undefined,
        to: undefined,
        data: {},
      });
      setUniqueKey(v4());
    }

    // Trigger initial validation
    form
      .validateFields({ validateOnly: true })
      .then(() => setFormValid(true))
      .catch(() => setFormValid(false));
  }, [edge, selectedGraph, fromNodeGUID, form, edgeWithOldData?.GUID, graph?.Name]);

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
  const readonlyLabels = edge?.Labels || edgeWithOldData?.Labels || [];

  const handleSubmit = async () => {
    try {
      const values = await form.validateFields();
      const tags: Record<string, string> = convertTagsToRecord(values.tags);

      // Check if this is an existing edge (either API edge with GUID or local edge with id)
      const isExistingEdge = edgeWithOldData?.GUID;

      if (isExistingEdge) {
        // Edit edge
        if ((edgeWithOldData as any)?.isLocal && updateLocalEdge) {
          // Use local state update for graph viewer
          const updatedEdgeData = {
            id: edgeWithOldData.GUID,
            source: values.from,
            target: values.to,
            cost: values.cost,
            label: values.name,
            data: JSON.stringify(values.data),
            sourceX: 0, // These will be set by the graph layout
            sourceY: 0,
            targetX: 0,
            targetY: 0,
            isLocal: true,
            Name: values.name,
            From: values.from,
            To: values.to,
            Cost: values.cost,
            Data: values.data,
            Labels: values.labels || [],
            Tags: tags,
            Vectors: convertVectorsToAPIRecord(values.vectors),
          };
          updateLocalEdge(updatedEdgeData);
          toast.success('Update Edge successfully');
          setIsAddEditEdgeVisible(false);
          onEdgeUpdated && (await onEdgeUpdated());
        } else {
          // Fallback to API call for other contexts
          const data: Edge = {
            TenantGUID: edgeWithOldData.TenantGUID || '',
            LastUpdateUtc: edgeWithOldData.LastUpdateUtc || new Date().toISOString(),
            GUID: edgeWithOldData.GUID,
            GraphGUID: edgeWithOldData.GraphGUID || selectedGraph,
            CreatedUtc: edgeWithOldData.CreatedUtc || new Date().toISOString(),
            Name: values.name,
            From: values.from,
            To: values.to,
            Cost: values.cost,
            Data: values.data,
            Labels: values.labels || [],
            Tags: tags,
            Vectors: convertVectorsToAPIRecord(values.vectors),
          };
          const res = await updateEdgeById(data);
          if (res) {
            // Reflect change locally for immediate UI update
            if (updateLocalEdge) {
              const edgeId = edgeWithOldData.GUID;
              const updatedEdgeData = {
                id: edgeId,
                source: values.from,
                target: values.to,
                cost: values.cost,
                label: values.name,
                data: JSON.stringify(values.data ?? {}),
                sourceX: 0,
                sourceY: 0,
                targetX: 0,
                targetY: 0,
                isLocal: false,
              };
              updateLocalEdge(updatedEdgeData);
            }

            // Refetch the edge data to ensure UI reflects the latest changes
            if (edgeWithOldData?.GUID) {
              await refetchEdge();
            }

            toast.success('Update Edge successfully');
            setIsAddEditEdgeVisible(false);
            onEdgeUpdated && (await onEdgeUpdated());
          } else {
            throw new Error('Failed to update edge - no response received');
          }
        }
      } else {
        // Add edge - always call API first, then optionally mirror locally
        const data: EdgeCreateRequest = {
          GraphGUID: selectedGraph,
          Name: values.name,
          From: values.from,
          To: values.to,
          Cost: values.cost,
          Data: values.data,
          Labels: values.labels || [],
          Tags: tags,
          Vectors: convertVectorsToAPIRecord(values.vectors),
        };
        const res = await createEdges(data);
        if (res) {
          // Mirror into local graph state if available so user sees it instantly
          const created: any = (res as any)?.data || res;
          if (addLocalEdge) {
            const idForLocal = created?.GUID || v4();
            addLocalEdge({
              id: idForLocal,
              source: created?.From || values.from,
              target: created?.To || values.to,
              cost: created?.Cost ?? values.cost ?? 0,
              label: created?.Name || values.name,
              data: JSON.stringify(created?.Data ?? values.data ?? {}),
              sourceX: 0,
              sourceY: 0,
              targetX: 0,
              targetY: 0,
              isLocal: false,
              Name: created?.Name || values.name,
              From: created?.From || values.from,
              To: created?.To || values.to,
              Cost: created?.Cost ?? values.cost ?? 0,
              Data: created?.Data ?? values.data ?? {},
              Labels: created?.Labels ?? values.labels ?? [],
              Tags: created?.Tags ?? tags ?? {},
              Vectors: created?.Vectors ?? convertVectorsToAPIRecord(values.vectors) ?? [],
            });
          }
          toast.success('Add Edge successfully');
          setIsAddEditEdgeVisible(false);
          onEdgeUpdated && (await onEdgeUpdated());
        } else {
          throw new Error('Failed to create edge - no response received');
        }
      }
    } catch (error: unknown) {
      console.error('Error submitting form:', error);
      const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
      toast.error(`Failed to update edge: ${errorMessage}`);
    }
  };

  return (
    <LitegraphModal
      title={getCreateEditViewModelTitle(
        'Edge',
        isEdgeLoading,
        !edgeWithOldData?.GUID,
        !!edgeWithOldData?.GUID,
        Boolean(readonly && !!edgeWithOldData?.GUID)
      )}
      okText={edgeWithOldData?.GUID ? 'Update' : 'Create'}
      open={isAddEditEdgeVisible}
      onOk={handleSubmit}
      loading={isEdgeLoading}
      confirmLoading={isCreateLoading || isUpdateLoading}
      onCancel={() => {
        setIsAddEditEdgeVisible(false);
        onClose && onClose();
      }}
      width={isReadonlyView ? '95vw' : 800}
      centered={isReadonlyView}
      cancelText={readonly ? 'Close' : 'Cancel'}
      okButtonProps={{ disabled: isEdgeLoading || !formValid, hidden: readonly }}
      styles={readonlyModalBodyStyles}
      data-testid="add-edit-edge-modal"
      forceRender
    >
      {!isAddEditEdgeVisible ? (
        <Form form={form} style={{ display: 'none' }} />
      ) : isEdgeLoading ? (
        <>
          <Form form={form} style={{ display: 'none' }} />
          <PageLoading />
        </>
      ) : (
        <Form
          initialValues={{
            ...initialValues,
            guid: edge?.GUID || edgeWithOldData?.GUID || '',
            from: edge?.From || edgeWithOldData?.From || fromNodeGUID || '',
          }}
          form={form}
          layout="vertical"
          wrapperCol={{ span: 24 }}
          onValuesChange={(_, allValues) => setFormValues(allValues)}
          requiredMark={!readonly}
        >
          {isReadonlyView ? (
            <div
              className={`${modalStyles.summaryGrid} ${modalStyles.summaryGridExpanded}`}
              data-testid="edge-view-summary-grid"
            >
              <LitegraphFormItem
                label="Graph"
                name="graphName"
                tooltip="The graph this edge belongs to"
              >
                <LitegraphInput readOnly variant="borderless" />
              </LitegraphFormItem>

              <LitegraphFormItem
                label={
                  <LitegraphFlex align="center" gap={8}>
                    <span>GUID</span>
                    <CopyButton
                      getText={() => form.getFieldValue('guid') || ''}
                      tooltipTitle="Copy GUID"
                    />
                  </LitegraphFlex>
                }
                name="guid"
                tooltip="Globally unique identifier for this edge"
              >
                <LitegraphInput readOnly variant="borderless" />
              </LitegraphFormItem>

              <LitegraphFormItem
                label="Name"
                name="name"
                tooltip="Display name for the edge"
                rules={validationRules.Name}
              >
                <LitegraphInput
                  placeholder="Enter edge name"
                  data-testid="edge-name-input"
                  readOnly={readonly}
                  variant={readonly ? 'borderless' : 'outlined'}
                />
              </LitegraphFormItem>

              <LitegraphFormItem
                label="Cost"
                name="cost"
                tooltip="Traversal cost for this edge"
                rules={validationRules.Cost}
              >
                <LitegraphInput
                  readOnly={readonly}
                  variant={readonly ? 'borderless' : 'outlined'}
                  placeholder="Enter edge cost"
                  type="number"
                  onChange={(e) => {
                    const value = parseFloat(e.target.value);
                    form.setFieldsValue({ cost: isNaN(value) ? 0 : value });
                  }}
                />
              </LitegraphFormItem>

              <NodeSelector
                name="from"
                readonly={readonly}
                label="From Node"
                tooltip="Source node of the edge"
                rules={validationRules.From}
                localNodes={currentNodes}
              />
              <NodeSelector
                name="to"
                readonly={readonly}
                label="To Node"
                tooltip="Target node of the edge"
                rules={validationRules.To}
                localNodes={currentNodes}
              />
            </div>
          ) : (
            <>
              <LitegraphFlex vertical={!readonly} gap={readonly ? 10 : 0}>
                <LitegraphFormItem
                  className="flex-1"
                  label="Graph"
                  name="graphName"
                  tooltip="The graph this edge belongs to"
                >
                  <LitegraphInput readOnly variant="borderless" />
                </LitegraphFormItem>
                <LitegraphFormItem
                  className="flex-1"
                  label="Name"
                  name="name"
                  tooltip="Display name for the edge"
                  rules={validationRules.Name}
                >
                  <LitegraphInput
                    placeholder="Enter edge name"
                    data-testid="edge-name-input"
                    readOnly={readonly}
                    variant={readonly ? 'borderless' : 'outlined'}
                  />
                </LitegraphFormItem>
              </LitegraphFlex>
              <LitegraphFlex gap={10}>
                <NodeSelector
                  name="from"
                  readonly={readonly}
                  className="flex-1"
                  label="From Node"
                  tooltip="Source node of the edge"
                  rules={validationRules.From}
                  localNodes={currentNodes}
                />
                <NodeSelector
                  name="to"
                  readonly={readonly}
                  className="flex-1"
                  label="To Node"
                  tooltip="Target node of the edge"
                  rules={validationRules.To}
                  localNodes={currentNodes}
                />
              </LitegraphFlex>
              <LitegraphFlex gap={10}>
                <LitegraphFormItem
                  className="flex-1"
                  label="Cost"
                  name="cost"
                  tooltip="Traversal cost for this edge"
                  rules={validationRules.Cost}
                >
                  <LitegraphInput
                    readOnly={readonly}
                    variant={readonly ? 'borderless' : 'outlined'}
                    placeholder="Enter edge cost"
                    type="number"
                    onChange={(e) => {
                      const value = parseFloat(e.target.value);
                      form.setFieldsValue({ cost: isNaN(value) ? 0 : value });
                    }}
                  />
                </LitegraphFormItem>
                <LabelInput
                  name="labels"
                  className="flex-1"
                  readonly={readonly}
                  tooltip="Labels associated with this edge"
                />
              </LitegraphFlex>
            </>
          )}

          {isReadonlyView && (
            <Form.Item
              label="Labels"
              tooltip="Labels associated with this edge"
              className={modalStyles.fullSpan}
            >
              {readonlyLabels.length > 0 ? (
                <div className={modalStyles.badgeList} data-testid="edge-label-badges">
                  {readonlyLabels.map((label, index) => (
                    <Tag key={`${label}-${index}`} bordered={false} className={modalStyles.badge}>
                      {label}
                    </Tag>
                  ))}
                </div>
              ) : (
                <span className={modalStyles.emptyValue}>N/A</span>
              )}
            </Form.Item>
          )}

          <Form.Item
            label="Tags"
            tooltip="Key-value tags for this edge"
            className={isReadonlyView ? modalStyles.fullSpan : undefined}
          >
            <TagsInput name="tags" readonly={readonly} />
          </Form.Item>
          <Form.Item
            label="Vectors"
            tooltip="Vector embeddings for this edge"
            className={isReadonlyView ? modalStyles.fullSpan : undefined}
          >
            <VectorsInput name="vectors" readonly={readonly} />
          </Form.Item>
          <LitegraphFormItem
            className={isReadonlyView ? modalStyles.fullSpan : undefined}
            name="data"
            tooltip="Arbitrary JSON data attached to this edge"
            label={
              <LitegraphFlex align="center" gap={8}>
                <span>Data</span>
                {readonly && (
                  <CopyButton
                    getText={() => JSON.stringify(form.getFieldValue('data') || {}, null, 2)}
                    tooltipTitle="Copy Data"
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

export default AddEditEdge;
