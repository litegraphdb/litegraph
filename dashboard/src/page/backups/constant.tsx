import { DeleteOutlined, DownloadOutlined, MoreOutlined } from '@ant-design/icons';
import { formatDateTime } from '@/utils/dateUtils';
import { Button, Dropdown, MenuProps, TableProps } from 'antd';
import { formatBytes } from '@/utils/appUtils';
import { LoaderIcon } from 'react-hot-toast';
import { onNameFilter } from '@/constants/table';
import { FilterDropdownProps } from 'antd/es/table/interface';
import { BackupMetaData } from 'litegraphdb/dist/types/types';
import TableSearch from '@/components/table-search/TableSearch';
import { columnTooltip } from '@/utils/tooltipUtils';
import LitegraphTooltip from '@/components/base/tooltip/Tooltip';

type Translator = (key: string, values?: Record<string, string | number>) => string;

const truncateCellStyle = {
  display: 'block',
  maxWidth: '100%',
  overflow: 'hidden',
  textOverflow: 'ellipsis',
  whiteSpace: 'nowrap',
} as const;

const monoTruncateCellStyle = {
  ...truncateCellStyle,
  fontFamily: 'monospace',
  fontSize: 12,
} as const;

export const tableColumns = (
  t: Translator,
  handleDelete: (backup: BackupMetaData) => void,
  handleDownload: (backup: BackupMetaData) => void,
  isDownloading: boolean
): TableProps<BackupMetaData>['columns'] => [
  {
    title: columnTooltip(t('columns.filename'), t('columns.filenameDesc')),
    dataIndex: 'Filename',
    key: 'Filename',
    width: 220,
    ellipsis: true,
    filterDropdown: (props: FilterDropdownProps) => (
      <TableSearch {...props} placeholder={t('search.filename')} />
    ),
    sorter: (a: BackupMetaData, b: BackupMetaData) => a.Filename.localeCompare(b.Filename),
    onFilter: (value, record) => onNameFilter(value, record.Filename),
    render: (Filename: string) => (
      <span data-testid="backup-filename" style={truncateCellStyle} title={Filename}>
        {Filename}
      </span>
    ),
  },
  {
    title: columnTooltip(t('columns.size'), t('columns.sizeDesc')),
    dataIndex: 'Length',
    key: 'Length',
    width: 100,
    render: (Length: number) => <div>{formatBytes(Length)}</div>,
  },
  {
    title: columnTooltip(t('columns.sha256'), t('columns.sha256Desc')),
    dataIndex: 'SHA256Hash',
    key: 'SHA256Hash',
    width: 240,
    ellipsis: true,
    render: (hash: string) => (
      <span style={monoTruncateCellStyle} title={hash}>
        {hash}
      </span>
    ),
  },
  {
    title: columnTooltip(t('columns.createdUtc'), t('columns.createdUtcDesc')),
    dataIndex: 'CreatedUtc',
    key: 'CreatedUtc',
    width: 150,
    ellipsis: true,
    sorter: (a: BackupMetaData, b: BackupMetaData) =>
      new Date(a.CreatedUtc).getTime() - new Date(b.CreatedUtc).getTime(),
    render: (CreatedUtc: string) => <div>{formatDateTime(CreatedUtc)}</div>,
  },
  {
    title: columnTooltip(t('columns.actions'), t('columns.actionsDesc')),
    key: 'actions',
    width: 70,
    render: (_: any, record: BackupMetaData) => {
      const items: MenuProps['items'] = [
        {
          key: 'download',
          label: t('rowActions.download'),
          onClick: () => handleDownload(record),
          icon: isDownloading ? <LoaderIcon /> : <DownloadOutlined />,
        },
        {
          key: 'delete',
          label: t('rowActions.delete'),
          onClick: () => handleDelete(record),
          icon: <DeleteOutlined />,
        },
      ];
      return (
        <Dropdown menu={{ items }} trigger={['click']} placement="bottomRight">
          <LitegraphTooltip title={t('rowActions.menu')}>
            <Button
              type="text"
              icon={<MoreOutlined style={{ fontSize: '20px' }} />}
              role="backup-action-menu"
              style={{ fontSize: '16px' }}
            />
          </LitegraphTooltip>
        </Dropdown>
      );
    },
  },
];
