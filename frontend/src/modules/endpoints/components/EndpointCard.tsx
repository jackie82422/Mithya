import { Card, Space, Typography, Button, Popconfirm, Flex, Tooltip, Switch } from 'antd';
import { DeleteOutlined, SettingOutlined, RightOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import type { MockEndpoint } from '@/shared/types';
import ProtocolTag from '@/shared/components/ProtocolTag';
import HttpMethodTag from '@/shared/components/HttpMethodTag';
import StatusBadge from '@/shared/components/StatusBadge';

interface EndpointCardProps {
  endpoint: MockEndpoint;
  onDelete: (id: string) => void;
  onSetDefault: (endpoint: MockEndpoint) => void;
  onToggle: (id: string) => void;
  toggleLoading?: boolean;
}

export default function EndpointCard({ endpoint, onDelete, onSetDefault, onToggle, toggleLoading }: EndpointCardProps) {
  const { t } = useTranslation();
  const navigate = useNavigate();

  return (
    <Card
      className="apple-endpoint-card"
      style={{
        marginBottom: 16,
        borderRadius: 16,
        padding: 4,
        opacity: endpoint.isActive ? 1 : 0.55,
        transition: 'opacity 0.2s ease',
      }}
      onClick={() => navigate(`/endpoints/${endpoint.id}`)}
    >
      <Flex justify="space-between" align="flex-start">
        <div style={{ flex: 1 }}>
          <Flex align="center" gap={8} style={{ marginBottom: 8 }}>
            <Typography.Title level={5} style={{ margin: 0 }}>
              {endpoint.name}
            </Typography.Title>
            <StatusBadge active={endpoint.isActive} />
          </Flex>
          <Flex align="center" gap={8} style={{ marginBottom: 8 }}>
            <ProtocolTag protocol={endpoint.protocol} />
            <HttpMethodTag method={endpoint.httpMethod} />
            <Typography.Text code>{endpoint.path}</Typography.Text>
          </Flex>
          <Flex align="center" gap={16}>
            <Typography.Text type="secondary">
              {endpoint.serviceName}
            </Typography.Text>
            <Typography.Text type="secondary">
              {t('endpoints.rulesCount', { count: endpoint.rules?.length ?? 0 })}
            </Typography.Text>
          </Flex>
        </div>
        <Space onClick={(e) => e.stopPropagation()}>
          <Tooltip title={endpoint.isActive ? t('common.toggleDisable') : t('common.toggleEnable')}>
            <Switch
              size="small"
              checked={endpoint.isActive}
              loading={toggleLoading}
              onChange={(_, e) => {
                e.stopPropagation();
                onToggle(endpoint.id);
              }}
            />
          </Tooltip>
          <Button
            size="small"
            type="text"
            icon={<SettingOutlined />}
            onClick={(e) => {
              e.stopPropagation();
              onSetDefault(endpoint);
            }}
          >
            {t('endpoints.setDefaultResponse')}
          </Button>
          <Popconfirm
            title={t('endpoints.deleteConfirm', { name: endpoint.name })}
            onConfirm={(e) => {
              e?.stopPropagation();
              onDelete(endpoint.id);
            }}
            onCancel={(e) => e?.stopPropagation()}
            okText={t('common.yes')}
            cancelText={t('common.no')}
          >
            <Tooltip title={t('common.delete')}>
              <Button size="small" type="text" danger icon={<DeleteOutlined />} />
            </Tooltip>
          </Popconfirm>
          <RightOutlined style={{ color: 'var(--color-text-secondary)', fontSize: 12 }} />
        </Space>
      </Flex>
    </Card>
  );
}
