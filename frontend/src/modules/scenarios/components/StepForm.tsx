import { useEffect } from 'react';
import { Modal, Form, Input, InputNumber, Select } from 'antd';
import { useTranslation } from 'react-i18next';
import type { CreateStepRequest, MockEndpoint, ScenarioStep } from '@/shared/types';
import CodeEditor from '@/shared/components/CodeEditor';

interface StepFormProps {
  open: boolean;
  onCancel: () => void;
  onSubmit: (values: CreateStepRequest) => void;
  loading?: boolean;
  editing?: ScenarioStep | null;
  endpoints?: MockEndpoint[];
  existingStates?: string[];
}

interface FormValues {
  stateName: string;
  endpointId: string;
  responseStatusCode: number;
  responseBody: string;
  delayMs: number;
  nextState: string;
  priority: number;
}

export default function StepForm({
  open,
  onCancel,
  onSubmit,
  loading,
  editing,
  endpoints,
  existingStates = [],
}: StepFormProps) {
  const { t } = useTranslation();
  const [form] = Form.useForm<FormValues>();
  const isEdit = !!editing;

  useEffect(() => {
    if (open && editing) {
      form.setFieldsValue({
        stateName: editing.stateName,
        endpointId: editing.endpointId,
        responseStatusCode: editing.responseStatusCode,
        responseBody: editing.responseBody,
        delayMs: editing.delayMs,
        nextState: editing.nextState ?? '',
        priority: editing.priority,
      });
    }
  }, [open, editing, form]);

  const handleOk = async () => {
    const values = await form.validateFields();
    onSubmit({
      stateName: values.stateName,
      endpointId: values.endpointId,
      responseStatusCode: values.responseStatusCode,
      responseBody: values.responseBody,
      delayMs: values.delayMs,
      nextState: values.nextState || null,
      priority: values.priority,
    });
  };

  const stateOptions = existingStates.map((s) => ({ label: s, value: s }));

  return (
    <Modal
      title={isEdit ? t('scenarios.stepForm.editTitle') : t('scenarios.stepForm.title')}
      open={open}
      onCancel={() => {
        form.resetFields();
        onCancel();
      }}
      onOk={handleOk}
      confirmLoading={loading}
      okText={t('common.save')}
      cancelText={t('common.cancel')}
      width={700}
      destroyOnClose
    >
      <Form
        form={form}
        layout="vertical"
        preserve={false}
        initialValues={{
          stateName: '',
          endpointId: '',
          responseStatusCode: 200,
          responseBody: '{\n  \n}',
          delayMs: 0,
          nextState: '',
          priority: 100,
        }}
      >
        <Form.Item
          name="stateName"
          label={t('scenarios.stateName')}
          rules={[{ required: true, message: t('validation.required', { field: t('scenarios.stateName') }) }]}
        >
          <Select
            mode="tags"
            maxCount={1}
            placeholder={t('scenarios.stateNamePlaceholder')}
            options={stateOptions}
            onChange={(vals) => form.setFieldValue('stateName', vals[vals.length - 1] ?? '')}
          />
        </Form.Item>

        <Form.Item
          name="endpointId"
          label={t('scenarios.selectEndpoint')}
          rules={[{ required: true, message: t('validation.requiredSelect', { field: t('scenarios.selectEndpoint') }) }]}
        >
          <Select placeholder={t('scenarios.selectEndpoint')}>
            {endpoints?.map((ep) => (
              <Select.Option key={ep.id} value={ep.id}>
                {ep.httpMethod} {ep.path} - {ep.name}
              </Select.Option>
            ))}
          </Select>
        </Form.Item>

        <Form.Item
          name="responseStatusCode"
          label={t('rules.statusCode')}
        >
          <InputNumber min={100} max={599} style={{ width: 140 }} />
        </Form.Item>

        <Form.Item
          name="responseBody"
          label={t('rules.responseBody')}
          rules={[{ required: true, message: t('validation.required', { field: t('rules.responseBody') }) }]}
        >
          <CodeEditorField />
        </Form.Item>

        <Form.Item
          name="delayMs"
          label={t('rules.delayMs')}
        >
          <InputNumber min={0} max={60000} addonAfter="ms" style={{ width: 160 }} />
        </Form.Item>

        <Form.Item
          name="nextState"
          label={t('scenarios.nextState')}
          tooltip={t('scenarios.nextStateHint')}
        >
          <Select
            mode="tags"
            maxCount={1}
            placeholder={t('scenarios.nextStatePlaceholder')}
            options={stateOptions}
            onChange={(vals) => form.setFieldValue('nextState', vals[vals.length - 1] ?? '')}
          />
        </Form.Item>

        <Form.Item name="priority" label={t('rules.priority')}>
          <InputNumber min={0} max={9999} style={{ width: 160 }} />
        </Form.Item>
      </Form>
    </Modal>
  );
}

function CodeEditorField({
  value,
  onChange,
}: {
  value?: string;
  onChange?: (v: string) => void;
}) {
  return <CodeEditor value={value ?? ''} onChange={onChange} height={180} />;
}
