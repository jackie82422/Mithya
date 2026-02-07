import { Card, Button, Typography, message } from 'antd';
import { DownloadOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import { useEndpoints } from '@/modules/endpoints/hooks';

export default function ExportPanel() {
  const { t } = useTranslation();
  const { data: endpoints } = useEndpoints();

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
    a.download = `mock-server-export-${new Date().toISOString().slice(0, 10)}.json`;
    a.click();
    URL.revokeObjectURL(url);
    message.success(t('importExport.exportSuccess'));
  };

  return (
    <Card title={t('importExport.export')}>
      <Typography.Paragraph>{t('importExport.exportDesc')}</Typography.Paragraph>
      <Button type="primary" icon={<DownloadOutlined />} onClick={handleExport}>
        {t('importExport.exportButton')}
      </Button>
    </Card>
  );
}
