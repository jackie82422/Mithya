import { useQuery } from '@tanstack/react-query';
import apiClient from '../api/client';

interface ServerConfig {
  mithyaPort: number;
  mithyaUrl: string;
  mithyaHost: string;
  adminApiUrl: string;
}

export function useServerConfig() {
  return useQuery<ServerConfig>({
    queryKey: ['serverConfig'],
    queryFn: async () => {
      const { data } = await apiClient.get<ServerConfig>('/config');
      return data;
    },
    staleTime: Infinity,
  });
}
