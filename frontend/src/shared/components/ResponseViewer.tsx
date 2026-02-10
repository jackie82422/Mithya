import { useState } from 'react';
import { Spin, Typography, Button, Flex } from 'antd';
import { CopyOutlined, DownOutlined, RightOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import type { TryRequestResponse } from '../api/tryRequestApi';

interface ResponseViewerProps {
  response?: TryRequestResponse | null;
  loading?: boolean;
  error?: Error | null;
}

const BODY_COLLAPSE_LINES = 50;

function statusColor(code: number): string {
  if (code >= 200 && code < 300) return 'var(--get-color)';
  if (code >= 300 && code < 400) return 'var(--color-primary)';
  if (code >= 400 && code < 500) return 'var(--put-color)';
  return 'var(--delete-color)';
}

function statusBg(code: number): string {
  if (code >= 200 && code < 300) return 'var(--get-bg)';
  if (code >= 300 && code < 400) return 'var(--rest-bg, var(--get-bg))';
  if (code >= 400 && code < 500) return 'var(--put-bg)';
  return 'var(--delete-bg)';
}

function formatBody(raw: string): string {
  try {
    return JSON.stringify(JSON.parse(raw), null, 2);
  } catch {
    return raw;
  }
}

export default function ResponseViewer({ response, loading, error }: ResponseViewerProps) {
  const { t } = useTranslation();
  const [headersExpanded, setHeadersExpanded] = useState(false);
  const [bodyExpanded, setBodyExpanded] = useState(false);

  if (loading) {
    return (
      <Flex align="center" gap={8} style={{ padding: 16 }}>
        <Spin size="small" />
        <Typography.Text type="secondary">{t('tryRequest.sending')}</Typography.Text>
      </Flex>
    );
  }

  if (error) {
    const axiosError = error as import('axios').AxiosError<{ error?: string }>;
    const detail = axiosError.response?.data?.error;
    return (
      <div
        style={{
          padding: 12,
          borderRadius: 8,
          background: 'var(--delete-bg)',
          color: 'var(--delete-color)',
          fontSize: 13,
        }}
      >
        {detail || error.message || t('common.error')}
      </div>
    );
  }

  if (!response) return null;

  const formattedBody = formatBody(response.body);
  const bodyLines = formattedBody.split('\n');
  const needsCollapse = bodyLines.length > BODY_COLLAPSE_LINES;
  const displayBody = needsCollapse && !bodyExpanded
    ? bodyLines.slice(0, BODY_COLLAPSE_LINES).join('\n') + '\n...'
    : formattedBody;

  const headerEntries = Object.entries(response.headers);

  return (
    <div style={{ marginTop: 12 }}>
      {/* Status line */}
      <Flex align="center" gap={8} style={{ marginBottom: 8 }}>
        <span
          style={{
            padding: '2px 10px',
            borderRadius: 100,
            fontSize: 13,
            fontWeight: 600,
            background: statusBg(response.statusCode),
            color: statusColor(response.statusCode),
          }}
        >
          {response.statusCode}
        </span>
        <span
          style={{
            padding: '2px 8px',
            borderRadius: 100,
            fontSize: 12,
            background: 'var(--condition-bg)',
            color: 'var(--color-text-secondary)',
          }}
        >
          {response.elapsedMs} ms
        </span>
      </Flex>

      {/* Headers (collapsible) */}
      {headerEntries.length > 0 && (
        <div style={{ marginBottom: 8 }}>
          <Flex
            align="center"
            gap={4}
            style={{ cursor: 'pointer', marginBottom: 4 }}
            onClick={() => setHeadersExpanded((v) => !v)}
          >
            {headersExpanded
              ? <DownOutlined style={{ fontSize: 10, color: 'var(--color-text-secondary)' }} />
              : <RightOutlined style={{ fontSize: 10, color: 'var(--color-text-secondary)' }} />
            }
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>
              {t('tryRequest.responseHeaders')} ({headerEntries.length})
            </Typography.Text>
          </Flex>
          {headersExpanded && (
            <pre
              style={{
                padding: 8,
                borderRadius: 8,
                fontSize: 12,
                lineHeight: 1.5,
                background: 'var(--code-bg)',
                color: 'var(--color-text)',
                border: '1px solid var(--color-border)',
                overflowX: 'auto',
                whiteSpace: 'pre-wrap',
                wordBreak: 'break-all',
                margin: 0,
              }}
            >
              {headerEntries.map(([k, v]) => `${k}: ${v}`).join('\n')}
            </pre>
          )}
        </div>
      )}

      {/* Body */}
      <Flex justify="space-between" align="center" style={{ marginBottom: 4 }}>
        <Typography.Text type="secondary" style={{ fontSize: 12 }}>
          {t('tryRequest.responseBody')}
        </Typography.Text>
        <Button
          size="small"
          type="text"
          icon={<CopyOutlined />}
          onClick={() => navigator.clipboard.writeText(formattedBody)}
        />
      </Flex>
      <pre
        style={{
          padding: 12,
          borderRadius: 8,
          fontSize: 12,
          lineHeight: 1.5,
          background: 'var(--code-bg)',
          color: 'var(--color-text)',
          border: '1px solid var(--color-border)',
          overflowX: 'auto',
          whiteSpace: 'pre-wrap',
          wordBreak: 'break-all',
          maxHeight: bodyExpanded ? 'none' : 400,
          margin: 0,
        }}
      >
        {displayBody}
      </pre>
      {needsCollapse && (
        <Button
          size="small"
          type="link"
          onClick={() => setBodyExpanded((v) => !v)}
          style={{ padding: 0, marginTop: 4 }}
        >
          {bodyExpanded ? t('tryRequest.showLess') : t('tryRequest.showMore')}
        </Button>
      )}
    </div>
  );
}
