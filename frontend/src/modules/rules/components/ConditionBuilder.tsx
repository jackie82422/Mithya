import { Button, Select, Input, Card, Flex, Typography, Segmented } from 'antd';
import { PlusOutlined, DeleteOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import {
  FieldSourceType,
  FieldSourceTypeLabel,
  MatchOperator,
} from '@/shared/types';
import type { MatchCondition, LogicMode } from '@/shared/types';
import CodeEditor from '@/shared/components/CodeEditor';

interface ConditionBuilderProps {
  value?: MatchCondition[];
  onChange?: (conditions: MatchCondition[]) => void;
  logicMode?: LogicMode;
  onLogicModeChange?: (mode: LogicMode) => void;
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

const noValueOperators = [MatchOperator.Exists, MatchOperator.IsEmpty, MatchOperator.NotExists];

export default function ConditionBuilder({ value = [], onChange, logicMode = 'AND', onLogicModeChange }: ConditionBuilderProps) {
  const { t } = useTranslation();

  const operatorOptions = [
    { value: MatchOperator.Equals, label: t('rules.opEquals') },
    { value: MatchOperator.NotEquals, label: t('rules.opNotEquals') },
    { value: MatchOperator.Contains, label: t('rules.opContains') },
    { value: MatchOperator.Regex, label: t('rules.opRegex') },
    { value: MatchOperator.StartsWith, label: t('rules.opStartsWith') },
    { value: MatchOperator.EndsWith, label: t('rules.opEndsWith') },
    { value: MatchOperator.GreaterThan, label: t('rules.opGreaterThan') },
    { value: MatchOperator.LessThan, label: t('rules.opLessThan') },
    { value: MatchOperator.Exists, label: t('rules.opExists') },
    { value: MatchOperator.NotExists, label: t('rules.opNotExists') },
    { value: MatchOperator.IsEmpty, label: t('rules.opIsEmpty') },
    { value: MatchOperator.JsonSchema, label: t('rules.opJsonSchema') },
  ];

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
      {value.length > 1 && (
        <Flex align="center" gap={8} style={{ marginBottom: 12 }}>
          <Typography.Text type="secondary" style={{ fontSize: 12 }}>
            {t('rules.logicMode')}:
          </Typography.Text>
          <Segmented
            size="small"
            options={[
              { label: t('rules.logicAnd'), value: 'AND' },
              { label: t('rules.logicOr'), value: 'OR' },
            ]}
            value={logicMode}
            onChange={(val) => onLogicModeChange?.(val as LogicMode)}
          />
        </Flex>
      )}

      {value.map((cond, i) => {
        const needsJsonPath = cond.sourceType === FieldSourceType.Body;
        const pathError =
          needsJsonPath && cond.fieldPath && !cond.fieldPath.startsWith('$.');
        const hideValue = noValueOperators.includes(cond.operator);
        const isJsonSchema = cond.operator === MatchOperator.JsonSchema;

        return (
          <div key={i}>
            <Card
              size="small"
              style={{ marginBottom: 0, background: 'var(--condition-bg)' }}
            >
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
                      {t('validation.bodyFieldPathPrefix')}
                    </Typography.Text>
                  )}
                </div>
                <Select
                  style={{ width: 140 }}
                  value={cond.operator}
                  onChange={(v) => update(i, 'operator', v)}
                  options={operatorOptions}
                />
                {!hideValue && !isJsonSchema && (
                  <Input
                    style={{ flex: 1, minWidth: 120 }}
                    placeholder={t('rules.form.matchValuePlaceholder')}
                    value={cond.value}
                    onChange={(e) => update(i, 'value', e.target.value)}
                  />
                )}
                <Button
                  type="text"
                  danger
                  icon={<DeleteOutlined />}
                  onClick={() => remove(i)}
                  size="small"
                />
              </Flex>
              {isJsonSchema && (
                <div style={{ marginTop: 8 }}>
                  <CodeEditor
                    value={cond.value}
                    onChange={(val) => update(i, 'value', val)}
                    height={120}
                  />
                </div>
              )}
            </Card>
            {i < value.length - 1 && (
              <div style={{ textAlign: 'center', padding: '4px 0', color: 'var(--color-text-secondary)' }}>
                <span
                  style={{
                    display: 'inline-block',
                    padding: '1px 10px',
                    borderRadius: 100,
                    fontSize: 11,
                    fontWeight: 600,
                    background: 'var(--condition-bg)',
                    border: '1px solid var(--color-border)',
                  }}
                >
                  {logicMode === 'AND' ? t('rules.logicAnd') : t('rules.logicOr')}
                </span>
              </div>
            )}
          </div>
        );
      })}
      <Button
        type="dashed"
        icon={<PlusOutlined />}
        onClick={add}
        block
        style={{ borderRadius: 12, height: 40, marginTop: 8 }}
      >
        {t('rules.form.addCondition')}
      </Button>
    </div>
  );
}
