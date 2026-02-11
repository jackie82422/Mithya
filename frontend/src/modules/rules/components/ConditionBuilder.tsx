import { useMemo, useState } from 'react';
import { Button, Select, Input, Card, Flex, Typography, Segmented } from 'antd';
import { PlusOutlined, DeleteOutlined, AimOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import {
  FieldSourceType,
  FieldSourceTypeLabel,
  MatchOperator,
  ProtocolType,
} from '@/shared/types';
import type { MatchCondition, LogicMode } from '@/shared/types';
import { useProtocols } from '@/shared/hooks/useProtocols';
import CodeEditor from '@/shared/components/CodeEditor';
import XPathPicker from './XPathPicker';

interface ConditionBuilderProps {
  value?: MatchCondition[];
  onChange?: (conditions: MatchCondition[]) => void;
  logicMode?: LogicMode;
  onLogicModeChange?: (mode: LogicMode) => void;
  protocol?: ProtocolType;
}

const restDefaultFieldPaths: Record<FieldSourceType, string> = {
  [FieldSourceType.Body]: '$.id',
  [FieldSourceType.Header]: 'Content-Type',
  [FieldSourceType.Query]: 'id',
  [FieldSourceType.Path]: 'id',
  [FieldSourceType.Metadata]: 'key',
};

const restPlaceholders: Record<FieldSourceType, string> = {
  [FieldSourceType.Body]: '$.user.id',
  [FieldSourceType.Header]: 'Content-Type',
  [FieldSourceType.Query]: 'page',
  [FieldSourceType.Path]: 'id',
  [FieldSourceType.Metadata]: 'key',
};

const allOperatorKeys: MatchOperator[] = [
  MatchOperator.Equals,
  MatchOperator.NotEquals,
  MatchOperator.Contains,
  MatchOperator.Regex,
  MatchOperator.StartsWith,
  MatchOperator.EndsWith,
  MatchOperator.GreaterThan,
  MatchOperator.LessThan,
  MatchOperator.Exists,
  MatchOperator.NotExists,
  MatchOperator.IsEmpty,
  MatchOperator.JsonSchema,
];

const operatorI18nKey: Record<MatchOperator, string> = {
  [MatchOperator.Equals]: 'rules.opEquals',
  [MatchOperator.NotEquals]: 'rules.opNotEquals',
  [MatchOperator.Contains]: 'rules.opContains',
  [MatchOperator.Regex]: 'rules.opRegex',
  [MatchOperator.StartsWith]: 'rules.opStartsWith',
  [MatchOperator.EndsWith]: 'rules.opEndsWith',
  [MatchOperator.GreaterThan]: 'rules.opGreaterThan',
  [MatchOperator.LessThan]: 'rules.opLessThan',
  [MatchOperator.Exists]: 'rules.opExists',
  [MatchOperator.NotExists]: 'rules.opNotExists',
  [MatchOperator.IsEmpty]: 'rules.opIsEmpty',
  [MatchOperator.JsonSchema]: 'rules.opJsonSchema',
};

const noValueOperators = [MatchOperator.Exists, MatchOperator.IsEmpty, MatchOperator.NotExists];

function isValidSoapXPath(path: string): boolean {
  return path.startsWith('/') || path.includes('local-name(');
}

export default function ConditionBuilder({ value = [], onChange, logicMode = 'AND', onLogicModeChange, protocol }: ConditionBuilderProps) {
  const { t } = useTranslation();
  const { data: protocols } = useProtocols();
  const [xpathPickerIndex, setXpathPickerIndex] = useState<number | null>(null);

  const schema = useMemo(() => {
    if (!protocol || !protocols) return null;
    return protocols.find((p) => p.protocol === protocol) ?? null;
  }, [protocol, protocols]);

  const availableSources = useMemo(() => {
    if (schema) {
      return Object.values(FieldSourceType)
        .filter((v): v is FieldSourceType => typeof v === 'number')
        .filter((v) => schema.supportedSources.includes(v));
    }
    return Object.values(FieldSourceType).filter((v): v is FieldSourceType => typeof v === 'number');
  }, [schema]);

  const operatorOptions = useMemo(() => {
    const allowed = schema
      ? allOperatorKeys.filter((op) => schema.supportedOperators.includes(op))
      : allOperatorKeys;
    return allowed.map((op) => ({ value: op, label: t(operatorI18nKey[op]) }));
  }, [schema, t]);

  const defaultFieldPaths = useMemo((): Record<number, string> => {
    if (schema?.exampleFieldPaths) {
      const map: Record<number, string> = {};
      for (const source of availableSources) {
        const key = FieldSourceTypeLabel[source];
        map[source] = schema.exampleFieldPaths[key] ?? restDefaultFieldPaths[source] ?? '';
      }
      return map;
    }
    return restDefaultFieldPaths;
  }, [schema, availableSources]);

  const placeholders = useMemo((): Record<number, string> => {
    if (schema?.exampleFieldPaths) {
      const map: Record<number, string> = {};
      for (const source of availableSources) {
        const key = FieldSourceTypeLabel[source];
        map[source] = schema.exampleFieldPaths[key] ?? restPlaceholders[source] ?? '';
      }
      return map;
    }
    return restPlaceholders;
  }, [schema, availableSources]);

  const isSoap = protocol === ProtocolType.SOAP;

  const update = (index: number, field: keyof MatchCondition, val: unknown) => {
    const next = [...value];
    if (field === 'sourceType') {
      const newSource = val as FieldSourceType;
      next[index] = {
        ...next[index],
        sourceType: newSource,
        fieldPath: defaultFieldPaths[newSource] ?? '',
      };
    } else {
      next[index] = { ...next[index], [field]: val };
    }
    onChange?.(next);
  };

  const add = () => {
    const sourceType = availableSources[0] ?? FieldSourceType.Body;
    onChange?.([
      ...value,
      {
        sourceType,
        fieldPath: defaultFieldPaths[sourceType] ?? '',
        operator: operatorOptions[0]?.value ?? MatchOperator.Equals,
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
        const needsBodyValidation = cond.sourceType === FieldSourceType.Body;
        const pathError = needsBodyValidation && cond.fieldPath && (
          isSoap
            ? !isValidSoapXPath(cond.fieldPath)
            : !cond.fieldPath.startsWith('$.')
        );
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
                  {availableSources.map((val) => (
                    <Select.Option key={val} value={val}>
                      {FieldSourceTypeLabel[val]}
                    </Select.Option>
                  ))}
                </Select>
                <div>
                  <Flex gap={4} align="center">
                    <Input
                      style={{ width: 180 }}
                      status={pathError ? 'error' : undefined}
                      placeholder={placeholders[cond.sourceType] ?? ''}
                      value={cond.fieldPath}
                      onChange={(e) => update(i, 'fieldPath', e.target.value)}
                    />
                    {isSoap && cond.sourceType === FieldSourceType.Body && (
                      <Button
                        size="small"
                        type="text"
                        icon={<AimOutlined />}
                        onClick={() => setXpathPickerIndex(i)}
                        title={t('soap.pickXPath')}
                      />
                    )}
                  </Flex>
                  {pathError && (
                    <Typography.Text type="danger" style={{ fontSize: 12 }}>
                      {isSoap
                        ? t('validation.soapBodyFieldPathPrefix')
                        : t('validation.bodyFieldPathPrefix')}
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
      <XPathPicker
        open={xpathPickerIndex !== null}
        onClose={() => setXpathPickerIndex(null)}
        onSelect={(xpath) => {
          if (xpathPickerIndex !== null) {
            update(xpathPickerIndex, 'fieldPath', xpath);
          }
        }}
      />
    </div>
  );
}
