import { useMemo } from 'react';
import { Typography } from 'antd';
import type { ScenarioStep } from '@/shared/types';

interface StateFlowDiagramProps {
  steps: ScenarioStep[];
  initialState: string;
  currentState: string;
}

export default function StateFlowDiagram({ steps, initialState, currentState }: StateFlowDiagramProps) {
  const { states, transitions } = useMemo(() => {
    const stateSet = new Set<string>();
    stateSet.add(initialState);
    const transSet = new Map<string, string[]>();

    steps.forEach((step) => {
      stateSet.add(step.stateName);
      if (step.nextState) {
        stateSet.add(step.nextState);
        const key = `${step.stateName}→${step.nextState}`;
        if (!transSet.has(key)) {
          transSet.set(key, []);
        }
      }
    });

    return {
      states: [...stateSet],
      transitions: [...transSet.keys()].map((k) => {
        const [from, to] = k.split('→');
        return { from, to };
      }),
    };
  }, [steps, initialState]);

  return (
    <div
      style={{
        padding: 16,
        borderRadius: 12,
        background: 'var(--code-bg)',
        border: '1px solid var(--color-border)',
        overflowX: 'auto',
      }}
    >
      <div style={{ display: 'flex', gap: 12, flexWrap: 'wrap', alignItems: 'center' }}>
        <span style={{ fontSize: 12, color: 'var(--color-text-secondary)' }}>[*]</span>
        <span style={{ color: 'var(--color-text-secondary)' }}>→</span>
        {states.map((state) => (
          <span
            key={state}
            style={{
              display: 'inline-block',
              padding: '4px 12px',
              borderRadius: 8,
              fontSize: 13,
              fontWeight: state === currentState ? 600 : 400,
              background: state === currentState ? 'var(--active-bg)' : 'var(--condition-bg)',
              color: state === currentState ? 'var(--active-color)' : 'var(--color-text)',
              border: `2px solid ${state === currentState ? 'var(--active-color)' : 'var(--color-border)'}`,
            }}
          >
            {state === currentState ? `● ${state}` : state}
          </span>
        ))}
      </div>
      {transitions.length > 0 && (
        <div style={{ marginTop: 12, display: 'flex', flexWrap: 'wrap', gap: 8 }}>
          {transitions.map(({ from, to }) => (
            <Typography.Text
              key={`${from}-${to}`}
              type="secondary"
              style={{ fontSize: 12 }}
            >
              {from} → {to}
            </Typography.Text>
          ))}
        </div>
      )}
    </div>
  );
}
