import { Input } from 'antd';
import React from 'react';
import LitegraphFlex from '../base/flex/Flex';
import { FilterDropdownProps } from 'antd/es/table/interface';

const TableSearch = ({
  setSelectedKeys,
  selectedKeys,
  confirm,
  placeholder = 'Search',
}: FilterDropdownProps & { placeholder?: string }) => {
  return (
    <LitegraphFlex className="p">
      <Input.Search
        autoFocus
        placeholder={placeholder}
        value={selectedKeys[0]}
        onChange={(e) => {
          setSelectedKeys(e.target.value ? [e.target.value] : []);

          if (!e.target.value) {
            confirm();
          }
        }}
        allowClear
        onSearch={() => {
          confirm();
        }}
      />
    </LitegraphFlex>
  );
};

export default TableSearch;
