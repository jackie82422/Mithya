import { Card, Typography, Flex, Button, Popconfirm, Tooltip } from 'antd';
import { EditOutlined, DeleteOutlined, RightOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import type { EndpointGroup } from '@/shared/types';

interface GroupCardProps {
  group: EndpointGroup;
  onOpen: (group: EndpointGroup) => void;
  onEdit: (group: EndpointGroup) => void;
  onDelete: (id: string) => void;
}

export default function GroupCard({ group, onOpen, onEdit, onDelete }: GroupCardProps) {
  const { t } = useTranslation();
  const color = group.color || '#1677ff';

  const endpointNames = group.endpoints?.slice(0, 3).map((ep) => ep.name) ?? [];

  return (
    <Card
      hoverable
      style={{
        borderRadius: 16,
        cursor: 'pointer',
        borderLeft: `4px solid ${color}`,
        height: '100%',
      }}
      styles={{ body: { padding: '16px 20px', height: '100%', display: 'flex', flexDirection: 'column' } }}
      onClick={() => onOpen(group)}
    >
      <Flex align="center" gap={8} style={{ marginBottom: 8 }}>
        <span
          style={{
            width: 12,
            height: 12,
            borderRadius: '50%',
            background: color,
            flexShrink: 0,
          }}
        />
        <Typography.Text strong style={{ fontSize: 15, flex: 1 }}>
          {group.name}
        </Typography.Text>
        <RightOutlined style={{ fontSize: 12, color: 'var(--color-text-secondary)' }} />
      </Flex>

      <Typography.Text type="secondary" style={{ fontSize: 13, display: 'block', marginBottom: 4 }}>
        {t('groups.endpointCount', { count: group.endpointCount ?? 0 })}
      </Typography.Text>

      {group.description && (
        <Typography.Text
          type="secondary"
          style={{ fontSize: 12, display: 'block', marginBottom: 8 }}
          ellipsis
        >
          {group.description}
        </Typography.Text>
      )}

      {endpointNames.length > 0 && (
        <Typography.Text
          type="secondary"
          style={{ fontSize: 11, display: 'block', marginBottom: 8, opacity: 0.7 }}
          ellipsis
        >
          {endpointNames.join(' → ')}
          {(group.endpoints?.length ?? 0) > 3 && ' ...'}
        </Typography.Text>
      )}

      <Flex justify="flex-end" gap={4} style={{ marginTop: 'auto' }} onClick={(e) => e.stopPropagation()}>
        <Tooltip title={t('groups.edit')}>
          <Button size="small" type="text" icon={<EditOutlined />} onClick={() => onEdit(group)} />
        </Tooltip>
        <Popconfirm
          title={t('groups.deleteConfirm')}
          onConfirm={() => onDelete(group.id)}
          okText={t('common.yes')}
          cancelText={t('common.no')}
        >
          <Tooltip title={t('groups.delete')}>
            <Button size="small" type="text" danger icon={<DeleteOutlined />} />
          </Tooltip>
        </Popconfirm>
      </Flex>
    </Card>
  );
}
