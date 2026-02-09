import { useEffect } from 'react';
import { Modal, Form, Input, InputNumber, Select, Switch, Radio, Space } from 'antd';
import { useTranslation } from 'react-i18next';
import type { CreateProxyConfigRequest, ProxyConfig, MockEndpoint } from '@/shared/types';
import CodeEditor from '@/shared/components/CodeEditor';

interface ProxyConfigFormProps {
  open: boolean;
  onCancel: () => void;
  onSubmit: (values: CreateProxyConfigRequest) => void;
  loading?: boolean;
  editing?: ProxyConfig | null;
  endpoints?: MockEndpoint[];
}

interface FormValues {
  scope: 'global' | 'endpoint';
  endpointId: string | null;
  targetBaseUrl: string;
  stripPathPrefix: string;
  timeoutMs: number;
  forwardHeaders: boolean;
  isRecording: boolean;
  additionalHeadersStr: string;
}

export default function ProxyConfigForm({
  open,
  onCancel,
  onSubmit,
  loading,
  editing,
  endpoints,
}: ProxyConfigFormProps) {
  const { t } = useTranslation();
  const [form] = Form.useForm<FormValues>();
  const isEdit = !!editing;
  const scope = Form.useWatch('scope', form);

  useEffect(() => {
    if (open && editing) {
      form.setFieldsValue({
        scope: editing.endpointId ? 'endpoint' : 'global',
        endpointId: editing.endpointId,
        targetBaseUrl: editing.targetBaseUrl,
        stripPathPrefix: editing.stripPathPrefix ?? '',
        timeoutMs: editing.timeoutMs,
        forwardHeaders: editing.forwardHeaders,
        isRecording: editing.isRecording,
        additionalHeadersStr: editing.additionalHeaders
          ? (typeof editing.additionalHeaders === 'string'
            ? editing.additionalHeaders
            : JSON.stringify(editing.additionalHeaders, null, 2))
          : '{}',
      });
    }
  }, [open, editing, form]);

  const handleOk = async () => {
    const values = await form.validateFields();
    onSubmit({
      endpointId: values.scope === 'global' ? null : values.endpointId,
      targetBaseUrl: values.targetBaseUrl,
      stripPathPrefix: values.stripPathPrefix || null,
      timeoutMs: values.timeoutMs,
      forwardHeaders: values.forwardHeaders,
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
          scope: 'global',
          endpointId: null,
          targetBaseUrl: '',
          stripPathPrefix: '',
          timeoutMs: 10000,
          forwardHeaders: true,
          isRecording: false,
          additionalHeadersStr: '{}',
        }}
      >
        <Form.Item name="scope" label={t('proxy.scope')}>
          <Radio.Group>
            <Radio value="global">{t('proxy.scopeGlobal')}</Radio>
            <Radio value="endpoint">{t('proxy.scopeEndpoint')}</Radio>
          </Radio.Group>
        </Form.Item>

        {scope === 'endpoint' && (
          <Form.Item
            name="endpointId"
            label={t('proxy.selectEndpoint')}
            rules={[{ required: true, message: t('validation.requiredSelect', { field: t('proxy.selectEndpoint') }) }]}
          >
            <Select placeholder={t('proxy.selectEndpoint')}>
              {endpoints?.map((ep) => (
                <Select.Option key={ep.id} value={ep.id}>
                  {ep.httpMethod} {ep.path} - {ep.name}
                </Select.Option>
              ))}
            </Select>
          </Form.Item>
        )}

        <Form.Item
          name="targetBaseUrl"
          label={t('proxy.targetBaseUrl')}
          rules={[{ required: true, message: t('validation.required', { field: t('proxy.targetBaseUrl') }) }]}
        >
          <Input placeholder={t('proxy.targetPlaceholder')} />
        </Form.Item>

        <Form.Item name="stripPathPrefix" label={t('proxy.stripPathPrefix')}>
          <Input placeholder={t('proxy.stripPrefixPlaceholder')} />
        </Form.Item>

        <Form.Item name="timeoutMs" label={t('proxy.timeout')}>
          <InputNumber min={1000} max={120000} addonAfter="ms" style={{ width: 200 }} />
        </Form.Item>

        <Space size="large">
          <Form.Item name="forwardHeaders" valuePropName="checked">
            <Switch />
          </Form.Item>
          <span style={{ position: 'relative', top: -12 }}>{t('proxy.forwardHeaders')}</span>

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
