import { useQuery } from '@tanstack/react-query';
import { logsApi } from './api';

export function useLogs(limit = 100) {
  return useQuery({
    queryKey: ['logs', limit],
    queryFn: () => logsApi.getAll(limit),
  });
}

export function useLogsByEndpoint(endpointId: string, limit = 100) {
  return useQuery({
    queryKey: ['logs', 'endpoint', endpointId, limit],
    queryFn: () => logsApi.getByEndpoint(endpointId, limit),
    enabled: !!endpointId,
  });
}
