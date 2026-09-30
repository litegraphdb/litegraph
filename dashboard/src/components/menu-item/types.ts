export interface MenuItemProps {
  key: string;
  icon?: React.ReactNode;
  label?: string;
  title?: string;
  /** When 'group', renders as a non-clickable section header with children. */
  type?: 'group';
  /** i18n key resolved at render time; falls back to `label` when absent. */
  labelKey?: string;
  /** i18n key resolved at render time; falls back to `title` when absent. */
  titleKey?: string;
  path?: string;
  /** When true, the item is also selected on any path below `path` (hub entries with tabs). */
  matchPrefix?: boolean;
  children?: MenuItemProps[];
  props?: any;
}
