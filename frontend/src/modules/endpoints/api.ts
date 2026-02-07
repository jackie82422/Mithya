import apiClient from '@/shared/api/client';
import type {
  MockEndpoint,
  CreateEndpointRequest,
  SetDefaultResponseRequest,
} from '@/shared/types';

export const endpointsApi = {
  getAll: () =>
    apiClient.get<MockEndpoint[]>('/endpoints').then((r) => r.data),

  getById: (id: string) =>
    apiClient.get<MockEndpoint>(`/endpoints/${id}`).then((r) => r.data),

  create: (data: CreateEndpointRequest) =>
    apiClient.post<MockEndpoint>('/endpoints', data).then((r) => r.data),

  setDefaultResponse: (id: string, data: SetDefaultResponseRequest) =>
    apiClient.put<MockEndpoint>(`/endpoints/${id}/default`, data).then((r) => r.data),

  delete: (id: string) =>
    apiClient.delete(`/endpoints/${id}`),
};
