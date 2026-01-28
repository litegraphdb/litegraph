import React, { useState, useEffect, useCallback } from 'react';
import { Form } from 'antd';
import { debounce } from 'lodash';
import useFormInstance from 'antd/es/form/hooks/useFormInstance';
import { useSearchEdgesMutation, useGetEdgeByIdQuery } from '@/lib/store/slice/slice';
import { useSelectedGraph } from '@/hooks/entityHooks';
import { skipToken } from '@reduxjs/toolkit/query';
import { Rule } from 'antd/es/form';
import { useWatch } from 'antd/es/form/Form';
import { Edge } from 'litegraphdb/dist/types/types';
import LitegraphSelect from '../base/select/Select';

type EdgeSelectorProps = {
  name: string;
  className?: string;
  readonly?: boolean;
  label?: string;
  required?: boolean;
  rules?: Rule[];
};

const EdgeSelector: React.FC<EdgeSelectorProps> = ({
  name,
  className,
  readonly,
  label,
  required,
  rules,
}) => {
  const selectedGraph = useSelectedGraph();
  const [searchEdges] = useSearchEdgesMutation();
  const form = useFormInstance();
  const [options, setOptions] = useState<Edge[]>([]);
  const [fetching, setFetching] = useState(false);
  const formValue = useWatch(name, form);

  const currentGUID = formValue;
  const edgeExists = !!options.find((opt) => opt.GUID === currentGUID);

  const {
    data: edgeData,
    isFetching: isFetchingEdge,
    isLoading: isLoadingEdge,
  } = useGetEdgeByIdQuery(
    !edgeExists && currentGUID && selectedGraph
      ? { graphId: selectedGraph, edgeId: currentGUID }
      : skipToken
  );

  const isEdgeLoading = isFetchingEdge || isLoadingEdge;

  useEffect(() => {
    if (edgeData) {
      setOptions((prev) => {
        if (prev.find((opt) => opt.GUID === edgeData.GUID)) {
          return prev;
        }
        return [...prev, edgeData];
      });
    }
  }, [edgeData]);

  const loadEdges = useCallback(
    debounce(async (value: string) => {
      setFetching(true);
      try {
        const response = await searchEdges({
          GraphGUID: selectedGraph,
          Name: value,
          Ordering: 'CreatedDescending',
        });
        setOptions(response?.data ? [response?.data as any] : []);
      } catch (err) {
        console.error('Failed to fetch edges', err);
        setOptions([]);
      } finally {
        setFetching(false);
      }
    }, 500),
    [searchEdges, selectedGraph]
  );

  const handleSearch = (value: string) => {
    if (value) {
      loadEdges(value);
    } else {
      setOptions([]);
    }
  };

  return (
    <Form.Item name={name} className={className} label={label} required={required} rules={rules}>
      <LitegraphSelect
        showSearch
        loading={isEdgeLoading || fetching}
        placeholder="Search and select edge"
        filterOption={false}
        onSearch={handleSearch}
        notFoundContent={fetching || isEdgeLoading ? 'Loading...' : 'No edges found'}
        options={options.map((edge) => ({
          label: edge.Name,
          value: edge.GUID,
          edge,
        }))}
        onChange={(value) => {
          form.setFieldValue(name, value);
        }}
        readonly={readonly}
        variant={readonly ? 'borderless' : 'outlined'}
      />
    </Form.Item>
  );
};

export default EdgeSelector;
