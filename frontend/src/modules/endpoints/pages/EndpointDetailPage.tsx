import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Typography,
  Button,
  Spin,
  Card,
  Divider,
  Empty,
  Flex,
  Breadcrumb,
  Timeline,
} from 'antd';
import { PlusOutlined, ArrowLeftOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import { useEndpoint } from '../hooks';
import { useRules, useCreateRule, useUpdateRule, useDeleteRule } from '@/modules/rules/hooks';
import ProtocolTag from '@/shared/components/ProtocolTag';
import HttpMethodTag from '@/shared/components/HttpMethodTag';
import StatusBadge from '@/shared/components/StatusBadge';
import RuleCard from '@/modules/rules/components/RuleCard';
import RuleForm from '@/modules/rules/components/RuleForm';
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
  const [ruleFormOpen, setRuleFormOpen] = useState(false);
  const [editingRule, setEditingRule] = useState<MockRule | null>(null);

  if (isLoading) {
    return (
      <Flex justify="center" style={{ padding: 80 }}>
        <Spin size="large" />
      </Flex>
    );
  }

  if (!endpoint) {
    return <Empty />;
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
          <StatusBadge active={endpoint.isActive} />
        </Flex>
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
            <Typography.Text code>{endpoint.path}</Typography.Text>
          </InfoItem>
          <InfoItem label={t('endpoints.defaultStatusCode')}>
            {endpoint.defaultStatusCode ?? '-'}
          </InfoItem>
          <InfoItem label={t('common.createdAt')}>
            {new Date(endpoint.createdAt).toLocaleString()}
          </InfoItem>
        </div>
      </Card>

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
      />
    </div>
  );
}
