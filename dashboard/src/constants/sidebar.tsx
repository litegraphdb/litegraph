import { paths } from './constant';
import {
  HomeOutlined,
  ShareAltOutlined,
  NodeIndexOutlined,
  ApiOutlined,
  CrownOutlined,
  TeamOutlined,
  TagsOutlined,
  LockOutlined,
  SaveOutlined,
} from '@ant-design/icons';
import { MenuItemProps } from '@/components/menu-item/types';

export const tenantDashboardRoutes: MenuItemProps[] = [
  {
    key: '/',
    icon: <HomeOutlined />,
    label: 'Home',
    path: paths.dashboardHome,
  },
  {
    key: '/graphs',
    icon: <ShareAltOutlined />,
    label: 'Graphs',
    path: paths.graphs,
  },

  {
    key: '/nodes',
    icon: <NodeIndexOutlined />,
    label: 'Nodes',
    path: paths.nodes,
  },
  {
    key: '/edges',
    icon: <ApiOutlined />,
    label: 'Edges',
    path: paths.edges,
  },
  {
    key: '/labels',
    icon: <TagsOutlined />,
    label: 'Labels',
    path: paths.labels,
  },
  {
    key: '/tags',
    icon: <TagsOutlined />,
    label: 'Tags',
    path: paths.tags,
  },
  {
    key: '/vectors',
    icon: <TagsOutlined />,
    label: 'Vectors',
    path: paths.vectors,
  },
];

export const adminDashboardRoutes: MenuItemProps[] = [
  {
    key: '/',
    icon: <CrownOutlined />,
    label: 'Tenants',
    path: paths.adminDashboard,
  },
  {
    key: '/users',
    icon: <TeamOutlined />,
    label: 'Users',
    path: paths.users,
  },
  {
    key: '/credentials',
    icon: <LockOutlined />,
    label: 'Credentials',
    path: paths.credentials,
  },
  {
    key: '/backups',
    icon: <SaveOutlined />,
    label: 'Backups',
    path: paths.backups,
  },
];
