import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { message } from 'antd';
import { useTranslation } from 'react-i18next';
import { serviceProxyApi } from './api';
import { getApiErrorMessage } from '@/shared/utils/errorUtils';
import type { CreateServiceProxyRequest } from '@/shared/types';

const KEYS = {
  all: ['serviceProxies'] as const,
  services: ['serviceProxies', 'services'] as const,
};

export function useServiceProxies() {
  return useQuery({
    queryKey: KEYS.all,
    queryFn: serviceProxyApi.getAll,
  });
}

export function useAvailableServices() {
  return useQuery({
    queryKey: KEYS.services,
    queryFn: serviceProxyApi.getServices,
  });
}

export function useCreateServiceProxy() {
  const qc = useQueryClient();
  const { t } = useTranslation();
  return useMutation({
    mutationFn: (data: CreateServiceProxyRequest) => serviceProxyApi.create(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: KEYS.all });
      qc.invalidateQueries({ queryKey: KEYS.services });
      message.success(t('common.success'));
    },
    onError: (err) => message.error(getApiErrorMessage(err, t('common.error'))),
  });
}

export function useUpdateServiceProxy() {
  const qc = useQueryClient();
  const { t } = useTranslation();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: CreateServiceProxyRequest }) =>
      serviceProxyApi.update(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: KEYS.all });
      qc.invalidateQueries({ queryKey: KEYS.services });
      message.success(t('common.success'));
    },
    onError: (err) => message.error(getApiErrorMessage(err, t('common.error'))),
  });
}

export function useDeleteServiceProxy() {
  const qc = useQueryClient();
  const { t } = useTranslation();
  return useMutation({
    mutationFn: (id: string) => serviceProxyApi.delete(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: KEYS.all });
      qc.invalidateQueries({ queryKey: KEYS.services });
      message.success(t('common.success'));
    },
    onError: (err) => message.error(getApiErrorMessage(err, t('common.error'))),
  });
}

export function useToggleServiceProxy() {
  const qc = useQueryClient();
  const { t } = useTranslation();
  return useMutation({
    mutationFn: (id: string) => serviceProxyApi.toggleActive(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: KEYS.all });
      message.success(t('common.success'));
    },
    onError: (err) => message.error(getApiErrorMessage(err, t('common.error'))),
  });
}

export function useToggleRecording() {
  const qc = useQueryClient();
  const { t } = useTranslation();
  return useMutation({
    mutationFn: (id: string) => serviceProxyApi.toggleRecording(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: KEYS.all });
      message.success(t('common.success'));
    },
    onError: (err) => message.error(getApiErrorMessage(err, t('common.error'))),
  });
}

export function useToggleFallback() {
  const qc = useQueryClient();
  const { t } = useTranslation();
  return useMutation({
    mutationFn: (id: string) => serviceProxyApi.toggleFallback(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: KEYS.all });
      message.success(t('common.success'));
    },
    onError: (err) => message.error(getApiErrorMessage(err, t('common.error'))),
  });
}
