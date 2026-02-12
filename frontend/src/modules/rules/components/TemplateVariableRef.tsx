import { Collapse, Typography } from 'antd';
import { useTranslation } from 'react-i18next';

const { Text } = Typography;

const codeStyle: React.CSSProperties = {
  padding: '1px 6px',
  borderRadius: 4,
  fontSize: 12,
  fontFamily: 'monospace',
  background: 'var(--code-bg)',
  border: '1px solid var(--color-border)',
};

interface VarItem {
  code: string;
  labelKey: string;
}

const requestVars: VarItem[] = [
  { code: '{{request.method}}', labelKey: 'rules.templateVarMethod' },
  { code: '{{request.path}}', labelKey: 'rules.templateVarPath' },
  { code: '{{request.body}}', labelKey: 'rules.templateVarBody' },
  { code: '{{request.headers.X}}', labelKey: 'rules.templateVarHeaders' },
  { code: '{{request.query.page}}', labelKey: 'rules.templateVarQuery' },
  { code: '{{request.pathParams.id}}', labelKey: 'rules.templateVarPathParams' },
];

const helperVars: VarItem[] = [
  { code: '{{now "yyyy-MM-dd"}}', labelKey: 'rules.templateHelperNow' },
  { code: '{{uuid}}', labelKey: 'rules.templateHelperUuid' },
  { code: '{{randomInt 1 100}}', labelKey: 'rules.templateHelperRandomInt' },
  { code: '{{jsonPath request.body "$.user.name"}}', labelKey: 'rules.templateHelperJsonPath' },
  { code: '{{math 5 "+" 3}}', labelKey: 'rules.templateHelperMath' },
];

function VarRow({ code, label }: { code: string; label: string }) {
  return (
    <div style={{ display: 'flex', gap: 12, alignItems: 'center', padding: '3px 0' }}>
      <code style={codeStyle}>{code}</code>
      <Text type="secondary" style={{ fontSize: 12 }}>{label}</Text>
    </div>
  );
}

export default function TemplateVariableRef() {
  const { t } = useTranslation();

  return (
    <Collapse
      size="small"
      style={{ marginBottom: 12 }}
      items={[
        {
          key: 'vars',
          label: t('rules.templateVariables'),
          children: (
            <div>
              <Text strong style={{ fontSize: 12, display: 'block', marginBottom: 4 }}>
                Request Context:
              </Text>
              {requestVars.map((v) => (
                <VarRow key={v.code} code={v.code} label={t(v.labelKey)} />
              ))}
              <Text strong style={{ fontSize: 12, display: 'block', marginTop: 12, marginBottom: 4 }}>
                Helpers:
              </Text>
              {helperVars.map((v) => (
                <VarRow key={v.code} code={v.code} label={t(v.labelKey)} />
              ))}
              <Text strong style={{ fontSize: 12, display: 'block', marginTop: 12, marginBottom: 4 }}>
                Conditionals:
              </Text>
              <VarRow code={'{{#if (eq request.method "POST")}}...{{/if}}'} label="Conditional block" />
              <VarRow code={'{{#each items}}{{this}}{{/each}}'} label="Loop iteration" />
            </div>
          ),
        },
      ]}
    />
  );
}
