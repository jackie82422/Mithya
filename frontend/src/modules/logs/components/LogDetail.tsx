import { Drawer, Descriptions, Tag, Typography } from 'antd';
import { useTranslation } from 'react-i18next';
import type { MockRequestLog } from '@/shared/types';
import HttpMethodTag from '@/shared/components/HttpMethodTag';
import CodeEditor from '@/shared/components/CodeEditor';

interface LogDetailProps {
  log: MockRequestLog | null;
  open: boolean;
  onClose: () => void;
}

function tryFormat(raw: string | null): string {
  if (!raw) return '';
  try {
    return JSON.stringify(JSON.parse(raw), null, 2);
  } catch {
    return raw;
  }
}

export default function LogDetail({ log, open, onClose }: LogDetailProps) {
  const { t } = useTranslation();

  if (!log) return null;

  return (
    <Drawer
      title={t('logs.detail')}
      open={open}
      onClose={onClose}
      width={700}
    >
      <Descriptions bordered column={1} size="small">
        <Descriptions.Item label={t('logs.timestamp')}>
          {new Date(log.timestamp).toLocaleString()}
        </Descriptions.Item>
        <Descriptions.Item label={t('logs.method')}>
          <HttpMethodTag method={log.method} />
        </Descriptions.Item>
        <Descriptions.Item label={t('logs.path')}>
          <Typography.Text code>{log.path}</Typography.Text>
        </Descriptions.Item>
        <Descriptions.Item label={t('logs.queryString')}>
          {log.queryString || '-'}
        </Descriptions.Item>
        <Descriptions.Item label={t('logs.statusCode')}>
          <Tag color={log.responseStatusCode < 400 ? 'green' : 'red'}>
            {log.responseStatusCode}
          </Tag>
        </Descriptions.Item>
        <Descriptions.Item label={t('logs.responseTime')}>
          {log.responseTimeMs} {t('logs.ms')}
        </Descriptions.Item>
        <Descriptions.Item label={t('logs.matchStatus')}>
          <Tag color={log.isMatched ? 'success' : 'warning'}>
            {log.isMatched ? t('logs.matched') : t('logs.unmatched')}
          </Tag>
        </Descriptions.Item>
      </Descriptions>

      {log.headers && (
        <>
          <Typography.Title level={5} style={{ marginTop: 16 }}>
            {t('logs.requestHeaders')}
          </Typography.Title>
          <CodeEditor value={tryFormat(log.headers)} readOnly height={150} />
        </>
      )}

      {log.body && (
        <>
          <Typography.Title level={5} style={{ marginTop: 16 }}>
            {t('logs.requestBody')}
          </Typography.Title>
          <CodeEditor value={tryFormat(log.body)} readOnly height={200} />
        </>
      )}

      {log.responseBody && (
        <>
          <Typography.Title level={5} style={{ marginTop: 16 }}>
            {t('logs.responseBody')}
          </Typography.Title>
          <CodeEditor value={tryFormat(log.responseBody)} readOnly height={200} />
        </>
      )}
    </Drawer>
  );
}
