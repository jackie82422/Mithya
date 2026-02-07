import apiClient from '@/shared/api/client';
import type { MockRule, CreateRuleRequest } from '@/shared/types';

export const rulesApi = {
  getByEndpoint: (endpointId: string) =>
    apiClient.get<MockRule[]>(`/endpoints/${endpointId}/rules`).then((r) => r.data),

  create: (endpointId: string, data: CreateRuleRequest) =>
    apiClient.post<MockRule>(`/endpoints/${endpointId}/rules`, data).then((r) => r.data),

  delete: (endpointId: string, ruleId: string) =>
    apiClient.delete(`/endpoints/${endpointId}/rules/${ruleId}`),
};
