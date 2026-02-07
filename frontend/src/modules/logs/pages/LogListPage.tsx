import { useState } from 'react';
import { Typography, Flex } from 'antd';
import { useTranslation } from 'react-i18next';
import { useLogs } from '../hooks';
import LogTable from '../components/LogTable';
import LogDetail from '../components/LogDetail';
import type { MockRequestLog } from '@/shared/types';

export default function LogListPage() {
  const { t } = useTranslation();
  const { data: logs, isLoading } = useLogs(500);
  const [selectedLog, setSelectedLog] = useState<MockRequestLog | null>(null);

  return (
    <div>
      <Flex justify="space-between" align="center" style={{ marginBottom: 24 }}>
        <Typography.Title level={2} style={{ margin: 0, fontWeight: 600, letterSpacing: '-0.5px' }}>
          {t('logs.title')}
        </Typography.Title>
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
