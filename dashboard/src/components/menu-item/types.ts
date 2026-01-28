export interface MenuItemProps {
  key: string;
  icon?: React.ReactNode;
  label?: string;
  path?: string;
  children?: MenuItemProps[];
  props?: any;
}
