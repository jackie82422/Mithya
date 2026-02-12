import { useState } from 'react';
import { Card, Upload, Button, Typography, message, Space } from 'antd';
import { UploadOutlined, ImportOutlined, ArrowRightOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { useImportJson } from '../hooks';
import CodeEditor from '@/shared/components/CodeEditor';

export default function ImportPanel() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const [preview, setPreview] = useState('');
  const importMutation = useImportJson();

  const handleFileRead = (file: File) => {
    const reader = new FileReader();
    reader.onload = (e) => {
      const text = e.target?.result as string;
      setPreview(text);
    };
    reader.readAsText(file);
    return false;
  };

  const handleImport = async () => {
    if (!preview) return;
    try {
      const data = JSON.parse(preview);
      const endpoints = Array.isArray(data) ? data : data.endpoints ?? [];
      const serviceProxies = Array.isArray(data) ? undefined : data.serviceProxies;
      const result = await importMutation.mutateAsync({ endpoints, serviceProxies });

      if (result.skipped > 0) {
        message.warning(
          t('importExport.importPartial', {
            imported: result.imported,
            skipped: result.skipped,
          }),
        );
      } else {
        message.success(t('importExport.importSuccess', { count: result.imported }));
      }
      setPreview('');
    } catch {
      message.error(t('importExport.importError'));
    }
  };

  return (
    <Card title={t('importExport.import')}>
      <Typography.Paragraph>{t('importExport.importDesc')}</Typography.Paragraph>
      <Space direction="vertical" style={{ width: '100%' }}>
        <Upload
          accept=".json"
          beforeUpload={handleFileRead}
          showUploadList={false}
          maxCount={1}
        >
          <Button icon={<UploadOutlined />}>{t('importExport.selectFile')}</Button>
        </Upload>

        {preview && (
          <>
            <Typography.Text strong>{t('importExport.preview')}:</Typography.Text>
            <CodeEditor value={preview} readOnly height={300} />
            <Button
              type="primary"
              icon={<ImportOutlined />}
              onClick={handleImport}
              loading={importMutation.isPending}
            >
              {importMutation.isPending
                ? t('importExport.importing')
                : t('importExport.importButton')}
            </Button>
          </>
        )}

        {importMutation.isSuccess && !preview && (
          <Button
            type="link"
            icon={<ArrowRightOutlined />}
            onClick={() => navigate('/endpoints')}
            style={{ paddingLeft: 0 }}
          >
            {t('importExport.goToEndpoints')}
          </Button>
        )}
      </Space>
    </Card>
  );
}
