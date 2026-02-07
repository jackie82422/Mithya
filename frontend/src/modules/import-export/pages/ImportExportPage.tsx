import { Typography, Row, Col, Space } from 'antd';
import { useTranslation } from 'react-i18next';
import ExportPanel from '../components/ExportPanel';
import ImportPanel from '../components/ImportPanel';

export default function ImportExportPage() {
  const { t } = useTranslation();

  return (
    <Space direction="vertical" size="large" style={{ width: '100%' }}>
      <Typography.Title level={2} style={{ fontWeight: 600, letterSpacing: '-0.5px' }}>
        {t('importExport.title')}
      </Typography.Title>
      <Row gutter={24}>
        <Col xs={24} md={12}>
          <ExportPanel />
        </Col>
        <Col xs={24} md={12}>
          <ImportPanel />
        </Col>
      </Row>
    </Space>
  );
}
