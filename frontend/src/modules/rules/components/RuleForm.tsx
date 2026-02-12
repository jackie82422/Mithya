import { useEffect, useState } from 'react';
import { Modal, Form, Input, InputNumber, Divider, message } from 'antd';
import { useTranslation } from 'react-i18next';
import type { CreateRuleRequest, LogicMode, MatchCondition, MockRule, ProtocolType } from '@/shared/types';
import { FaultType, FieldSourceType, ProtocolType as PT, parseMatchConditions } from '@/shared/types';
import ConditionBuilder from './ConditionBuilder';
import ResponseEditor from './ResponseEditor';

interface RuleFormProps {
  open: boolean;
  onCancel: () => void;
  onSubmit: (values: CreateRuleRequest) => void;
  loading?: boolean;
  editingRule?: MockRule | null;
  endpointPath?: string;
  endpointMethod?: string;
  endpointProtocol?: ProtocolType;
}

interface FormValues {
  ruleName: string;
  priority: number;
  conditions: MatchCondition[];
  statusCode: number;
  responseBody: string;
  responseHeadersStr: string;
  delayMs: number;
  isTemplate: boolean;
  faultConfig?: { minDelay?: number; maxDelay?: number; statusCode?: number; byteCount?: number; timeoutMs?: number };
}

function parseJson<T>(raw: string | null, fallback: T): T {
  if (!raw) return fallback;
  try {
    return JSON.parse(raw);
  } catch {
    return fallback;
  }
}

export default function RuleForm({ open, onCancel, onSubmit, loading, editingRule, endpointPath, endpointMethod, endpointProtocol }: RuleFormProps) {
  const { t } = useTranslation();
  const [form] = Form.useForm<FormValues>();
  const [logicMode, setLogicMode] = useState<LogicMode>('AND');
  const isEdit = !!editingRule;
  const isSoap = endpointProtocol === PT.SOAP;

  useEffect(() => {
    if (open && editingRule) {
      form.setFieldsValue({
        ruleName: editingRule.ruleName,
        priority: editingRule.priority,
        conditions: parseMatchConditions(editingRule.matchConditions),
        statusCode: editingRule.responseStatusCode,
        responseBody: editingRule.responseBody,
        responseHeadersStr: editingRule.responseHeaders
          ? JSON.stringify(parseJson(editingRule.responseHeaders, {}), null, 2)
          : '{\n  "Content-Type": "application/json"\n}',
        delayMs: editingRule.delayMs,
        isTemplate: editingRule.isTemplate ?? false,
      });
      setLogicMode(editingRule.logicMode === 1 ? 'OR' : 'AND');
    }
  }, [open, editingRule, form]);

  const handleOk = async () => {
    const values = await form.validateFields();
    const conditions: MatchCondition[] = values.conditions || [];
    for (const c of conditions) {
      if (!c.fieldPath) {
        message.error(t('validation.fieldPathRequired'));
        return;
      }
      if (c.sourceType === FieldSourceType.Body) {
        const isSoap = endpointProtocol === PT.SOAP;
        if (isSoap) {
          if (!c.fieldPath.startsWith('/') && !c.fieldPath.includes('local-name(')) {
            message.error(t('validation.soapBodyFieldPathPrefix'));
            return;
          }
        } else if (!c.fieldPath.startsWith('$.')) {
          message.error(t('validation.bodyFieldPathPrefix'));
          return;
        }
      }
    }
    let responseHeaders: Record<string, string> | undefined;
    if (values.responseHeadersStr) {
      try {
        responseHeaders = JSON.parse(values.responseHeadersStr);
      } catch {
        responseHeaders = undefined;
      }
    }
    const faultType = (values as unknown as Record<string, unknown>).faultType as FaultType | undefined;
    const faultConfig = values.faultConfig;
    onSubmit({
      ruleName: values.ruleName,
      priority: values.priority,
      conditions: values.conditions || [],
      statusCode: values.statusCode,
      responseBody: values.responseBody,
      responseHeaders,
      delayMs: values.delayMs,
      isTemplate: values.isTemplate ?? false,
      faultType: faultType ?? FaultType.None,
      faultConfig: faultConfig && faultType !== FaultType.None ? JSON.stringify(faultConfig) : null,
      logicMode: logicMode === 'OR' ? 1 : 0,
    });
  };

  return (
    <Modal
      title={isEdit ? t('rules.form.editTitle') : t('rules.form.title')}
      open={open}
      onCancel={() => {
        form.resetFields();
        onCancel();
      }}
      onOk={handleOk}
      confirmLoading={loading}
      okText={t('common.save')}
      cancelText={t('common.cancel')}
      width={800}
      destroyOnClose
    >
      <Form
        form={form}
        layout="vertical"
        preserve={false}
        initialValues={{
          priority: 100,
          statusCode: 200,
          delayMs: 0,
          conditions: [],
          responseBody: isSoap
            ? '<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/">\n  <soapenv:Header/>\n  <soapenv:Body>\n    \n  </soapenv:Body>\n</soapenv:Envelope>'
            : '{\n  \n}',
          responseHeadersStr: isSoap
            ? '{\n  "Content-Type": "text/xml; charset=utf-8"\n}'
            : '{\n  "Content-Type": "application/json"\n}',
          isTemplate: false,
        }}
      >
        <Form.Item
          name="ruleName"
          label={t('rules.ruleName')}
          rules={[{ required: true, message: t('validation.required', { field: t('rules.ruleName') }) }]}
        >
          <Input placeholder={t('rules.form.ruleNamePlaceholder')} />
        </Form.Item>
        <Form.Item
          name="priority"
          label={t('rules.priority')}
          tooltip={t('rules.priorityHint')}
        >
          <InputNumber min={0} max={9999} style={{ width: 160 }} />
        </Form.Item>

        <Divider>{t('rules.conditions')}</Divider>
        <Form.Item name="conditions">
          <ConditionBuilder logicMode={logicMode} onLogicModeChange={setLogicMode} protocol={endpointProtocol} />
        </Form.Item>

        <Divider>{t('rules.response')}</Divider>
        <ResponseEditor endpointPath={endpointPath} endpointMethod={endpointMethod} endpointProtocol={endpointProtocol} />
      </Form>
    </Modal>
  );
}
