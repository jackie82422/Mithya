import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { message } from 'antd';
import { useTranslation } from 'react-i18next';
import { rulesApi } from './api';
import type { CreateRuleRequest } from '@/shared/types';

export function useRules(endpointId: string) {
  return useQuery({
    queryKey: ['rules', endpointId],
    queryFn: () => rulesApi.getByEndpoint(endpointId),
    enabled: !!endpointId,
  });
}

export function useCreateRule(endpointId: string) {
  const qc = useQueryClient();
  const { t } = useTranslation();
  return useMutation({
    mutationFn: (data: CreateRuleRequest) => rulesApi.create(endpointId, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['rules', endpointId] });
      qc.invalidateQueries({ queryKey: ['endpoints', endpointId] });
      qc.invalidateQueries({ queryKey: ['endpoints'] });
      message.success(t('common.success'));
    },
    onError: () => message.error(t('common.error')),
  });
}

export function useDeleteRule(endpointId: string) {
  const qc = useQueryClient();
  const { t } = useTranslation();
  return useMutation({
    mutationFn: (ruleId: string) => rulesApi.delete(endpointId, ruleId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['rules', endpointId] });
      qc.invalidateQueries({ queryKey: ['endpoints', endpointId] });
      qc.invalidateQueries({ queryKey: ['endpoints'] });
      message.success(t('common.success'));
    },
    onError: () => message.error(t('common.error')),
  });
}
