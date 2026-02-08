import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Typography, Button, Flex, Empty } from 'antd';
import { PlusOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import type { CreateScenarioRequest, Scenario } from '@/shared/types';
import {
  useScenarios,
  useCreateScenario,
  useDeleteScenario,
  useToggleScenario,
  useResetScenario,
} from '../hooks';
import ScenarioCard from '../components/ScenarioCard';
import ScenarioForm from '../components/ScenarioForm';

export default function ScenarioListPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { data: scenarios, isLoading } = useScenarios();
  const createScenario = useCreateScenario();
  const deleteScenario = useDeleteScenario();
  const toggleScenario = useToggleScenario();
  const resetScenario = useResetScenario();
  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<Scenario | null>(null);

  const handleSubmit = (values: CreateScenarioRequest) => {
    createScenario.mutate(values, {
      onSuccess: () => setFormOpen(false),
    });
  };

  const handleEdit = (scenario: Scenario) => {
    setEditing(scenario);
    setFormOpen(true);
  };

  const handleCancel = () => {
    setFormOpen(false);
    setEditing(null);
  };

  return (
    <div>
      <Flex justify="space-between" align="center" style={{ marginBottom: 8 }}>
        <div>
          <Typography.Title level={2} style={{ margin: 0, fontWeight: 600, letterSpacing: '-0.5px' }}>
            {t('scenarios.title')}
          </Typography.Title>
          <Typography.Text type="secondary">{t('scenarios.subtitle')}</Typography.Text>
        </div>
        <Button
          type="primary"
          icon={<PlusOutlined />}
          onClick={() => {
            setEditing(null);
            setFormOpen(true);
          }}
        >
          {t('common.create')}
        </Button>
      </Flex>

      <div style={{ marginTop: 24 }}>
        {!isLoading && (!scenarios || scenarios.length === 0) ? (
          <Empty description={t('scenarios.noScenarios')}>
            <Button type="primary" onClick={() => setFormOpen(true)}>
              {t('common.create')}
            </Button>
          </Empty>
        ) : (
          scenarios?.map((scenario) => (
            <ScenarioCard
              key={scenario.id}
              scenario={scenario}
              onView={(s) => navigate(`/scenarios/${s.id}`)}
              onEdit={handleEdit}
              onDelete={(id) => deleteScenario.mutate(id)}
              onToggle={(id) => toggleScenario.mutate(id)}
              onReset={(id) => resetScenario.mutate(id)}
              toggleLoading={toggleScenario.isPending}
            />
          ))
        )}
      </div>

      <ScenarioForm
        open={formOpen}
        onCancel={handleCancel}
        onSubmit={handleSubmit}
        loading={createScenario.isPending}
        editing={editing}
      />
    </div>
  );
}
