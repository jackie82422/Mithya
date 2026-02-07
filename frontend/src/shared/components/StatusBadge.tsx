import { Badge } from 'antd';
import { useTranslation } from 'react-i18next';

interface StatusBadgeProps {
  active: boolean;
}

export default function StatusBadge({ active }: StatusBadgeProps) {
  const { t } = useTranslation();
  return (
    <Badge
      status={active ? 'success' : 'default'}
      text={active ? t('common.active') : t('common.inactive')}
    />
  );
}
