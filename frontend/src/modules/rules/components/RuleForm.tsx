import { useEffect } from 'react';
import { Modal, Form, Input, InputNumber, Divider, message } from 'antd';
import { useTranslation } from 'react-i18next';
import type { CreateRuleRequest, MatchCondition, MockRule } from '@/shared/types';
import { FieldSourceType, parseMatchConditions } from '@/shared/types';
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
}

function parseJson<T>(raw: string | null, fallback: T): T {
  if (!raw) return fallback;
  try {
    return JSON.parse(raw);
  } catch {
    return fallback;
  }
}

export default function RuleForm({ open, onCancel, onSubmit, loading, editingRule, endpointPath, endpointMethod }: RuleFormProps) {
  const { t } = useTranslation();
  const [form] = Form.useForm<FormValues>();
  const isEdit = !!editingRule;

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
      if (c.sourceType === FieldSourceType.Body && !c.fieldPath.startsWith('$.')) {
        message.error(t('validation.bodyFieldPathPrefix'));
        return;
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
    onSubmit({
      ruleName: values.ruleName,
      priority: values.priority,
      conditions: values.conditions || [],
      statusCode: values.statusCode,
      responseBody: values.responseBody,
      responseHeaders,
      delayMs: values.delayMs,
      isTemplate: values.isTemplate ?? false,
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
          responseBody: '{\n  \n}',
          responseHeadersStr: '{\n  "Content-Type": "application/json"\n}',
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
          <ConditionBuilder />
        </Form.Item>

        <Divider>{t('rules.response')}</Divider>
        <ResponseEditor endpointPath={endpointPath} endpointMethod={endpointMethod} />
      </Form>
    </Modal>
  );
}
