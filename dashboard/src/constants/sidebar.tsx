import { paths } from './constant';
import {
  HomeOutlined,
  ShareAltOutlined,
  ApartmentOutlined,
  BranchesOutlined,
  CrownOutlined,
  TeamOutlined,
  TagOutlined,
  TagsOutlined,
  RadarChartOutlined,
  LockOutlined,
  SaveOutlined,
} from '@ant-design/icons';
import { MenuItemProps } from '@/components/menu-item/types';

export const tenantDashboardRoutes: MenuItemProps[] = [
  {
    key: '/',
    icon: <HomeOutlined />,
    label: 'Home',
    title: 'Dashboard overview',
    path: paths.dashboardHome,
  },
  {
    key: '/graphs',
    icon: <ShareAltOutlined />,
    label: 'Graphs',
    title: 'Manage graph containers',
    path: paths.graphs,
  },

  {
    key: '/nodes',
    icon: <ApartmentOutlined />,
    label: 'Nodes',
    title: 'Manage graph nodes',
    path: paths.nodes,
  },
  {
    key: '/edges',
    icon: <BranchesOutlined />,
    label: 'Edges',
    title: 'Manage graph edges',
    path: paths.edges,
  },
  {
    key: '/labels',
    icon: <TagOutlined />,
    label: 'Labels',
    title: 'Manage classification labels',
    path: paths.labels,
  },
  {
    key: '/tags',
    icon: <TagsOutlined />,
    label: 'Tags',
    title: 'Manage key-value tags',
    path: paths.tags,
  },
  {
    key: '/vectors',
    icon: <RadarChartOutlined />,
    label: 'Vectors',
    title: 'Manage vector embeddings',
    path: paths.vectors,
  },
];

export const adminDashboardRoutes: MenuItemProps[] = [
  {
    key: '/',
    icon: <CrownOutlined />,
    label: 'Tenants',
    title: 'Manage tenants',
    path: paths.adminDashboard,
  },
  {
    key: '/users',
    icon: <TeamOutlined />,
    label: 'Users',
    title: 'Manage user accounts',
    path: paths.users,
  },
  {
    key: '/credentials',
    icon: <LockOutlined />,
    label: 'Credentials',
    title: 'Manage API credentials',
    path: paths.credentials,
  },
  {
    key: '/backups',
    icon: <SaveOutlined />,
    label: 'Backups',
    title: 'Manage database backups',
    path: paths.backups,
  },
];
