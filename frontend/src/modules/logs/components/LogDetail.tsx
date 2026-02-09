import { Drawer, Descriptions, Typography } from 'antd';
import { useTranslation } from 'react-i18next';
import type { MockRequestLog } from '@/shared/types';
import { FaultType, FaultTypeLabel } from '@/shared/types';
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

function StatusCodePill({ code }: { code: number }) {
  const ok = code < 400;
  return (
    <span
      style={{
        padding: '2px 10px',
        borderRadius: 100,
        fontSize: 12,
        fontWeight: 600,
        background: ok ? 'var(--get-bg)' : 'var(--delete-bg)',
        color: ok ? 'var(--get-color)' : 'var(--delete-color)',
      }}
    >
      {code}
    </span>
  );
}

function MatchPill({ matched, isDefault, label }: { matched: boolean; isDefault?: boolean; label: string }) {
  const bg = !matched ? 'var(--put-bg)' : isDefault ? 'var(--patch-bg, var(--put-bg))' : 'var(--active-bg)';
  const color = !matched ? 'var(--put-color)' : isDefault ? 'var(--patch-color, var(--put-color))' : 'var(--active-color)';
  return (
    <span
      style={{
        padding: '2px 10px',
        borderRadius: 100,
        fontSize: 12,
        fontWeight: 500,
        background: bg,
        color: color,
      }}
    >
      {label}
    </span>
  );
}

export default function LogDetail({ log, open, onClose }: LogDetailProps) {
  const { t } = useTranslation();

  if (!log) return null;

  return (
    <Drawer
      title={t('logs.detail')}
      open={open}
      onClose={onClose}
      width={720}
      styles={{ body: { padding: 24 } }}
    >
      <Descriptions column={1} size="small">
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
          <StatusCodePill code={log.responseStatusCode} />
        </Descriptions.Item>
        <Descriptions.Item label={t('logs.responseTime')}>
          {log.responseTimeMs} {t('logs.ms')}
        </Descriptions.Item>
        <Descriptions.Item label={t('logs.matchStatus')}>
          {(() => {
            const isDefault = log.isMatched && !log.ruleId;
            const label = !log.isMatched ? t('logs.unmatched') : isDefault ? t('logs.matchedDefault') : t('logs.matched');
            return <MatchPill matched={log.isMatched} isDefault={isDefault} label={label} />;
          })()}
        </Descriptions.Item>
        {log.isProxied && (
          <Descriptions.Item label={t('proxy.proxyTarget')}>
            <Typography.Text code>{log.proxyTargetUrl ?? '-'}</Typography.Text>
          </Descriptions.Item>
        )}
        {log.faultTypeApplied != null && log.faultTypeApplied !== FaultType.None && (
          <Descriptions.Item label={t('rules.faultType')}>
            <span style={{
              padding: '2px 8px',
              borderRadius: 100,
              fontSize: 12,
              fontWeight: 500,
              background: 'var(--delete-bg)',
              color: 'var(--delete-color)',
            }}>
              {FaultTypeLabel[log.faultTypeApplied]}
            </span>
          </Descriptions.Item>
        )}
      </Descriptions>

      {log.headers && (
        <>
          <Typography.Title level={5} style={{ marginTop: 20 }}>
            {t('logs.requestHeaders')}
          </Typography.Title>
          <CodeEditor value={tryFormat(log.headers)} readOnly height={150} />
        </>
      )}

      {log.body && (
        <>
          <Typography.Title level={5} style={{ marginTop: 20 }}>
            {t('logs.requestBody')}
          </Typography.Title>
          <CodeEditor value={tryFormat(log.body)} readOnly height={200} />
        </>
      )}

      {log.responseBody && (
        <>
          <Typography.Title level={5} style={{ marginTop: 20 }}>
            {t('logs.responseBody')}
          </Typography.Title>
          <CodeEditor value={tryFormat(log.responseBody)} readOnly height={200} />
        </>
      )}
    </Drawer>
  );
}
