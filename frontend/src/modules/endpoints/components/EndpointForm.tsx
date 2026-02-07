import { Modal, Form, Input, Select } from 'antd';
import { useTranslation } from 'react-i18next';
import { ProtocolType, ProtocolTypeLabel } from '@/shared/types';
import type { CreateEndpointRequest } from '@/shared/types';

interface EndpointFormProps {
  open: boolean;
  onCancel: () => void;
  onSubmit: (values: CreateEndpointRequest) => void;
  loading?: boolean;
}

const httpMethods = ['GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'HEAD', 'OPTIONS'];

export default function EndpointForm({ open, onCancel, onSubmit, loading }: EndpointFormProps) {
  const { t } = useTranslation();
  const [form] = Form.useForm<CreateEndpointRequest>();

  const handleOk = async () => {
    const values = await form.validateFields();
    onSubmit(values);
    form.resetFields();
  };

  return (
    <Modal
      title={t('endpoints.form.title')}
      open={open}
      onCancel={() => {
        form.resetFields();
        onCancel();
      }}
      onOk={handleOk}
      confirmLoading={loading}
      okText={t('common.save')}
      cancelText={t('common.cancel')}
      destroyOnClose
    >
      <Form form={form} layout="vertical" preserve={false}>
        <Form.Item
          name="name"
          label={t('endpoints.name')}
          rules={[{ required: true }]}
        >
          <Input placeholder={t('endpoints.form.namePlaceholder')} />
        </Form.Item>
        <Form.Item
          name="serviceName"
          label={t('endpoints.serviceName')}
          rules={[{ required: true }]}
        >
          <Input placeholder={t('endpoints.form.serviceNamePlaceholder')} />
        </Form.Item>
        <Form.Item
          name="protocol"
          label={t('endpoints.protocol')}
          rules={[{ required: true }]}
        >
          <Select placeholder={t('endpoints.form.selectProtocol')}>
            {Object.entries(ProtocolTypeLabel).map(([val, label]) => (
              <Select.Option key={val} value={Number(val)}>
                {label}
              </Select.Option>
            ))}
          </Select>
        </Form.Item>
        <Form.Item
          name="httpMethod"
          label={t('endpoints.httpMethod')}
          rules={[{ required: true }]}
        >
          <Select placeholder={t('endpoints.form.selectMethod')}>
            {httpMethods.map((m) => (
              <Select.Option key={m} value={m}>
                {m}
              </Select.Option>
            ))}
          </Select>
        </Form.Item>
        <Form.Item
          name="path"
          label={t('endpoints.path')}
          rules={[{ required: true }]}
        >
          <Input placeholder={t('endpoints.form.pathPlaceholder')} />
        </Form.Item>
      </Form>
    </Modal>
  );
}
