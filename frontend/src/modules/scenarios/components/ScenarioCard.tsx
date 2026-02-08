import { Card, Typography, Button, Popconfirm, Flex, Space, Switch, Tooltip } from 'antd';
import { DeleteOutlined, EditOutlined, UndoOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import type { Scenario } from '@/shared/types';
import StatusBadge from '@/shared/components/StatusBadge';

interface ScenarioCardProps {
  scenario: Scenario;
  onView: (scenario: Scenario) => void;
  onEdit: (scenario: Scenario) => void;
  onDelete: (id: string) => void;
  onToggle: (id: string) => void;
  onReset: (id: string) => void;
  toggleLoading?: boolean;
}

export default function ScenarioCard({
  scenario,
  onView,
  onEdit,
  onDelete,
  onToggle,
  onReset,
  toggleLoading,
}: ScenarioCardProps) {
  const { t } = useTranslation();
  const stateNames = [...new Set(scenario.steps?.map((s) => s.stateName) ?? [])];

  return (
    <Card
      size="small"
      style={{
        marginBottom: 12,
        opacity: scenario.isActive ? 1 : 0.55,
        transition: 'opacity 0.2s ease',
        cursor: 'pointer',
      }}
      onClick={() => onView(scenario)}
    >
      <Flex justify="space-between" align="flex-start">
        <div style={{ flex: 1 }}>
          <Flex align="center" gap={8} style={{ marginBottom: 8 }}>
            <Typography.Text strong style={{ fontSize: 15 }}>
              {scenario.name}
            </Typography.Text>
            <StatusBadge active={scenario.isActive} />
          </Flex>
          {scenario.description && (
            <Typography.Text type="secondary" style={{ fontSize: 13, display: 'block', marginBottom: 4 }}>
              {scenario.description}
            </Typography.Text>
          )}
          <Space size={[4, 4]} wrap style={{ marginBottom: 4 }}>
            {stateNames.map((state) => (
              <span
                key={state}
                style={{
                  display: 'inline-block',
                  padding: '2px 8px',
                  borderRadius: 6,
                  fontSize: 12,
                  background: state === scenario.currentState ? 'var(--active-bg)' : 'var(--condition-bg)',
                  color: state === scenario.currentState ? 'var(--active-color)' : 'var(--color-text-secondary)',
                  border: '1px solid var(--color-border)',
                  fontWeight: state === scenario.currentState ? 600 : 400,
                }}
              >
                {state === scenario.currentState ? `● ${state}` : state}
              </span>
            ))}
          </Space>
          <Typography.Text type="secondary" style={{ fontSize: 12, display: 'block' }}>
            {t('scenarios.stepCount', { count: scenario.steps?.length ?? 0 })}
          </Typography.Text>
        </div>
        <Space onClick={(e) => e.stopPropagation()}>
          <Tooltip title={scenario.isActive ? t('common.toggleDisable') : t('common.toggleEnable')}>
            <Switch
              size="small"
              checked={scenario.isActive}
              loading={toggleLoading}
              onChange={() => onToggle(scenario.id)}
            />
          </Tooltip>
          <Tooltip title={t('scenarios.resetState')}>
            <Popconfirm
              title={t('scenarios.resetConfirm')}
              onConfirm={() => onReset(scenario.id)}
              okText={t('common.yes')}
              cancelText={t('common.no')}
            >
              <Button size="small" type="text" icon={<UndoOutlined />} />
            </Popconfirm>
          </Tooltip>
          <Tooltip title={t('common.edit')}>
            <Button size="small" type="text" icon={<EditOutlined />} onClick={() => onEdit(scenario)} />
          </Tooltip>
          <Popconfirm
            title={t('scenarios.deleteConfirm', { name: scenario.name })}
            onConfirm={() => onDelete(scenario.id)}
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
