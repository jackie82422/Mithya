import { Card, Space, Typography, Button, Popconfirm, Flex, Tooltip, Switch, Checkbox } from 'antd';
import { DeleteOutlined, EditOutlined, SettingOutlined, RightOutlined, DisconnectOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import type { MockEndpoint, EndpointGroup } from '@/shared/types';
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
  groups?: EndpointGroup[];
  onGroupClick?: (groupId: string) => void;
  inGroupView?: boolean;
  onRemoveFromGroup?: (endpointId: string) => void;
}

export default function EndpointCard({ endpoint, onDelete, onSetDefault, onToggle, onEdit, toggleLoading, selectable, selected, onSelect, groups, onGroupClick, inGroupView, onRemoveFromGroup }: EndpointCardProps) {
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
            <Flex align="center" gap={16} wrap="wrap">
              <Typography.Text type="secondary">
                {endpoint.serviceName}
              </Typography.Text>
              <Typography.Text type="secondary">
                {t('endpoints.rulesCount', { count: endpoint.rules?.length ?? 0 })}
              </Typography.Text>
              {!inGroupView && groups && groups.length > 0 && (
                <Flex gap={4} onClick={(e) => e.stopPropagation()}>
                  {groups.slice(0, 2).map((g) => (
                    <span
                      key={g.id}
                      style={{
                        display: 'inline-flex',
                        alignItems: 'center',
                        gap: 4,
                        padding: '1px 8px',
                        borderRadius: 100,
                        fontSize: 11,
                        fontWeight: 500,
                        background: `${g.color || '#1677ff'}18`,
                        color: g.color || '#1677ff',
                        cursor: onGroupClick ? 'pointer' : 'default',
                      }}
                      onClick={() => onGroupClick?.(g.id)}
                    >
                      <span style={{ width: 6, height: 6, borderRadius: '50%', background: g.color || '#1677ff' }} />
                      {g.name}
                    </span>
                  ))}
                  {groups.length > 2 && (
                    <span
                      style={{
                        padding: '1px 6px',
                        borderRadius: 100,
                        fontSize: 11,
                        color: 'var(--color-text-secondary)',
                        background: 'var(--condition-bg)',
                      }}
                    >
                      +{groups.length - 2}
                    </span>
                  )}
                </Flex>
              )}
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
            {inGroupView && onRemoveFromGroup && (
              <Tooltip title={t('groups.removeFromGroup')}>
                <Button
                  size="small"
                  type="text"
                  icon={<DisconnectOutlined />}
                  onClick={(e) => {
                    e.stopPropagation();
                    onRemoveFromGroup(endpoint.id);
                  }}
                />
              </Tooltip>
            )}
            <RightOutlined style={{ color: 'var(--color-primary)', fontSize: 14, marginLeft: 4 }} />
          </Flex>
        )}
      </Flex>
    </Card>
  );
}
