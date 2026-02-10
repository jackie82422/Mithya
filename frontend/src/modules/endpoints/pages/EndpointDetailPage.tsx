import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Typography,
  Button,
  Spin,
  Card,
  Divider,
  Flex,
  Breadcrumb,
  Timeline,
  Result,
  Empty,
  Switch,
  Tooltip,
} from 'antd';
import { PlusOutlined, ArrowLeftOutlined, ApiOutlined, ThunderboltOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import { useEndpoint, useToggleEndpoint } from '../hooks';
import { useServerConfig } from '@/shared/hooks/useServerConfig';
import { useServiceProxies } from '@/modules/proxy/hooks';
import { useRules, useCreateRule, useUpdateRule, useDeleteRule, useToggleRule } from '@/modules/rules/hooks';
import ProtocolTag from '@/shared/components/ProtocolTag';
import HttpMethodTag from '@/shared/components/HttpMethodTag';
import StatusBadge from '@/shared/components/StatusBadge';
import RuleCard from '@/modules/rules/components/RuleCard';
import RuleForm from '@/modules/rules/components/RuleForm';
import TryRequestDrawer from '@/shared/components/TryRequestDrawer';
import { breakableUrl } from '@/shared/utils/urlFormat';
import type { CreateRuleRequest, MockRule } from '@/shared/types';

function InfoItem({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div style={{ minWidth: 0 }}>
      <div style={{ fontSize: 12, color: 'var(--color-text-secondary)', marginBottom: 4 }}>
        {label}
      </div>
      <div style={{ fontSize: 14, color: 'var(--color-text)' }}>{children}</div>
    </div>
  );
}

export default function EndpointDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation();
  const { data: endpoint, isLoading } = useEndpoint(id!);
  const { data: rules } = useRules(id!);
  const createRule = useCreateRule(id!);
  const updateRule = useUpdateRule(id!);
  const deleteRule = useDeleteRule(id!);
  const toggleEndpoint = useToggleEndpoint();
  const toggleRule = useToggleRule(id!);
  const { data: config } = useServerConfig();
  const { data: proxies } = useServiceProxies();
  const [ruleFormOpen, setRuleFormOpen] = useState(false);
  const [editingRule, setEditingRule] = useState<MockRule | null>(null);
  const [tryDrawerOpen, setTryDrawerOpen] = useState(false);

  if (isLoading) {
    return (
      <Flex justify="center" style={{ padding: 80 }}>
        <Spin size="large" />
      </Flex>
    );
  }

  if (!endpoint) {
    return (
      <Result
        status="warning"
        title={t('endpoints.notFound')}
        subTitle={t('endpoints.notFoundDesc')}
        extra={
          <Button type="primary" onClick={() => navigate('/endpoints')}>
            {t('endpoints.backToList')}
          </Button>
        }
      />
    );
  }

  const sortedRules = [...(rules ?? [])].sort((a, b) => a.priority - b.priority);

  const handleSubmitRule = (values: CreateRuleRequest) => {
    if (editingRule) {
      updateRule.mutate(
        { ruleId: editingRule.id, data: values },
        {
          onSuccess: () => {
            setRuleFormOpen(false);
            setEditingRule(null);
          },
        },
      );
    } else {
      createRule.mutate(values, {
        onSuccess: () => {
          setRuleFormOpen(false);
        },
      });
    }
  };

  const handleEdit = (rule: MockRule) => {
    setEditingRule(rule);
    setRuleFormOpen(true);
  };

  const handleCancel = () => {
    setRuleFormOpen(false);
    setEditingRule(null);
  };

  return (
    <div>
      <Breadcrumb
        style={{ marginBottom: 16 }}
        items={[
          {
            title: (
              <a onClick={() => navigate('/endpoints')}>{t('endpoints.title')}</a>
            ),
          },
          { title: endpoint.name },
        ]}
      />

      <Flex justify="space-between" align="center" style={{ marginBottom: 20 }}>
        <Flex align="center" gap={12}>
          <Button
            type="text"
            icon={<ArrowLeftOutlined />}
            onClick={() => navigate('/endpoints')}
          />
          <Typography.Title level={2} style={{ margin: 0, fontWeight: 600, letterSpacing: '-0.5px' }}>
            {endpoint.name}
          </Typography.Title>
          <Tooltip title={endpoint.isActive ? t('common.toggleDisable') : t('common.toggleEnable')}>
            <Switch
              checked={endpoint.isActive}
              loading={toggleEndpoint.isPending}
              onChange={() => toggleEndpoint.mutate(endpoint.id)}
              size="small"
            />
          </Tooltip>
          <StatusBadge active={endpoint.isActive} />
        </Flex>
        {config?.mockServerUrl && (
          <Button
            icon={<ThunderboltOutlined />}
            onClick={() => setTryDrawerOpen(true)}
          >
            {t('tryRequest.title')}
          </Button>
        )}
      </Flex>

      <Card style={{ marginBottom: 24 }}>
        <div
          style={{
            display: 'grid',
            gridTemplateColumns: 'repeat(auto-fill, minmax(200px, 1fr))',
            gap: 24,
          }}
        >
          <InfoItem label={t('endpoints.serviceName')}>{endpoint.serviceName}</InfoItem>
          <InfoItem label={t('endpoints.protocol')}>
            <ProtocolTag protocol={endpoint.protocol} />
          </InfoItem>
          <InfoItem label={t('endpoints.httpMethod')}>
            <HttpMethodTag method={endpoint.httpMethod} />
          </InfoItem>
          <InfoItem label={t('endpoints.path')}>
            <Typography.Text code style={{ fontSize: 13 }}>
              {breakableUrl(endpoint.path)}
            </Typography.Text>
          </InfoItem>
          {config?.mockServerUrl && (
            <div style={{ gridColumn: 'span 2' }}>
              <InfoItem label={t('endpoints.mockUrl')}>
                <Flex
                  align="flex-start"
                  gap={8}
                  style={{
                    fontFamily: 'monospace',
                    fontSize: 13,
                    background: 'var(--code-bg)',
                    border: '1px solid var(--color-border)',
                    borderRadius: 6,
                    padding: '4px 8px',
                    overflowWrap: 'break-word',
                    wordBreak: 'normal',
                  }}
                >
                  <span style={{ flex: 1, minWidth: 0 }}>
                    {breakableUrl(`${config.mockServerUrl}${endpoint.path}`)}
                  </span>
                  <Typography.Text
                    copyable={{ text: `${config.mockServerUrl}${endpoint.path}` }}
                    style={{ flexShrink: 0 }}
                  />
                </Flex>
              </InfoItem>
            </div>
          )}
          <InfoItem label={t('endpoints.defaultStatusCode')}>
            {endpoint.defaultStatusCode ?? '-'}
          </InfoItem>
          <InfoItem label={t('common.createdAt')}>
            {new Date(endpoint.createdAt).toLocaleString()}
          </InfoItem>
        </div>
      </Card>

      {(() => {
        const serviceProxy = proxies?.find((p) => p.serviceName === endpoint.serviceName);
        return (
          <Card
            size="small"
            style={{ marginBottom: 24 }}
            title={
              <Flex align="center" gap={8}>
                <ApiOutlined />
                <span>{t('proxy.endpointDetail.title')}</span>
              </Flex>
            }
            extra={
              <Button type="link" size="small" onClick={() => navigate('/proxy')}>
                {t('proxy.endpointDetail.configure')}
              </Button>
            }
          >
            {!serviceProxy ? (
              <Typography.Text type="secondary">
                {t('proxy.endpointDetail.noProxy')}
              </Typography.Text>
            ) : (
              <div>
                <Typography.Text type="secondary" style={{ fontSize: 12, display: 'block', marginBottom: 4 }}>
                  {t('proxy.serviceName')}: <Typography.Text strong>{serviceProxy.serviceName}</Typography.Text>
                </Typography.Text>
                <Typography.Text type="secondary" style={{ fontSize: 12, display: 'block', marginBottom: 4 }}>
                  {t('proxy.targetBaseUrl')}: <Typography.Text code>{serviceProxy.targetBaseUrl}</Typography.Text>
                </Typography.Text>
                <Typography.Text type="secondary" style={{ fontSize: 12, display: 'block', marginBottom: 4 }}>
                  {t('proxy.fallbackEnabled')}: <StatusBadge active={serviceProxy.fallbackEnabled} />
                </Typography.Text>
                <Typography.Text type="secondary" style={{ fontSize: 12, display: 'block' }}>
                  {serviceProxy.fallbackEnabled
                    ? t('proxy.endpointDetail.fallbackEnabled')
                    : t('proxy.endpointDetail.fallbackDisabled')}
                </Typography.Text>
              </div>
            )}
          </Card>
        );
      })()}

      <Divider />

      <Flex justify="space-between" align="center" style={{ marginBottom: 16 }}>
        <Typography.Title level={4} style={{ margin: 0, fontWeight: 600 }}>
          {t('rules.title')} ({sortedRules.length})
        </Typography.Title>
        <Button
          type="primary"
          icon={<PlusOutlined />}
          onClick={() => {
            setEditingRule(null);
            setRuleFormOpen(true);
          }}
        >
          {t('rules.create')}
        </Button>
      </Flex>

      {sortedRules.length === 0 ? (
        <Empty description={t('rules.noRules')}>
          <Button type="primary" onClick={() => setRuleFormOpen(true)}>
            {t('rules.create')}
          </Button>
        </Empty>
      ) : (
        <Timeline
          items={sortedRules.map((rule) => ({
            children: (
              <RuleCard
                key={rule.id}
                rule={rule}
                endpoint={endpoint}
                onEdit={handleEdit}
                onDelete={(ruleId) => deleteRule.mutate(ruleId)}
                onToggle={(ruleId) => toggleRule.mutate(ruleId)}
                toggleLoading={toggleRule.isPending}
              />
            ),
          }))}
        />
      )}

      <RuleForm
        open={ruleFormOpen}
        onCancel={handleCancel}
        onSubmit={handleSubmitRule}
        loading={editingRule ? updateRule.isPending : createRule.isPending}
        editingRule={editingRule}
        endpointPath={endpoint.path}
        endpointMethod={endpoint.httpMethod}
      />

      {config?.mockServerUrl && (
        <TryRequestDrawer
          open={tryDrawerOpen}
          onClose={() => setTryDrawerOpen(false)}
          initialMethod={endpoint.httpMethod}
          initialUrl={`${config.mockServerUrl}${endpoint.path}`}
        />
      )}
    </div>
  );
}
