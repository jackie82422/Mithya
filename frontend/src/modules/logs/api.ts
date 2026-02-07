import apiClient from '@/shared/api/client';
import type { MockRequestLog } from '@/shared/types';

export const logsApi = {
  getAll: (limit = 100) =>
    apiClient.get<MockRequestLog[]>('/logs', { params: { limit } }).then((r) => r.data),

  getByEndpoint: (endpointId: string, limit = 100) =>
    apiClient
      .get<MockRequestLog[]>(`/logs/endpoint/${endpointId}`, { params: { limit } })
      .then((r) => r.data),
};
