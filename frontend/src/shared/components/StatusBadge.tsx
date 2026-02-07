import { useTranslation } from 'react-i18next';

interface StatusBadgeProps {
  active: boolean;
}

export default function StatusBadge({ active }: StatusBadgeProps) {
  const { t } = useTranslation();
  return (
    <span
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        gap: 6,
        padding: '2px 10px',
        borderRadius: 100,
        fontSize: 12,
        fontWeight: 500,
        background: active ? 'var(--active-bg)' : 'var(--inactive-bg)',
        color: active ? 'var(--active-color)' : 'var(--inactive-color)',
      }}
    >
      <span
        style={{
          width: 6,
          height: 6,
          borderRadius: '50%',
          background: active ? 'var(--active-color)' : 'var(--inactive-color)',
        }}
      />
      {active ? t('common.active') : t('common.inactive')}
    </span>
  );
}
