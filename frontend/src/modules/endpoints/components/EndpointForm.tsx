import { useEffect } from 'react';
import { Modal, Form, Input, Select, Tooltip } from 'antd';
import { useTranslation } from 'react-i18next';
import { ProtocolType, ProtocolTypeLabel } from '@/shared/types';
import type { CreateEndpointRequest, MockEndpoint } from '@/shared/types';

interface EndpointFormProps {
  open: boolean;
  onCancel: () => void;
  onSubmit: (values: CreateEndpointRequest) => void;
  loading?: boolean;
  editingEndpoint?: MockEndpoint | null;
}

const httpMethods = ['GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'HEAD', 'OPTIONS'];

export default function EndpointForm({ open, onCancel, onSubmit, loading, editingEndpoint }: EndpointFormProps) {
  const { t } = useTranslation();
  const [form] = Form.useForm<CreateEndpointRequest>();
  const isEdit = !!editingEndpoint;

  useEffect(() => {
    if (open && editingEndpoint) {
      form.setFieldsValue({
        name: editingEndpoint.name,
        serviceName: editingEndpoint.serviceName,
        protocol: editingEndpoint.protocol,
        httpMethod: editingEndpoint.httpMethod,
        path: editingEndpoint.path,
      });
    }
  }, [open, editingEndpoint, form]);

  const handleOk = async () => {
    const values = await form.validateFields();
    onSubmit(values);
  };

  const handleCancel = () => {
    form.resetFields();
    onCancel();
  };

  return (
    <Modal
      title={isEdit ? t('endpoints.form.editTitle') : t('endpoints.form.title')}
      open={open}
      onCancel={handleCancel}
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
          rules={[{ required: true, message: t('validation.required', { field: t('endpoints.name') }) }]}
        >
          <Input placeholder={t('endpoints.form.namePlaceholder')} />
        </Form.Item>
        <Form.Item
          name="serviceName"
          label={t('endpoints.serviceName')}
          rules={[{ required: true, message: t('validation.required', { field: t('endpoints.serviceName') }) }]}
        >
          <Input placeholder={t('endpoints.form.serviceNamePlaceholder')} />
        </Form.Item>
        <Form.Item
          name="protocol"
          label={t('endpoints.protocol')}
          rules={[{ required: true, message: t('validation.requiredSelect', { field: t('endpoints.protocol') }) }]}
        >
          <Tooltip title={isEdit ? t('endpoints.form.protocolImmutable') : undefined}>
            <Select placeholder={t('endpoints.form.selectProtocol')} disabled={isEdit}>
              {Object.entries(ProtocolTypeLabel).map(([val, label]) => (
                <Select.Option key={val} value={Number(val)}>
                  {label}
                </Select.Option>
              ))}
            </Select>
          </Tooltip>
        </Form.Item>
        <Form.Item
          name="httpMethod"
          label={t('endpoints.httpMethod')}
          rules={[{ required: true, message: t('validation.requiredSelect', { field: t('endpoints.httpMethod') }) }]}
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
          rules={[{ required: true, message: t('validation.required', { field: t('endpoints.path') }) }]}
        >
          <Input placeholder={t('endpoints.form.pathPlaceholder')} />
        </Form.Item>
      </Form>
    </Modal>
  );
}
