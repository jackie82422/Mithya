import { Typography, Spin, Flex, Space, Card, Button, Empty } from 'antd';
import { PlusOutlined, CopyOutlined, LinkOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useEndpoints } from '@/modules/endpoints/hooks';
import { useLogs } from '@/modules/logs/hooks';
import { useServerConfig } from '@/shared/hooks/useServerConfig';
import StatsCards from '../components/StatsCards';
import EndpointOverview from '../components/EndpointOverview';
import RecentLogs from '../components/RecentLogs';

export default function DashboardPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { data: endpoints, isLoading: epLoading } = useEndpoints();
  const { data: logs, isLoading: logLoading } = useLogs(100);
  const { data: config } = useServerConfig();

  const isLoading = epLoading || logLoading;

  if (isLoading) {
    return (
      <Flex justify="center" style={{ padding: 80 }}>
        <Spin size="large" />
      </Flex>
    );
  }

  const hasEndpoints = (endpoints?.length ?? 0) > 0;

  return (
    <Space direction="vertical" size="large" style={{ width: '100%' }}>
      <div>
        <Typography.Title level={3} style={{ margin: 0, fontWeight: 600, letterSpacing: '-0.5px' }}>
          {t('dashboard.title')}
        </Typography.Title>
        <Typography.Text type="secondary" style={{ fontSize: 14 }}>
          {t('dashboard.subtitle')}
        </Typography.Text>
      </div>
      <StatsCards endpoints={endpoints ?? []} logs={logs ?? []} />
      {config?.mithyaUrl && (
        <Card size="small">
          <Flex align="center" gap={8}>
            <LinkOutlined style={{ color: 'var(--color-primary)' }} />
            <Typography.Text type="secondary">{t('dashboard.mithyaUrl')}:</Typography.Text>
            <Typography.Text
              copyable={{ icon: <CopyOutlined /> }}
              code
              style={{ fontSize: 13 }}
            >
              {config.mithyaUrl}
            </Typography.Text>
          </Flex>
        </Card>
      )}
      {hasEndpoints ? (
        <>
          <EndpointOverview endpoints={endpoints ?? []} />
          <RecentLogs logs={logs ?? []} />
        </>
      ) : (
        <Card>
          <Empty description={t('dashboard.emptyDesc')}>
            <Button
              type="primary"
              icon={<PlusOutlined />}
              onClick={() => navigate('/endpoints')}
            >
              {t('endpoints.create')}
            </Button>
          </Empty>
        </Card>
      )}
    </Space>
  );
}
