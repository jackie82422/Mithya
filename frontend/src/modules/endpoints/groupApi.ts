import apiClient from '@/shared/api/client';
import type { EndpointGroup, CreateGroupRequest, UpdateGroupRequest } from '@/shared/types';

export const groupApi = {
  getAll: () =>
    apiClient.get<EndpointGroup[]>('/groups').then((r) => r.data),

  getById: (id: string) =>
    apiClient.get<EndpointGroup>(`/groups/${id}`).then((r) => r.data),

  create: (data: CreateGroupRequest) =>
    apiClient.post<EndpointGroup>('/groups', data).then((r) => r.data),

  update: (id: string, data: UpdateGroupRequest) =>
    apiClient.put<EndpointGroup>(`/groups/${id}`, data).then((r) => r.data),

  delete: (id: string) =>
    apiClient.delete(`/groups/${id}`),

  addEndpoints: (groupId: string, endpointIds: string[]) =>
    apiClient.post<EndpointGroup>(`/groups/${groupId}/endpoints`, { endpointIds }).then((r) => r.data),

  removeEndpoint: (groupId: string, endpointId: string) =>
    apiClient.delete(`/groups/${groupId}/endpoints/${endpointId}`),

  getEndpointGroups: (endpointId: string) =>
    apiClient.get<EndpointGroup[]>(`/endpoints/${endpointId}/groups`).then((r) => r.data),
};
