import { Card, Table, Tag, Typography } from 'antd';
import { useTranslation } from 'react-i18next';
import type { MockRequestLog } from '@/shared/types';
import HttpMethodTag from '@/shared/components/HttpMethodTag';

interface RecentLogsProps {
  logs: MockRequestLog[];
}

export default function RecentLogs({ logs }: RecentLogsProps) {
  const { t } = useTranslation();

  return (
    <Card title={t('dashboard.recentLogs')}>
      {logs.length === 0 ? (
        <Typography.Text type="secondary">{t('dashboard.noLogs')}</Typography.Text>
      ) : (
        <Table
          dataSource={logs.slice(0, 10)}
          rowKey="id"
          size="small"
          pagination={false}
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
              render: (code: number) => (
                <Tag color={code < 400 ? 'green' : 'red'}>{code}</Tag>
              ),
            },
            {
              title: t('logs.matchStatus'),
              dataIndex: 'isMatched',
              width: 100,
              render: (matched: boolean) => (
                <Tag color={matched ? 'success' : 'warning'}>
                  {matched ? t('logs.matched') : t('logs.unmatched')}
                </Tag>
              ),
            },
          ]}
        />
      )}
    </Card>
  );
}
