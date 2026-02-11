import { useQuery, useQueries, useMutation, useQueryClient } from '@tanstack/react-query';
import { useMemo } from 'react';
import { message } from 'antd';
import { useTranslation } from 'react-i18next';
import { groupApi } from './groupApi';
import { getApiErrorMessage } from '@/shared/utils/errorUtils';
import type { CreateGroupRequest, UpdateGroupRequest, EndpointGroup } from '@/shared/types';

export function useGroups() {
  return useQuery({
    queryKey: ['groups'],
    queryFn: groupApi.getAll,
  });
}

/** Fetch all groups with their endpoint lists, and build endpointId → groups[] map */
export function useGroupsWithEndpoints() {
  const { data: groups } = useGroups();

  const detailQueries = useQueries({
    queries: (groups ?? []).map((g) => ({
      queryKey: ['groups', g.id],
      queryFn: () => groupApi.getById(g.id),
      enabled: !!groups,
      staleTime: 30_000,
    })),
  });

  const groupDetails = detailQueries
    .filter((q) => q.isSuccess && q.data)
    .map((q) => q.data!);

  const endpointGroupMap = useMemo(() => {
    const map: Record<string, EndpointGroup[]> = {};
    for (const g of groupDetails) {
      if (!g.endpoints) continue;
      for (const ep of g.endpoints) {
        if (!map[ep.id]) map[ep.id] = [];
        map[ep.id].push({ ...g, endpoints: undefined });
      }
    }
    return map;
  }, [groupDetails]);

  const groupedEndpointIds = useMemo(() => {
    const set = new Set<string>();
    for (const g of groupDetails) {
      if (!g.endpoints) continue;
      for (const ep of g.endpoints) set.add(ep.id);
    }
    return set;
  }, [groupDetails]);

  return { groups: groups ?? [], endpointGroupMap, groupedEndpointIds };
}

export function useGroup(id: string) {
  return useQuery({
    queryKey: ['groups', id],
    queryFn: () => groupApi.getById(id),
    enabled: !!id,
  });
}

export function useEndpointGroups(endpointId: string) {
  return useQuery({
    queryKey: ['endpointGroups', endpointId],
    queryFn: () => groupApi.getEndpointGroups(endpointId),
    enabled: !!endpointId,
  });
}

export function useCreateGroup() {
  const qc = useQueryClient();
  const { t } = useTranslation();
  return useMutation({
    mutationFn: (data: CreateGroupRequest) => groupApi.create(data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['groups'] });
      message.success(t('common.success'));
    },
    onError: (err) => message.error(getApiErrorMessage(err, t('common.error'))),
  });
}

export function useUpdateGroup() {
  const qc = useQueryClient();
  const { t } = useTranslation();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateGroupRequest }) => groupApi.update(id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['groups'] });
      message.success(t('common.success'));
    },
    onError: (err) => message.error(getApiErrorMessage(err, t('common.error'))),
  });
}

export function useDeleteGroup() {
  const qc = useQueryClient();
  const { t } = useTranslation();
  return useMutation({
    mutationFn: (id: string) => groupApi.delete(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['groups'] });
      message.success(t('common.success'));
    },
    onError: (err) => message.error(getApiErrorMessage(err, t('common.error'))),
  });
}

export function useAddEndpointsToGroup() {
  const qc = useQueryClient();
  const { t } = useTranslation();
  return useMutation({
    mutationFn: ({ groupId, endpointIds }: { groupId: string; endpointIds: string[] }) =>
      groupApi.addEndpoints(groupId, endpointIds),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['groups'] });
      qc.invalidateQueries({ queryKey: ['endpointGroups'] });
      message.success(t('common.success'));
    },
    onError: (err) => message.error(getApiErrorMessage(err, t('common.error'))),
  });
}

export function useRemoveEndpointFromGroup() {
  const qc = useQueryClient();
  const { t } = useTranslation();
  return useMutation({
    mutationFn: ({ groupId, endpointId }: { groupId: string; endpointId: string }) =>
      groupApi.removeEndpoint(groupId, endpointId),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['groups'] });
      qc.invalidateQueries({ queryKey: ['endpointGroups'] });
      message.success(t('common.success'));
    },
    onError: (err) => message.error(getApiErrorMessage(err, t('common.error'))),
  });
}
