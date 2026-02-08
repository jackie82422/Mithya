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
  Result,
  Empty,
  Switch,
  Tooltip,
  Popconfirm,
  Space,
} from 'antd';
import { PlusOutlined, ArrowLeftOutlined, UndoOutlined, DeleteOutlined, EditOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';
import type { CreateStepRequest, ScenarioStep } from '@/shared/types';
import { useEndpoints } from '@/modules/endpoints/hooks';
import {
  useScenario,
  useToggleScenario,
  useResetScenario,
  useAddStep,
  useUpdateStep,
  useDeleteStep,
} from '../hooks';
import StatusBadge from '@/shared/components/StatusBadge';
import StateFlowDiagram from '../components/StateFlowDiagram';
import StepForm from '../components/StepForm';

export default function ScenarioDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation();
  const { data: scenario, isLoading } = useScenario(id!);
  const { data: endpoints } = useEndpoints();
  const toggleScenario = useToggleScenario();
  const resetScenario = useResetScenario();
  const addStep = useAddStep(id!);
  const updateStep = useUpdateStep(id!);
  const deleteStep = useDeleteStep(id!);
  const [stepFormOpen, setStepFormOpen] = useState(false);
  const [editingStep, setEditingStep] = useState<ScenarioStep | null>(null);

  if (isLoading) {
    return (
      <Flex justify="center" style={{ padding: 80 }}>
        <Spin size="large" />
      </Flex>
    );
  }

  if (!scenario) {
    return (
      <Result
        status="warning"
        title={t('scenarios.noScenarios')}
        extra={
          <Button type="primary" onClick={() => navigate('/scenarios')}>
            {t('scenarios.title')}
          </Button>
        }
      />
    );
  }

  const steps = scenario.steps ?? [];
  const existingStates = [...new Set(steps.flatMap((s) => [s.stateName, s.nextState].filter(Boolean) as string[]))];
  if (!existingStates.includes(scenario.initialState)) {
    existingStates.unshift(scenario.initialState);
  }

  const stepsByState = steps.reduce<Record<string, ScenarioStep[]>>((acc, step) => {
    if (!acc[step.stateName]) acc[step.stateName] = [];
    acc[step.stateName].push(step);
    return acc;
  }, {});

  const handleSubmitStep = (values: CreateStepRequest) => {
    if (editingStep) {
      updateStep.mutate(
        { stepId: editingStep.id, data: values },
        { onSuccess: () => { setStepFormOpen(false); setEditingStep(null); } },
      );
    } else {
      addStep.mutate(values, {
        onSuccess: () => setStepFormOpen(false),
      });
    }
  };

  const getEndpointLabel = (endpointId: string) => {
    const ep = endpoints?.find((e) => e.id === endpointId);
    return ep ? `${ep.httpMethod} ${ep.path}` : endpointId;
  };

  return (
    <div>
      <Breadcrumb
        style={{ marginBottom: 16 }}
        items={[
          { title: <a onClick={() => navigate('/scenarios')}>{t('scenarios.title')}</a> },
          { title: scenario.name },
        ]}
      />

      <Flex justify="space-between" align="center" style={{ marginBottom: 20 }}>
        <Flex align="center" gap={12}>
          <Button type="text" icon={<ArrowLeftOutlined />} onClick={() => navigate('/scenarios')} />
          <Typography.Title level={2} style={{ margin: 0, fontWeight: 600, letterSpacing: '-0.5px' }}>
            {scenario.name}
          </Typography.Title>
          <Tooltip title={scenario.isActive ? t('common.toggleDisable') : t('common.toggleEnable')}>
            <Switch
              checked={scenario.isActive}
              loading={toggleScenario.isPending}
              onChange={() => toggleScenario.mutate(scenario.id)}
              size="small"
            />
          </Tooltip>
          <StatusBadge active={scenario.isActive} />
        </Flex>
        <Popconfirm
          title={t('scenarios.resetConfirm')}
          onConfirm={() => resetScenario.mutate(scenario.id)}
          okText={t('common.yes')}
          cancelText={t('common.no')}
        >
          <Button icon={<UndoOutlined />}>{t('scenarios.resetState')}</Button>
        </Popconfirm>
      </Flex>

      <Card style={{ marginBottom: 24 }}>
        <Flex gap={24} wrap="wrap">
          <div>
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>{t('scenarios.currentState')}</Typography.Text>
            <div>
              <span style={{
                padding: '2px 10px',
                borderRadius: 100,
                fontSize: 13,
                fontWeight: 600,
                background: 'var(--active-bg)',
                color: 'var(--active-color)',
              }}>
                ● {scenario.currentState}
              </span>
            </div>
          </div>
          <div>
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>{t('scenarios.initialState')}</Typography.Text>
            <div><Typography.Text>{scenario.initialState}</Typography.Text></div>
          </div>
        </Flex>
      </Card>

      <Typography.Title level={4} style={{ fontWeight: 600 }}>
        {t('scenarios.stateFlow')}
      </Typography.Title>
      <StateFlowDiagram
        steps={steps}
        initialState={scenario.initialState}
        currentState={scenario.currentState}
      />

      <Divider />

      <Flex justify="space-between" align="center" style={{ marginBottom: 16 }}>
        <Typography.Title level={4} style={{ margin: 0, fontWeight: 600 }}>
          {t('scenarios.steps')} ({steps.length})
        </Typography.Title>
        <Button
          type="primary"
          icon={<PlusOutlined />}
          onClick={() => {
            setEditingStep(null);
            setStepFormOpen(true);
          }}
        >
          {t('scenarios.addStep')}
        </Button>
      </Flex>

      {steps.length === 0 ? (
        <Empty description={t('scenarios.noScenarios')}>
          <Button type="primary" onClick={() => setStepFormOpen(true)}>
            {t('scenarios.addStep')}
          </Button>
        </Empty>
      ) : (
        Object.entries(stepsByState).map(([stateName, stateSteps]) => (
          <div key={stateName} style={{ marginBottom: 16 }}>
            <Typography.Text strong style={{ fontSize: 13, display: 'block', marginBottom: 8 }}>
              {t('scenarios.stateName')}: {stateName}
            </Typography.Text>
            {stateSteps
              .sort((a, b) => a.priority - b.priority)
              .map((step, idx) => (
                <Card key={step.id} size="small" style={{ marginBottom: 8, background: 'var(--condition-bg)' }}>
                  <Flex justify="space-between" align="flex-start">
                    <div>
                      <Typography.Text strong>#{idx + 1} {getEndpointLabel(step.endpointId)}</Typography.Text>
                      <div style={{ marginTop: 4 }}>
                        <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                          → {step.responseStatusCode}
                          {step.nextState ? ` → ${t('scenarios.nextState')}: ${step.nextState}` : ` → Stay: ${step.stateName}`}
                        </Typography.Text>
                      </div>
                    </div>
                    <Space>
                      <Tooltip title={t('common.edit')}>
                        <Button
                          size="small"
                          type="text"
                          icon={<EditOutlined />}
                          onClick={() => {
                            setEditingStep(step);
                            setStepFormOpen(true);
                          }}
                        />
                      </Tooltip>
                      <Popconfirm
                        title={t('scenarios.deleteStepConfirm')}
                        onConfirm={() => deleteStep.mutate(step.id)}
                        okText={t('common.yes')}
                        cancelText={t('common.no')}
                      >
                        <Tooltip title={t('common.delete')}>
                          <Button size="small" type="text" danger icon={<DeleteOutlined />} />
                        </Tooltip>
                      </Popconfirm>
                    </Space>
                  </Flex>
                </Card>
              ))}
          </div>
        ))
      )}

      <StepForm
        open={stepFormOpen}
        onCancel={() => { setStepFormOpen(false); setEditingStep(null); }}
        onSubmit={handleSubmitStep}
        loading={editingStep ? updateStep.isPending : addStep.isPending}
        editing={editingStep}
        endpoints={endpoints}
        existingStates={existingStates}
      />
    </div>
  );
}
