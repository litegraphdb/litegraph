export interface MenuItemProps {
  key: string;
  icon?: React.ReactNode;
  label?: string;
  title?: string;
  path?: string;
  children?: MenuItemProps[];
  props?: any;
}
