import apiClient from '@/shared/api/client';
import type { MockRule, CreateRuleRequest, TemplatePreviewRequest, TemplatePreviewResponse } from '@/shared/types';

export const templateApi = {
  preview: (data: TemplatePreviewRequest) =>
    apiClient.post<TemplatePreviewResponse>('/templates/preview', data).then((r) => r.data),
};

export const rulesApi = {
  getByEndpoint: (endpointId: string) =>
    apiClient.get<MockRule[]>(`/endpoints/${endpointId}/rules`).then((r) => r.data),

  create: (endpointId: string, data: CreateRuleRequest) =>
    apiClient.post<MockRule>(`/endpoints/${endpointId}/rules`, data).then((r) => r.data),

  update: (endpointId: string, ruleId: string, data: CreateRuleRequest) =>
    apiClient.put<MockRule>(`/endpoints/${endpointId}/rules/${ruleId}`, data).then((r) => r.data),

  delete: (endpointId: string, ruleId: string) =>
    apiClient.delete(`/endpoints/${endpointId}/rules/${ruleId}`),

  toggleActive: (endpointId: string, ruleId: string) =>
    apiClient.patch<MockRule>(`/endpoints/${endpointId}/rules/${ruleId}/toggle`).then((r) => r.data),
};
