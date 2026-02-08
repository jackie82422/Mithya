import { Card, Space, Typography, Button, Popconfirm, Flex, Tooltip, Switch, Checkbox } from 'antd';
import { DeleteOutlined, EditOutlined, SettingOutlined, RightOutlined } from '@ant-design/icons';
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
  onEdit: (endpoint: MockEndpoint) => void;
  toggleLoading?: boolean;
  selectable?: boolean;
  selected?: boolean;
  onSelect?: (id: string) => void;
}

export default function EndpointCard({ endpoint, onDelete, onSetDefault, onToggle, onEdit, toggleLoading, selectable, selected, onSelect }: EndpointCardProps) {
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
        transition: 'opacity 0.2s ease, border-color 0.2s ease',
        borderColor: selected ? 'var(--color-primary)' : undefined,
      }}
      onClick={() => {
        if (selectable) {
          onSelect?.(endpoint.id);
        } else {
          navigate(`/endpoints/${endpoint.id}`);
        }
      }}
    >
      <Flex justify="space-between" align="flex-start">
        <Flex align="flex-start" gap={12} style={{ flex: 1 }}>
          {selectable && (
            <Checkbox
              checked={selected}
              onClick={(e) => e.stopPropagation()}
              onChange={() => onSelect?.(endpoint.id)}
              style={{ marginTop: 4 }}
            />
          )}
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
        </Flex>
        {!selectable && (
          <Flex
            align="center"
            gap={8}
            onClick={(e) => e.stopPropagation()}
            style={{
              paddingLeft: 16,
              borderLeft: '1px solid var(--color-border)',
              marginLeft: 16,
            }}
          >
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
            <Tooltip title={t('common.edit')}>
              <Button
                size="small"
                type="text"
                icon={<EditOutlined />}
                onClick={(e) => {
                  e.stopPropagation();
                  onEdit(endpoint);
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
            <RightOutlined style={{ color: 'var(--color-primary)', fontSize: 14, marginLeft: 4 }} />
          </Flex>
        )}
      </Flex>
    </Card>
  );
}
