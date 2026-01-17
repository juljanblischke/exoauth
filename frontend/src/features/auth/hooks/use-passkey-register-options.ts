import { useMutation } from '@tanstack/react-query'
import { passkeysApi } from '../api/passkeys-api'

export function usePasskeyRegisterOptions() {
  return useMutation({
    mutationFn: passkeysApi.getRegisterOptions,
  })
}
