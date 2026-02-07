import { Button, Select, Input, Card, Flex, Typography } from 'antd';
import { PlusOutlined, DeleteOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import {
  FieldSourceType,
  FieldSourceTypeLabel,
  MatchOperator,
  MatchOperatorLabel,
} from '@/shared/types';
import type { MatchCondition } from '@/shared/types';

interface ConditionBuilderProps {
  value?: MatchCondition[];
  onChange?: (conditions: MatchCondition[]) => void;
}

const defaultFieldPaths: Record<FieldSourceType, string> = {
  [FieldSourceType.Body]: '$.id',
  [FieldSourceType.Header]: 'Content-Type',
  [FieldSourceType.Query]: 'id',
  [FieldSourceType.Path]: 'id',
  [FieldSourceType.Metadata]: 'key',
};

const placeholders: Record<FieldSourceType, string> = {
  [FieldSourceType.Body]: '$.user.id',
  [FieldSourceType.Header]: 'Content-Type',
  [FieldSourceType.Query]: 'page',
  [FieldSourceType.Path]: 'id',
  [FieldSourceType.Metadata]: 'key',
};

export default function ConditionBuilder({ value = [], onChange }: ConditionBuilderProps) {
  const { t } = useTranslation();

  const update = (index: number, field: keyof MatchCondition, val: unknown) => {
    const next = [...value];
    if (field === 'sourceType') {
      const newSource = val as FieldSourceType;
      next[index] = {
        ...next[index],
        sourceType: newSource,
        fieldPath: defaultFieldPaths[newSource],
      };
    } else {
      next[index] = { ...next[index], [field]: val };
    }
    onChange?.(next);
  };

  const add = () => {
    const sourceType = FieldSourceType.Body;
    onChange?.([
      ...value,
      {
        sourceType,
        fieldPath: defaultFieldPaths[sourceType],
        operator: MatchOperator.Equals,
        value: '',
      },
    ]);
  };

  const remove = (index: number) => {
    onChange?.(value.filter((_, i) => i !== index));
  };

  return (
    <div>
      {value.map((cond, i) => {
        const needsJsonPath = cond.sourceType === FieldSourceType.Body;
        const pathError =
          needsJsonPath && cond.fieldPath && !cond.fieldPath.startsWith('$.');

        return (
          <Card key={i} size="small" style={{ marginBottom: 8 }}>
            <Flex gap={8} wrap="wrap" align="center">
              <Select
                style={{ width: 120 }}
                value={cond.sourceType}
                onChange={(v) => update(i, 'sourceType', v)}
              >
                {Object.entries(FieldSourceTypeLabel).map(([val, label]) => (
                  <Select.Option key={val} value={Number(val)}>
                    {label}
                  </Select.Option>
                ))}
              </Select>
              <div>
                <Input
                  style={{ width: 180 }}
                  status={pathError ? 'error' : undefined}
                  placeholder={placeholders[cond.sourceType]}
                  value={cond.fieldPath}
                  onChange={(e) => update(i, 'fieldPath', e.target.value)}
                />
                {pathError && (
                  <Typography.Text type="danger" style={{ fontSize: 12 }}>
                    Body must start with $.
                  </Typography.Text>
                )}
              </div>
              <Select
                style={{ width: 130 }}
                value={cond.operator}
                onChange={(v) => update(i, 'operator', v)}
              >
                {Object.entries(MatchOperatorLabel).map(([val, label]) => (
                  <Select.Option key={val} value={Number(val)}>
                    {label}
                  </Select.Option>
                ))}
              </Select>
              <Input
                style={{ flex: 1, minWidth: 120 }}
                placeholder={t('rules.form.matchValuePlaceholder')}
                value={cond.value}
                onChange={(e) => update(i, 'value', e.target.value)}
              />
              <Button
                danger
                icon={<DeleteOutlined />}
                onClick={() => remove(i)}
                size="small"
              />
            </Flex>
          </Card>
        );
      })}
      <Button type="dashed" icon={<PlusOutlined />} onClick={add} block>
        {t('rules.form.addCondition')}
      </Button>
    </div>
  );
}
