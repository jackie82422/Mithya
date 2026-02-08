import { useState } from 'react';
import { Typography, Flex, Button, Switch, Space } from 'antd';
import { ReloadOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import { useLogs } from '../hooks';
import LogTable from '../components/LogTable';
import LogDetail from '../components/LogDetail';
import type { MockRequestLog } from '@/shared/types';

const POLL_INTERVAL = 5000;

export default function LogListPage() {
  const { t } = useTranslation();
  const [autoRefresh, setAutoRefresh] = useState(false);
  const { data: logs, isLoading, refetch } = useLogs(500, autoRefresh ? POLL_INTERVAL : false);
  const [selectedLog, setSelectedLog] = useState<MockRequestLog | null>(null);

  return (
    <div>
      <Flex justify="space-between" align="center" style={{ marginBottom: 24 }}>
        <Typography.Title level={2} style={{ margin: 0, fontWeight: 600, letterSpacing: '-0.5px' }}>
          {t('logs.title')}
        </Typography.Title>
        <Space size="middle">
          <Flex align="center" gap={8}>
            <Switch
              size="small"
              checked={autoRefresh}
              onChange={setAutoRefresh}
            />
            <Typography.Text type="secondary" style={{ fontSize: 13 }}>
              {t('logs.autoRefresh')}
            </Typography.Text>
          </Flex>
          <Button
            icon={<ReloadOutlined />}
            onClick={() => refetch()}
            loading={isLoading}
          >
            {t('logs.refresh')}
          </Button>
        </Space>
      </Flex>
      <LogTable
        logs={logs ?? []}
        loading={isLoading}
        onRowClick={(log) => setSelectedLog(log)}
      />
      <LogDetail
        log={selectedLog}
        open={!!selectedLog}
        onClose={() => setSelectedLog(null)}
      />
    </div>
  );
}
