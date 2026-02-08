import { Card, Button, Typography, message, Flex, Row, Col } from 'antd';
import {
  DownloadOutlined,
  ApiOutlined,
  BranchesOutlined,
  FileTextOutlined,
} from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import { useEndpoints } from '@/modules/endpoints/hooks';

function SummaryItem({
  icon,
  label,
  value,
}: {
  icon: React.ReactNode;
  label: string;
  value: number;
}) {
  return (
    <Flex align="center" gap={10}>
      <div
        style={{
          width: 36,
          height: 36,
          borderRadius: 10,
          background: 'var(--condition-bg)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          fontSize: 16,
          color: 'var(--color-text-secondary)',
        }}
      >
        {icon}
      </div>
      <div>
        <div style={{ fontSize: 12, color: 'var(--color-text-secondary)' }}>{label}</div>
        <div style={{ fontSize: 18, fontWeight: 700, color: 'var(--color-text)', lineHeight: 1.2 }}>
          {value}
        </div>
      </div>
    </Flex>
  );
}

export default function ExportPanel() {
  const { t } = useTranslation();
  const { data: endpoints } = useEndpoints();

  const totalEndpoints = endpoints?.length ?? 0;
  const totalRules = endpoints?.reduce((sum, e) => sum + (e.rules?.length ?? 0), 0) ?? 0;
  const fileName = `mock-server-export-${new Date().toISOString().slice(0, 10)}.json`;

  const handleExport = () => {
    if (!endpoints?.length) {
      message.warning(t('importExport.noDataToExport'));
      return;
    }
    const blob = new Blob([JSON.stringify(endpoints, null, 2)], {
      type: 'application/json',
    });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    a.click();
    URL.revokeObjectURL(url);
    message.success(t('importExport.exportSuccess'));
  };

  return (
    <Card title={t('importExport.export')}>
      <Typography.Paragraph type="secondary">
        {t('importExport.exportDesc')}
      </Typography.Paragraph>

      <Row gutter={[24, 16]} style={{ marginBottom: 24 }}>
        <Col>
          <SummaryItem
            icon={<ApiOutlined />}
            label={t('dashboard.totalEndpoints')}
            value={totalEndpoints}
          />
        </Col>
        <Col>
          <SummaryItem
            icon={<BranchesOutlined />}
            label={t('dashboard.totalRules')}
            value={totalRules}
          />
        </Col>
      </Row>

      <Flex align="center" gap={12}>
        <Button type="primary" icon={<DownloadOutlined />} onClick={handleExport}>
          {t('importExport.exportButton')}
        </Button>
        <Typography.Text type="secondary" style={{ fontSize: 13 }}>
          <FileTextOutlined style={{ marginRight: 4 }} />
          {fileName}
        </Typography.Text>
      </Flex>
    </Card>
  );
}
