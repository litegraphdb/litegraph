export interface MenuItemProps {
  key: string;
  icon?: React.ReactNode;
  label?: string;
  title?: string;
  /** i18n key resolved at render time; falls back to `label` when absent. */
  labelKey?: string;
  /** i18n key resolved at render time; falls back to `title` when absent. */
  titleKey?: string;
  path?: string;
  children?: MenuItemProps[];
  props?: any;
}
