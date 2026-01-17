import { useMutation } from '@tanstack/react-query'
import { mfaApi } from '../api/mfa-api'

export function useMfaSetup() {
  return useMutation({
    mutationFn: (setupToken?: string) => mfaApi.setup(setupToken),
  })
}
