import React, { useState, useEffect, useCallback } from 'react';
import { Form } from 'antd';
import { debounce } from 'lodash';
import useFormInstance from 'antd/es/form/hooks/useFormInstance';
import { Node } from 'litegraphdb/dist/types/types';
import { useSearchNodesMutation, useGetNodeByIdQuery } from '@/lib/store/slice/slice';
import { useSelectedGraph } from '@/hooks/entityHooks';
import { skipToken } from '@reduxjs/toolkit/query';
import { Rule } from 'antd/es/form';
import { useWatch } from 'antd/es/form/Form';
import LitegraphSelect from '../base/select/Select';

type NodeSelectorProps = {
  name: string; // form field name
  className?: string;
  readonly?: boolean;
  label?: string;
  required?: boolean;
  rules?: Rule[];
  // Add support for local nodes
  localNodes?: any[];
};

const NodeSelector: React.FC<NodeSelectorProps> = ({
  name,
  className,
  readonly,
  label,
  required,
  rules,
  localNodes = [],
}) => {
  const selectedGraph = useSelectedGraph();
  const [searchNodes] = useSearchNodesMutation();
  const form = useFormInstance();
  const [options, setOptions] = useState<Node[]>([]);
  const [fetching, setFetching] = useState(false);
  const formValue = useWatch(name, form);
  // Get current GUID from form value
  const currentGUID = formValue;
  // Check if node already exists in options
  const nodeExists = !!options.find((opt) => opt.GUID === currentGUID);

  // Query node by ID only if needed
  const {
    data: nodeData,
    isFetching: isFetchingNode,
    isLoading: isLoadingNode,
  } = useGetNodeByIdQuery(
    !nodeExists && currentGUID && selectedGraph
      ? { graphId: selectedGraph, nodeId: currentGUID }
      : skipToken
  );
  const isNodeLoading = isFetchingNode || isLoadingNode;

  // Add fetched node to options if not already present
  useEffect(() => {
    if (nodeData) {
      setOptions((prev) => {
        if (prev.find((opt) => opt.GUID === nodeData.GUID)) {
          return prev;
        }
        return [...prev, nodeData];
      });
    }
  }, [nodeData]);

  // Add local nodes to options
  useEffect(() => {
    if (localNodes && localNodes.length > 0) {
      setOptions((prev) => {
        const newOptions = [...prev];
        localNodes.forEach((localNode) => {
          // Check if this local node is already in options
          if (!newOptions.find((opt) => opt.GUID === localNode.id)) {
            // Convert NodeData format to Node format for consistency
            const nodeFormat: Node = {
              GUID: localNode.id,
              Name: localNode.label,
              Labels: [localNode.type || 'Unknown'],
              TenantGUID: '',
              GraphGUID: selectedGraph || '',
              CreatedUtc: new Date().toISOString(),
              LastUpdateUtc: new Date().toISOString(),
              Data: {},
              Tags: {},
              Vectors: [],
            };
            newOptions.push(nodeFormat);
          }
        });
        return newOptions;
      });
    }
  }, [localNodes, selectedGraph]);

  // Initialize options with local nodes when component first renders
  useEffect(() => {
    if (localNodes && localNodes.length > 0 && options.length === 0) {
      const initialLocalNodeOptions = localNodes.map((localNode) => ({
        GUID: localNode.id,
        Name: localNode.label,
        Labels: [localNode.type || 'Unknown'],
        TenantGUID: '',
        GraphGUID: selectedGraph || '',
        CreatedUtc: new Date().toISOString(),
        LastUpdateUtc: new Date().toISOString(),
        Data: {},
        Tags: {},
        Vectors: [],
      }));
      setOptions(initialLocalNodeOptions);
    }
  }, [localNodes, selectedGraph, options.length]);

  const loadNodes = useCallback(
    debounce(async (value: string) => {
      setFetching(true);
      try {
        const response = await searchNodes({
          GraphGUID: selectedGraph,
          Name: value,
          Ordering: 'CreatedDescending',
        });

        // Convert local nodes to Node format
        const localNodeOptions =
          localNodes?.map((localNode) => ({
            GUID: localNode.id,
            Name: localNode.label,
            Labels: [localNode.type || 'Unknown'],
            TenantGUID: '',
            GraphGUID: selectedGraph || '',
            CreatedUtc: new Date().toISOString(),
            LastUpdateUtc: new Date().toISOString(),
            Data: {},
            Tags: {},
            Vectors: [],
          })) || [];

        // Combine API search results with local nodes
        const apiNodes = response?.data?.Nodes || [];
        const allOptions = [...apiNodes, ...localNodeOptions];

        // Remove duplicates based on GUID
        const uniqueOptions = allOptions.filter(
          (node, index, self) => index === self.findIndex((n) => n.GUID === node.GUID)
        );

        setOptions(uniqueOptions);
      } catch (err) {
        console.error('Failed to fetch nodes', err);
        // On error, show only local nodes
        const localNodeOptions =
          localNodes?.map((localNode) => ({
            GUID: localNode.id,
            Name: localNode.label,
            Labels: [localNode.type || 'Unknown'],
            TenantGUID: '',
            GraphGUID: selectedGraph || '',
            CreatedUtc: new Date().toISOString(),
            LastUpdateUtc: new Date().toISOString(),
            Data: {},
            Tags: {},
            Vectors: [],
          })) || [];
        setOptions(localNodeOptions);
      } finally {
        setFetching(false);
      }
    }, 500),
    [searchNodes, selectedGraph, localNodes]
  );

  const handleSearch = (value: string) => {
    if (value) {
      loadNodes(value);
    } else {
      // When search is empty, show only local nodes
      const localNodeOptions =
        localNodes?.map((localNode) => ({
          GUID: localNode.id,
          Name: localNode.label,
          Labels: [localNode.type || 'Unknown'],
          TenantGUID: '',
          GraphGUID: selectedGraph || '',
          CreatedUtc: new Date().toISOString(),
          LastUpdateUtc: new Date().toISOString(),
          Data: {},
          Tags: {},
          Vectors: [],
        })) || [];
      setOptions(localNodeOptions);
    }
  };

  return (
    <Form.Item name={name} className={className} label={label} required={required} rules={rules}>
      <LitegraphSelect
        showSearch
        loading={isNodeLoading || fetching}
        placeholder="Search and select node"
        filterOption={false}
        onSearch={handleSearch}
        notFoundContent={fetching || isNodeLoading ? 'Loading...' : 'No nodes found'}
        options={options.map((node) => ({
          label: node.Name,
          value: node.GUID,
          node,
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

export default NodeSelector;
