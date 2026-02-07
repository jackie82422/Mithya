import { useState } from 'react';
import { Card, Upload, Button, Typography, message, Space } from 'antd';
import { UploadOutlined, ImportOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import { useQueryClient } from '@tanstack/react-query';
import { endpointsApi } from '@/modules/endpoints/api';
import { rulesApi } from '@/modules/rules/api';
import type { MockEndpoint } from '@/shared/types';
import CodeEditor from '@/shared/components/CodeEditor';

export default function ImportPanel() {
  const { t } = useTranslation();
  const qc = useQueryClient();
  const [preview, setPreview] = useState('');
  const [importing, setImporting] = useState(false);

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
      setImporting(true);
      const data: MockEndpoint[] = JSON.parse(preview);
      let count = 0;
      for (const ep of data) {
        const created = await endpointsApi.create({
          name: ep.name,
          serviceName: ep.serviceName,
          protocol: ep.protocol,
          path: ep.path,
          httpMethod: ep.httpMethod,
          protocolSettings: ep.protocolSettings,
        });

        if (ep.defaultResponse) {
          await endpointsApi.setDefaultResponse(created.id, {
            statusCode: ep.defaultStatusCode ?? 200,
            responseBody: ep.defaultResponse,
          });
        }

        if (ep.rules?.length) {
          for (const rule of ep.rules) {
            let conditions;
            try {
              conditions = JSON.parse(rule.matchConditions);
            } catch {
              conditions = [];
            }
            let responseHeaders;
            try {
              responseHeaders = rule.responseHeaders
                ? JSON.parse(rule.responseHeaders)
                : undefined;
            } catch {
              responseHeaders = undefined;
            }
            await rulesApi.create(created.id, {
              ruleName: rule.ruleName,
              priority: rule.priority,
              conditions,
              statusCode: rule.responseStatusCode,
              responseBody: rule.responseBody,
              responseHeaders,
              delayMs: rule.delayMs,
            });
          }
        }
        count++;
      }
      qc.invalidateQueries({ queryKey: ['endpoints'] });
      message.success(t('importExport.importSuccess', { count }));
      setPreview('');
    } catch {
      message.error(t('importExport.importError'));
    } finally {
      setImporting(false);
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
              loading={importing}
            >
              {importing ? t('importExport.importing') : t('importExport.importButton')}
            </Button>
          </>
        )}
      </Space>
    </Card>
  );
}
