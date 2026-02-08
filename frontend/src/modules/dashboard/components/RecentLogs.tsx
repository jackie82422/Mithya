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
              render: (p: string) => <Typography.Text code>{p}</Typography.Text>,
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
              render: (matched: boolean) => (
                <MatchPill
                  matched={matched}
                  label={matched ? t('logs.matched') : t('logs.unmatched')}
                />
              ),
            },
          ]}
        />
      )}
    </Card>
  );
}
