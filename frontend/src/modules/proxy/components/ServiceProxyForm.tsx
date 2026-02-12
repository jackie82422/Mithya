import { useEffect } from 'react';
import { Modal, Form, Input, InputNumber, Select, Switch, Space } from 'antd';
import { useTranslation } from 'react-i18next';
import type { CreateServiceProxyRequest, ServiceProxy } from '@/shared/types';
import { useAvailableServices } from '../hooks';
import CodeEditor from '@/shared/components/CodeEditor';

interface ServiceProxyFormProps {
  open: boolean;
  onCancel: () => void;
  onSubmit: (values: CreateServiceProxyRequest) => void;
  loading?: boolean;
  editing?: ServiceProxy | null;
}

interface FormValues {
  serviceName: string;
  targetBaseUrl: string;
  stripPathPrefix: string;
  timeoutMs: number;
  forwardHeaders: boolean;
  fallbackEnabled: boolean;
  isRecording: boolean;
  additionalHeadersStr: string;
}

export default function ServiceProxyForm({
  open,
  onCancel,
  onSubmit,
  loading,
  editing,
}: ServiceProxyFormProps) {
  const { t } = useTranslation();
  const [form] = Form.useForm<FormValues>();
  const isEdit = !!editing;
  const { data: services } = useAvailableServices();

  useEffect(() => {
    if (open && editing) {
      form.setFieldsValue({
        serviceName: editing.serviceName,
        targetBaseUrl: editing.targetBaseUrl,
        stripPathPrefix: editing.stripPathPrefix ?? '',
        timeoutMs: editing.timeoutMs,
        forwardHeaders: editing.forwardHeaders,
        fallbackEnabled: editing.fallbackEnabled,
        isRecording: editing.isRecording,
        additionalHeadersStr: editing.additionalHeaders
          ? (typeof editing.additionalHeaders === 'string'
            ? editing.additionalHeaders
            : JSON.stringify(editing.additionalHeaders, null, 2))
          : '{}',
      });
    }
  }, [open, editing, form]);

  // When creating, only show services without existing proxy
  const availableServices = isEdit
    ? services
    : services?.filter((s) => !s.hasProxy);

  const handleOk = async () => {
    const values = await form.validateFields();
    onSubmit({
      serviceName: values.serviceName,
      targetBaseUrl: values.targetBaseUrl,
      stripPathPrefix: values.stripPathPrefix || null,
      timeoutMs: values.timeoutMs,
      forwardHeaders: values.forwardHeaders,
      fallbackEnabled: values.fallbackEnabled,
      isRecording: values.isRecording,
      additionalHeaders: values.additionalHeadersStr && values.additionalHeadersStr.trim() !== '{}'
        ? (() => { try { JSON.parse(values.additionalHeadersStr); return values.additionalHeadersStr; } catch { return null; } })()
        : null,
    });
  };

  return (
    <Modal
      title={isEdit ? t('proxy.form.editTitle') : t('proxy.form.title')}
      open={open}
      onCancel={() => {
        form.resetFields();
        onCancel();
      }}
      onOk={handleOk}
      confirmLoading={loading}
      okText={t('common.save')}
      cancelText={t('common.cancel')}
      width={640}
      destroyOnClose
    >
      <Form
        form={form}
        layout="vertical"
        preserve={false}
        initialValues={{
          serviceName: '',
          targetBaseUrl: '',
          stripPathPrefix: '',
          timeoutMs: 10000,
          forwardHeaders: true,
          fallbackEnabled: true,
          isRecording: false,
          additionalHeadersStr: '{}',
        }}
      >
        <Form.Item
          name="serviceName"
          label={t('proxy.serviceName')}
          rules={[{ required: true, message: t('validation.requiredSelect', { field: t('proxy.serviceName') }) }]}
        >
          <Select
            placeholder={t('proxy.selectService')}
            disabled={isEdit}
            showSearch
            optionFilterProp="children"
          >
            {availableServices?.map((s) => (
              <Select.Option key={s.serviceName} value={s.serviceName}>
                {s.serviceName} ({t('proxy.endpointCount', { count: s.endpointCount })})
              </Select.Option>
            ))}
          </Select>
        </Form.Item>

        <Form.Item
          name="targetBaseUrl"
          label={t('proxy.targetBaseUrl')}
          rules={[{ required: true, message: t('validation.required', { field: t('proxy.targetBaseUrl') }) }]}
        >
          <Input placeholder={t('proxy.targetPlaceholder')} />
        </Form.Item>

        <Form.Item name="stripPathPrefix" label={t('proxy.stripPathPrefix')}>
          <Input placeholder="/api/v1" />
        </Form.Item>

        <Form.Item name="timeoutMs" label={t('proxy.timeout')}>
          <InputNumber min={1000} max={120000} addonAfter="ms" style={{ width: 200 }} />
        </Form.Item>

        <Space size="large">
          <Form.Item name="forwardHeaders" valuePropName="checked">
            <Switch />
          </Form.Item>
          <span style={{ position: 'relative', top: -12 }}>{t('proxy.forwardHeaders')}</span>

          <Form.Item name="fallbackEnabled" valuePropName="checked">
            <Switch />
          </Form.Item>
          <span style={{ position: 'relative', top: -12 }}>{t('proxy.fallbackEnabled')}</span>

          <Form.Item name="isRecording" valuePropName="checked">
            <Switch />
          </Form.Item>
          <span style={{ position: 'relative', top: -12 }}>{t('proxy.enableRecording')}</span>
        </Space>

        <Form.Item name="additionalHeadersStr" label={t('proxy.additionalHeaders')}>
          <CodeEditorField height={120} />
        </Form.Item>
      </Form>
    </Modal>
  );
}

function CodeEditorField({
  value,
  onChange,
  height = 120,
}: {
  value?: string;
  onChange?: (v: string) => void;
  height?: number;
}) {
  return <CodeEditor value={value ?? '{}'} onChange={onChange} height={height} />;
}
