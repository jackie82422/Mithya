import { Table, Typography, Space } from 'antd';
import { useTranslation } from 'react-i18next';
import type { MockRequestLog } from '@/shared/types';
import { FaultType, FaultTypeLabel } from '@/shared/types';
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
        whiteSpace: 'nowrap',
        background: ok ? 'var(--get-bg)' : 'var(--delete-bg)',
        color: ok ? 'var(--get-color)' : 'var(--delete-color)',
      }}
    >
      {code}
    </span>
  );
}

function MatchPill({ matched, isDefault, label }: { matched: boolean; isDefault?: boolean; label: string }) {
  const bg = !matched ? 'var(--put-bg)' : isDefault ? 'var(--patch-bg, var(--put-bg))' : 'var(--active-bg)';
  const color = !matched ? 'var(--put-color)' : isDefault ? 'var(--patch-color, var(--put-color))' : 'var(--active-color)';
  return (
    <span
      style={{
        padding: '2px 10px',
        borderRadius: 100,
        fontSize: 12,
        fontWeight: 500,
        whiteSpace: 'nowrap',
        background: bg,
        color: color,
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
      scroll={{ x: 900 }}
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
          ellipsis: true,
          render: (_: string, record) => {
            const qs = record.queryString?.startsWith('?')
              ? record.queryString
              : record.queryString ? `?${record.queryString}` : '';
            const fullPath = `${record.path}${qs}`;
            return <Typography.Text code>{fullPath}</Typography.Text>;
          },
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
          width: 130,
          filters: [
            { text: t('logs.matched'), value: true },
            { text: t('logs.unmatched'), value: false },
          ],
          onFilter: (value, record) => record.isMatched === value,
          render: (matched: boolean, record) => {
            if (record.isProxied) {
              return (
                <span style={{
                  padding: '2px 10px',
                  borderRadius: 100,
                  fontSize: 12,
                  fontWeight: 500,
                  whiteSpace: 'nowrap',
                  background: 'var(--stats-blue-bg)',
                  color: 'var(--stats-blue-icon)',
                }}>
                  {t('proxy.proxied')}
                </span>
              );
            }
            const isDefault = matched && !record.ruleId;
            const label = !matched ? t('logs.unmatched') : isDefault ? t('logs.matchedDefault') : t('logs.matched');
            return (
              <Space size={4}>
                <MatchPill matched={matched} isDefault={isDefault} label={label} />
                {record.faultTypeApplied != null && record.faultTypeApplied !== FaultType.None && (
                  <span style={{
                    padding: '2px 8px',
                    borderRadius: 100,
                    fontSize: 11,
                    fontWeight: 500,
                    whiteSpace: 'nowrap',
                    background: 'var(--delete-bg)',
                    color: 'var(--delete-color)',
                  }}>
                    {FaultTypeLabel[record.faultTypeApplied]}
                  </span>
                )}
              </Space>
            );
          },
        },
      ]}
    />
  );
}
