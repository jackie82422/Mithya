import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { message } from 'antd';
import { useTranslation } from 'react-i18next';
import { endpointsApi } from './api';
import { getApiErrorMessage } from '@/shared/utils/errorUtils';
import type { CreateEndpointRequest, UpdateEndpointRequest, SetDefaultResponseRequest } from '@/shared/types';

export function useEndpoints() {
  return useQuery({
    queryKey: ['endpoints'],
    queryFn: endpointsApi.getAll,
  });
}

export function useEndpoint(id: string) {
  return useQuery({
    queryKey: ['endpoints', id],
    queryFn: () => endpointsApi.getById(id),
    enabled: !!id,
  });
}

export function useCreateEndpoint() {
  const qc = useQueryClient();
  const { t } = useTranslation();
  return useMutation({
    mutationFn: (data: CreateEndpointRequest) => endpointsApi.create(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['endpoints'] });
      message.success(t('common.success'));
    },
    onError: (err) => message.error(getApiErrorMessage(err, t('common.error'))),
  });
}

export function useUpdateEndpoint() {
  const qc = useQueryClient();
  const { t } = useTranslation();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateEndpointRequest }) =>
      endpointsApi.update(id, data),
    onSuccess: (_data, variables) => {
      qc.invalidateQueries({ queryKey: ['endpoints'] });
      qc.invalidateQueries({ queryKey: ['endpoints', variables.id] });
      message.success(t('common.success'));
    },
    onError: (err) => message.error(getApiErrorMessage(err, t('common.error'))),
  });
}

export function useSetDefaultResponse() {
  const qc = useQueryClient();
  const { t } = useTranslation();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: SetDefaultResponseRequest }) =>
      endpointsApi.setDefaultResponse(id, data),
    onSuccess: (_data, variables) => {
      qc.invalidateQueries({ queryKey: ['endpoints'] });
      qc.invalidateQueries({ queryKey: ['endpoints', variables.id] });
      message.success(t('common.success'));
    },
    onError: (err) => message.error(getApiErrorMessage(err, t('common.error'))),
  });
}

export function useDeleteEndpoint() {
  const qc = useQueryClient();
  const { t } = useTranslation();
  return useMutation({
    mutationFn: (id: string) => endpointsApi.delete(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['endpoints'] });
      message.success(t('common.success'));
    },
    onError: (err) => message.error(getApiErrorMessage(err, t('common.error'))),
  });
}

export function useToggleEndpoint() {
  const qc = useQueryClient();
  const { t } = useTranslation();
  return useMutation({
    mutationFn: (id: string) => endpointsApi.toggleActive(id),
    onSuccess: (_data, id) => {
      qc.invalidateQueries({ queryKey: ['endpoints'] });
      qc.invalidateQueries({ queryKey: ['endpoints', id] });
      message.success(t('common.success'));
    },
    onError: (err) => message.error(getApiErrorMessage(err, t('common.error'))),
  });
}
