import apiClient from '@/shared/api/client';
import type { ProxyConfig, CreateProxyConfigRequest } from '@/shared/types';

export const proxyApi = {
  getAll: () =>
    apiClient.get<ProxyConfig[]>('/proxy-configs').then((r) => r.data),

  getById: (id: string) =>
    apiClient.get<ProxyConfig>(`/proxy-configs/${id}`).then((r) => r.data),

  create: (data: CreateProxyConfigRequest) =>
    apiClient.post<ProxyConfig>('/proxy-configs', data).then((r) => r.data),

  update: (id: string, data: CreateProxyConfigRequest) =>
    apiClient.put<ProxyConfig>(`/proxy-configs/${id}`, data).then((r) => r.data),

  delete: (id: string) =>
    apiClient.delete(`/proxy-configs/${id}`),

  toggleActive: (id: string) =>
    apiClient.patch<ProxyConfig>(`/proxy-configs/${id}/toggle`).then((r) => r.data),

  toggleRecording: (id: string) =>
    apiClient.patch<ProxyConfig>(`/proxy-configs/${id}/toggle-recording`).then((r) => r.data),
};
