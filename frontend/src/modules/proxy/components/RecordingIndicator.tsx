import { useTranslation } from 'react-i18next';
import { useServiceProxies } from '../hooks';

export default function RecordingIndicator() {
  const { t } = useTranslation();
  const { data: proxies } = useServiceProxies();
  const isRecording = proxies?.some((p) => p.isActive && p.isRecording);

  if (!isRecording) return null;

  return (
    <span
      style={{
        display: 'inline-flex',
        alignItems: 'center',
        gap: 6,
        color: 'var(--delete-color)',
        fontSize: 12,
        fontWeight: 500,
      }}
    >
      <span
        style={{
          width: 8,
          height: 8,
          borderRadius: '50%',
          background: 'var(--delete-color)',
          animation: 'pulse 1.5s infinite',
        }}
      />
      {t('proxy.recording')}
    </span>
  );
}
