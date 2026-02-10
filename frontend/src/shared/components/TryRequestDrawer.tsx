import { useState, useEffect, useCallback } from 'react';
import { Drawer, Select, Input, Button, Flex, Space, Typography, message } from 'antd';
import { SendOutlined, CopyOutlined, PlusOutlined, DeleteOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import { useTryRequest } from '../hooks/useTryRequest';
import ResponseViewer from './ResponseViewer';
import CodeEditor from './CodeEditor';

interface TryRequestDrawerProps {
  open: boolean;
  onClose: () => void;
  initialMethod?: string;
  initialUrl?: string;
  initialHeaders?: Record<string, string>;
  initialBody?: string;
}

const HTTP_METHODS = ['GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'HEAD', 'OPTIONS'];
const BODY_HIDDEN_METHODS = ['GET', 'HEAD', 'OPTIONS'];

interface HeaderRow {
  key: string;
  value: string;
}

export default function TryRequestDrawer({
  open,
  onClose,
  initialMethod = 'GET',
  initialUrl = '',
  initialHeaders,
  initialBody,
}: TryRequestDrawerProps) {
  const { t } = useTranslation();
  const mutation = useTryRequest();

  const [method, setMethod] = useState(initialMethod.toUpperCase());
  const [url, setUrl] = useState(initialUrl);
  const [headers, setHeaders] = useState<HeaderRow[]>([]);
  const [body, setBody] = useState(initialBody ?? '');

  useEffect(() => {
    if (open) {
      setMethod(initialMethod.toUpperCase());
      setUrl(initialUrl);
      setHeaders(
        initialHeaders
          ? Object.entries(initialHeaders).map(([key, value]) => ({ key, value }))
          : [],
      );
      setBody(initialBody ?? '');
      mutation.reset();
    }
  }, [open, initialMethod, initialUrl, initialHeaders, initialBody]);

  const handleSend = useCallback(() => {
    if (!url) return;
    const hdrs: Record<string, string> = {};
    for (const h of headers) {
      if (h.key.trim()) hdrs[h.key.trim()] = h.value;
    }
    mutation.mutate({
      method,
      url,
      headers: Object.keys(hdrs).length > 0 ? hdrs : undefined,
      body: BODY_HIDDEN_METHODS.includes(method) ? undefined : body || undefined,
    });
  }, [method, url, headers, body, mutation]);

  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key === 'Enter') {
        e.preventDefault();
        handleSend();
      }
    },
    [handleSend],
  );

  const buildCurl = () => {
    const parts: string[] = ['curl'];
    if (method !== 'GET') parts.push(`-X ${method}`);
    parts.push(`'${url}'`);
    for (const h of headers) {
      if (h.key.trim()) parts.push(`-H '${h.key.trim()}: ${h.value}'`);
    }
    if (!BODY_HIDDEN_METHODS.includes(method) && body) {
      parts.push(`-d '${body}'`);
    }
    return parts.join(' \\\n  ');
  };

  const handleCopyCurl = () => {
    navigator.clipboard.writeText(buildCurl());
    message.success(t('rules.copiedCurl'));
  };

  const addHeader = () => setHeaders((prev) => [...prev, { key: '', value: '' }]);
  const removeHeader = (idx: number) => setHeaders((prev) => prev.filter((_, i) => i !== idx));
  const updateHeader = (idx: number, field: 'key' | 'value', val: string) => {
    setHeaders((prev) => prev.map((h, i) => (i === idx ? { ...h, [field]: val } : h)));
  };

  return (
    <Drawer
      title={t('tryRequest.title')}
      open={open}
      onClose={onClose}
      width={560}
      destroyOnClose
    >
      <div onKeyDown={handleKeyDown}>
        {/* Method + URL */}
        <Flex gap={8} style={{ marginBottom: 12 }}>
          <Select
            value={method}
            onChange={setMethod}
            style={{ width: 120 }}
            options={HTTP_METHODS.map((m) => ({ label: m, value: m }))}
          />
          <Input
            value={url}
            onChange={(e) => setUrl(e.target.value)}
            placeholder="https://..."
            style={{ flex: 1 }}
          />
        </Flex>

        {/* Headers */}
        <Flex justify="space-between" align="center" style={{ marginBottom: 8 }}>
          <Typography.Text strong style={{ fontSize: 13 }}>
            {t('tryRequest.headers')}
          </Typography.Text>
          <Button size="small" type="text" icon={<PlusOutlined />} onClick={addHeader} />
        </Flex>
        {headers.map((h, idx) => (
          <Flex key={idx} gap={8} style={{ marginBottom: 6 }}>
            <Input
              size="small"
              placeholder="Key"
              value={h.key}
              onChange={(e) => updateHeader(idx, 'key', e.target.value)}
              style={{ flex: 1 }}
            />
            <Input
              size="small"
              placeholder="Value"
              value={h.value}
              onChange={(e) => updateHeader(idx, 'value', e.target.value)}
              style={{ flex: 1 }}
            />
            <Button
              size="small"
              type="text"
              danger
              icon={<DeleteOutlined />}
              onClick={() => removeHeader(idx)}
            />
          </Flex>
        ))}

        {/* Body */}
        {!BODY_HIDDEN_METHODS.includes(method) && (
          <div style={{ marginTop: 12 }}>
            <Typography.Text strong style={{ fontSize: 13, display: 'block', marginBottom: 8 }}>
              {t('tryRequest.body')}
            </Typography.Text>
            <CodeEditor value={body} onChange={setBody} height={160} />
          </div>
        )}

        {/* Actions */}
        <Flex gap={8} style={{ marginTop: 16 }}>
          <Button
            type="primary"
            icon={<SendOutlined />}
            onClick={handleSend}
            loading={mutation.isPending}
            disabled={!url}
          >
            {t('tryRequest.send')}
          </Button>
          <Button icon={<CopyOutlined />} onClick={handleCopyCurl}>
            {t('tryRequest.copyCurl')}
          </Button>
        </Flex>

        {/* Response */}
        <ResponseViewer
          response={mutation.data}
          loading={mutation.isPending}
          error={mutation.error}
        />
      </div>
    </Drawer>
  );
}
