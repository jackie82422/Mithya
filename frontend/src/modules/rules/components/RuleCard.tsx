import { useState, useCallback, useRef, useEffect } from 'react';
import { Card, Typography, Button, Popconfirm, Flex, Space, Modal, message, Switch, Tooltip } from 'antd';
import { DeleteOutlined, EditOutlined, CodeOutlined, CopyOutlined, SendOutlined, DownOutlined, RightOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import type { MockRule, MockEndpoint, MatchCondition } from '@/shared/types';
import {
  FaultType,
  FaultTypeLabel,
  FieldSourceType,
  FieldSourceTypeLabel,
  MatchOperatorLabel,
  ProtocolType,
  parseMatchConditions,
} from '@/shared/types';
import StatusBadge from '@/shared/components/StatusBadge';
import { useServerConfig } from '@/shared/hooks/useServerConfig';
import { useTryRequest } from '@/shared/hooks/useTryRequest';
import { parseCurlToPayload } from '@/shared/utils/curlParser';
import ResponseViewer from '@/shared/components/ResponseViewer';

interface RuleCardProps {
  rule: MockRule;
  endpoint: MockEndpoint;
  onEdit: (rule: MockRule) => void;
  onDelete: (ruleId: string) => void;
  onToggle: (ruleId: string) => void;
  toggleLoading?: boolean;
}

const XML_TAG_RE = /^[a-zA-Z_][\w.-]*$/;

function extractXmlTag(xpath: string): string {
  // Try local-name()='TagName' first
  const localNameMatch = xpath.match(/local-name\(\)\s*=\s*['"]([^'"]+)['"]/);
  if (localNameMatch) return localNameMatch[1];

  // Try last path segment: /Envelope/Body/UserName → UserName
  const lastSegment = xpath.replace(/\[.*?\]/g, '').split('/').filter(Boolean).pop() ?? '';
  if (XML_TAG_RE.test(lastSegment)) return lastSegment;

  return 'element';
}

function buildSoapBody(conditions: MatchCondition[], endpoint: MockEndpoint): string {
  const bodyConditions = conditions.filter((c) => c.sourceType === FieldSourceType.Body);
  if (bodyConditions.length === 0) {
    return [
      '<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/">',
      '  <soapenv:Header/>',
      '  <soapenv:Body>',
      '    <!-- TODO: add request body -->',
      '  </soapenv:Body>',
      '</soapenv:Envelope>',
    ].join('\n');
  }

  const elements = bodyConditions.map((bc) => {
    const tag = extractXmlTag(bc.fieldPath);
    return `    <${tag}>${bc.value}</${tag}>`;
  });

  const action = endpoint.path.replace(/^\//, '').replace(/\//g, '.');
  return [
    '<soapenv:Envelope xmlns:soapenv="http://schemas.xmlsoap.org/soap/envelope/">',
    '  <soapenv:Header/>',
    '  <soapenv:Body>',
    `    <${action}>`,
    ...elements.map((e) => '  ' + e),
    `    </${action}>`,
    '  </soapenv:Body>',
    '</soapenv:Envelope>',
  ].join('\n');
}

function buildCurl(endpoint: MockEndpoint, conditions: MatchCondition[], baseUrl: string): string {
  const method = endpoint.httpMethod.toUpperCase();
  const isSoap = endpoint.protocol === ProtocolType.SOAP;
  let path = endpoint.path;
  const pathConditions = conditions.filter((c) => c.sourceType === FieldSourceType.Path);
  for (const pc of pathConditions) {
    path = path.replace(`{${pc.fieldPath}}`, pc.value || `{${pc.fieldPath}}`);
  }
  path = path.replace(/\{([^}]+)\}/g, '1');

  const url = `${baseUrl}${path}`;
  const parts: string[] = ['curl'];

  if (method !== 'GET') {
    parts.push(`-X ${method}`);
  }

  const queryConditions = conditions.filter((c) => c.sourceType === FieldSourceType.Query);
  let fullUrl = url;
  if (queryConditions.length > 0) {
    const qs = queryConditions.map((c) => `${c.fieldPath}=${c.value}`).join('&');
    fullUrl = `${url}?${qs}`;
  }
  parts.push(`'${fullUrl}'`);

  const headerConditions = conditions.filter((c) => c.sourceType === FieldSourceType.Header);
  const hasContentType = headerConditions.some(
    (c) => c.fieldPath.toLowerCase() === 'content-type',
  );

  for (const hc of headerConditions) {
    parts.push(`-H '${hc.fieldPath}: ${hc.value}'`);
  }

  if (isSoap) {
    if (!hasContentType) {
      parts.push("-H 'Content-Type: text/xml; charset=utf-8'");
    }
    const soapAction = headerConditions.find((c) => c.fieldPath.toLowerCase() === 'soapaction');
    if (!soapAction) {
      let actionValue = endpoint.path;
      try {
        const settings = endpoint.protocolSettings ? JSON.parse(endpoint.protocolSettings) : {};
        if (settings.soapAction) actionValue = settings.soapAction;
      } catch { /* ignore */ }
      parts.push(`-H 'SOAPAction: "${actionValue}"'`);
    }
    const soapBody = buildSoapBody(conditions, endpoint);
    parts.push(`-d '${soapBody}'`);
  } else {
    const bodyConditions = conditions.filter((c) => c.sourceType === FieldSourceType.Body);
    if (bodyConditions.length > 0) {
      if (!hasContentType) {
        parts.push("-H 'Content-Type: application/json'");
      }
      const bodyObj: Record<string, unknown> = {};
      for (const bc of bodyConditions) {
        const fieldPath = bc.fieldPath.startsWith('$.') ? bc.fieldPath.slice(2) : bc.fieldPath;
        setNestedValue(bodyObj, fieldPath, bc.value);
      }
      parts.push(`-d '${JSON.stringify(bodyObj)}'`);
    } else if (['POST', 'PUT', 'PATCH'].includes(method)) {
      if (!hasContentType) {
        parts.push("-H 'Content-Type: application/json'");
      }
      parts.push("-d '{}'");
    }
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

function formatJson(raw: string | null | undefined): string {
  if (!raw) return '';
  try {
    return JSON.stringify(JSON.parse(raw), null, 2);
  } catch {
    return raw;
  }
}

export default function RuleCard({ rule, endpoint, onEdit, onDelete, onToggle, toggleLoading }: RuleCardProps) {
  const { t } = useTranslation();
  const { data: serverConfig } = useServerConfig();
  const conditions = parseMatchConditions(rule.matchConditions);
  const [curlOpen, setCurlOpen] = useState(false);
  const [expanded, setExpanded] = useState(false);
  const tryRequest = useTryRequest();

  const mockBaseUrl = serverConfig?.mithyaUrl ?? window.location.origin;
  const curlCmd = buildCurl(endpoint, conditions, mockBaseUrl);

  const handleCopy = () => {
    navigator.clipboard.writeText(curlCmd);
    message.success(t('rules.copiedCurl'));
  };

  const handleSend = useCallback(() => {
    const payload = parseCurlToPayload(curlCmd);
    tryRequest.mutate(payload);
  }, [curlCmd, tryRequest]);

  const handleModalClose = () => {
    setCurlOpen(false);
    tryRequest.reset();
  };

  const handleCurlKeyDown = useCallback(
    (e: React.KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key === 'Enter') {
        e.preventDefault();
        handleSend();
      }
    },
    [handleSend],
  );

  const curlContainerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (curlOpen) {
      setTimeout(() => curlContainerRef.current?.focus(), 100);
    }
  }, [curlOpen]);

  const responseHeaders = rule.responseHeaders ? formatJson(rule.responseHeaders) : null;

  return (
    <>
      <Card size="small" style={{ marginBottom: 12, opacity: rule.isActive ? 1 : 0.55, transition: 'opacity 0.2s ease' }}>
        <Flex justify="space-between" align="flex-start">
          <div
            style={{ flex: 1, cursor: 'pointer' }}
            onClick={() => setExpanded((v) => !v)}
          >
            <Flex align="center" gap={8} style={{ marginBottom: 8 }}>
              {expanded
                ? <DownOutlined style={{ fontSize: 10, color: 'var(--color-text-secondary)' }} />
                : <RightOutlined style={{ fontSize: 10, color: 'var(--color-text-secondary)' }} />
              }
              <span
                style={{
                  padding: '2px 10px',
                  borderRadius: 100,
                  fontSize: 12,
                  fontWeight: 600,
                  background: 'var(--post-bg)',
                  color: 'var(--post-color)',
                }}
              >
                #{rule.priority}
              </span>
              <Typography.Text strong>{rule.ruleName}</Typography.Text>
              <StatusBadge active={rule.isActive} />
              {rule.isTemplate && (
                <span
                  className="pill-tag"
                  style={{
                    padding: '2px 8px',
                    borderRadius: 100,
                    fontSize: 11,
                    fontWeight: 500,
                    background: 'var(--rest-bg, var(--get-bg))',
                    color: 'var(--rest-color, var(--get-color))',
                  }}
                >
                  {t('rules.template')}
                </span>
              )}
              {rule.faultType !== undefined && rule.faultType !== FaultType.None && (
                <span
                  className="pill-tag"
                  style={{
                    padding: '2px 8px',
                    borderRadius: 100,
                    fontSize: 11,
                    fontWeight: 500,
                    background: 'var(--delete-bg)',
                    color: 'var(--delete-color)',
                  }}
                >
                  {FaultTypeLabel[rule.faultType]}
                </span>
              )}
              {rule.logicMode === 1 && (
                <span
                  className="pill-tag"
                  style={{
                    padding: '2px 8px',
                    borderRadius: 100,
                    fontSize: 11,
                    fontWeight: 500,
                    background: 'var(--put-bg)',
                    color: 'var(--put-color)',
                  }}
                >
                  OR
                </span>
              )}
            </Flex>
            <Space size={[4, 4]} wrap style={{ marginBottom: 4 }}>
              {conditions.map((c, i) => (
                <span
                  key={i}
                  style={{
                    display: 'inline-block',
                    padding: '2px 8px',
                    borderRadius: 6,
                    fontSize: 12,
                    background: 'var(--condition-bg)',
                    color: 'var(--color-text-secondary)',
                    border: '1px solid var(--color-border)',
                  }}
                >
                  {FieldSourceTypeLabel[c.sourceType]}.{c.fieldPath}{' '}
                  {MatchOperatorLabel[c.operator]} {c.value && `"${c.value}"`}
                </span>
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
            <Tooltip title={rule.isActive ? t('common.toggleDisable') : t('common.toggleEnable')}>
              <Switch
                size="small"
                checked={rule.isActive}
                loading={toggleLoading}
                onChange={() => onToggle(rule.id)}
              />
            </Tooltip>
            <Tooltip title="cURL">
              <Button
                size="small"
                type="text"
                icon={<CodeOutlined />}
                onClick={() => setCurlOpen(true)}
              />
            </Tooltip>
            <Tooltip title={t('common.edit')}>
              <Button size="small" type="text" icon={<EditOutlined />} onClick={() => onEdit(rule)} />
            </Tooltip>
            <Popconfirm
              title={t('rules.deleteConfirm', { name: rule.ruleName })}
              onConfirm={() => onDelete(rule.id)}
              okText={t('common.yes')}
              cancelText={t('common.no')}
            >
              <Tooltip title={t('common.delete')}>
                <Button size="small" type="text" danger icon={<DeleteOutlined />} />
              </Tooltip>
            </Popconfirm>
          </Space>
        </Flex>

        {expanded && (
          <div style={{ marginTop: 12, borderTop: '1px solid var(--color-border)', paddingTop: 12 }}>
            <Typography.Text strong style={{ fontSize: 12 }}>
              {t('rules.responseBody')}
            </Typography.Text>
            <pre
              style={{
                marginTop: 4,
                padding: 12,
                borderRadius: 8,
                fontSize: 12,
                lineHeight: 1.5,
                background: 'var(--code-bg)',
                color: 'var(--color-text)',
                border: '1px solid var(--color-border)',
                overflowX: 'auto',
                whiteSpace: 'pre-wrap',
                wordBreak: 'break-all',
                maxHeight: 300,
              }}
            >
              {formatJson(rule.responseBody)}
            </pre>

            {responseHeaders && (
              <>
                <Typography.Text strong style={{ fontSize: 12, marginTop: 8, display: 'block' }}>
                  {t('rules.responseHeaders')}
                </Typography.Text>
                <pre
                  style={{
                    marginTop: 4,
                    padding: 12,
                    borderRadius: 8,
                    fontSize: 12,
                    lineHeight: 1.5,
                    background: 'var(--code-bg)',
                    color: 'var(--color-text)',
                    border: '1px solid var(--color-border)',
                    overflowX: 'auto',
                    whiteSpace: 'pre-wrap',
                    wordBreak: 'break-all',
                  }}
                >
                  {responseHeaders}
                </pre>
              </>
            )}
          </div>
        )}
      </Card>

      <Modal
        title="cURL"
        open={curlOpen}
        onCancel={handleModalClose}
        footer={
          <Flex justify="space-between">
            <Button
              icon={<SendOutlined />}
              type="primary"
              onClick={handleSend}
              loading={tryRequest.isPending}
            >
              {t('tryRequest.send')}
            </Button>
            <Button icon={<CopyOutlined />} onClick={handleCopy}>
              {t('rules.copyCurl')}
            </Button>
          </Flex>
        }
        width={700}
      >
        <div tabIndex={-1} ref={curlContainerRef} onKeyDown={handleCurlKeyDown} style={{ outline: 'none' }}>
          <pre
            style={{
              background: '#1a1a2e',
              color: '#e0e0e0',
              padding: 20,
              borderRadius: 14,
              fontSize: 13,
              lineHeight: 1.6,
              overflowX: 'auto',
              whiteSpace: 'pre-wrap',
              wordBreak: 'break-all',
            }}
          >
            {curlCmd}
          </pre>
          <ResponseViewer
            response={tryRequest.data}
            loading={tryRequest.isPending}
            error={tryRequest.error}
          />
        </div>
      </Modal>
    </>
  );
}
