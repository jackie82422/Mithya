import { Modal, Form, Input, InputNumber, Divider, message } from 'antd';
import { useTranslation } from 'react-i18next';
import type { CreateRuleRequest, MatchCondition } from '@/shared/types';
import { FieldSourceType } from '@/shared/types';
import ConditionBuilder from './ConditionBuilder';
import ResponseEditor from './ResponseEditor';

interface RuleFormProps {
  open: boolean;
  onCancel: () => void;
  onSubmit: (values: CreateRuleRequest) => void;
  loading?: boolean;
}

interface FormValues {
  ruleName: string;
  priority: number;
  conditions: MatchCondition[];
  statusCode: number;
  responseBody: string;
  responseHeadersStr: string;
  delayMs: number;
}

export default function RuleForm({ open, onCancel, onSubmit, loading }: RuleFormProps) {
  const { t } = useTranslation();
  const [form] = Form.useForm<FormValues>();

  const handleOk = async () => {
    const values = await form.validateFields();
    const conditions: MatchCondition[] = values.conditions || [];
    for (const c of conditions) {
      if (!c.fieldPath) {
        message.error('FieldPath is required for all conditions');
        return;
      }
      if (c.sourceType === FieldSourceType.Body && !c.fieldPath.startsWith('$.')) {
        message.error('Body FieldPath must start with $.');
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
    });
    form.resetFields();
  };

  return (
    <Modal
      title={t('rules.form.title')}
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
        }}
      >
        <Form.Item
          name="ruleName"
          label={t('rules.ruleName')}
          rules={[{ required: true }]}
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
        <ResponseEditor />
      </Form>
    </Modal>
  );
}
