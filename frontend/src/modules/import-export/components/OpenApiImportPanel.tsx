import { useState, useMemo } from 'react';
import {
  Card, Upload, Button, Typography, message, Space, Table, Alert,
  Input, Progress, Tooltip,
} from 'antd';
import { UploadOutlined, ImportOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import { useQueryClient } from '@tanstack/react-query';
import type { ColumnsType } from 'antd/es/table';
import { endpointsApi } from '@/modules/endpoints/api';
import { ProtocolType } from '@/shared/types';
import HttpMethodTag from '@/shared/components/HttpMethodTag';
import {
  parseOpenApiSpec,
  type ParsedEndpoint,
  type OpenApiParseResult,
} from '../utils/openapi-parser';

export default function OpenApiImportPanel() {
  const { t } = useTranslation();
  const qc = useQueryClient();

  const [result, setResult] = useState<OpenApiParseResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [serviceName, setServiceName] = useState('');
  const [selectedKeys, setSelectedKeys] = useState<string[]>([]);
  const [importing, setImporting] = useState(false);
  const [progress, setProgress] = useState({ current: 0, total: 0 });

  const handleFileRead = (file: File) => {
    const reader = new FileReader();
    reader.onload = (e) => {
      const text = e.target?.result as string;
      try {
        const parsed = parseOpenApiSpec(text);
        setResult(parsed);
        setError(null);
        setServiceName(parsed.title);
        setSelectedKeys(parsed.endpoints.map((ep) => ep.key));
      } catch (err) {
        setResult(null);
        const msg = (err as Error).message;
        if (msg === 'PARSE_ERROR') setError(t('importExport.openapi.parseError'));
        else if (msg === 'INVALID_SPEC') setError(t('importExport.openapi.invalidSpec'));
        else if (msg === 'NO_ENDPOINTS') setError(t('importExport.openapi.noEndpoints'));
        else setError(t('importExport.openapi.parseError'));
      }
    };
    reader.readAsText(file);
    return false;
  };

  const selectedCount = selectedKeys.length;

  const handleImport = async () => {
    if (!result || selectedCount === 0) return;

    const toImport = result.endpoints.filter((ep) => selectedKeys.includes(ep.key));
    setImporting(true);
    setProgress({ current: 0, total: toImport.length });

    let success = 0;
    let failed = 0;

    for (const ep of toImport) {
      try {
        const created = await endpointsApi.create({
          name: ep.name,
          serviceName: serviceName || ep.serviceName,
          protocol: ProtocolType.REST,
          path: ep.path,
          httpMethod: ep.httpMethod,
        });

        await endpointsApi.setDefaultResponse(created.id, {
          statusCode: ep.defaultStatusCode,
          responseBody: ep.defaultResponseBody,
        });

        success++;
      } catch {
        failed++;
      }
      setProgress((prev) => ({ ...prev, current: prev.current + 1 }));
    }

    qc.invalidateQueries({ queryKey: ['endpoints'] });

    if (failed === 0) {
      message.success(t('importExport.openapi.importSuccess', { count: success }));
    } else {
      message.warning(t('importExport.openapi.importPartial', { success, failed }));
    }

    setImporting(false);
    setResult(null);
    setProgress({ current: 0, total: 0 });
  };

  const columns: ColumnsType<ParsedEndpoint> = useMemo(
    () => [
      {
        title: 'Method',
        dataIndex: 'httpMethod',
        width: 100,
        render: (method: string) => <HttpMethodTag method={method} />,
      },
      {
        title: 'Path',
        dataIndex: 'path',
        render: (path: string) => <code>{path}</code>,
      },
      {
        title: 'Name',
        dataIndex: 'name',
        ellipsis: true,
      },
      {
        title: 'Status',
        dataIndex: 'defaultStatusCode',
        width: 80,
      },
      {
        title: 'Response',
        dataIndex: 'defaultResponseBody',
        width: 200,
        ellipsis: true,
        render: (body: string) => {
          const truncated = body.length > 60 ? body.slice(0, 60) + '...' : body;
          return (
            <Tooltip title={<pre style={{ maxHeight: 300, overflow: 'auto', margin: 0 }}>{body}</pre>}>
              <code style={{ fontSize: 12 }}>{truncated}</code>
            </Tooltip>
          );
        },
      },
    ],
    [],
  );

  const specVersionLabel = result?.specVersion === 'swagger2' ? 'Swagger 2.0' : 'OpenAPI 3.x';

  return (
    <Card title={t('importExport.openapi.tabTitle')}>
      <Space direction="vertical" style={{ width: '100%' }} size="middle">
        <Typography.Paragraph>{t('importExport.openapi.desc')}</Typography.Paragraph>

        <Upload
          accept=".json,.yaml,.yml"
          beforeUpload={handleFileRead}
          showUploadList={false}
          maxCount={1}
        >
          <Button icon={<UploadOutlined />}>{t('importExport.openapi.selectFile')}</Button>
        </Upload>

        <Typography.Text type="secondary">{t('importExport.openapi.acceptFormats')}</Typography.Text>

        {error && <Alert type="error" message={error} showIcon />}

        {result && (
          <>
            {result.warnings.length > 0 && (
              <Alert
                type="warning"
                message={t('importExport.openapi.warnings')}
                description={result.warnings.map((w, i) => <div key={i}>{w}</div>)}
                showIcon
              />
            )}

            <Typography.Text>
              {t('importExport.openapi.specInfo', {
                specVersion: specVersionLabel,
                title: result.title,
                version: result.version,
                count: result.endpoints.length,
              })}
            </Typography.Text>

            <div>
              <Typography.Text strong>{t('importExport.openapi.serviceName')}</Typography.Text>
              <Typography.Text type="secondary" style={{ marginLeft: 8 }}>
                {t('importExport.openapi.serviceNameHint')}
              </Typography.Text>
              <Input
                value={serviceName}
                onChange={(e) => setServiceName(e.target.value)}
                style={{ marginTop: 4 }}
              />
            </div>

            <Table
              dataSource={result.endpoints}
              columns={columns}
              rowKey="key"
              size="small"
              pagination={false}
              scroll={{ y: 400 }}
              rowSelection={{
                selectedRowKeys: selectedKeys,
                onChange: (keys) => setSelectedKeys(keys as string[]),
              }}
            />

            {importing && (
              <div>
                <Typography.Text>
                  {t('importExport.openapi.progress', {
                    current: progress.current,
                    total: progress.total,
                  })}
                </Typography.Text>
                <Progress
                  percent={Math.round((progress.current / progress.total) * 100)}
                  status="active"
                />
              </div>
            )}

            <Button
              type="primary"
              icon={<ImportOutlined />}
              onClick={handleImport}
              loading={importing}
              disabled={selectedCount === 0}
            >
              {importing
                ? t('importExport.openapi.importing')
                : t('importExport.openapi.importButton', { count: selectedCount })}
            </Button>
          </>
        )}
      </Space>
    </Card>
  );
}
