import { Card, Typography, Button, Popconfirm, Flex, Space, Switch, Tooltip } from 'antd';
import { DeleteOutlined, EditOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import type { ServiceProxy } from '@/shared/types';
import StatusBadge from '@/shared/components/StatusBadge';

interface ServiceProxyCardProps {
  proxy: ServiceProxy;
  onEdit: (proxy: ServiceProxy) => void;
  onDelete: (id: string) => void;
  onToggle: (id: string) => void;
  onToggleRecording: (id: string) => void;
  onToggleFallback: (id: string) => void;
}

export default function ServiceProxyCard({
  proxy,
  onEdit,
  onDelete,
  onToggle,
  onToggleRecording,
  onToggleFallback,
}: ServiceProxyCardProps) {
  const { t } = useTranslation();

  return (
    <Card
      size="small"
      style={{
        marginBottom: 12,
        opacity: proxy.isActive ? 1 : 0.55,
        transition: 'opacity 0.2s ease',
      }}
    >
      <Flex justify="space-between" align="flex-start">
        <div style={{ flex: 1 }}>
          <Flex align="center" gap={8} style={{ marginBottom: 8 }}>
            <Typography.Text strong style={{ fontSize: 14 }}>
              {proxy.serviceName}
            </Typography.Text>
            <StatusBadge active={proxy.isActive} />
            {proxy.isActive && proxy.isRecording && (
              <span
                style={{
                  display: 'inline-flex',
                  alignItems: 'center',
                  gap: 4,
                  padding: '2px 8px',
                  borderRadius: 100,
                  fontSize: 11,
                  fontWeight: 500,
                  background: 'var(--delete-bg)',
                  color: 'var(--delete-color)',
                }}
              >
                <span
                  style={{
                    width: 6,
                    height: 6,
                    borderRadius: '50%',
                    background: 'var(--delete-color)',
                    animation: 'pulse 1.5s infinite',
                  }}
                />
                {t('proxy.recording')}
              </span>
            )}
          </Flex>

          <Typography.Text type="secondary" style={{ fontSize: 12, display: 'block', marginBottom: 4 }}>
            {t('proxy.targetBaseUrl')}: <Typography.Text code>{proxy.targetBaseUrl}</Typography.Text>
          </Typography.Text>

          {proxy.stripPathPrefix && (
            <Typography.Text type="secondary" style={{ fontSize: 12, display: 'block', marginBottom: 4 }}>
              {t('proxy.stripPathPrefix')}: <Typography.Text code>{proxy.stripPathPrefix}</Typography.Text>
            </Typography.Text>
          )}

          <Typography.Text type="secondary" style={{ fontSize: 12, display: 'block', marginBottom: 4 }}>
            {t('proxy.timeout')}: {proxy.timeoutMs}ms
          </Typography.Text>

          <Flex gap={16} style={{ marginTop: 8 }}>
            <Tooltip title={proxy.isActive ? t('common.toggleDisable') : t('common.toggleEnable')}>
              <Space size={4}>
                <Switch
                  size="small"
                  checked={proxy.isActive}
                  onChange={() => onToggle(proxy.id)}
                />
                <Typography.Text style={{ fontSize: 12 }}>{t('common.status')}</Typography.Text>
              </Space>
            </Tooltip>

            <Tooltip title={t('proxy.fallbackDescription')}>
              <Space size={4}>
                <Switch
                  size="small"
                  checked={proxy.fallbackEnabled}
                  onChange={() => onToggleFallback(proxy.id)}
                />
                <Typography.Text style={{ fontSize: 12 }}>{t('proxy.fallbackEnabled')}</Typography.Text>
              </Space>
            </Tooltip>

            <Tooltip title={t('proxy.enableRecording')}>
              <Space size={4}>
                <Switch
                  size="small"
                  checked={proxy.isRecording}
                  onChange={() => onToggleRecording(proxy.id)}
                  checkedChildren="REC"
                />
                <Typography.Text style={{ fontSize: 12 }}>{t('proxy.recording')}</Typography.Text>
              </Space>
            </Tooltip>
          </Flex>
        </div>

        <Space>
          <Tooltip title={t('common.edit')}>
            <Button size="small" type="text" icon={<EditOutlined />} onClick={() => onEdit(proxy)} />
          </Tooltip>
          <Popconfirm
            title={t('proxy.deleteConfirm')}
            onConfirm={() => onDelete(proxy.id)}
            okText={t('common.yes')}
            cancelText={t('common.no')}
          >
            <Tooltip title={t('common.delete')}>
              <Button size="small" type="text" danger icon={<DeleteOutlined />} />
            </Tooltip>
          </Popconfirm>
        </Space>
      </Flex>
    </Card>
  );
}
