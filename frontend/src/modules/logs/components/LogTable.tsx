import { Table, Typography } from 'antd';
import { useTranslation } from 'react-i18next';
import type { MockRequestLog } from '@/shared/types';
import HttpMethodTag from '@/shared/components/HttpMethodTag';

interface LogTableProps {
  logs: MockRequestLog[];
  loading?: boolean;
  onRowClick: (log: MockRequestLog) => void;
}

function StatusCodePill({ code }: { code: number }) {
  const ok = code < 400;
  return (
    <span
      style={{
        padding: '2px 10px',
        borderRadius: 100,
        fontSize: 12,
        fontWeight: 600,
        background: ok ? 'var(--get-bg)' : 'var(--delete-bg)',
        color: ok ? 'var(--get-color)' : 'var(--delete-color)',
      }}
    >
      {code}
    </span>
  );
}

function MatchPill({ matched, label }: { matched: boolean; label: string }) {
  return (
    <span
      style={{
        padding: '2px 10px',
        borderRadius: 100,
        fontSize: 12,
        fontWeight: 500,
        background: matched ? 'var(--active-bg)' : 'var(--put-bg)',
        color: matched ? 'var(--active-color)' : 'var(--put-color)',
      }}
    >
      {label}
    </span>
  );
}

export default function LogTable({ logs, loading, onRowClick }: LogTableProps) {
  const { t } = useTranslation();

  return (
    <Table
      dataSource={logs}
      rowKey="id"
      loading={loading}
      size="small"
      pagination={{ pageSize: 20, showSizeChanger: true, pageSizeOptions: [10, 20, 50, 100] }}
      onRow={(record) => ({
        onClick: () => onRowClick(record),
        style: { cursor: 'pointer' },
      })}
      columns={[
        {
          title: t('logs.timestamp'),
          dataIndex: 'timestamp',
          width: 180,
          sorter: (a, b) => new Date(a.timestamp).getTime() - new Date(b.timestamp).getTime(),
          defaultSortOrder: 'descend',
          render: (ts: string) => new Date(ts).toLocaleString(),
        },
        {
          title: t('logs.method'),
          dataIndex: 'method',
          width: 90,
          filters: ['GET', 'POST', 'PUT', 'PATCH', 'DELETE'].map((m) => ({
            text: m,
            value: m,
          })),
          onFilter: (value, record) => record.method === value,
          render: (m: string) => <HttpMethodTag method={m} />,
        },
        {
          title: t('logs.path'),
          dataIndex: 'path',
          render: (p: string) => <Typography.Text code>{p}</Typography.Text>,
        },
        {
          title: t('logs.statusCode'),
          dataIndex: 'responseStatusCode',
          width: 100,
          render: (code: number) => <StatusCodePill code={code} />,
        },
        {
          title: t('logs.responseTime'),
          dataIndex: 'responseTimeMs',
          width: 120,
          sorter: (a, b) => a.responseTimeMs - b.responseTimeMs,
          render: (ms: number) => `${ms} ${t('logs.ms')}`,
        },
        {
          title: t('logs.matchStatus'),
          dataIndex: 'isMatched',
          width: 110,
          filters: [
            { text: t('logs.matched'), value: true },
            { text: t('logs.unmatched'), value: false },
          ],
          onFilter: (value, record) => record.isMatched === value,
          render: (matched: boolean) => (
            <MatchPill
              matched={matched}
              label={matched ? t('logs.matched') : t('logs.unmatched')}
            />
          ),
        },
      ]}
    />
  );
}
