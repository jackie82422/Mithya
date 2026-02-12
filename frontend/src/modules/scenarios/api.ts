import apiClient from '@/shared/api/client';
import type {
  Scenario,
  CreateScenarioRequest,
  UpdateScenarioRequest,
  ScenarioStep,
  CreateStepRequest,
} from '@/shared/types';

export const scenariosApi = {
  getAll: () =>
    apiClient.get<Scenario[]>('/scenarios').then((r) => r.data),

  getById: (id: string) =>
    apiClient.get<Scenario>(`/scenarios/${id}`).then((r) => r.data),

  create: (data: CreateScenarioRequest) =>
    apiClient.post<Scenario>('/scenarios', data).then((r) => r.data),

  update: (id: string, data: UpdateScenarioRequest) =>
    apiClient.put<Scenario>(`/scenarios/${id}`, data).then((r) => r.data),

  delete: (id: string) =>
    apiClient.delete(`/scenarios/${id}`),

  toggleActive: (id: string) =>
    apiClient.patch<Scenario>(`/scenarios/${id}/toggle`).then((r) => r.data),

  resetState: (id: string) =>
    apiClient.post(`/scenarios/${id}/reset`),

  addStep: (id: string, data: CreateStepRequest) =>
    apiClient.post<ScenarioStep>(`/scenarios/${id}/steps`, data).then((r) => r.data),

  updateStep: (id: string, stepId: string, data: CreateStepRequest) =>
    apiClient.put<ScenarioStep>(`/scenarios/${id}/steps/${stepId}`, data).then((r) => r.data),

  deleteStep: (id: string, stepId: string) =>
    apiClient.delete(`/scenarios/${id}/steps/${stepId}`),
};
