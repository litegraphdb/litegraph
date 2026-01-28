'use client';
import { Form } from 'antd';
import LitegraphModal from '@/components/base/modal/Modal';
import LitegraphFormItem from '@/components/base/form/FormItem';
import { Dispatch, SetStateAction, useEffect, useState } from 'react';
import LitegraphInput from '@/components/base/input/Input';
import { JsonEditor } from 'jsoneditor-react';
import { v4 } from 'uuid';
import { validationRules } from './constant';
import { NodeType } from '@/types/types';
import toast from 'react-hot-toast';
import VectorsInput from '@/components/inputs/vectors-input.tsx/VectorsInput';
import LabelInput from '@/components/inputs/label-input/LabelInput';
import TagsInput from '@/components/inputs/tags-input/TagsInput';
import { convertTagsToRecord } from '@/components/inputs/tags-input/utils';
import { convertVectorsToAPIRecord } from '@/components/inputs/vectors-input.tsx/utils';
import LitegraphFlex from '@/components/base/flex/Flex';
import { CopyOutlined } from '@ant-design/icons';
import { copyJsonToClipboard } from '@/utils/jsonCopyUtils';
import {
  useCreateNodeMutation,
  useGetGraphByIdQuery,
  useGetNodeByIdQuery,
  useUpdateNodeMutation,
} from '@/lib/store/slice/slice';
import { Node, NodeCreateRequest } from 'litegraphdb/dist/types/types';
import PageLoading from '@/components/base/loading/PageLoading';
import { getCreateEditViewModelTitle } from '@/utils/appUtils';

const initialValues = {
  graphName: '',
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
  const [form] = Form.useForm();
  const [formValid, setFormValid] = useState(false);
  const [uniqueKey, setUniqueKey] = useState(v4());

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

            toast.success('Update Node successfully');
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
          toast.success('Add Node successfully');
          setIsAddEditNodeVisible(false);
          onNodeUpdated && (await onNodeUpdated());
        } else {
          throw new Error('Failed to create node - no response received');
        }
      }
    } catch (error: unknown) {
      console.error('Error submitting form:', error);
      const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
      toast.error(`Failed to update node: ${errorMessage}`);
    }
  };

  useEffect(() => {
    if (node && nodeWithOldData?.GUID) {
      // Reset the form and set values for the new node
      form.resetFields();
      // Ensure form values are updated when editing
      form.setFieldsValue({
        name: node.Name || '',
        data: node.Data || {},
        labels: node.Labels || [],
        tags: Object.entries(node.Tags || {}).map(([key, value]) => ({
          key,
          value,
        })),
        vectors: node.Vectors || [],
      });
      setUniqueKey(v4());
    } else if (!nodeWithOldData?.GUID) {
      form.resetFields();
      setUniqueKey(v4());
    }
    graph?.Name && form.setFieldValue('graphName', graph.Name);
  }, [node, nodeWithOldData?.GUID, selectedGraph, graph?.Name]);

  useEffect(() => {
    // Trigger initial validation
    form
      .validateFields({ validateOnly: true })
      .then(() => setFormValid(true))
      .catch(() => setFormValid(false));
  }, [form]);

  return (
    <LitegraphModal
      maskClosable={false}
      title={getCreateEditViewModelTitle(
        'Node',
        isGraphLoading || isNodeLoading,
        !nodeWithOldData,
        !!nodeWithOldData,
        Boolean(readonly && !!nodeWithOldData)
      )}
      okText={nodeWithOldData?.GUID ? 'Update' : 'Create'}
      open={isAddEditNodeVisible}
      onOk={handleSubmit}
      confirmLoading={isCreateLoading || isUpdateLoading}
      onCancel={() => {
        setIsAddEditNodeVisible(false);
        onClose && onClose();
      }}
      width={800}
      cancelText={readonly ? 'Close' : 'Cancel'}
      okButtonProps={{
        disabled: !formValid,
        'data-testid': 'add-node-submit-button',
        hidden: readonly,
      }}
      data-testid="add-edit-node-modal"
    >
      {isGraphLoading || isNodeLoading ? (
        <PageLoading />
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
          <LitegraphFlex gap={readonly ? 10 : 0} vertical={!readonly}>
            <LitegraphFormItem className="flex-1" label="Graph" name="graphName">
              <LitegraphInput readOnly variant="borderless" />
            </LitegraphFormItem>

            <LitegraphFormItem
              className="flex-1"
              label="Name"
              name="name"
              rules={validationRules.name}
            >
              <LitegraphInput
                placeholder="Enter node name"
                data-testid="node-name-input"
                readOnly={readonly}
                variant={readonly ? 'borderless' : 'outlined'}
              />
            </LitegraphFormItem>
          </LitegraphFlex>
          <LabelInput name="labels" readonly={readonly} />

          <Form.Item label="Tags">
            <TagsInput name="tags" readonly={readonly} />
          </Form.Item>
          <Form.Item label="Vectors">
            <VectorsInput name="vectors" readonly={readonly} />
          </Form.Item>
          <LitegraphFormItem
            name="data"
            label={
              <LitegraphFlex align="center" gap={8}>
                <span>Data</span>
                {readonly && (
                  <CopyOutlined
                    style={{ cursor: 'pointer' }}
                    onClick={() => {
                      const data = form.getFieldValue('data') || {};
                      copyJsonToClipboard(data, 'Data');
                    }}
                  />
                )}
              </LitegraphFlex>
            }
          >
            <JsonEditor
              key={uniqueKey}
              value={form.getFieldValue('data') || {}}
              onChange={(json: any) => {
                form.setFieldsValue({ data: json });
              }}
              mode={readonly ? 'view' : 'code'}
              enableSort={false}
              enableTransform={false}
              mainMenuBar={!readonly}
              statusBar={!readonly}
              navigationBar={!readonly}
              data-testid="node-data-input"
            />
          </LitegraphFormItem>
        </Form>
      )}
    </LitegraphModal>
  );
};

export default AddEditNode;
