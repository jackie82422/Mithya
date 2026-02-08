import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { message } from 'antd';
import { useTranslation } from 'react-i18next';
import { proxyApi } from './api';
import { getApiErrorMessage } from '@/shared/utils/errorUtils';
import type { CreateProxyConfigRequest } from '@/shared/types';

export function useProxyConfigs() {
  return useQuery({
    queryKey: ['proxyConfigs'],
    queryFn: proxyApi.getAll,
  });
}

export function useCreateProxyConfig() {
  const qc = useQueryClient();
  const { t } = useTranslation();
  return useMutation({
    mutationFn: (data: CreateProxyConfigRequest) => proxyApi.create(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['proxyConfigs'] });
      message.success(t('common.success'));
    },
    onError: (err) => message.error(getApiErrorMessage(err, t('common.error'))),
  });
}

export function useUpdateProxyConfig() {
  const qc = useQueryClient();
  const { t } = useTranslation();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: CreateProxyConfigRequest }) =>
      proxyApi.update(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['proxyConfigs'] });
      message.success(t('common.success'));
    },
    onError: (err) => message.error(getApiErrorMessage(err, t('common.error'))),
  });
}

export function useDeleteProxyConfig() {
  const qc = useQueryClient();
  const { t } = useTranslation();
  return useMutation({
    mutationFn: (id: string) => proxyApi.delete(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['proxyConfigs'] });
      message.success(t('common.success'));
    },
    onError: (err) => message.error(getApiErrorMessage(err, t('common.error'))),
  });
}

export function useToggleProxyConfig() {
  const qc = useQueryClient();
  const { t } = useTranslation();
  return useMutation({
    mutationFn: (id: string) => proxyApi.toggleActive(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['proxyConfigs'] });
      message.success(t('common.success'));
    },
    onError: (err) => message.error(getApiErrorMessage(err, t('common.error'))),
  });
}

export function useToggleRecording() {
  const qc = useQueryClient();
  const { t } = useTranslation();
  return useMutation({
    mutationFn: (id: string) => proxyApi.toggleRecording(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['proxyConfigs'] });
      message.success(t('common.success'));
    },
    onError: (err) => message.error(getApiErrorMessage(err, t('common.error'))),
  });
}
