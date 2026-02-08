import { useEffect } from 'react';
import { Modal, Form, Input } from 'antd';
import { useTranslation } from 'react-i18next';
import type { CreateScenarioRequest, Scenario } from '@/shared/types';

interface ScenarioFormProps {
  open: boolean;
  onCancel: () => void;
  onSubmit: (values: CreateScenarioRequest) => void;
  loading?: boolean;
  editing?: Scenario | null;
}

export default function ScenarioForm({ open, onCancel, onSubmit, loading, editing }: ScenarioFormProps) {
  const { t } = useTranslation();
  const [form] = Form.useForm<CreateScenarioRequest>();
  const isEdit = !!editing;

  useEffect(() => {
    if (open && editing) {
      form.setFieldsValue({
        name: editing.name,
        description: editing.description ?? '',
        initialState: editing.initialState,
      });
    }
  }, [open, editing, form]);

  const handleOk = async () => {
    const values = await form.validateFields();
    onSubmit(values);
  };

  return (
    <Modal
      title={isEdit ? t('scenarios.form.editTitle') : t('scenarios.form.title')}
      open={open}
      onCancel={() => {
        form.resetFields();
        onCancel();
      }}
      onOk={handleOk}
      confirmLoading={loading}
      okText={t('common.save')}
      cancelText={t('common.cancel')}
      width={520}
      destroyOnClose
    >
      <Form
        form={form}
        layout="vertical"
        preserve={false}
        initialValues={{ name: '', description: '', initialState: '' }}
      >
        <Form.Item
          name="name"
          label={t('scenarios.name')}
          rules={[{ required: true, message: t('validation.required', { field: t('scenarios.name') }) }]}
        >
          <Input placeholder={t('scenarios.namePlaceholder')} />
        </Form.Item>
        <Form.Item name="description" label={t('scenarios.description')}>
          <Input.TextArea rows={2} placeholder={t('scenarios.descriptionPlaceholder')} />
        </Form.Item>
        <Form.Item
          name="initialState"
          label={t('scenarios.initialState')}
          rules={[{ required: true, message: t('validation.required', { field: t('scenarios.initialState') }) }]}
        >
          <Input placeholder={t('scenarios.initialStatePlaceholder')} />
        </Form.Item>
      </Form>
    </Modal>
  );
}
