import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { message } from 'antd';
import { useTranslation } from 'react-i18next';
import { scenariosApi } from './api';
import { getApiErrorMessage } from '@/shared/utils/errorUtils';
import type { CreateScenarioRequest, UpdateScenarioRequest, CreateStepRequest } from '@/shared/types';

export function useScenarios() {
  return useQuery({
    queryKey: ['scenarios'],
    queryFn: scenariosApi.getAll,
  });
}

export function useScenario(id: string) {
  return useQuery({
    queryKey: ['scenarios', id],
    queryFn: () => scenariosApi.getById(id),
    enabled: !!id,
  });
}

export function useCreateScenario() {
  const qc = useQueryClient();
  const { t } = useTranslation();
  return useMutation({
    mutationFn: (data: CreateScenarioRequest) => scenariosApi.create(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['scenarios'] });
      message.success(t('common.success'));
    },
    onError: (err) => message.error(getApiErrorMessage(err, t('common.error'))),
  });
}

export function useUpdateScenario() {
  const qc = useQueryClient();
  const { t } = useTranslation();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateScenarioRequest }) =>
      scenariosApi.update(id, data),
    onSuccess: (_data, variables) => {
      qc.invalidateQueries({ queryKey: ['scenarios'] });
      qc.invalidateQueries({ queryKey: ['scenarios', variables.id] });
      message.success(t('common.success'));
    },
    onError: (err) => message.error(getApiErrorMessage(err, t('common.error'))),
  });
}

export function useDeleteScenario() {
  const qc = useQueryClient();
  const { t } = useTranslation();
  return useMutation({
    mutationFn: (id: string) => scenariosApi.delete(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['scenarios'] });
      message.success(t('common.success'));
    },
    onError: (err) => message.error(getApiErrorMessage(err, t('common.error'))),
  });
}

export function useToggleScenario() {
  const qc = useQueryClient();
  const { t } = useTranslation();
  return useMutation({
    mutationFn: (id: string) => scenariosApi.toggleActive(id),
    onSuccess: (_data, id) => {
      qc.invalidateQueries({ queryKey: ['scenarios'] });
      qc.invalidateQueries({ queryKey: ['scenarios', id] });
      message.success(t('common.success'));
    },
    onError: (err) => message.error(getApiErrorMessage(err, t('common.error'))),
  });
}

export function useResetScenario() {
  const qc = useQueryClient();
  const { t } = useTranslation();
  return useMutation({
    mutationFn: (id: string) => scenariosApi.resetState(id),
    onSuccess: (_data, id) => {
      qc.invalidateQueries({ queryKey: ['scenarios'] });
      qc.invalidateQueries({ queryKey: ['scenarios', id] });
      message.success(t('common.success'));
    },
    onError: (err) => message.error(getApiErrorMessage(err, t('common.error'))),
  });
}

export function useAddStep(scenarioId: string) {
  const qc = useQueryClient();
  const { t } = useTranslation();
  return useMutation({
    mutationFn: (data: CreateStepRequest) => scenariosApi.addStep(scenarioId, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['scenarios', scenarioId] });
      message.success(t('common.success'));
    },
    onError: (err) => message.error(getApiErrorMessage(err, t('common.error'))),
  });
}

export function useUpdateStep(scenarioId: string) {
  const qc = useQueryClient();
  const { t } = useTranslation();
  return useMutation({
    mutationFn: ({ stepId, data }: { stepId: string; data: CreateStepRequest }) =>
      scenariosApi.updateStep(scenarioId, stepId, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['scenarios', scenarioId] });
      message.success(t('common.success'));
    },
    onError: (err) => message.error(getApiErrorMessage(err, t('common.error'))),
  });
}

export function useDeleteStep(scenarioId: string) {
  const qc = useQueryClient();
  const { t } = useTranslation();
  return useMutation({
    mutationFn: (stepId: string) => scenariosApi.deleteStep(scenarioId, stepId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['scenarios', scenarioId] });
      message.success(t('common.success'));
    },
    onError: (err) => message.error(getApiErrorMessage(err, t('common.error'))),
  });
}
