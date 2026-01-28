'use client';
import { Form } from 'antd';
import { GraphData } from '@/types/types';
import LitegraphModal from '@/components/base/modal/Modal';
import LitegraphFormItem from '@/components/base/form/FormItem';
import { useEffect, useState } from 'react';
import LitegraphInput from '@/components/base/input/Input';
import { validationRules } from './constant';
import { JsonEditor } from 'jsoneditor-react';
import { v4 } from 'uuid';
import toast from 'react-hot-toast';
import LabelInput from '@/components/inputs/label-input/LabelInput';
import TagsInput from '@/components/inputs/tags-input/TagsInput';
import VectorsInput from '@/components/inputs/vectors-input.tsx/VectorsInput';
import { convertTagsToRecord } from '@/components/inputs/tags-input/utils';
import { convertVectorsToAPIRecord } from '@/components/inputs/vectors-input.tsx/utils';
import {
  useCreateGraphMutation,
  useGetGraphByIdQuery,
  useUpdateGraphMutation,
} from '@/lib/store/slice/slice';
import { GraphCreateRequest } from 'litegraphdb/dist/types/types';
import { getCreateEditViewModelTitle } from '@/utils/appUtils';
import PageLoading from '@/components/base/loading/PageLoading';
import { cloneDeep } from 'lodash';

const initialValues = {
  name: '',
  data: {},
  tags: [],
  labels: [],
  vectors: [],
};

interface AddEditGraphProps {
  isAddEditGraphVisible: boolean;
  setIsAddEditGraphVisible: (open: boolean) => void;
  graph: GraphData | null;
  onDone?: () => void;
}

//trigger initial validation

const AddEditGraph = ({
  isAddEditGraphVisible,
  setIsAddEditGraphVisible,
  graph: graphWithOldData,
  onDone,
}: AddEditGraphProps) => {
  const [form] = Form.useForm();
  const data = Form.useWatch('data', form);
  const [formValid, setFormValid] = useState(false);
  const {
    data: graph,
    isLoading: isGraphLoading1,
    isFetching: isGraphFetching,
    refetch: refetchGraph,
  } = useGetGraphByIdQuery(
    {
      graphId: graphWithOldData?.GUID || '',
      request: { includeData: true, includeSubordinates: true },
    },
    { skip: !graphWithOldData?.GUID }
  );

  const isGraphLoading = isGraphLoading1 || isGraphFetching;
  const [createGraph, { isLoading: isCreateLoading }] = useCreateGraphMutation();
  const [updateGraphById, { isLoading: isUpdateLoading }] = useUpdateGraphMutation();

  const [uniqueKey, setUniqueKey] = useState(v4());

  // Add form validation watcher
  const [formValues, setFormValues] = useState({});
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
      if (graph && graphWithOldData?.GUID) {
        // Edit Graph
        const data = {
          ...graph,
          GUID: graph.GUID,
          Name: values.name,
          Data: values.data || {},
          Labels: values.labels || [],
          Tags: tags,
          Vectors: convertVectorsToAPIRecord(values.vectors),
        };
        const res = await updateGraphById(data);
        if (res) {
          // Refetch the graph data to ensure UI reflects the latest changes
          if (graphWithOldData?.GUID) {
            await refetchGraph();
          }

          toast.success('Update Graph successfully');
          setIsAddEditGraphVisible(false);
          onDone?.();
        } else {
          throw new Error('Failed to update graph - no response received');
        }
      } else {
        // Add Graph
        const data: GraphCreateRequest = {
          Name: values.name,
          Data: values.data || {},
          Labels: values.labels || [],
          Tags: tags,
          Vectors: convertVectorsToAPIRecord(values.vectors),
        };
        const res = await createGraph(data);
        if (res) {
          toast.success('Add Graph successfully');
          setIsAddEditGraphVisible(false);
          onDone?.();
        } else {
          throw new Error('Failed to create graph - no response received');
        }
      }
    } catch (error: unknown) {
      console.error('Error submitting form:', error);
      const errorMessage = error instanceof Error ? error.message : 'Unknown error occurred';
      toast.error(`Failed to update graph: ${errorMessage}`);
    }
    // Trigger initial validation
    form
      .validateFields({ validateOnly: true })
      .then(() => setFormValid(true))
      .catch(() => setFormValid(false));
  };

  useEffect(() => {
    if (graph && graphWithOldData?.GUID) {
      // Ensure form values are updated when editing
      form.setFieldsValue({
        name: graph.Name || '',
        data: cloneDeep(graph.Data) || {},
        labels: graph.Labels || [],
        tags: Object.entries(graph.Tags || {}).map(([key, value]) => ({
          key,
          value,
        })),
        vectors: graph.Vectors || [],
      });
      setUniqueKey(v4());
    } else if (!graphWithOldData?.GUID) {
      form.resetFields();
      setUniqueKey(v4());
    }

    // Trigger initial validation
    form
      .validateFields({ validateOnly: true })
      .then(() => setFormValid(true))
      .catch(() => setFormValid(false));
  }, [form, graph, graphWithOldData?.GUID]);

  return (
    <LitegraphModal
      maskClosable={false}
      title={getCreateEditViewModelTitle(
        'Graph',
        isGraphLoading,
        !graphWithOldData,
        !!graphWithOldData
      )}
      okText={graphWithOldData?.GUID ? 'Update' : 'Create'}
      open={isAddEditGraphVisible}
      onOk={handleSubmit}
      confirmLoading={isCreateLoading || isUpdateLoading}
      onCancel={() => setIsAddEditGraphVisible(false)}
      width={800}
      okButtonProps={{ disabled: !formValid }}
      data-testid="add-edit-graph-modal"
    >
      {isGraphLoading ? (
        <PageLoading />
      ) : (
        <Form
          initialValues={initialValues}
          form={form}
          layout="vertical"
          labelCol={{ xs: 5, md: 5, lg: 4 }}
          wrapperCol={{ span: 24 }}
          onValuesChange={(_, allValues) => setFormValues(allValues)}
        >
          {/* Graph Name */}
          <LitegraphFormItem label="Name" name="name" rules={validationRules.name}>
            <LitegraphInput placeholder="Enter graph name" data-testid="graph-name-input" />
          </LitegraphFormItem>
          <LabelInput name="labels" />
          <Form.Item label="Tags">
            <TagsInput name="tags" />
          </Form.Item>
          <Form.Item label="Vectors">
            <VectorsInput name="vectors" />
          </Form.Item>
          <LitegraphFormItem label="Data" name="data">
            <JsonEditor
              key={uniqueKey}
              value={form.getFieldValue('data') || {}}
              onChange={(json: any) => {
                form.setFieldsValue({ data: json });
              }}
              mode="code"
              enableSort={false}
              enableTransform={false}
              data-testid="graph-data-input"
            />
          </LitegraphFormItem>
        </Form>
      )}
    </LitegraphModal>
  );
};

export default AddEditGraph;
