import { Card, Typography, Button, Popconfirm, Flex, Space, Switch, Tooltip } from 'antd';
import { DeleteOutlined, EditOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import type { ProxyConfig, MockEndpoint } from '@/shared/types';
import StatusBadge from '@/shared/components/StatusBadge';

interface ProxyConfigCardProps {
  config: ProxyConfig;
  endpoints?: MockEndpoint[];
  onEdit: (config: ProxyConfig) => void;
  onDelete: (id: string) => void;
  onToggle: (id: string) => void;
  onToggleRecording: (id: string) => void;
  toggleLoading?: boolean;
}

export default function ProxyConfigCard({
  config,
  endpoints,
  onEdit,
  onDelete,
  onToggle,
  onToggleRecording,
  toggleLoading,
}: ProxyConfigCardProps) {
  const { t } = useTranslation();
  const endpoint = endpoints?.find((e) => e.id === config.endpointId);
  const scopeLabel = config.endpointId
    ? `${endpoint?.httpMethod ?? ''} ${endpoint?.path ?? config.endpointId}`
    : t('proxy.scopeGlobal');

  return (
    <Card
      size="small"
      style={{
        marginBottom: 12,
        opacity: config.isActive ? 1 : 0.55,
        transition: 'opacity 0.2s ease',
      }}
    >
      <Flex justify="space-between" align="flex-start">
        <div style={{ flex: 1 }}>
          <Flex align="center" gap={8} style={{ marginBottom: 8 }}>
            <Typography.Text strong style={{ fontSize: 14 }}>
              {config.endpointId ? t('proxy.scopeEndpoint') : 'Global Proxy'}
            </Typography.Text>
            <StatusBadge active={config.isActive} />
            {config.isRecording && (
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
          <Typography.Text type="secondary" style={{ fontSize: 13, display: 'block', marginBottom: 4 }}>
            {scopeLabel}
          </Typography.Text>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {t('proxy.targetBaseUrl')}: <Typography.Text code>{config.targetBaseUrl}</Typography.Text>
          </Typography.Text>
          {config.stripPathPrefix && (
            <Typography.Text type="secondary" style={{ fontSize: 12, display: 'block' }}>
              {t('proxy.stripPathPrefix')}: <Typography.Text code>{config.stripPathPrefix}</Typography.Text>
            </Typography.Text>
          )}
          <Typography.Text type="secondary" style={{ fontSize: 12, display: 'block' }}>
            {t('proxy.timeout')}: {config.timeoutMs}ms
          </Typography.Text>
        </div>
        <Space>
          <Tooltip title={config.isActive ? t('common.toggleDisable') : t('common.toggleEnable')}>
            <Switch
              size="small"
              checked={config.isActive}
              loading={toggleLoading}
              onChange={() => onToggle(config.id)}
            />
          </Tooltip>
          <Tooltip title={t('proxy.enableRecording')}>
            <Switch
              size="small"
              checked={config.isRecording}
              onChange={() => onToggleRecording(config.id)}
              checkedChildren="REC"
            />
          </Tooltip>
          <Tooltip title={t('common.edit')}>
            <Button size="small" type="text" icon={<EditOutlined />} onClick={() => onEdit(config)} />
          </Tooltip>
          <Popconfirm
            title={t('proxy.deleteConfirm')}
            onConfirm={() => onDelete(config.id)}
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
