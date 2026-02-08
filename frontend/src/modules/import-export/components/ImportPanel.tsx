import { useState } from 'react';
import { Card, Upload, Button, Typography, message, Space, Modal } from 'antd';
import { UploadOutlined, ImportOutlined, ArrowRightOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import { useQueryClient } from '@tanstack/react-query';
import { endpointsApi } from '@/modules/endpoints/api';
import { rulesApi } from '@/modules/rules/api';
import type { MockEndpoint } from '@/shared/types';
import CodeEditor from '@/shared/components/CodeEditor';

export default function ImportPanel() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const qc = useQueryClient();
  const [preview, setPreview] = useState('');
  const [importing, setImporting] = useState(false);
  const [importedCount, setImportedCount] = useState<number | null>(null);

  const handleFileRead = (file: File) => {
    const reader = new FileReader();
    reader.onload = (e) => {
      const text = e.target?.result as string;
      setPreview(text);
    };
    reader.readAsText(file);
    return false;
  };

  const doImport = async (data: MockEndpoint[]) => {
    setImporting(true);
    try {
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
      setImportedCount(count);
      setPreview('');
    } catch {
      message.error(t('importExport.importError'));
    } finally {
      setImporting(false);
    }
  };

  const handleImport = async () => {
    if (!preview) return;
    try {
      const data: MockEndpoint[] = JSON.parse(preview);
      const existing = await endpointsApi.getAll();

      const existingSet = new Set(
        existing.map((ep) => `${ep.httpMethod.toUpperCase()}:${ep.path}`),
      );
      const duplicates = data.filter((ep) =>
        existingSet.has(`${ep.httpMethod.toUpperCase()}:${ep.path}`),
      );

      if (duplicates.length > 0) {
        Modal.confirm({
          title: t('importExport.duplicateConfirm'),
          content: (
            <div>
              <p>{t('importExport.duplicateWarning', { count: duplicates.length })}</p>
              <ul style={{ maxHeight: 200, overflow: 'auto', paddingLeft: 20 }}>
                {duplicates.map((ep, i) => (
                  <li key={i}>
                    <code>{ep.httpMethod.toUpperCase()} {ep.path}</code> — {ep.name}
                  </li>
                ))}
              </ul>
            </div>
          ),
          okText: t('common.confirm'),
          cancelText: t('common.cancel'),
          onOk: () => doImport(data),
        });
      } else {
        await doImport(data);
      }
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
              loading={importing}
            >
              {importing ? t('importExport.importing') : t('importExport.importButton')}
            </Button>
          </>
        )}

        {importedCount !== null && !preview && (
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
