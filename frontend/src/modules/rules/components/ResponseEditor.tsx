import { useState } from 'react';
import { Form, InputNumber, Space, Switch, Tooltip, Button, Divider } from 'antd';
import { QuestionCircleOutlined, EyeOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import { FaultType, ProtocolType } from '@/shared/types';
import CodeEditor from '@/shared/components/CodeEditor';
import TemplateVariableRef from './TemplateVariableRef';
import TemplatePreview from './TemplatePreview';
import FaultInjectionConfig from './FaultInjectionConfig';

interface ResponseEditorProps {
  endpointPath?: string;
  endpointMethod?: string;
  endpointProtocol?: ProtocolType;
}

export default function ResponseEditor({ endpointPath, endpointMethod, endpointProtocol }: ResponseEditorProps) {
  const { t } = useTranslation();
  const [isTemplate, setIsTemplate] = useState(false);
  const [previewOpen, setPreviewOpen] = useState(false);
  const [faultType, setFaultType] = useState<FaultType>(FaultType.None);
  const form = Form.useFormInstance();

  const bodyDisabled = [
    FaultType.ConnectionReset,
    FaultType.EmptyResponse,
    FaultType.MalformedResponse,
    FaultType.Timeout,
  ].includes(faultType);

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

      <Divider dashed style={{ margin: '12px 0' }} />
      <FaultInjectionConfig
        faultType={faultType}
        onFaultTypeChange={(ft) => {
          setFaultType(ft);
          form?.setFieldValue('faultType', ft);
        }}
      />
      <Form.Item name="faultType" hidden><input /></Form.Item>
      <Divider dashed style={{ margin: '12px 0' }} />

      <Form.Item name="isTemplate" valuePropName="checked" style={{ marginBottom: 12 }}>
        <Space>
          <Switch
            onChange={(checked) => setIsTemplate(checked)}
          />
          <span>{t('rules.enableTemplate')}</span>
          <Tooltip title={t('rules.templateTooltip')}>
            <QuestionCircleOutlined style={{ color: 'var(--color-text-secondary)' }} />
          </Tooltip>
        </Space>
      </Form.Item>

      {isTemplate && <TemplateVariableRef />}

      <Form.Item
        name="responseBody"
        label={t('rules.responseBody')}
        rules={[{ required: !bodyDisabled, message: t('validation.required', { field: t('rules.responseBody') }) }]}
      >
        <CodeEditorField language={isTemplate ? 'handlebars' : endpointProtocol === ProtocolType.SOAP ? 'xml' : 'json'} readOnly={bodyDisabled} />
      </Form.Item>

      {isTemplate && (
        <Button
          icon={<EyeOutlined />}
          onClick={() => setPreviewOpen(true)}
          style={{ marginBottom: 16 }}
        >
          {t('rules.previewTemplate')}
        </Button>
      )}

      <Form.Item name="responseHeadersStr" label={t('rules.responseHeaders')}>
        <CodeEditorField height={120} />
      </Form.Item>

      {isTemplate && (
        <TemplatePreview
          open={previewOpen}
          onClose={() => setPreviewOpen(false)}
          template={form?.getFieldValue('responseBody') ?? ''}
          endpointPath={endpointPath}
          endpointMethod={endpointMethod}
        />
      )}
    </>
  );
}

function CodeEditorField({
  value,
  onChange,
  height = 250,
  language = 'json' as 'json' | 'handlebars' | 'xml',
  readOnly = false,
}: {
  value?: string;
  onChange?: (v: string) => void;
  height?: number;
  language?: 'json' | 'handlebars' | 'xml';
  readOnly?: boolean;
}) {
  return <CodeEditor value={value ?? ''} onChange={onChange} height={height} language={language} readOnly={readOnly} />;
}
