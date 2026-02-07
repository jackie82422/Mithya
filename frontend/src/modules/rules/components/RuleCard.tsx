import { useState } from 'react';
import { Card, Typography, Tag, Button, Popconfirm, Flex, Space, Modal, message } from 'antd';
import { DeleteOutlined, EditOutlined, CodeOutlined, CopyOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import type { MockRule, MockEndpoint, MatchCondition } from '@/shared/types';
import {
  FieldSourceType,
  FieldSourceTypeLabel,
  MatchOperatorLabel,
  parseMatchConditions,
} from '@/shared/types';
import StatusBadge from '@/shared/components/StatusBadge';

interface RuleCardProps {
  rule: MockRule;
  endpoint: MockEndpoint;
  onEdit: (rule: MockRule) => void;
  onDelete: (ruleId: string) => void;
}

function buildCurl(endpoint: MockEndpoint, conditions: MatchCondition[]): string {
  const method = endpoint.httpMethod.toUpperCase();
  // Build path: replace {param} with sample values from Path conditions
  let path = endpoint.path;
  const pathConditions = conditions.filter((c) => c.sourceType === FieldSourceType.Path);
  for (const pc of pathConditions) {
    path = path.replace(`{${pc.fieldPath}}`, pc.value || `{${pc.fieldPath}}`);
  }
  // Fill remaining {param} with placeholder
  path = path.replace(/\{([^}]+)\}/g, '1');

  const url = `http://localhost:5001${path}`;

  const parts: string[] = ['curl'];

  if (method !== 'GET') {
    parts.push(`-X ${method}`);
  }

  // Query params
  const queryConditions = conditions.filter((c) => c.sourceType === FieldSourceType.Query);
  let fullUrl = url;
  if (queryConditions.length > 0) {
    const qs = queryConditions.map((c) => `${c.fieldPath}=${c.value}`).join('&');
    fullUrl = `${url}?${qs}`;
  }
  parts.push(`'${fullUrl}'`);

  // Headers
  const headerConditions = conditions.filter((c) => c.sourceType === FieldSourceType.Header);
  const hasContentType = headerConditions.some(
    (c) => c.fieldPath.toLowerCase() === 'content-type',
  );

  for (const hc of headerConditions) {
    parts.push(`-H '${hc.fieldPath}: ${hc.value}'`);
  }

  // Body
  const bodyConditions = conditions.filter((c) => c.sourceType === FieldSourceType.Body);
  if (bodyConditions.length > 0) {
    if (!hasContentType) {
      parts.push("-H 'Content-Type: application/json'");
    }
    // Build a JSON body from body conditions
    const bodyObj: Record<string, unknown> = {};
    for (const bc of bodyConditions) {
      const fieldPath = bc.fieldPath.startsWith('$.') ? bc.fieldPath.slice(2) : bc.fieldPath;
      setNestedValue(bodyObj, fieldPath, bc.value);
    }
    parts.push(`-d '${JSON.stringify(bodyObj)}'`);
  }

  return parts.join(' \\\n  ');
}

function setNestedValue(obj: Record<string, unknown>, path: string, value: string) {
  const keys = path.split('.');
  let current = obj;
  for (let i = 0; i < keys.length - 1; i++) {
    if (!(keys[i] in current) || typeof current[keys[i]] !== 'object') {
      current[keys[i]] = {};
    }
    current = current[keys[i]] as Record<string, unknown>;
  }
  current[keys[keys.length - 1]] = value;
}

export default function RuleCard({ rule, endpoint, onEdit, onDelete }: RuleCardProps) {
  const { t } = useTranslation();
  const conditions = parseMatchConditions(rule.matchConditions);
  const [curlOpen, setCurlOpen] = useState(false);

  const curlCmd = buildCurl(endpoint, conditions);

  const handleCopy = () => {
    navigator.clipboard.writeText(curlCmd);
    message.success('Copied!');
  };

  return (
    <>
      <Card size="small" style={{ marginBottom: 12 }}>
        <Flex justify="space-between" align="flex-start">
          <div style={{ flex: 1 }}>
            <Flex align="center" gap={8} style={{ marginBottom: 8 }}>
              <Tag color="geekblue">#{rule.priority}</Tag>
              <Typography.Text strong>{rule.ruleName}</Typography.Text>
              <StatusBadge active={rule.isActive} />
            </Flex>
            <Space size={[4, 4]} wrap style={{ marginBottom: 4 }}>
              {conditions.map((c, i) => (
                <Tag key={i}>
                  {FieldSourceTypeLabel[c.sourceType]}.{c.fieldPath}{' '}
                  {MatchOperatorLabel[c.operator]} {c.value && `"${c.value}"`}
                </Tag>
              ))}
              {conditions.length === 0 && (
                <Typography.Text type="secondary">({t('common.noData')})</Typography.Text>
              )}
            </Space>
            <div>
              <Typography.Text type="secondary">
                {t('rules.statusCode')}: {rule.responseStatusCode}
                {rule.delayMs > 0 && ` | ${t('rules.delayMs')}: ${rule.delayMs}ms`}
              </Typography.Text>
            </div>
          </div>
          <Space>
            <Button
              size="small"
              icon={<CodeOutlined />}
              onClick={() => setCurlOpen(true)}
              title="cURL"
            />
            <Button size="small" icon={<EditOutlined />} onClick={() => onEdit(rule)} />
            <Popconfirm
              title={t('rules.deleteConfirm', { name: rule.ruleName })}
              onConfirm={() => onDelete(rule.id)}
              okText={t('common.yes')}
              cancelText={t('common.no')}
            >
              <Button size="small" danger icon={<DeleteOutlined />} />
            </Popconfirm>
          </Space>
        </Flex>
      </Card>

      <Modal
        title="cURL"
        open={curlOpen}
        onCancel={() => setCurlOpen(false)}
        footer={
          <Button icon={<CopyOutlined />} type="primary" onClick={handleCopy}>
            Copy
          </Button>
        }
        width={700}
      >
        <pre
          style={{
            background: '#1e1e1e',
            color: '#d4d4d4',
            padding: 16,
            borderRadius: 8,
            fontSize: 13,
            lineHeight: 1.6,
            overflowX: 'auto',
            whiteSpace: 'pre-wrap',
            wordBreak: 'break-all',
          }}
        >
          {curlCmd}
        </pre>
      </Modal>
    </>
  );
}
