import { useEffect, useMemo } from 'react';
import { Modal, Form, Input, Select, Typography } from 'antd';
import { useTranslation } from 'react-i18next';
import { ProtocolType, ProtocolTypeLabel } from '@/shared/types';
import type { CreateEndpointRequest, MockEndpoint } from '@/shared/types';

function parseProtocolSettings(raw: string | null): Record<string, string> {
  if (!raw) return {};
  try { return JSON.parse(raw); } catch { return {}; }
}

interface EndpointFormProps {
  open: boolean;
  onCancel: () => void;
  onSubmit: (values: CreateEndpointRequest) => void;
  loading?: boolean;
  editingEndpoint?: MockEndpoint | null;
}

interface EndpointFormValues extends CreateEndpointRequest {
  soapAction?: string;
}

const VISIBLE_PROTOCOLS = [ProtocolType.REST, ProtocolType.SOAP];
const httpMethods = ['GET', 'POST', 'PUT', 'PATCH', 'DELETE', 'HEAD', 'OPTIONS'];

export default function EndpointForm({ open, onCancel, onSubmit, loading, editingEndpoint }: EndpointFormProps) {
  const { t } = useTranslation();
  const [form] = Form.useForm<EndpointFormValues>();
  const isEdit = !!editingEndpoint;
  const watchedProtocol = Form.useWatch('protocol', form);
  const isSoap = watchedProtocol === ProtocolType.SOAP;

  const editSettings = useMemo(
    () => editingEndpoint ? parseProtocolSettings(editingEndpoint.protocolSettings) : {},
    [editingEndpoint],
  );

  useEffect(() => {
    if (open && editingEndpoint) {
      form.setFieldsValue({
        name: editingEndpoint.name,
        serviceName: editingEndpoint.serviceName,
        protocol: editingEndpoint.protocol,
        httpMethod: editingEndpoint.httpMethod,
        path: editingEndpoint.path,
        soapAction: editSettings.soapAction ?? '',
      });
    }
  }, [open, editingEndpoint, editSettings, form]);

  useEffect(() => {
    if (isSoap) {
      form.setFieldValue('httpMethod', 'POST');
    }
  }, [isSoap, form]);

  const handleOk = async () => {
    const values = await form.validateFields();
    const { soapAction, ...rest } = values as CreateEndpointRequest & { soapAction?: string };
    if (soapAction) {
      rest.protocolSettings = JSON.stringify({ soapAction });
    }
    onSubmit(rest);
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
          <Select placeholder={t('endpoints.form.selectProtocol')} disabled={isEdit}>
            {VISIBLE_PROTOCOLS.map((val) => (
              <Select.Option key={val} value={val}>
                {ProtocolTypeLabel[val]}
              </Select.Option>
            ))}
          </Select>
        </Form.Item>
        <Form.Item
          name="httpMethod"
          label={t('endpoints.httpMethod')}
          rules={[{ required: true, message: t('validation.requiredSelect', { field: t('endpoints.httpMethod') }) }]}
          extra={isSoap ? <Typography.Text type="secondary" style={{ fontSize: 12 }}>{t('endpoints.form.soapMethodNote')}</Typography.Text> : undefined}
        >
          <Select placeholder={t('endpoints.form.selectMethod')} disabled={isSoap}>
            {(isSoap ? ['POST'] : httpMethods).map((m) => (
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
        {isSoap && (
          <Form.Item
            name="soapAction"
            label={t('soap.soapAction')}
          >
            <Input placeholder={t('soap.soapActionPlaceholder')} />
          </Form.Item>
        )}
      </Form>
    </Modal>
  );
}
