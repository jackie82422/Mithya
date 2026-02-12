import apiClient from '@/shared/api/client';
import type { MockEndpoint } from '@/shared/types';

// ── Export ──

export interface ExportData {
  version: string;
  exportedAt: string;
  endpoints: MockEndpoint[];
  serviceProxies: Array<{
    serviceName: string;
    targetBaseUrl: string;
    isActive: boolean;
    isRecording: boolean;
    forwardHeaders: boolean;
    additionalHeaders?: string;
    timeoutMs: number;
    stripPathPrefix?: string;
    fallbackEnabled: boolean;
  }>;
}

// ── Import JSON ──

export interface ImportJsonPayload {
  endpoints: MockEndpoint[];
  serviceProxies?: ExportData['serviceProxies'];
}

export interface ImportJsonResult {
  imported: number;
  skipped: number;
  endpoints: Array<{ id: string; name: string; path: string; rulesCount: number }>;
  duplicates: Array<{ name: string; path: string; httpMethod: string; reason: string }>;
  serviceProxies: { imported: number; details: Array<{ id: string; serviceName: string; action: string }> };
}

// ── API ──

export const importExportApi = {
  exportAll: () =>
    apiClient.get<ExportData>('/export').then((r) => r.data),

  importJson: (payload: ImportJsonPayload) =>
    apiClient.post<ImportJsonResult>('/import/json', payload).then((r) => r.data),
};
