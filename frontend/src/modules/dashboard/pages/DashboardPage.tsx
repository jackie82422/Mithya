import { Typography, Spin, Flex, Space } from 'antd';
import { useTranslation } from 'react-i18next';
import { useEndpoints } from '@/modules/endpoints/hooks';
import { useLogs } from '@/modules/logs/hooks';
import StatsCards from '../components/StatsCards';
import EndpointOverview from '../components/EndpointOverview';
import RecentLogs from '../components/RecentLogs';

export default function DashboardPage() {
  const { t } = useTranslation();
  const { data: endpoints, isLoading: epLoading } = useEndpoints();
  const { data: logs, isLoading: logLoading } = useLogs(100);

  const isLoading = epLoading || logLoading;

  if (isLoading) {
    return (
      <Flex justify="center" style={{ padding: 80 }}>
        <Spin size="large" />
      </Flex>
    );
  }

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
      <EndpointOverview endpoints={endpoints ?? []} />
      <RecentLogs logs={logs ?? []} />
    </Space>
  );
}
