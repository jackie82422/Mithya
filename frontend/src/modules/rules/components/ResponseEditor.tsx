import { Form, InputNumber, Space } from 'antd';
import { useTranslation } from 'react-i18next';
import CodeEditor from '@/shared/components/CodeEditor';

export default function ResponseEditor() {
  const { t } = useTranslation();

  return (
    <>
      <Space size="large" style={{ width: '100%' }}>
        <Form.Item
          name="statusCode"
          label={t('rules.statusCode')}
          initialValue={200}
        >
          <InputNumber min={100} max={599} style={{ width: 140 }} />
        </Form.Item>
        <Form.Item
          name="delayMs"
          label={t('rules.delayMs')}
          initialValue={0}
        >
          <InputNumber min={0} max={60000} style={{ width: 140 }} addonAfter="ms" />
        </Form.Item>
      </Space>
      <Form.Item
        name="responseBody"
        label={t('rules.responseBody')}
        rules={[{ required: true, message: t('validation.required', { field: t('rules.responseBody') }) }]}
      >
        <CodeEditorField />
      </Form.Item>
      <Form.Item name="responseHeadersStr" label={t('rules.responseHeaders')}>
        <CodeEditorField height={120} />
      </Form.Item>
    </>
  );
}

function CodeEditorField({
  value,
  onChange,
  height = 250,
}: {
  value?: string;
  onChange?: (v: string) => void;
  height?: number;
}) {
  return <CodeEditor value={value ?? ''} onChange={onChange} height={height} />;
}
