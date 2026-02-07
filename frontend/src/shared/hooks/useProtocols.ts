import { useQuery } from '@tanstack/react-query';
import apiClient from '../api/client';
import type { ProtocolSchema } from '../types';

export function useProtocols() {
  return useQuery<ProtocolSchema[]>({
    queryKey: ['protocols'],
    queryFn: async () => {
      const { data } = await apiClient.get<ProtocolSchema[]>('/protocols');
      return data;
    },
    staleTime: Infinity,
  });
}
