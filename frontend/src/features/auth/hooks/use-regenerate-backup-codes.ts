import { useMutation } from '@tanstack/react-query'
import { mfaApi } from '../api/mfa-api'

export function useRegenerateBackupCodes() {
  return useMutation({
    mutationFn: mfaApi.regenerateBackupCodes,
  })
}
