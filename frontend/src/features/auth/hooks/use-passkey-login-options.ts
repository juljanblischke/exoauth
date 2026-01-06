import { useMutation } from '@tanstack/react-query'
import { passkeysApi } from '../api/passkeys-api'

export function usePasskeyLoginOptions() {
  return useMutation({
    mutationFn: passkeysApi.getLoginOptions,
  })
}
