import { Card, Typography, Tag, Button, Popconfirm, Flex, Space } from 'antd';
import { DeleteOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import type { MockRule, MatchCondition } from '@/shared/types';
import { FieldSourceTypeLabel, MatchOperatorLabel } from '@/shared/types';
import StatusBadge from '@/shared/components/StatusBadge';

interface RuleCardProps {
  rule: MockRule;
  onDelete: (ruleId: string) => void;
}

function parseConditions(raw: string): MatchCondition[] {
  try {
    return JSON.parse(raw);
  } catch {
    return [];
  }
}

export default function RuleCard({ rule, onDelete }: RuleCardProps) {
  const { t } = useTranslation();
  const conditions = parseConditions(rule.matchConditions);

  return (
    <Card size="small" style={{ marginBottom: 12 }}>
      <Flex justify="space-between" align="flex-start">
        <div style={{ flex: 1 }}>
          <Flex align="center" gap={8} style={{ marginBottom: 8 }}>
            <Tag color="geekblue">#{rule.priority}</Tag>
            <Typography.Text strong>{rule.ruleName}</Typography.Text>
            <StatusBadge active={rule.isActive} />
          </Flex>
          <Space size={[4, 4]} wrap style={{ marginBottom: 4 }}>
            {conditions.map((c, i) => (
              <Tag key={i}>
                {FieldSourceTypeLabel[c.sourceType]}.{c.fieldPath}{' '}
                {MatchOperatorLabel[c.operator]} {c.value && `"${c.value}"`}
              </Tag>
            ))}
            {conditions.length === 0 && (
              <Typography.Text type="secondary">({t('common.noData')})</Typography.Text>
            )}
          </Space>
          <div>
            <Typography.Text type="secondary">
              {t('rules.statusCode')}: {rule.responseStatusCode}
              {rule.delayMs > 0 && ` | ${t('rules.delayMs')}: ${rule.delayMs}ms`}
            </Typography.Text>
          </div>
        </div>
        <Popconfirm
          title={t('rules.deleteConfirm', { name: rule.ruleName })}
          onConfirm={() => onDelete(rule.id)}
          okText={t('common.yes')}
          cancelText={t('common.no')}
        >
          <Button size="small" danger icon={<DeleteOutlined />} />
        </Popconfirm>
      </Flex>
    </Card>
  );
}
