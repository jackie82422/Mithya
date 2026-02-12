import { useState } from 'react';
import { Drawer, Button, Alert, Typography, Space } from 'antd';
import { PlayCircleOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import { useMutation } from '@tanstack/react-query';
import CodeEditor from '@/shared/components/CodeEditor';
import { templateApi } from '../api';
import type { TemplatePreviewRequest } from '@/shared/types';

interface TemplatePreviewProps {
  open: boolean;
  onClose: () => void;
  template: string;
  endpointPath?: string;
  endpointMethod?: string;
}

const defaultMockRequest = (method: string, path: string) =>
  JSON.stringify(
    {
      method: method || 'POST',
      path: path || '/api/user/123',
      body: '{"name": "John"}',
      headers: { Accept: 'application/json', 'Content-Type': 'application/json' },
      query: { page: '1' },
      pathParams: { id: '123' },
    },
    null,
    2,
  );

export default function TemplatePreview({
  open,
  onClose,
  template,
  endpointPath = '',
  endpointMethod = '',
}: TemplatePreviewProps) {
  const { t } = useTranslation();
  const [mockRequestStr, setMockRequestStr] = useState(() =>
    defaultMockRequest(endpointMethod, endpointPath),
  );
  const [renderedResult, setRenderedResult] = useState('');
  const [previewError, setPreviewError] = useState<string | null>(null);

  const previewMutation = useMutation({
    mutationFn: (data: TemplatePreviewRequest) => templateApi.preview(data),
    onSuccess: (data) => {
      setRenderedResult(data.rendered ?? '');
      setPreviewError(data.error ?? null);
    },
    onError: () => {
      setPreviewError('Failed to preview template');
      setRenderedResult('');
    },
  });

  const handlePreview = () => {
    try {
      const parsed = JSON.parse(mockRequestStr);
      previewMutation.mutate({
        template,
        mockRequest: {
          method: parsed.method ?? 'GET',
          path: parsed.path ?? '/',
          body: typeof parsed.body === 'string' ? parsed.body : JSON.stringify(parsed.body ?? ''),
          headers: parsed.headers ?? {},
          query: parsed.query ?? {},
          pathParams: parsed.pathParams ?? {},
        },
      });
    } catch {
      setPreviewError('Invalid JSON in mock request');
    }
  };

  return (
    <Drawer
      title={t('rules.previewTemplate')}
      open={open}
      onClose={onClose}
      width={600}
    >
      <Space direction="vertical" style={{ width: '100%' }} size="middle">
        <div>
          <Typography.Text strong style={{ display: 'block', marginBottom: 8 }}>
            {t('rules.mockRequest')}
          </Typography.Text>
          <CodeEditor
            value={mockRequestStr}
            onChange={(v) => setMockRequestStr(v)}
            height={220}
          />
        </div>

        <Button
          type="primary"
          icon={<PlayCircleOutlined />}
          onClick={handlePreview}
          loading={previewMutation.isPending}
        >
          {t('rules.previewTemplate')}
        </Button>

        {renderedResult && (
          <div>
            <Typography.Text strong style={{ display: 'block', marginBottom: 8 }}>
              {t('rules.renderedResult')}
            </Typography.Text>
            <CodeEditor value={renderedResult} readOnly height={180} />
          </div>
        )}

        {previewError && (
          <Alert type="error" message={previewError} showIcon />
        )}
      </Space>
    </Drawer>
  );
}
