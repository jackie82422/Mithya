import { Flex, Button, Tooltip } from 'antd';
import { SettingOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import type { EndpointGroup } from '@/shared/types';

interface EndpointGroupBarProps {
  groups: EndpointGroup[];
  selectedGroupId: string | null; // null = all, 'ungrouped' = ungrouped
  onSelect: (groupId: string | null) => void;
  onManage: () => void;
}

const PRESET_COLORS = [
  '#1677ff', '#52c41a', '#faad14', '#f5222d', '#722ed1',
  '#eb2f96', '#13c2c2', '#fa8c16',
];

function getColor(group: EndpointGroup): string {
  return group.color || PRESET_COLORS[group.name.length % PRESET_COLORS.length];
}

export default function EndpointGroupBar({ groups, selectedGroupId, onSelect, onManage }: EndpointGroupBarProps) {
  const { t } = useTranslation();

  const pillStyle = (active: boolean, color?: string): React.CSSProperties => ({
    padding: '4px 14px',
    borderRadius: 100,
    fontSize: 13,
    fontWeight: active ? 600 : 400,
    cursor: 'pointer',
    border: active ? `2px solid ${color ?? 'var(--color-primary)'}` : '1px solid var(--color-border)',
    background: active ? `${color ?? 'var(--color-primary)'}18` : 'transparent',
    color: active ? (color ?? 'var(--color-primary)') : 'var(--color-text-secondary)',
    transition: 'all 0.2s ease',
    whiteSpace: 'nowrap',
  });

  return (
    <Flex
      align="center"
      gap={8}
      style={{
        marginBottom: 16,
        overflowX: 'auto',
        paddingBottom: 4,
      }}
    >
      <span
        style={pillStyle(selectedGroupId === null)}
        onClick={() => onSelect(null)}
      >
        {t('groups.all')}
      </span>
      {groups.map((g) => {
        const color = getColor(g);
        return (
          <span
            key={g.id}
            style={pillStyle(selectedGroupId === g.id, color)}
            onClick={() => onSelect(g.id)}
          >
            <span
              style={{
                display: 'inline-block',
                width: 8,
                height: 8,
                borderRadius: '50%',
                background: color,
                marginRight: 6,
              }}
            />
            {g.name}
            {g.endpointCount !== undefined && (
              <span style={{ opacity: 0.6, marginLeft: 4 }}>({g.endpointCount})</span>
            )}
          </span>
        );
      })}
      <span
        style={pillStyle(selectedGroupId === 'ungrouped')}
        onClick={() => onSelect('ungrouped')}
      >
        {t('groups.ungrouped')}
      </span>
      <Tooltip title={t('groups.manage')}>
        <Button
          type="text"
          size="small"
          icon={<SettingOutlined />}
          onClick={onManage}
        />
      </Tooltip>
    </Flex>
  );
}
