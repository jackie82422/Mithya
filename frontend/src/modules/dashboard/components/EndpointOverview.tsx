import { Card, Table, Typography } from 'antd';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import type { MockEndpoint } from '@/shared/types';
import ProtocolTag from '@/shared/components/ProtocolTag';
import HttpMethodTag from '@/shared/components/HttpMethodTag';
import StatusBadge from '@/shared/components/StatusBadge';

interface EndpointOverviewProps {
  endpoints: MockEndpoint[];
}

export default function EndpointOverview({ endpoints }: EndpointOverviewProps) {
  const { t } = useTranslation();
  const navigate = useNavigate();

  return (
    <Card
      title={
        <span style={{ fontSize: 16, fontWeight: 600 }}>
          {t('dashboard.endpointOverview')}
        </span>
      }
    >
      <Table
        dataSource={endpoints}
        rowKey="id"
        size="small"
        pagination={false}
        scroll={{ x: 700 }}
        onRow={(record) => ({
          onClick: () => navigate(`/endpoints/${record.id}`),
          style: { cursor: 'pointer' },
        })}
        columns={[
          {
            title: t('common.name'),
            dataIndex: 'name',
            render: (name: string) => <Typography.Text strong>{name}</Typography.Text>,
          },
          {
            title: t('endpoints.protocol'),
            dataIndex: 'protocol',
            width: 100,
            render: (p) => <ProtocolTag protocol={p} />,
          },
          {
            title: t('endpoints.httpMethod'),
            dataIndex: 'httpMethod',
            width: 100,
            render: (m: string) => <HttpMethodTag method={m} />,
          },
          {
            title: t('endpoints.path'),
            dataIndex: 'path',
            render: (p: string) => <Typography.Text code>{p}</Typography.Text>,
          },
          {
            title: t('common.status'),
            dataIndex: 'isActive',
            width: 100,
            render: (active: boolean) => <StatusBadge active={active} />,
          },
          {
            title: t('rules.title'),
            dataIndex: 'rules',
            width: 80,
            render: (rules: unknown[]) => rules?.length ?? 0,
          },
        ]}
      />
    </Card>
  );
}
