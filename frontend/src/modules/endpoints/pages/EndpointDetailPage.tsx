import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import {
  Typography,
  Button,
  Spin,
  Descriptions,
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

      <Flex justify="space-between" align="center" style={{ marginBottom: 16 }}>
        <Flex align="center" gap={12}>
          <Button icon={<ArrowLeftOutlined />} onClick={() => navigate('/endpoints')} />
          <Typography.Title level={3} style={{ margin: 0 }}>
            {endpoint.name}
          </Typography.Title>
          <StatusBadge active={endpoint.isActive} />
        </Flex>
      </Flex>

      <Descriptions bordered column={2} size="small" style={{ marginBottom: 24 }}>
        <Descriptions.Item label={t('endpoints.serviceName')}>
          {endpoint.serviceName}
        </Descriptions.Item>
        <Descriptions.Item label={t('endpoints.protocol')}>
          <ProtocolTag protocol={endpoint.protocol} />
        </Descriptions.Item>
        <Descriptions.Item label={t('endpoints.httpMethod')}>
          <HttpMethodTag method={endpoint.httpMethod} />
        </Descriptions.Item>
        <Descriptions.Item label={t('endpoints.path')}>
          <Typography.Text code>{endpoint.path}</Typography.Text>
        </Descriptions.Item>
        <Descriptions.Item label={t('endpoints.defaultStatusCode')}>
          {endpoint.defaultStatusCode ?? '-'}
        </Descriptions.Item>
        <Descriptions.Item label={t('common.createdAt')}>
          {new Date(endpoint.createdAt).toLocaleString()}
        </Descriptions.Item>
      </Descriptions>

      <Divider />

      <Flex justify="space-between" align="center" style={{ marginBottom: 16 }}>
        <Typography.Title level={4} style={{ margin: 0 }}>
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
