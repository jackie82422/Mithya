import { AxiosError } from 'axios';

interface ApiErrorResponse {
  errors?: string[];
  message?: string;
}

export function getApiErrorMessage(err: unknown, fallback: string): string {
  if (err instanceof AxiosError && err.response?.data) {
    const data = err.response.data as ApiErrorResponse;
    if (Array.isArray(data.errors) && data.errors.length > 0) {
      return data.errors.join('; ');
    }
    if (typeof data.message === 'string' && data.message) {
      return data.message;
    }
  }
  return fallback;
}
