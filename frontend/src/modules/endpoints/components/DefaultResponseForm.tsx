import { Modal, Form, InputNumber } from 'antd';
import { useTranslation } from 'react-i18next';
import type { MockEndpoint, SetDefaultResponseRequest } from '@/shared/types';
import CodeEditor from '@/shared/components/CodeEditor';

interface DefaultResponseFormProps {
  open: boolean;
  endpoint: MockEndpoint | null;
  onCancel: () => void;
  onSubmit: (id: string, values: SetDefaultResponseRequest) => void;
  loading?: boolean;
}

export default function DefaultResponseForm({
  open,
  endpoint,
  onCancel,
  onSubmit,
  loading,
}: DefaultResponseFormProps) {
  const { t } = useTranslation();
  const [form] = Form.useForm<SetDefaultResponseRequest>();

  const handleOk = async () => {
    const values = await form.validateFields();
    if (endpoint) {
      onSubmit(endpoint.id, values);
    }
  };

  return (
    <Modal
      title={t('endpoints.defaultResponseForm.title')}
      open={open}
      onCancel={onCancel}
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
          statusCode: endpoint?.defaultStatusCode ?? 200,
          responseBody: endpoint?.defaultResponse ?? '{\n  \n}',
        }}
      >
        <Form.Item
          name="statusCode"
          label={t('endpoints.defaultResponseForm.statusCode')}
          rules={[{ required: true, message: t('validation.required', { field: t('endpoints.defaultResponseForm.statusCode') }) }]}
        >
          <InputNumber min={100} max={599} style={{ width: 160 }} />
        </Form.Item>
        <Form.Item
          name="responseBody"
          label={t('endpoints.defaultResponseForm.responseBody')}
          rules={[{ required: true, message: t('validation.required', { field: t('endpoints.defaultResponseForm.responseBody') }) }]}
        >
          <CodeEditorField />
        </Form.Item>
      </Form>
    </Modal>
  );
}

function CodeEditorField({ value, onChange }: { value?: string; onChange?: (v: string) => void }) {
  return <CodeEditor value={value ?? ''} onChange={onChange} height={300} />;
}
