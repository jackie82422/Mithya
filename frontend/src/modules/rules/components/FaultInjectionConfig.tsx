import { Form, InputNumber, Space, Alert, Segmented } from 'antd';
import { useTranslation } from 'react-i18next';
import { FaultType } from '@/shared/types';

interface FaultInjectionConfigProps {
  faultType: FaultType;
  onFaultTypeChange: (type: FaultType) => void;
}

export default function FaultInjectionConfig({ faultType, onFaultTypeChange }: FaultInjectionConfigProps) {
  const { t } = useTranslation();

  const options = [
    { label: t('rules.faultNone'), value: FaultType.None },
    { label: t('rules.faultRandomDelay'), value: FaultType.RandomDelay },
    { label: t('rules.faultConnectionReset'), value: FaultType.ConnectionReset },
    { label: t('rules.faultEmptyResponse'), value: FaultType.EmptyResponse },
    { label: t('rules.faultMalformed'), value: FaultType.MalformedResponse },
    { label: t('rules.faultTimeout'), value: FaultType.Timeout },
  ];

  return (
    <div style={{ marginBottom: 16 }}>
      <Form.Item label={t('rules.faultType')} style={{ marginBottom: 12 }}>
        <Segmented
          options={options}
          value={faultType}
          onChange={(val) => onFaultTypeChange(val as FaultType)}
          size="small"
        />
      </Form.Item>

      {faultType === FaultType.RandomDelay && (
        <Space size="large">
          <Form.Item name={['faultConfig', 'minDelay']} label={t('rules.faultMinDelay')} initialValue={100}>
            <InputNumber min={0} max={60000} addonAfter="ms" style={{ width: 160 }} />
          </Form.Item>
          <Form.Item name={['faultConfig', 'maxDelay']} label={t('rules.faultMaxDelay')} initialValue={3000}>
            <InputNumber min={0} max={60000} addonAfter="ms" style={{ width: 160 }} />
          </Form.Item>
        </Space>
      )}

      {faultType === FaultType.EmptyResponse && (
        <Form.Item name={['faultConfig', 'statusCode']} label={t('rules.faultStatusCode')} initialValue={500}>
          <InputNumber min={100} max={599} style={{ width: 160 }} />
        </Form.Item>
      )}

      {faultType === FaultType.MalformedResponse && (
        <Form.Item name={['faultConfig', 'byteCount']} label={t('rules.faultByteCount')} initialValue={256}>
          <InputNumber min={1} max={10000} style={{ width: 160 }} />
        </Form.Item>
      )}

      {faultType === FaultType.Timeout && (
        <Form.Item name={['faultConfig', 'timeoutMs']} label={t('rules.faultTimeoutDuration')} initialValue={30000}>
          <InputNumber min={1000} max={120000} addonAfter="ms" style={{ width: 180 }} />
        </Form.Item>
      )}

      {faultType !== FaultType.None && faultType !== FaultType.RandomDelay && (
        <Alert
          type="warning"
          message={t('rules.faultWarning')}
          showIcon
          style={{ marginTop: 8 }}
        />
      )}
    </div>
  );
}
