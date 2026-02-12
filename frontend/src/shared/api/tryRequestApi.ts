import apiClient from './client';

export interface TryRequestPayload {
  method: string;
  url: string;
  headers?: Record<string, string>;
  body?: string;
}

export interface TryRequestResponse {
  statusCode: number;
  headers: Record<string, string>;
  body: string;
  elapsedMs: number;
}

export const tryRequestApi = {
  send: (data: TryRequestPayload) =>
    apiClient.post<TryRequestResponse>('/try-request', data).then((r) => r.data),
};
