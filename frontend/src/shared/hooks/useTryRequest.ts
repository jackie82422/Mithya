import { useMutation } from '@tanstack/react-query';
import { tryRequestApi, type TryRequestPayload } from '../api/tryRequestApi';

export function useTryRequest() {
  return useMutation({
    mutationFn: (data: TryRequestPayload) => tryRequestApi.send(data),
  });
}
