import { Typography, Space, Tabs } from 'antd';
import { useTranslation } from 'react-i18next';
import ExportPanel from '../components/ExportPanel';
import ImportPanel from '../components/ImportPanel';
import OpenApiImportPanel from '../components/OpenApiImportPanel';

export default function ImportExportPage() {
  const { t } = useTranslation();

  return (
    <Space direction="vertical" size="large" style={{ width: '100%' }}>
      <Typography.Title level={2} style={{ fontWeight: 600, letterSpacing: '-0.5px' }}>
        {t('importExport.title')}
      </Typography.Title>
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
