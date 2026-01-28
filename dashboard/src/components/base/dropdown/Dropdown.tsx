import { Dropdown, DropDownProps } from 'antd';

const LitegraphDropdown = (props: DropDownProps) => {
  const { children, ...rest } = props;
  return <Dropdown {...rest}>{children}</Dropdown>;
};

export default LitegraphDropdown;
