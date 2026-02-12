import { useMutation, useQueryClient } from '@tanstack/react-query';
import { importExportApi } from './api';
import type { ImportJsonPayload } from './api';

export function useImportJson() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (payload: ImportJsonPayload) => importExportApi.importJson(payload),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['endpoints'] });
    },
  });
}

export function useExportAll() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => importExportApi.exportAll(),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['endpoints'] });
    },
  });
}
