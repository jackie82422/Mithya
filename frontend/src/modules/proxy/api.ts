import apiClient from '@/shared/api/client';
import type { ServiceProxy, CreateServiceProxyRequest, ServiceInfo } from '@/shared/types';

export const serviceProxyApi = {
  getAll: () =>
    apiClient.get<ServiceProxy[]>('/service-proxies').then((r) => r.data),

  getById: (id: string) =>
    apiClient.get<ServiceProxy>(`/service-proxies/${id}`).then((r) => r.data),

  getByServiceName: (serviceName: string) =>
    apiClient.get<ServiceProxy>(`/service-proxies/by-service/${serviceName}`).then((r) => r.data),

  create: (data: CreateServiceProxyRequest) =>
    apiClient.post<ServiceProxy>('/service-proxies', data).then((r) => r.data),

  update: (id: string, data: CreateServiceProxyRequest) =>
    apiClient.put<ServiceProxy>(`/service-proxies/${id}`, data).then((r) => r.data),

  delete: (id: string) =>
    apiClient.delete(`/service-proxies/${id}`),

  toggleActive: (id: string) =>
    apiClient.patch<ServiceProxy>(`/service-proxies/${id}/toggle`).then((r) => r.data),

  toggleRecording: (id: string) =>
    apiClient.patch<ServiceProxy>(`/service-proxies/${id}/toggle-recording`).then((r) => r.data),

  toggleFallback: (id: string) =>
    apiClient.patch<ServiceProxy>(`/service-proxies/${id}/toggle-fallback`).then((r) => r.data),

  getServices: () =>
    apiClient.get<ServiceInfo[]>('/service-proxies/services').then((r) => r.data),
};
