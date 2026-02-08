import apiClient from '@/shared/api/client';
import type {
  MockEndpoint,
  CreateEndpointRequest,
  UpdateEndpointRequest,
  SetDefaultResponseRequest,
} from '@/shared/types';

export const endpointsApi = {
  getAll: () =>
    apiClient.get<MockEndpoint[]>('/endpoints').then((r) => r.data),

  getById: (id: string) =>
    apiClient.get<MockEndpoint>(`/endpoints/${id}`).then((r) => r.data),

  create: (data: CreateEndpointRequest) =>
    apiClient.post<MockEndpoint>('/endpoints', data).then((r) => r.data),

  update: (id: string, data: UpdateEndpointRequest) =>
    apiClient.put<MockEndpoint>(`/endpoints/${id}`, data).then((r) => r.data),

  setDefaultResponse: (id: string, data: SetDefaultResponseRequest) =>
    apiClient.put<MockEndpoint>(`/endpoints/${id}/default`, data).then((r) => r.data),

  delete: (id: string) =>
    apiClient.delete(`/endpoints/${id}`),

  toggleActive: (id: string) =>
    apiClient.patch<MockEndpoint>(`/endpoints/${id}/toggle`).then((r) => r.data),
};
