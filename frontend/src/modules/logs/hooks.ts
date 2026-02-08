import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { message } from 'antd';
import { useTranslation } from 'react-i18next';
import { logsApi } from './api';
import { getApiErrorMessage } from '@/shared/utils/errorUtils';

export function useLogs(limit = 100, refetchInterval?: number | false) {
  return useQuery({
    queryKey: ['logs', limit],
    queryFn: () => logsApi.getAll(limit),
    refetchInterval: refetchInterval || false,
  });
}

export function useLogsByEndpoint(endpointId: string, limit = 100) {
  return useQuery({
    queryKey: ['logs', 'endpoint', endpointId, limit],
    queryFn: () => logsApi.getByEndpoint(endpointId, limit),
    enabled: !!endpointId,
  });
}

export function useClearLogs() {
  const qc = useQueryClient();
  const { t } = useTranslation();
  return useMutation({
    mutationFn: () => logsApi.clearAll(),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['logs'] });
      message.success(t('common.success'));
    },
    onError: (err) => message.error(getApiErrorMessage(err, t('common.error'))),
  });
}
