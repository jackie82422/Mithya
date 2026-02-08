import { Typography, Space, Tabs } from 'antd';
import { useTranslation } from 'react-i18next';
import ExportPanel from '../components/ExportPanel';
import ImportPanel from '../components/ImportPanel';
import OpenApiImportPanel from '../components/OpenApiImportPanel';

export default function ImportExportPage() {
  const { t } = useTranslation();

  return (
    <Space direction="vertical" size="large" style={{ width: '100%' }}>
      <div>
        <Typography.Title level={3} style={{ margin: 0, fontWeight: 600, letterSpacing: '-0.5px' }}>
          {t('importExport.title')}
        </Typography.Title>
        <Typography.Text type="secondary" style={{ fontSize: 14 }}>
          {t('importExport.subtitle')}
        </Typography.Text>
      </div>
      <Tabs
        items={[
          {
            key: 'export',
            label: t('importExport.export'),
            children: <ExportPanel />,
          },
          {
            key: 'import-json',
            label: t('importExport.import'),
            children: <ImportPanel />,
          },
          {
            key: 'import-openapi',
            label: t('importExport.openapi.tabTitle'),
            children: <OpenApiImportPanel />,
          },
        ]}
      />
    </Space>
  );
}
