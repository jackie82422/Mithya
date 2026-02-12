import { Card, Table, Typography } from 'antd';
import { useTranslation } from 'react-i18next';
import type { MockRequestLog } from '@/shared/types';
import HttpMethodTag from '@/shared/components/HttpMethodTag';

interface RecentLogsProps {
  logs: MockRequestLog[];
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

export default function RecentLogs({ logs }: RecentLogsProps) {
  const { t } = useTranslation();

  return (
    <Card
      title={
        <span style={{ fontSize: 16, fontWeight: 600 }}>
          {t('dashboard.recentLogs')}
        </span>
      }
    >
      {logs.length === 0 ? (
        <Typography.Text type="secondary">{t('dashboard.noLogs')}</Typography.Text>
      ) : (
        <Table
          dataSource={logs.slice(0, 10)}
          rowKey="id"
          size="small"
          pagination={false}
          scroll={{ x: 650 }}
          columns={[
            {
              title: t('logs.timestamp'),
              dataIndex: 'timestamp',
              width: 180,
              render: (ts: string) => new Date(ts).toLocaleString(),
            },
            {
              title: t('logs.method'),
              dataIndex: 'method',
              width: 90,
              render: (m: string) => <HttpMethodTag method={m} />,
            },
            {
              title: t('logs.path'),
              dataIndex: 'path',
              render: (p: string, record: MockRequestLog) => (
                <Typography.Text code>
                  {record.queryString ? `${p}${record.queryString.startsWith('?') ? '' : '?'}${record.queryString}` : p}
                </Typography.Text>
              ),
            },
            {
              title: t('logs.statusCode'),
              dataIndex: 'responseStatusCode',
              width: 100,
              render: (code: number) => <StatusCodePill code={code} />,
            },
            {
              title: t('logs.matchStatus'),
              dataIndex: 'isMatched',
              width: 100,
              render: (matched: boolean, record: MockRequestLog) => {
                const isDefault = matched && !record.ruleId;
                const label = !matched ? t('logs.unmatched') : isDefault ? t('logs.matchedDefault') : t('logs.matched');
                return (
                  <MatchPill matched={matched} isDefault={isDefault} label={label} />
                );
              },
            },
          ]}
        />
      )}
    </Card>
  );
}
