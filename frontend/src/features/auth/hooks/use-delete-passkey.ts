import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { passkeysApi } from '../api/passkeys-api'
import { PASSKEYS_QUERY_KEY } from './use-passkeys'
import type { PasskeyDto } from '../types/passkey'

export function useDeletePasskey() {
  const queryClient = useQueryClient()
  const { t } = useTranslation()

  return useMutation({
    mutationFn: (id: string) => passkeysApi.delete(id),
    onSuccess: (_, deletedId) => {
      // Remove the passkey from cache
      queryClient.setQueryData<PasskeyDto[]>(PASSKEYS_QUERY_KEY, (old) => {
        if (!old) return old
        return old.filter((passkey) => passkey.id !== deletedId)
      })
      toast.success(t('auth:passkeys.delete.success'))
    },
  })
}
